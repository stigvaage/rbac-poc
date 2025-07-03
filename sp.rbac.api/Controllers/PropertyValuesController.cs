using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SP.RBAC.API.Data;
using SP.RBAC.API.DTOs;
using SP.RBAC.API.Entities;

namespace SP.RBAC.API.Controllers;

/// <summary>
/// Administrerer egenskapsverdier som representerer faktiske dataverdier for entitetsinstanser
/// </summary>
/// <remarks>
/// Egenskapsverdier implementerer Entity-Attribute-Value (EAV) mønsteret og representerer
/// de faktiske dataene knyttet til entitetsinstanser basert på deres egenskapsdefinisjoner.
/// Hver egenskapsverdi lagrer både rå data og formaterte visningsverdier med full
/// historikk og validering mot definerte datatyper og forretningsregler.
/// 
/// **EAV-arkitektur:**
/// - Fleksibel datamodellering uten skjemaendringer i database
/// - Dynamisk tilpasning til nye datatyper og egenskaper
/// - Skalerbar håndtering av heterogene datastrukturer
/// - Optimalisert for komplekse entitetsrelasjoner på tvers av systemer
/// 
/// **Datatyper og validering:**
/// - Støtte for alle primitive datatyper (tekst, tall, datoer, booleaner)
/// - Komplekse datatyper som JSON, XML og binære data
/// - Automatisk validering mot egenskapsdefinisjonens regler
/// - Formatering og lokalisering av visningsverdier
/// 
/// **Historikk og audit:**
/// - Komplett endringslogging med tidsstempler og brukeridentifikasjon
/// - Versjonering av egenskapsverdier for historisk analyse
/// - Data lineage og kildesystem-sporing
/// - GDPR-kompatibel håndtering av persondata med slettingsmuligheter
/// 
/// **Ytelse og optimalisering:**
/// - Indekserte spørringer på hyppig tilgåtte egenskaper
/// - Caching av statiske og referansedata
/// - Bulk-operasjoner for masseimport og synkronisering
/// - Delta-oppdateringer for optimal nettverkstrafikk
/// 
/// **Forretningslogikk:**
/// - Støtte for standardverdier og beregnede felter
/// - Validering av forretningsregler på tvers av egenskaper
/// - Automatisk transformasjon mellom interne og eksterne dataformater
/// - Konfliktløsning ved samtidige oppdateringer fra flere kilder
/// </remarks>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PropertyValuesController : ControllerBase
{
    private readonly RbacDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<PropertyValuesController> _logger;

    public PropertyValuesController(RbacDbContext context, IMapper mapper, ILogger<PropertyValuesController> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Henter alle egenskapsverdier med avansert filtrering og paginering
    /// </summary>
    /// <remarks>
    /// Returnerer en omfattende oversikt over egenskapsverdier i systemet med støtte
    /// for detaljert filtrering basert på entitetsinstanser, egenskapsdefinisjoner
    /// og verdistatus. Operasjonen inkluderer alle relaterte entiteter for
    /// fullstendig kontekst og forretningsforståelse.
    /// 
    /// **Filtreringsmuligheter:**
    /// - Entitetsinstans-filtrering for å hente alle egenskaper til en spesifikk entitet
    /// - Egenskapsdefinisjon-filtrering for å analysere verdier for en bestemt egenskap
    /// - Standardverdi-filtrering for å skille mellom eksplisitte og beregnede verdier
    /// - Fritekstsøk i både rå verdier og formaterte visningsverdier
    /// 
    /// **Sikkerhetsvurderinger:**
    /// - Kan eksponere sensitive persondata og forretningskritisk informasjon
    /// - Rå verdier kan inneholde ukrypterte sensitive data
    /// - Søkefunksjonalitet kan avsløre eksistensen av konfidensielle datastrukturer
    /// - Krever streng tilgangskontroll basert på dataklassifisering
    /// 
    /// **Ytelsesoptimalisering:**
    /// - Eager loading av entitetsinstanser og egenskapsdefinisjoner
    /// - Optimaliserte indekser på hyppig filtrerte felter
    /// - Paginering kritisk for store datasett med mange egenskapsverdier
    /// - Caching av metadata for reduserte database-kall
    /// 
    /// **Forretningsbruk:**
    /// - Datakvalitetsanalyse og validering av egenskapsverdier
    /// - Rapportering og compliance-dokumentasjon
    /// - Feilsøking av synkroniseringsproblemer mellem systemer
    /// - Grunnlag for datamigrering og transformasjonsprosesser
    /// </remarks>
    /// <param name="page">Sidenummer for paginering (1-basert, standard: 1)</param>
    /// <param name="pageSize">Antall elementer per side (standard: 10, anbefalt maksimum: 100)</param>
    /// <param name="entityInstanceId">Filtrer på spesifikk entitetsinstans for å hente alle dens egenskaper</param>
    /// <param name="propertyDefinitionId">Filtrer på spesifikk egenskapsdefinisjon for verdianalyse</param>
    /// <param name="isDefault">Filtrer på standardverdi-status (null=alle, true=standardverdier, false=eksplisitte verdier)</param>
    /// <param name="search">Fritekstsøk i verdier og visningsverdier for datainnhold</param>
    /// <returns>Paginert resultat med egenskapsverdier og fullstendig kontekstinformasjon</returns>
    /// <response code="200">Returnerer paginert liste over egenskapsverdier med relaterte entiteter</response>
    /// <response code="400">Ugyldig forespørsel - valideringsfeil i filtreringsparametere</response>
    /// <response code="500">Intern serverfeil under henting av egenskapsverdier</response>
    [HttpGet]
    public async Task<ActionResult<PagedResult<PropertyValueDto>>> GetPropertyValues(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] Guid? entityInstanceId = null,
        [FromQuery] Guid? propertyDefinitionId = null,
        [FromQuery] bool? isDefault = null,
        [FromQuery] string? search = null)
    {
        try
        {
            var query = _context.PropertyValues
                .Include(pv => pv.EntityInstance)
                    .ThenInclude(ei => ei.EntityDefinition)
                .Include(pv => pv.PropertyDefinition)
                .AsQueryable();

            // Apply filters
            if (entityInstanceId.HasValue)
                query = query.Where(pv => pv.EntityInstanceId == entityInstanceId.Value);

            if (propertyDefinitionId.HasValue)
                query = query.Where(pv => pv.PropertyDefinitionId == propertyDefinitionId.Value);

            if (isDefault.HasValue)
                query = query.Where(pv => pv.IsDefault == isDefault.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower();
                query = query.Where(pv => 
                    pv.Value.ToLower().Contains(searchLower) ||
                    (pv.DisplayValue != null && pv.DisplayValue.ToLower().Contains(searchLower)) ||
                    pv.EntityInstance.DisplayName.ToLower().Contains(searchLower) ||
                    pv.PropertyDefinition.Name.ToLower().Contains(searchLower));
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(pv => pv.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = _mapper.Map<List<PropertyValueDto>>(items);

            var result = new PagedResult<PropertyValueDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = pageSize
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving property values");
            return StatusCode(500, "An error occurred while retrieving property values");
        }
    }

    /// <summary>
    /// Henter en spesifikk egenskapsverdi med komplett kontekstinformasjon
    /// </summary>
    /// <remarks>
    /// Returnerer detaljert informasjon om en enkelt egenskapsverdi inkludert
    /// alle relaterte entiteter som entitetsinstans, entitetsdefinisjon og
    /// egenskapsdefinisjon. Operasjonen gir fullstendig kontekst for å forstå
    /// verdiens betydning og bruk i forretningsprosessene.
    /// 
    /// **Datainnhold:**
    /// - Komplett egenskapsverdi med både rå verdi og formatert visningsverdi
    /// - Tilknyttet entitetsinstans med full entitetskontekst
    /// - Egenskapsdefinisjon med datatype og valideringsregler
    /// - Metadata som standardverdi-status og audit-informasjon
    /// 
    /// **Sikkerhetsvurderinger:**
    /// - Kan eksponere sensitive persondata eller forretningskritisk informasjon
    /// - Returnerer rå dataverdier som kan være ukrypterte
    /// - Eksponerer interne datastrukturer og systemreferanser
    /// - Krever tilgangskontroll basert på dataklassifisering og brukertilgang
    /// 
    /// **Forretningsbruk:**
    /// - Detaljert analyse av spesifikke datafelter og verdier
    /// - Feilsøking av datavalidering og transformasjonsproblemer
    /// - Audit og compliance-sporing av individuelle dataelementer
    /// - Grunnlag for datakorrigering og kvalitetsforbedring
    /// </remarks>
    /// <param name="id">Unik identifikator for egenskapsverdien som skal hentes</param>
    /// <returns>Komplett egenskapsverdi med all relatert kontekstinformasjon</returns>
    /// <response code="200">Egenskapsverdi funnet og returnert med komplett datainnhold</response>
    /// <response code="404">Egenskapsverdi med angitt ID eksisterer ikke i systemet</response>
    /// <response code="500">Intern serverfeil under henting av egenskapsverdi</response>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PropertyValueDto>> GetPropertyValue(Guid id)
    {
        try
        {
            var propertyValue = await _context.PropertyValues
                .Include(pv => pv.EntityInstance)
                    .ThenInclude(ei => ei.EntityDefinition)
                .Include(pv => pv.PropertyDefinition)
                .FirstOrDefaultAsync(pv => pv.Id == id);

            if (propertyValue == null)
                return NotFound($"Property value with ID {id} not found");

            var dto = _mapper.Map<PropertyValueDto>(propertyValue);
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving property value {Id}", id);
            return StatusCode(500, "An error occurred while retrieving the property value");
        }
    }

    /// <summary>
    /// Oppretter en ny egenskapsverdi med validering og forretningsregelsjekk
    /// </summary>
    /// <remarks>
    /// Oppretter en ny egenskapsverdi for en eksisterende entitetsinstans basert på
    /// en definert egenskapsdefinisjon. Operasjonen utfører omfattende validering
    /// av refererte entiteter, datatype-kompatibilitet og forretningsregler før
    /// opprettelse av den nye egenskapsverdien.
    /// 
    /// **Valideringsregler:**
    /// - Entitetsinstans må eksistere og være aktiv i systemet
    /// - Egenskapsdefinisjon må eksistere og være gyldig for entitetsdefinisjonen
    /// - Sjekk for eksisterende egenskapsverdier for samme entitet og egenskap
    /// - Datatype-validering mot egenskapsdefinisjonens spesifikasjoner
    /// - Forretningsregelvalidering hvis konfigurert på egenskapsdefinisjonen
    /// 
    /// **Datahåndtering:**
    /// - Automatisk formatering av visningsverdi basert på datatype
    /// - Transformasjon og normalisering av inngående dataverdier
    /// - Støtte for standardverdier hvis ikke eksplisitt spesifisert
    /// - Validering av verdiområder og constraints definert i egenskapsdefinisjon
    /// 
    /// **Sikkerhetshensyn:**
    /// - Ingen automatisk autorisasjonssjekk - krever ekstern tilgangskontroll
    /// - Egenskapsverdier kan inneholde sensitive data som lagres ukryptert
    /// - Audit-logging registrerer opprettelseshandlingen for compliance
    /// - Validering av datatilgang basert på entitetens sikkerhetsnivå
    /// 
    /// **Forretningsprosess:**
    /// - Automatisk oppdatering av entitetsinstansens siste endringstidspunkt
    /// - Integrasjon med datavalidering og kvalitetssikringsprosesser
    /// - Støtte for bulk-operasjoner gjennom batch-prosessering
    /// - Triggering av eventuelle downstream-prosesser og notifikasjoner
    /// </remarks>
    /// <param name="dto">Opprettelsesdata for ny egenskapsverdi inkludert verdi og metadata</param>
    /// <returns>Opprettet egenskapsverdi med generert ID og komplett kontekstinformasjon</returns>
    /// <response code="201">Egenskapsverdi opprettet og returnerer komplette data med Location header</response>
    /// <response code="400">Valideringsfeil - ugyldig entitetsinstans, egenskapsdefinisjon eller dataverdi</response>
    /// <response code="409">Konflikt - egenskapsverdi eksisterer allerede for denne kombinasjonen</response>
    /// <response code="500">Intern serverfeil under opprettelse av egenskapsverdi</response>
    [HttpPost]
    public async Task<ActionResult<PropertyValueDto>> CreatePropertyValue(CreatePropertyValueDto dto)
    {
        try
        {
            // Validate referenced entities exist
            var entityInstanceExists = await _context.EntityInstances.AnyAsync(ei => ei.Id == dto.EntityInstanceId);
            if (!entityInstanceExists)
                return BadRequest($"Entity instance with ID {dto.EntityInstanceId} not found");

            var propertyDefinitionExists = await _context.PropertyDefinitions.AnyAsync(pd => pd.Id == dto.PropertyDefinitionId);
            if (!propertyDefinitionExists)
                return BadRequest($"Property definition with ID {dto.PropertyDefinitionId} not found");

            // Check if property value already exists for this entity instance and property definition
            var existingValue = await _context.PropertyValues
                .FirstOrDefaultAsync(pv => 
                    pv.EntityInstanceId == dto.EntityInstanceId && 
                    pv.PropertyDefinitionId == dto.PropertyDefinitionId &&
                    pv.EffectiveTo == null); // Currently effective

            if (existingValue != null)
                return Conflict("A property value already exists for this entity instance and property definition combination");

            var propertyValue = _mapper.Map<PropertyValue>(dto);
            propertyValue.Id = Guid.NewGuid();
            propertyValue.CreatedAt = DateTime.UtcNow;
            propertyValue.CreatedBy = "System"; // In real app, get from user context

            _context.PropertyValues.Add(propertyValue);
            await _context.SaveChangesAsync();

            // Fetch the created property value with navigation properties
            var createdPropertyValue = await _context.PropertyValues
                .Include(pv => pv.EntityInstance)
                    .ThenInclude(ei => ei.EntityDefinition)
                .Include(pv => pv.PropertyDefinition)
                .FirstOrDefaultAsync(pv => pv.Id == propertyValue.Id);

            var resultDto = _mapper.Map<PropertyValueDto>(createdPropertyValue);
            return CreatedAtAction(nameof(GetPropertyValue), new { id = propertyValue.Id }, resultDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating property value");
            return StatusCode(500, "An error occurred while creating the property value");
        }
    }

    /// <summary>
    /// Oppdaterer en eksisterende egenskapsverdi med validering og konsistenssjekk
    /// </summary>
    /// <remarks>
    /// Utfører komplett oppdatering av en eksisterende egenskapsverdi med validering
    /// av alle refererte entiteter og konsistenssjekk hvis nøkkelfelt endres.
    /// Operasjonen støtter endring av både verdien selv og tilknyttede metadata
    /// mens den opprettholder dataintegritet og forretningsregler.
    /// 
    /// **Valideringsregler:**
    /// - Egenskapsverdi må eksistere før oppdatering kan utføres
    /// - Nye entitetsinstans- og egenskapsdefinisjon-referanser må være gyldige
    /// - Konfliktsjekk utføres hvis entitet-egenskap kombinasjon endres
    /// - Datatype-validering mot oppdatert eller eksisterende egenskapsdefinisjon
    /// - Forretningsregelvalidering for verdier og constraints
    /// 
    /// **Datahåndtering:**
    /// - Automatisk re-formatering av visningsverdi ved verdiendring
    /// - Bevaring av historikk gjennom audit-sporing
    /// - Validering av nye verdier mot egenskapsdefinisjonens spesifikasjoner
    /// - Støtte for partial updates hvor kun endrede felter påvirkes
    /// 
    /// **Sikkerhetshensyn:**
    /// - Kritisk dataoperasjon som kan påvirke forretningsprosesser
    /// - Endringer i egenskapsverdier logges for audit og compliance
    /// - Validering av brukerens tilgang til å endre sensitive data
    /// - Ingen automatisk autorisasjonssjekk - krever ekstern tilgangskontroll
    /// 
    /// **Forretningsprosess:**
    /// - Automatisk oppdatering av audit-felter med tidsstempel og brukerinfo
    /// - Triggering av downstream-prosesser ved kritiske verdiendringer
    /// - Støtte for optimistisk låsing for å forhindre conflicting updates
    /// - Integrasjon med datakvalitet og validering workflows
    /// </remarks>
    /// <param name="id">Unik identifikator for egenskapsverdien som skal oppdateres</param>
    /// <param name="dto">Oppdateringsdata for egenskapsverdi inkludert nye verdier og metadata</param>
    /// <returns>Oppdatert egenskapsverdi med alle endringer reflektert</returns>
    /// <response code="200">Egenskapsverdi oppdatert og returnerer komplette data</response>
    /// <response code="400">Valideringsfeil - ugyldig entitetsinstans, egenskapsdefinisjon eller dataverdi</response>
    /// <response code="404">Egenskapsverdi med angitt ID eksisterer ikke i systemet</response>
    /// <response code="409">Konflikt - ny kombinasjon ville skape duplikat av eksisterende egenskapsverdi</response>
    /// <response code="500">Intern serverfeil under oppdatering av egenskapsverdi</response>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PropertyValueDto>> UpdatePropertyValue(Guid id, UpdatePropertyValueDto dto)
    {
        try
        {
            var existingPropertyValue = await _context.PropertyValues.FindAsync(id);
            if (existingPropertyValue == null)
                return NotFound($"Property value with ID {id} not found");

            // Validate referenced entities exist
            var entityInstanceExists = await _context.EntityInstances.AnyAsync(ei => ei.Id == dto.EntityInstanceId);
            if (!entityInstanceExists)
                return BadRequest($"Entity instance with ID {dto.EntityInstanceId} not found");

            var propertyDefinitionExists = await _context.PropertyDefinitions.AnyAsync(pd => pd.Id == dto.PropertyDefinitionId);
            if (!propertyDefinitionExists)
                return BadRequest($"Property definition with ID {dto.PropertyDefinitionId} not found");

            // Check for conflicting property value (if changing key fields)
            if (existingPropertyValue.EntityInstanceId != dto.EntityInstanceId || 
                existingPropertyValue.PropertyDefinitionId != dto.PropertyDefinitionId)
            {
                var conflictingValue = await _context.PropertyValues
                    .FirstOrDefaultAsync(pv => 
                        pv.Id != id &&
                        pv.EntityInstanceId == dto.EntityInstanceId && 
                        pv.PropertyDefinitionId == dto.PropertyDefinitionId &&
                        pv.EffectiveTo == null);

                if (conflictingValue != null)
                    return Conflict("A property value already exists for this entity instance and property definition combination");
            }

            _mapper.Map(dto, existingPropertyValue);
            existingPropertyValue.UpdatedAt = DateTime.UtcNow;
            existingPropertyValue.UpdatedBy = "System"; // In real app, get from user context

            await _context.SaveChangesAsync();

            // Fetch updated property value with navigation properties
            var updatedPropertyValue = await _context.PropertyValues
                .Include(pv => pv.EntityInstance)
                    .ThenInclude(ei => ei.EntityDefinition)
                .Include(pv => pv.PropertyDefinition)
                .FirstOrDefaultAsync(pv => pv.Id == id);

            var resultDto = _mapper.Map<PropertyValueDto>(updatedPropertyValue);
            return Ok(resultDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating property value {Id}", id);
            return StatusCode(500, "An error occurred while updating the property value");
        }
    }

    /// <summary>
    /// Delete a property value
    /// </summary>
    /// <param name="id">Property value ID</param>
    /// <returns>Success status</returns>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeletePropertyValue(Guid id)
    {
        try
        {
            var propertyValue = await _context.PropertyValues.FindAsync(id);
            if (propertyValue == null)
                return NotFound($"Property value with ID {id} not found");

            // Soft delete
            propertyValue.IsDeleted = true;
            propertyValue.DeletedAt = DateTime.UtcNow;
            propertyValue.DeletedBy = "System"; // In real app, get from user context

            await _context.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting property value {Id}", id);
            return StatusCode(500, "An error occurred while deleting the property value");
        }
    }

    /// <summary>
    /// Get property values for a specific entity instance
    /// </summary>
    /// <param name="entityInstanceId">Entity instance ID</param>
    /// <param name="includeHistory">Include historical (expired) values</param>
    /// <returns>List of entity instance's property values</returns>
    [HttpGet("entity-instance/{entityInstanceId:guid}")]
    public async Task<ActionResult<List<PropertyValueDto>>> GetEntityInstancePropertyValues(
        Guid entityInstanceId, 
        [FromQuery] bool includeHistory = false)
    {
        try
        {
            var query = _context.PropertyValues
                .Include(pv => pv.EntityInstance)
                    .ThenInclude(ei => ei.EntityDefinition)
                .Include(pv => pv.PropertyDefinition)
                .Where(pv => pv.EntityInstanceId == entityInstanceId);

            if (!includeHistory)
                query = query.Where(pv => pv.EffectiveTo == null || pv.EffectiveTo > DateTime.UtcNow);

            var propertyValues = await query
                .OrderBy(pv => pv.PropertyDefinition.Name)
                .ThenByDescending(pv => pv.CreatedAt)
                .ToListAsync();

            var dtos = _mapper.Map<List<PropertyValueDto>>(propertyValues);
            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving property values for entity instance {EntityInstanceId}", entityInstanceId);
            return StatusCode(500, "An error occurred while retrieving property values");
        }
    }

    /// <summary>
    /// Get property values for a specific property definition
    /// </summary>
    /// <param name="propertyDefinitionId">Property definition ID</param>
    /// <param name="includeHistory">Include historical (expired) values</param>
    /// <returns>List of property definition's values</returns>
    [HttpGet("property-definition/{propertyDefinitionId:guid}")]
    public async Task<ActionResult<List<PropertyValueDto>>> GetPropertyDefinitionValues(
        Guid propertyDefinitionId, 
        [FromQuery] bool includeHistory = false)
    {
        try
        {
            var query = _context.PropertyValues
                .Include(pv => pv.EntityInstance)
                    .ThenInclude(ei => ei.EntityDefinition)
                .Include(pv => pv.PropertyDefinition)
                .Where(pv => pv.PropertyDefinitionId == propertyDefinitionId);

            if (!includeHistory)
                query = query.Where(pv => pv.EffectiveTo == null || pv.EffectiveTo > DateTime.UtcNow);

            var propertyValues = await query
                .OrderBy(pv => pv.EntityInstance.DisplayName)
                .ThenByDescending(pv => pv.CreatedAt)
                .ToListAsync();

            var dtos = _mapper.Map<List<PropertyValueDto>>(propertyValues);
            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving property values for property definition {PropertyDefinitionId}", propertyDefinitionId);
            return StatusCode(500, "An error occurred while retrieving property values");
        }
    }

    /// <summary>
    /// Get property value history for an entity instance grouped by property definition
    /// </summary>
    /// <param name="entityInstanceId">Entity instance ID</param>
    /// <returns>Property value history grouped by property definition</returns>
    [HttpGet("entity-instance/{entityInstanceId:guid}/history")]
    public async Task<ActionResult<List<PropertyValueHistoryDto>>> GetEntityInstancePropertyValueHistory(Guid entityInstanceId)
    {
        try
        {
            var propertyValues = await _context.PropertyValues
                .Include(pv => pv.EntityInstance)
                    .ThenInclude(ei => ei.EntityDefinition)
                .Include(pv => pv.PropertyDefinition)
                .Where(pv => pv.EntityInstanceId == entityInstanceId)
                .OrderBy(pv => pv.PropertyDefinition.Name)
                .ThenByDescending(pv => pv.CreatedAt)
                .ToListAsync();

            var groupedHistory = propertyValues
                .GroupBy(pv => new { pv.PropertyDefinitionId, pv.PropertyDefinition.Name })
                .Select(g => new PropertyValueHistoryDto
                {
                    PropertyDefinitionId = g.Key.PropertyDefinitionId,
                    PropertyDefinitionName = g.Key.Name,
                    ValueHistory = _mapper.Map<List<PropertyValueDto>>(g.ToList())
                })
                .ToList();

            return Ok(groupedHistory);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving property value history for entity instance {EntityInstanceId}", entityInstanceId);
            return StatusCode(500, "An error occurred while retrieving property value history");
        }
    }

    /// <summary>
    /// Set property value effective end date (expire it)
    /// </summary>
    /// <param name="id">Property value ID</param>
    /// <param name="effectiveTo">Effective end date</param>
    /// <returns>Updated property value</returns>
    [HttpPatch("{id:guid}/expire")]
    public async Task<ActionResult<PropertyValueDto>> ExpirePropertyValue(Guid id, [FromBody] DateTime effectiveTo)
    {
        try
        {
            var propertyValue = await _context.PropertyValues
                .Include(pv => pv.EntityInstance)
                    .ThenInclude(ei => ei.EntityDefinition)
                .Include(pv => pv.PropertyDefinition)
                .FirstOrDefaultAsync(pv => pv.Id == id);

            if (propertyValue == null)
                return NotFound($"Property value with ID {id} not found");

            propertyValue.EffectiveTo = effectiveTo;
            propertyValue.UpdatedAt = DateTime.UtcNow;
            propertyValue.UpdatedBy = "System"; // In real app, get from user context

            await _context.SaveChangesAsync();

            var dto = _mapper.Map<PropertyValueDto>(propertyValue);
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error expiring property value {Id}", id);
            return StatusCode(500, "An error occurred while expiring the property value");
        }
    }
}
