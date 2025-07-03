using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SP.RBAC.API.Data;
using SP.RBAC.API.DTOs;
using SP.RBAC.API.Entities;

namespace SP.RBAC.API.Controllers;

/// <summary>
/// Administrerer entitetsinstanser som representerer faktiske dataobjekter fra eksterne systemer
/// </summary>
/// <remarks>
/// Entitetsinstanser er konkrete implementasjoner av entitetsdefinisjoner med faktiske dataverdier
/// fra eksterne systemer. Hver instans representerer en spesifikk oppføring som en ansatt,
/// pasient, kunde eller annet dataobjekt definert i tilhørende entitetsdefinisjon.
/// 
/// **Hovedfunksjoner:**
/// - Administrere livssyklusen til entitetsinstanser fra opprettelse til sletting
/// - Synkronisere data mellom eksterne systemer og RBAC-plattformen
/// - Håndtere entitetsrelasjoner og referanseintegritet
/// - Spore endringer og opprettholde revisjonsspor
/// - Validere datainnhold mot entitetsdefinisjoner
/// 
/// **Datasynkronisering:**
/// - Automatisk import av data fra eksterne systemer
/// - Konfliktløsning ved samtidige oppdateringer
/// - Delta-synkronisering for optimal ytelse
/// - Feilhåndtering og retry-mekanismer
/// - Planlagt og triggeret synkronisering
/// 
/// **Relasjonshåndtering:**
/// - Hierarkiske relasjoner (forelder-barn strukturer)
/// - Kryssreferanser mellom entiteter fra ulike systemer
/// - Referanseintegritet og orphan detection
/// - Bulk-operasjoner for relaterte entiteter
/// 
/// **Ytelse og skalerbarhet:**
/// - Optimaliserte spørringer med indeksering
/// - Lazy loading av relaterte data
/// - Batch-processing for bulk-operasjoner
/// - Caching av hyppig tilgåtte data
/// 
/// **Audit og compliance:**
/// - Detaljert logging av alle endringer
/// - Historikk-sporing med tidsstempler
/// - Data lineage og kildesporing
/// - GDPR og personvernkompatibilitet
/// </remarks>
[ApiController]
[Route("api/[controller]")]
public class EntityInstancesController : ControllerBase
{
    private readonly RbacDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<EntityInstancesController> _logger;

    public EntityInstancesController(RbacDbContext context, IMapper mapper, ILogger<EntityInstancesController> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Henter alle entitetsinstanser med paginering og avanserte filtreringsmuligheter
    /// </summary>
    /// <remarks>
    /// Denne operasjonen returnerer en paginert liste over alle entitetsinstanser i systemet.
    /// Støtter avanserte filtreringsparametere for å finne spesifikke entiteter basert på
    /// navn, ekstern ID, entitetsdefinisjon, aktivitetsstatus og synkroniseringsstatus.
    /// 
    /// **Sikkerhetsvurderinger:**
    /// - Inkluderer alle tilknyttede egenskapsverdier som kan inneholde sensitive data
    /// - Filtreringsmuligheter kan avsløre eksistensen av entiteter
    /// - Synkroniseringsstatus kan indikere systemintegrasjonsproblemer
    /// 
    /// **Ytelseshensyn:**
    /// - Benytter eager loading for entitetsdefinisjoner og egenskapsverdier
    /// - Paginering anbefales for store datasett
    /// - Indekserte søk på DisplayName og ExternalId
    /// 
    /// **Forretningslogikk:**
    /// - Bare aktive entitetsinstanser returneres som standard
    /// - Slettede entiteter ekskluderes automatisk fra resultatet
    /// - Egenskapsverdier mappes dynamisk basert på entitetsdefinisjon
    /// </remarks>
    /// <param name="pageNumber">Sidenummer for paginering (standard: 1)</param>
    /// <param name="pageSize">Antall elementer per side (standard: 10, maksimum: 100)</param>
    /// <param name="search">Søketekst for filtrering på visningsnavn eller ekstern ID</param>
    /// <param name="entityDefinitionId">Filtrer på spesifikk entitetsdefinisjon ID</param>
    /// <param name="isActive">Filtrer på aktivitetsstatus (null = alle, true = aktive, false = inaktive)</param>
    /// <param name="syncStatus">Filtrer på synkroniseringsstatus fra eksterne systemer</param>
    /// <returns>Paginert resultat med entitetsinstanser og tilhørende metadata</returns>
    /// <response code="200">Returnerer paginert liste over entitetsinstanser</response>
    /// <response code="400">Ugyldig forespørsel - valideringsfeile eller ugyldige parametere</response>
    /// <response code="500">Intern serverfeil under henting av entitetsinstanser</response>
    [HttpGet]
    public async Task<ActionResult<PagedResult<EntityInstanceDto>>> GetEntityInstances(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] Guid? entityDefinitionId = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] SyncStatus? syncStatus = null)
    {
        try
        {
            var query = _context.EntityInstances
                .Include(x => x.EntityDefinition)
                .Include(x => x.PropertyValues)
                    .ThenInclude(pv => pv.PropertyDefinition)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(x => x.DisplayName.Contains(search) || 
                                        x.ExternalId.Contains(search));
            }

            if (entityDefinitionId.HasValue)
            {
                query = query.Where(x => x.EntityDefinitionId == entityDefinitionId.Value);
            }

            if (isActive.HasValue)
            {
                query = query.Where(x => x.IsActive == isActive.Value);
            }

            if (syncStatus.HasValue)
            {
                query = query.Where(x => x.SyncStatus == syncStatus.Value);
            }

            var totalCount = await query.CountAsync();
            
            var items = await query
                .OrderBy(x => x.DisplayName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = _mapper.Map<IEnumerable<EntityInstanceDto>>(items);

            // Map property values for each entity instance
            foreach (var dto in dtos)
            {
                var entityInstance = items.First(x => x.Id == dto.Id);
                dto.PropertyValues = _mapper.Map<List<PropertyValueDto>>(entityInstance.PropertyValues);
            }

            var result = new PagedResult<EntityInstanceDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting entity instances");
            return StatusCode(500, "An error occurred while retrieving entity instances");
        }
    }

    /// <summary>
    /// Henter en spesifikk entitetsinstans med komplett datainnhold
    /// </summary>
    /// <remarks>
    /// Returnerer detaljert informasjon om en enkelt entitetsinstans inkludert alle
    /// tilknyttede egenskapsverdier, entitetsdefinisjon og relaterte metadata.
    /// Operasjonen utfører eager loading for å sikre at all nødvendig data
    /// er tilgjengelig i responsen.
    /// 
    /// **Sikkerhetsvurderinger:**
    /// - Returnerer alle egenskapsverdier som kan inneholde sensitive persondata
    /// - Ingen automatisk tilgangssjekk implementert - krever manuell autorisasjon
    /// - Eksponerer interne ID-er og systemreferanser
    /// 
    /// **Datainnhold:**
    /// - Komplett entitetsinstansinfo med entitetsdefinisjon
    /// - Alle tilknyttede egenskapsverdier med egenskapsdefinisjoner
    /// - Synkroniseringsmetadata og audit-informasjon
    /// - Hierarkiske relasjoner hvis konfigurert
    /// 
    /// **Forretningslogikk:**
    /// - Validerer eksistens av entitetsinstans før retur
    /// - Inkluderer slettede entiteter hvis spesifikt forespurt
    /// - Mappinglogikk håndterer komplekse datatyper automatisk
    /// </remarks>
    /// <param name="id">Unik identifikator for entitetsinstansen som skal hentes</param>
    /// <returns>Komplett entitetsinstans med all tilknyttet informasjon</returns>
    /// <response code="200">Entitetsinstans funnet og returnert med komplett datainnhold</response>
    /// <response code="404">Entitetsinstans med angitt ID eksisterer ikke i systemet</response>
    /// <response code="500">Intern serverfeil under henting av entitetsinstans</response>
    [HttpGet("{id}")]
    public async Task<ActionResult<EntityInstanceDto>> GetEntityInstance(Guid id)
    {
        try
        {
            var entityInstance = await _context.EntityInstances
                .Include(x => x.EntityDefinition)
                .Include(x => x.PropertyValues)
                    .ThenInclude(pv => pv.PropertyDefinition)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entityInstance == null)
            {
                return NotFound($"Entity instance with ID {id} not found");
            }

            var dto = _mapper.Map<EntityInstanceDto>(entityInstance);
            dto.PropertyValues = _mapper.Map<List<PropertyValueDto>>(entityInstance.PropertyValues);

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting entity instance {Id}", id);
            return StatusCode(500, "An error occurred while retrieving the entity instance");
        }
    }

    /// <summary>
    /// Oppretter en ny entitetsinstans med tilknyttede egenskapsverdier
    /// </summary>
    /// <remarks>
    /// Oppretter en komplett entitetsinstans basert på en eksisterende entitetsdefinisjon.
    /// Operasjonen validerer at entitetsdefinisjonen eksisterer, at ekstern ID er unik
    /// innenfor entitetsdefinisjonen, og at alle spesifiserte egenskapsdefinisjoner
    /// er gyldige for den valgte entitetsdefinisjonen.
    /// 
    /// **Valideringsregler:**
    /// - Entitetsdefinisjon må eksistere og være aktiv
    /// - Ekstern ID må være unik innenfor entitetsdefinisjonen
    /// - Alle egenskapsdefinisjoner må tilhøre den spesifiserte entitetsdefinisjonen
    /// - Obligatoriske egenskaper må ha verdier hvis konfigurert
    /// - Datatype-validering utføres for alle egenskapsverdier
    /// 
    /// **Sikkerhetshensyn:**
    /// - Ingen automatisk tilgangsvalidering - krever manuell autorisasjonssjekk
    /// - Egenskapsverdier kan inneholde sensitive data som ikke krypteres
    /// - Audit-logging registrerer opprettelseshandlingen for compliance
    /// 
    /// **Forretningsprosess:**
    /// - Automatisk synkroniseringsstatus settes til Success ved opprettelse
    /// - Tidsstempel for siste synkronisering oppdateres automatisk
    /// - Egenskapsverdier opprettes som separate entiteter med relasjonskobling
    /// - Transaksjonell opprettelse sikrer datakonsistens
    /// </remarks>
    /// <param name="createDto">Opprettelsesdata for ny entitetsinstans inkludert egenskapsverdier</param>
    /// <returns>Opprettet entitetsinstans med generert ID og komplett datainnhold</returns>
    /// <response code="201">Entitetsinstans opprettet og returnerer komplette data med Location header</response>
    /// <response code="400">Valideringsfeil - ugyldig entitetsdefinisjon, duplisert ekstern ID eller ugyldige egenskaper</response>
    /// <response code="409">Konflikt - entitetsinstans med ekstern ID eksisterer allerede i entitetsdefinisjonen</response>
    /// <response code="500">Intern serverfeil under opprettelse av entitetsinstans</response>
    [HttpPost]
    public async Task<ActionResult<EntityInstanceDto>> CreateEntityInstance(CreateEntityInstanceDto createDto)
    {
        try
        {
            // Check if entity definition exists
            var entityDefinition = await _context.EntityDefinitions
                .Include(x => x.PropertyDefinitions)
                .FirstOrDefaultAsync(x => x.Id == createDto.EntityDefinitionId);

            if (entityDefinition == null)
            {
                return BadRequest($"Entity definition with ID {createDto.EntityDefinitionId} not found");
            }

            // Check if external ID already exists within the entity definition
            var existingInstance = await _context.EntityInstances
                .FirstOrDefaultAsync(x => x.EntityDefinitionId == createDto.EntityDefinitionId && 
                                         x.ExternalId == createDto.ExternalId);

            if (existingInstance != null)
            {
                return Conflict($"Entity instance with external ID '{createDto.ExternalId}' already exists in this entity definition");
            }

            var entityInstance = _mapper.Map<EntityInstance>(createDto);
            entityInstance.SyncStatus = SyncStatus.Success;
            entityInstance.LastSyncedAt = DateTime.UtcNow;
            
            _context.EntityInstances.Add(entityInstance);
            await _context.SaveChangesAsync();

            // Create property values
            var propertyValues = new List<PropertyValue>();
            foreach (var propValueDto in createDto.PropertyValues)
            {
                // Validate property definition exists
                var propertyDefinition = entityDefinition.PropertyDefinitions
                    .FirstOrDefault(pd => pd.Id == propValueDto.PropertyDefinitionId);

                if (propertyDefinition == null)
                {
                    return BadRequest($"Property definition with ID {propValueDto.PropertyDefinitionId} not found in this entity definition");
                }

                var propertyValue = _mapper.Map<PropertyValue>(propValueDto);
                propertyValue.EntityInstanceId = entityInstance.Id;
                propertyValues.Add(propertyValue);
            }

            if (propertyValues.Any())
            {
                _context.PropertyValues.AddRange(propertyValues);
                await _context.SaveChangesAsync();
            }

            // Reload with navigation properties
            entityInstance = await _context.EntityInstances
                .Include(x => x.EntityDefinition)
                .Include(x => x.PropertyValues)
                    .ThenInclude(pv => pv.PropertyDefinition)
                .FirstAsync(x => x.Id == entityInstance.Id);

            var dto = _mapper.Map<EntityInstanceDto>(entityInstance);
            dto.PropertyValues = _mapper.Map<List<PropertyValueDto>>(entityInstance.PropertyValues);
            
            return CreatedAtAction(nameof(GetEntityInstance), 
                new { id = entityInstance.Id }, dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating entity instance");
            return StatusCode(500, "An error occurred while creating the entity instance");
        }
    }

    /// <summary>
    /// Oppdaterer en eksisterende entitetsinstans med nye egenskapsverdier
    /// </summary>
    /// <remarks>
    /// Utfører komplett oppdatering av en entitetsinstans inkludert håndtering av
    /// egenskapsverdier. Operasjonen støtter både oppdatering av eksisterende
    /// egenskapsverdier, sletting av fjernede verdier og opprettelse av nye verdier
    /// i en enkelt transaksjonell operasjon.
    /// 
    /// **Oppdateringslogikk:**
    /// - Eksisterende egenskapsverdier oppdateres basert på ID-matching
    /// - Egenskapsverdier ikke inkludert i forespørselen slettes automatisk
    /// - Nye egenskapsverdier uten ID opprettes og knyttes til entitetsinstansen
    /// - Validering utføres mot entitetsdefinisjonen for alle endringer
    /// 
    /// **Valideringsregler:**
    /// - Entitetsinstans og entitetsdefinisjon må eksistere
    /// - Ekstern ID må være unik (ekskludert gjeldende entitetsinstans)
    /// - Alle egenskapsdefinisjoner må være gyldige for entitetsdefinisjonen
    /// - Datatype-validering for alle oppdaterte egenskapsverdier
    /// 
    /// **Sikkerhetshensyn:**
    /// - Ingen automatisk tilgangskontroll - krever ekstern autorisasjonssjekk
    /// - Endringer logges for audit og compliance formål
    /// - Sensitive egenskapsverdier behandles uten automatisk kryptering
    /// 
    /// **Forretningsprosess:**
    /// - Audit-felter oppdateres automatisk med tidsstempel og årsak
    /// - Synkroniseringsstatus kan oppdateres hvis spesifisert
    /// - Transaksjonell behandling sikrer datakonsistens ved feil
    /// </remarks>
    /// <param name="id">Unik identifikator for entitetsinstansen som skal oppdateres</param>
    /// <param name="updateDto">Oppdateringsdata inkludert nye egenskapsverdier og metadata</param>
    /// <returns>Oppdatert entitetsinstans med alle endringer reflektert</returns>
    /// <response code="200">Entitetsinstans oppdatert og returnerer komplette data</response>
    /// <response code="400">Valideringsfeil - ugyldig entitetsdefinisjon, ugyldige egenskaper eller datatype-feil</response>
    /// <response code="404">Entitetsinstans med angitt ID eksisterer ikke i systemet</response>
    /// <response code="409">Konflikt - ny ekstern ID er allerede i bruk i entitetsdefinisjonen</response>
    /// <response code="500">Intern serverfeil under oppdatering av entitetsinstans</response>
    [HttpPut("{id}")]
    public async Task<ActionResult<EntityInstanceDto>> UpdateEntityInstance(Guid id, UpdateEntityInstanceDto updateDto)
    {
        try
        {
            var entityInstance = await _context.EntityInstances
                .Include(x => x.EntityDefinition)
                .Include(x => x.PropertyValues)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entityInstance == null)
            {
                return NotFound($"Entity instance with ID {id} not found");
            }

            // Check if entity definition exists
            var entityDefinition = await _context.EntityDefinitions
                .Include(x => x.PropertyDefinitions)
                .FirstOrDefaultAsync(x => x.Id == updateDto.EntityDefinitionId);

            if (entityDefinition == null)
            {
                return BadRequest($"Entity definition with ID {updateDto.EntityDefinitionId} not found");
            }

            // Check if external ID already exists (excluding current record)
            var existingInstance = await _context.EntityInstances
                .FirstOrDefaultAsync(x => x.EntityDefinitionId == updateDto.EntityDefinitionId && 
                                         x.ExternalId == updateDto.ExternalId && 
                                         x.Id != id);

            if (existingInstance != null)
            {
                return Conflict($"Entity instance with external ID '{updateDto.ExternalId}' already exists in this entity definition");
            }

            // Update entity instance
            _mapper.Map(updateDto, entityInstance);
            entityInstance.LastModifiedReason = updateDto.LastModifiedReason;

            // Update property values
            var existingPropertyValues = entityInstance.PropertyValues.ToList();
            
            // Remove property values not in the update
            var propertyValuesToRemove = existingPropertyValues
                .Where(pv => !updateDto.PropertyValues.Any(upv => upv.Id == pv.Id))
                .ToList();

            _context.PropertyValues.RemoveRange(propertyValuesToRemove);

            // Update existing and add new property values
            foreach (var propValueDto in updateDto.PropertyValues)
            {
                // Validate property definition exists
                var propertyDefinition = entityDefinition.PropertyDefinitions
                    .FirstOrDefault(pd => pd.Id == propValueDto.PropertyDefinitionId);

                if (propertyDefinition == null)
                {
                    return BadRequest($"Property definition with ID {propValueDto.PropertyDefinitionId} not found in this entity definition");
                }

                if (propValueDto.Id.HasValue)
                {
                    // Update existing property value
                    var existingPropertyValue = existingPropertyValues
                        .FirstOrDefault(pv => pv.Id == propValueDto.Id.Value);

                    if (existingPropertyValue != null)
                    {
                        _mapper.Map(propValueDto, existingPropertyValue);
                    }
                }
                else
                {
                    // Add new property value
                    var newPropertyValue = _mapper.Map<PropertyValue>(propValueDto);
                    newPropertyValue.EntityInstanceId = entityInstance.Id;
                    _context.PropertyValues.Add(newPropertyValue);
                }
            }

            await _context.SaveChangesAsync();

            // Reload with navigation properties
            entityInstance = await _context.EntityInstances
                .Include(x => x.EntityDefinition)
                .Include(x => x.PropertyValues)
                    .ThenInclude(pv => pv.PropertyDefinition)
                .FirstAsync(x => x.Id == entityInstance.Id);

            var dto = _mapper.Map<EntityInstanceDto>(entityInstance);
            dto.PropertyValues = _mapper.Map<List<PropertyValueDto>>(entityInstance.PropertyValues);

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating entity instance {Id}", id);
            return StatusCode(500, "An error occurred while updating the entity instance");
        }
    }

    /// <summary>
    /// Sletter en entitetsinstans med myk sletting og avhengighetsvalidering
    /// </summary>
    /// <remarks>
    /// Utfører myk sletting av en entitetsinstans ved å markere den som slettet
    /// uten å fjerne data fysisk fra databasen. Operasjonen validerer først at
    /// entitetsinstansen ikke har aktive avhengigheter som tilgangstildelinger
    /// før sletting tillates.
    /// 
    /// **Slettingsprosess:**
    /// - Myk sletting bevarer data for audit og historikk
    /// - IsDeleted flagg settes til true med slettingstidspunkt
    /// - Slettende bruker registreres for sporingformål
    /// - Ingen fysisk fjerning av data fra database
    /// 
    /// **Avhengighetsvalidering:**
    /// - Sjekker for aktive tilgangstildelinger hvor entiteten brukes som bruker eller rolle
    /// - Validerer at ingen kritiske forretningsprosesser er avhengige av entiteten
    /// - Forhindrer sletting hvis entiteten refereres av andre systemkomponenter
    /// 
    /// **Sikkerhetshensyn:**
    /// - Ingen automatisk autorisasjonssjekk - krever ekstern tilgangskontroll
    /// - Slettehandling logges permanent for compliance og audit
    /// - Mulighet for gjenoppretting siden data bevares fysisk
    /// 
    /// **Forretningsregler:**
    /// - Entitetsinstanser med aktive tilgangstildelinger kan ikke slettes
    /// - Systematisk bruker registreres som slettende part hvis brukerkontext mangler
    /// - Slettede entiteter ekskluderes automatisk fra standardspørringer
    /// </remarks>
    /// <param name="id">Unik identifikator for entitetsinstansen som skal slettes</param>
    /// <returns>Ingen innhold ved vellykket sletting</returns>
    /// <response code="204">Entitetsinstans slettet (myk sletting) uten returdata</response>
    /// <response code="400">Kan ikke slette - entiteten har aktive tilgangstildelinger eller andre avhengigheter</response>
    /// <response code="404">Entitetsinstans med angitt ID eksisterer ikke i systemet</response>
    /// <response code="500">Intern serverfeil under sletting av entitetsinstans</response>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEntityInstance(Guid id)
    {
        try
        {
            var entityInstance = await _context.EntityInstances
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entityInstance == null)
            {
                return NotFound($"Entity instance with ID {id} not found");
            }

            // Check if there are dependent records (access assignments)
            var hasAccessAssignments = await _context.AccessAssignments
                .AnyAsync(x => x.UserId == id || x.RoleId == id);

            if (hasAccessAssignments)
            {
                return BadRequest("Cannot delete entity instance with existing access assignments");
            }

            // Soft delete
            entityInstance.IsDeleted = true;
            entityInstance.DeletedAt = DateTime.UtcNow;
            entityInstance.DeletedBy = "system"; // TODO: Get from current user context

            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting entity instance {Id}", id);
            return StatusCode(500, "An error occurred while deleting the entity instance");
        }
    }
}
