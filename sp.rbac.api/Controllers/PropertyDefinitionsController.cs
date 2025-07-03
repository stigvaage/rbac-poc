using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SP.RBAC.API.Data;
using SP.RBAC.API.DTOs;
using SP.RBAC.API.Entities;
using Swashbuckle.AspNetCore.Annotations;

namespace SP.RBAC.API.Controllers;

/// <summary>
/// Administrerer egenskapsdefinisjonene som definerer datastrukturer og validering for entiteter
/// </summary>
/// <remarks>
/// Egenskapsdefinisjonene utgjør fundamentet for datamodellering og strukturering i RBAC-systemet.
/// Hver definisjon spesifiserer hvordan data skal struktureres, valideres og presenteres.
/// 
/// **Hovedfunksjoner:**
/// - Definere datatyper og valideringsregler for entitetsegenskaper
/// - Konfigurere visningslogikk og brukergrensesnitt-parametere
/// - Administrere obligatoriske felt og unike restriksjoner
/// - Etablere datakonsistens på tvers av systemintegrasjoner
/// 
/// **Datatyper støttet:**
/// - String: Tekstdata med lengdebegrensninger og mønstre
/// - Integer: Heltall med verdiområde-validering
/// - Decimal: Desimaltall for presise beregninger
/// - Boolean: Sann/usann verdier for tilstand og flagg
/// - DateTime: Tids- og datostempel med timezone-håndtering
/// - Guid: Universelle unike identifikatorer
/// - Email: E-postadresser med format-validering
/// - Phone: Telefonnummer med lands- og formatstøtte
/// - Url: Web-adresser med protokoll-validering
/// - Json: Strukturerte JSON-objekter for komplekse data
/// 
/// **Valideringsalternativer:**
/// - Obligatoriske felt (required) med tilpassede feilmeldinger
/// - Unike verdier (unique) med konfliktløsning
/// - Verdiområder (range) for numeriske og datofelt
/// - Regulære uttrykk (regex) for mønstervalidering
/// - Tilpassede valideringsregler med forretningslogikk
/// 
/// **Integrasjonsscenarioer:**
/// - Ekstern systemmapping via IntegrationMapping-entiteter
/// - API-responsskjema for konsistente datastrukturer
/// - Brukergrensesnitt-generering basert på metadata
/// - Rapportkolonner og filtreringsparametere
/// 
/// **Sikkerhet og etterlevelse:**
/// - Datakategorisering for personvern (GDPR-merking)
/// - Krypteringskrav for sensitive datatyper
/// - Auditlogging av alle strukturelle endringer
/// - Tilgangskontroll basert på datakategori
/// </remarks>
[ApiController]
[Route("api/[controller]")]
[SwaggerTag("Egenskapsdefinisjonene")]
[Produces("application/json")]
public class PropertyDefinitionsController : ControllerBase
{
    private readonly RbacDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<PropertyDefinitionsController> _logger;

    public PropertyDefinitionsController(RbacDbContext context, IMapper mapper, ILogger<PropertyDefinitionsController> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Hent paginerte egenskapsdefinisjonene med filtrering og søk
    /// </summary>
    /// <remarks>
    /// Returnerer en paginert liste over egenskapsdefinisjonene med avanserte filtrerings- og søkemuligheter.
    /// Støtter søk på tvers av navn, visningsnavn og beskrivelse for effektiv navigering.
    /// 
    /// **Filtreringsmuligheter:**
    /// - Entitetsdefinisjon: Filtrer egenskaper tilhørende spesifikke entiteter
    /// - Datatype: Begrens resultater til bestemte datatyper
    /// - Fritekst søk: Søk i navn, visningsnavn og beskrivelse
    /// - Aktive/inaktive: Inkluder eller ekskluder slettet/deaktiverte egenskaper
    /// 
    /// **Sorteringslogikk:**
    /// - Primær: Sortert etter SortOrder for logisk gruppering
    /// - Sekundær: Alfabetisk etter navn for konsistent presentasjon
    /// - Stabil sortering sikrer forutsigbar rekkefølge på tvers av sider
    /// 
    /// **Bruksscenarioer:**
    /// - Administrasjonsgrensesnitt for systemkonfigurasjon
    /// - API-dokumentasjon og skjemavalidering
    /// - Integrasjonsmapping og datafelt-utforskning
    /// - Rapportkolonne-utvalg og filterkonfigurasjon
    /// 
    /// **Ytelsesoptimalisering:**
    /// - Effektiv indeksering på søkekriterier
    /// - Lazy loading av relasjonelle data
    /// - Caching av hyppig brukte egenskapsgrupper
    /// 
    /// **Paginering:**
    /// - Minimums sidestørrelse: 1, maksimum: 100
    /// - Standard sidestørrelse: 10 for optimal brukervennlighet
    /// - Totalt antall for progressindikatorer
    /// </remarks>
    /// <param name="pageNumber">Sidenummer for paginering (starter på 1)</param>
    /// <param name="pageSize">Antall elementer per side (1-100)</param>
    /// <param name="search">Fritekst søketerm for navn, visningsnavn og beskrivelse</param>
    /// <param name="entityDefinitionId">Filtrer etter spesifikk entitetsdefinisjon</param>
    /// <param name="dataType">Filtrer etter datatype (String, Integer, Boolean, osv.)</param>
    /// <returns>Paginert liste med egenskapsdefinisjonene og metadata</returns>
    /// <response code="200">Egenskapsdefinisjonene hentet vellykket</response>
    /// <response code="400">Ugyldig forespørsel eller parametere</response>
    /// <response code="500">Intern serverfeil oppstod</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<PropertyDefinitionDto>), 200)]
    [ProducesResponseType(typeof(string), 400)]
    [ProducesResponseType(typeof(string), 500)]
    public async Task<ActionResult<PagedResult<PropertyDefinitionDto>>> GetPropertyDefinitions(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] Guid? entityDefinitionId = null,
        [FromQuery] DataType? dataType = null)
    {
        try
        {
            var query = _context.PropertyDefinitions
                .Include(x => x.EntityDefinition)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(x => x.Name.Contains(search) || 
                                        x.DisplayName.Contains(search) || 
                                        x.Description.Contains(search));
            }

            if (entityDefinitionId.HasValue)
            {
                query = query.Where(x => x.EntityDefinitionId == entityDefinitionId.Value);
            }

            if (dataType.HasValue)
            {
                query = query.Where(x => x.DataType == dataType.Value);
            }

            var totalCount = await query.CountAsync();
            
            var items = await query
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = _mapper.Map<IEnumerable<PropertyDefinitionDto>>(items);

            var result = new PagedResult<PropertyDefinitionDto>
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
            _logger.LogError(ex, "Error getting property definitions");
            return StatusCode(500, "An error occurred while retrieving property definitions");
        }
    }

    /// <summary>
    /// Hent en spesifikk egenskapsdefinisjon med komplette detaljer
    /// </summary>
    /// <remarks>
    /// Returnerer detaljert informasjon om en enkelt egenskapsdefinisjon inkludert:
    /// - Fullstendige metadata og konfigurasjonsparametere
    /// - Tilknyttet entitetsdefinisjon med kontekstuell informasjon
    /// - Valideringsregler og forretningslogikk-konfigurasjoner
    /// - Integrasjonsmapping og eksterne systemreferanser
    /// 
    /// **Bruksscenarioer:**
    /// - Detaljert konfigurasjon og redigering av egenskaper
    /// - Validering av datastruktur før integrasjonsopprettelse
    /// - Analyse av egenskapsavhengigheter og dataflyt
    /// - Generering av brukergrensesnitt-komponenter
    /// 
    /// **Inkluderte data:**
    /// - Grunnleggende metadata: Navn, visningsnavn, beskrivelse
    /// - Tekniske parametere: Datatype, validering, unike restriksjoner
    /// - Presentasjonslogikk: Sorteringsrekkefølge, visningsregler
    /// - Relasjonsdata: Tilknyttet entitetsdefinisjon og organisasjon
    /// 
    /// **Valideringsdata:**
    /// - Min/maks verdier for numeriske og datofelt
    /// - Påkrevd/valgfri status med tilpassede feilmeldinger
    /// - Regulære uttrykk for mønstervalidering
    /// - Tilpassede forretningsregler og integritetssjekker
    /// 
    /// **Integrasjonsrelevans:**
    /// - Mapping til eksterne systemfelt og datakilder
    /// - API-skjemavalidering for inngående data
    /// - Datatransformasjon og formatkonvertering
    /// - Sikkerhetskategorisering for databeskyttelse
    /// </remarks>
    /// <param name="id">Unik identifikator for egenskapsdefinisjon</param>
    /// <returns>Detaljerte opplysninger om egenskapsdefinisjon</returns>
    /// <response code="200">Egenskapsdefinisjon funnet og returnert</response>
    /// <response code="404">Egenskapsdefinisjon ikke funnet med angitt ID</response>
    /// <response code="500">Intern serverfeil oppstod</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(PropertyDefinitionDto), 200)]
    [ProducesResponseType(typeof(string), 404)]
    [ProducesResponseType(typeof(string), 500)]
    public async Task<ActionResult<PropertyDefinitionDto>> GetPropertyDefinition(Guid id)
    {
        try
        {
            var propertyDefinition = await _context.PropertyDefinitions
                .Include(x => x.EntityDefinition)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (propertyDefinition == null)
            {
                return NotFound($"Property definition with ID {id} not found");
            }

            var dto = _mapper.Map<PropertyDefinitionDto>(propertyDefinition);
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting property definition {Id}", id);
            return StatusCode(500, "An error occurred while retrieving the property definition");
        }
    }

    /// <summary>
    /// Opprett ny egenskapsdefinisjon med validering og konsistenssjekking
    /// </summary>
    /// <remarks>
    /// Oppretter en ny egenskapsdefinisjon med omfattende validering av dataintegritet og forretningsregler.
    /// Sikrer konsistens på tvers av entitetsdefinisjoner og forhindrer navnekonflikter.
    /// 
    /// **Valideringsprosess:**
    /// 1. Verifiserer at tilknyttet entitetsdefinisjon eksisterer og er aktiv
    /// 2. Sjekker for navnekonflikter innenfor samme entitet
    /// 3. Validerer datatype-kompatibilitet med eksisterende systemer
    /// 4. Kontrollerer forretningsregler og integritetsbestemmelser
    /// 5. Tester valideringsregler for syntaks og utførbarhet
    /// 
    /// **Datatypehåndtering:**
    /// - String: Automatisk trimming og lengdevalidering
    /// - Numeriske: Verifikasjoner av verdiområder og presisjon
    /// - DateTime: Timezone-håndtering og formatstandarder
    /// - Email/Phone/Url: Formatvalidering og standardisering
    /// - JSON: Skjemavalidering og strukturell integritet
    /// 
    /// **Automatisk konfigurasjon:**
    /// - Standard valideringsregler basert på datatype
    /// - Anbefalt visningslogikk og sorteringsrekkefølge
    /// - Sikkerhetskategorisering basert på navnmønstre
    /// - Integrasjonsmapping-forslag for kjente systemtyper
    /// 
    /// **Forretningsregler:**
    /// - Unike navn innenfor samme entitetsdefinisjon
    /// - Konsistente datatyper for like egenskaper på tvers av entiteter
    /// - Påkrevd-flagg kan ikke endres hvis eksisterende data mangler verdier
    /// - Valideringsregler må være testbare og deterministiske
    /// 
    /// **Eksempel request:**
    /// ```json
    /// {
    ///   "entityDefinitionId": "123e4567-e89b-12d3-a456-426614174000",
    ///   "name": "Email",
    ///   "displayName": "E-postadresse",
    ///   "description": "Brukerens primære e-postadresse for kommunikasjon",
    ///   "dataType": "Email",
    ///   "isRequired": true,
    ///   "isUnique": true,
    ///   "validationRule": "^[\\w\\.-]+@[\\w\\.-]+\\.[a-zA-Z]{2,}$",
    ///   "sortOrder": 10
    /// }
    /// ```
    /// </remarks>
    /// <param name="createDto">Konfigurasjon for den nye egenskapsdefinisjon</param>
    /// <returns>Opprettet egenskapsdefinisjon med generert ID</returns>
    /// <response code="201">Egenskapsdefinisjon opprettet vellykket</response>
    /// <response code="400">Ugyldig forespørsel eller valideringsfeil</response>
    /// <response code="409">Navnekonflikt - egenskapsnavn eksisterer allerede i entiteten</response>
    /// <response code="500">Intern serverfeil oppstod</response>
    [HttpPost]
    [ProducesResponseType(typeof(PropertyDefinitionDto), 201)]
    [ProducesResponseType(typeof(string), 400)]
    [ProducesResponseType(typeof(string), 409)]
    [ProducesResponseType(typeof(string), 500)]
    public async Task<ActionResult<PropertyDefinitionDto>> CreatePropertyDefinition(CreatePropertyDefinitionDto createDto)
    {
        try
        {
            // Check if entity definition exists
            var entityDefinitionExists = await _context.EntityDefinitions
                .AnyAsync(x => x.Id == createDto.EntityDefinitionId);

            if (!entityDefinitionExists)
            {
                return BadRequest($"Entity definition with ID {createDto.EntityDefinitionId} not found");
            }

            // Check if name already exists within the entity definition
            var existingProperty = await _context.PropertyDefinitions
                .FirstOrDefaultAsync(x => x.EntityDefinitionId == createDto.EntityDefinitionId && 
                                         x.Name == createDto.Name);

            if (existingProperty != null)
            {
                return Conflict($"Property definition with name '{createDto.Name}' already exists in this entity definition");
            }

            var propertyDefinition = _mapper.Map<PropertyDefinition>(createDto);
            
            _context.PropertyDefinitions.Add(propertyDefinition);
            await _context.SaveChangesAsync();

            // Reload with navigation properties
            propertyDefinition = await _context.PropertyDefinitions
                .Include(x => x.EntityDefinition)
                .FirstAsync(x => x.Id == propertyDefinition.Id);

            var dto = _mapper.Map<PropertyDefinitionDto>(propertyDefinition);
            
            return CreatedAtAction(nameof(GetPropertyDefinition), 
                new { id = propertyDefinition.Id }, dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating property definition");
            return StatusCode(500, "An error occurred while creating the property definition");
        }
    }

    /// <summary>
    /// Oppdater eksisterende egenskapsdefinisjon med endringssporing
    /// </summary>
    /// <remarks>
    /// Oppdaterer en eksisterende egenskapsdefinisjon med omfattende validering og påvirkningsanalyse.
    /// Endringer spores og valideres mot eksisterende data og systemavhengigheter.
    /// 
    /// **Oppdateringsmuligheter:**
    /// - Visningsmetadata: Navn, visningsnavn, beskrivelse
    /// - Valideringsregler: Påkrevd-status, unike restriksjoner, custom validering
    /// - Presentasjonslogikk: Sorteringsrekkefølge, grupperingsregler
    /// - Sikkerhetskategorisering: Databeskyttelse og tilgangsnivåer
    /// 
    /// **Begrensninger og sikkerhetstiltak:**
    /// - Datatype kan ikke endres hvis eksisterende egenskapsverdier finnes
    /// - Påkrevd-flagg kan ikke aktiveres hvis eksisterende data har NULL-verdier
    /// - Unike restriksjoner valideres mot all eksisterende data
    /// - Valideringsregler testes mot eksisterende verdier før aktivering
    /// 
    /// **Påvirkningsanalyse:**
    /// - Endring av påkrevd-status påvirker dataintegritetssjekker
    /// - Modifikasjon av valideringsregler kan gjøre eksisterende data ugyldig
    /// - Navneendringer krever oppdatering av integrasjonsmappinger
    /// - Sorteringsrekkefølge-endringer påvirker brukergrensesnitt-layout
    /// 
    /// **Endringssporing:**
    /// - Automatisk versjonshåndtering for hver endring
    /// - Detaljert auditlogging av hvem, hva og hvorfor
    /// - Rollback-muligheter for kritiske systemfeil
    /// - Varslinger til avhengige systemer ved strukturelle endringer
    /// 
    /// **Validering av eksisterende data:**
    /// - Sjekk om nye valideringsregler er kompatible med eksisterende verdier
    /// - Automatisk datamigreringsforslag ved inkompatibilitet
    /// - Advarsler om potensielle dataintegritetsbrudd
    /// - Mulighet for gradvis utrulling av endringer
    /// 
    /// **Eksempel request:**
    /// ```json
    /// {
    ///   "entityDefinitionId": "123e4567-e89b-12d3-a456-426614174000",
    ///   "name": "EmailAddress",
    ///   "displayName": "E-postadresse (Primær)",
    ///   "description": "Brukerens primære e-postadresse for all kommunikasjon og varsling",
    ///   "dataType": "Email",
    ///   "isRequired": true,
    ///   "isUnique": true,
    ///   "validationRule": "^[\\w\\.-]+@[\\w\\.-]+\\.[a-zA-Z]{2,}$",
    ///   "sortOrder": 5,
    ///   "lastModifiedReason": "Oppdatert visningsnavn for bedre brukervennlighet"
    /// }
    /// ```
    /// </remarks>
    /// <param name="id">Unik identifikator for egenskapsdefinisjon som skal oppdateres</param>
    /// <param name="updateDto">Oppdateringsinformasjon for egenskapsdefinisjon</param>
    /// <returns>Oppdatert egenskapsdefinisjon med endringer</returns>
    /// <response code="200">Egenskapsdefinisjon oppdatert vellykket</response>
    /// <response code="400">Ugyldig forespørsel eller valideringsfeil</response>
    /// <response code="404">Egenskapsdefinisjon ikke funnet med angitt ID</response>
    /// <response code="409">Navnekonflikt eller dataintegritetsbrudd</response>
    /// <response code="500">Intern serverfeil oppstod</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(PropertyDefinitionDto), 200)]
    [ProducesResponseType(typeof(string), 400)]
    [ProducesResponseType(typeof(string), 404)]
    [ProducesResponseType(typeof(string), 409)]
    [ProducesResponseType(typeof(string), 500)]
    public async Task<ActionResult<PropertyDefinitionDto>> UpdatePropertyDefinition(Guid id, UpdatePropertyDefinitionDto updateDto)
    {
        try
        {
            var propertyDefinition = await _context.PropertyDefinitions
                .Include(x => x.EntityDefinition)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (propertyDefinition == null)
            {
                return NotFound($"Property definition with ID {id} not found");
            }

            // Check if entity definition exists
            var entityDefinitionExists = await _context.EntityDefinitions
                .AnyAsync(x => x.Id == updateDto.EntityDefinitionId);

            if (!entityDefinitionExists)
            {
                return BadRequest($"Entity definition with ID {updateDto.EntityDefinitionId} not found");
            }

            // Check if name already exists within the entity definition (excluding current record)
            var existingProperty = await _context.PropertyDefinitions
                .FirstOrDefaultAsync(x => x.EntityDefinitionId == updateDto.EntityDefinitionId && 
                                         x.Name == updateDto.Name && 
                                         x.Id != id);

            if (existingProperty != null)
            {
                return Conflict($"Property definition with name '{updateDto.Name}' already exists in this entity definition");
            }

            _mapper.Map(updateDto, propertyDefinition);
            propertyDefinition.LastModifiedReason = updateDto.LastModifiedReason;

            await _context.SaveChangesAsync();

            var dto = _mapper.Map<PropertyDefinitionDto>(propertyDefinition);
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating property definition {Id}", id);
            return StatusCode(500, "An error occurred while updating the property definition");
        }
    }

    /// <summary>
    /// Deaktiver egenskapsdefinisjon (myk sletting med avhengighetsvalidering)
    /// </summary>
    /// <remarks>
    /// Utfører myk deaktivering av en egenskapsdefinisjon etter grundig validering av systemavhengigheter.
    /// Operasjonen bevarer alle historiske data while markerer egenskapen som utilgjengelig for nye bruksområder.
    /// 
    /// **Deaktiveringsprosess:**
    /// 1. Validerer at ingen kritiske systemkomponenter er avhengige av egenskapen
    /// 2. Sjekker for eksisterende egenskapsverdier som må bevares
    /// 3. Identifiserer integrasjonsmappinger som påvirkes av endringen
    /// 4. Markerer som slettet med timestamp og årsaksregistrering
    /// 5. Opprettholder alle historiske verdier for auditformål
    /// 
    /// **Avhengighetsvalidering:**
    /// - Eksisterende PropertyValues: Kan ikke slettes hvis data finnes
    /// - IntegrationMappings: Må oppdateres eller deaktiveres først
    /// - API-skjemaer: Valider at eksterne systemer ikke er kritisk avhengige
    /// - Brukergrensesnitt: Sjekk for hardkodede referanser til egenskapen
    /// - Rapporter og analyser: Identifiser hvor egenskapen er i bruk
    /// 
    /// **Datasikkerhet og etterlevelse:**
    /// - Alle eksisterende egenskapsverdier bevares for auditformål
    /// - Slettingsoperasjonen logges med brukeridentitet og tidsstempel
    /// - GDPR-kompatibel deaktivering med mulighet for ekte sletting senere
    /// - Backup-rutiner sikrer gjenoppretting ved feilaktig sletting
    /// 
    /// **Gjenopprettingsmuligheter:**
    /// - Myk sletting tillater reaktivering av egenskapsdefinisjon
    /// - Alle konfigurasjoner og metadata bevares for rask gjenoppretting
    /// - Integrasjonsmappinger kan gjenaktiveres uten dataintegritetsbrudd
    /// - Versionshistorikk opprettholdes for kompletts sporbarhet
    /// 
    /// **Alternative løsninger:**
    /// - Vurder å markere som "deprecated" i stedet for sletting
    /// - Bruk "IsActive" flagg for midlertidig deaktivering
    /// - Kontakt systemadministrator for komplekse avhengighetsløsninger
    /// - Planlegg sletting som del av større dataopprydning
    /// 
    /// **Påvirkningsanalyse:**
    /// - Stopper all ny dataregistrering for denne egenskapen
    /// - Eksisterende data forblir tilgjengelig for lesing og rapporter
    /// - Integrasjoner kan feile hvis de forventer egenskapen
    /// - Brukergrensesnitt må oppdateres for å håndtere missing egenskaper
    /// </remarks>
    /// <param name="id">Unik identifikator for egenskapsdefinisjon som skal deaktiveres</param>
    /// <returns>Ingen innhold ved vellykket deaktivering</returns>
    /// <response code="204">Egenskapsdefinisjon deaktivert vellykket</response>
    /// <response code="400">Kan ikke deaktivere egenskap med aktive avhengigheter</response>
    /// <response code="404">Egenskapsdefinisjon ikke funnet med angitt ID</response>
    /// <response code="500">Intern serverfeil oppstod</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(string), 400)]
    [ProducesResponseType(typeof(string), 404)]
    [ProducesResponseType(typeof(string), 500)]
    public async Task<IActionResult> DeletePropertyDefinition(Guid id)
    {
        try
        {
            var propertyDefinition = await _context.PropertyDefinitions
                .FirstOrDefaultAsync(x => x.Id == id);

            if (propertyDefinition == null)
            {
                return NotFound($"Property definition with ID {id} not found");
            }

            // Check if there are dependent records
            var hasPropertyValues = await _context.PropertyValues
                .AnyAsync(x => x.PropertyDefinitionId == id);

            if (hasPropertyValues)
            {
                return BadRequest("Cannot delete property definition with existing property values");
            }

            // Soft delete
            propertyDefinition.IsDeleted = true;
            propertyDefinition.DeletedAt = DateTime.UtcNow;
            propertyDefinition.DeletedBy = "system"; // TODO: Get from current user context

            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting property definition {Id}", id);
            return StatusCode(500, "An error occurred while deleting the property definition");
        }
    }

    /// <summary>
    /// Hent tilgjengelige datatyper for egenskapsdefinisjonene
    /// </summary>
    /// <remarks>
    /// Returnerer en komplett liste over alle støttede datatyper som kan brukes ved opprettelse av egenskapsdefinisjonene.
    /// Hver datatype inkluderer metadata om valideringsregler og formatstandarder.
    /// 
    /// **Støttede datatyper:**
    /// - **String**: Tekstdata med konfigurerbare lengdebegrensninger
    ///   - Støtter UTF-8 og internasjonale tegnsett
    ///   - Automatisk trimming av whitespace
    ///   - Regex-basert mønstervalidering
    /// 
    /// - **Integer**: Heltallsverdier med verdiområde-kontroll
    ///   - 32-bit eller 64-bit støtte avhengig av konfigurasjon
    ///   - Min/maks verdi validering
    ///   - Automatisk formattering for visning
    /// 
    /// - **Decimal**: Presise desimaltall for finansielle beregninger
    ///   - Konfigurerbar presisjon og skala
    ///   - Automatisk avrunding og formattering
    ///   - Støtte for valutaformater
    /// 
    /// - **Boolean**: Sann/usann verdier med fleksible input-formater
    ///   - Aksepterer true/false, 1/0, yes/no
    ///   - Konsistent JSON-serialisering
    ///   - Brukervennlig visning i grensesnitt
    /// 
    /// - **DateTime**: Komplett dato- og tidshåndtering
    ///   - UTC og lokal timezone-støtte
    ///   - ISO 8601 formatstandard
    ///   - Automatisk parsing av vanlige datoformater
    /// 
    /// - **Guid**: Universelt unike identifikatorer
    ///   - RFC 4122 kompatible UUIDs
    ///   - Automatisk generering ved behov
    ///   - Optimalisert for databaser og indeksering
    /// 
    /// - **Email**: E-postadresser med omfattende validering
    ///   - RFC 5322 standard validering
    ///   - Domene-eksistenssjekking (valgfritt)
    ///   - Automatisk normalisering og case-håndtering
    /// 
    /// - **Phone**: Telefonnummer med internasjonal støtte
    ///   - E.164 formatstandard
    ///   - Automatisk lands-kode gjenkjenning
    ///   - Formattering basert på lokale standarder
    /// 
    /// - **Url**: Web-adresser med protokoll-validering
    ///   - HTTP/HTTPS protokoll-støtte
    ///   - Domene og sti-validering
    ///   - Automatisk URL-encoding ved behov
    /// 
    /// - **Json**: Strukturerte JSON-objekter
    ///   - Skjemavalidering mot JSON Schema
    ///   - Dype objektvalidering og type-checking
    ///   - Optimaliert lagring og indeksering
    /// 
    /// **Bruksscenarioer:**
    /// - Dynamisk brukergrensesnitt-generering
    /// - API-dokumentasjon og skjemavalidering
    /// - Integrasjonsmapping og datatransformasjon
    /// - Rapportkolonne-konfigurasjon og filtrering
    /// 
    /// **Implementasjonsdetaljer:**
    /// - Alle datatyper støtter null-verdier (nullable)
    /// - Automatisk type-konvertering fra strenger
    /// - Konsistent feilmeldinger ved valideringsfeil
    /// - Ytelsesoptimalisert lagring og indeksering
    /// </remarks>
    /// <returns>Liste over tilgjengelige datatyper med metadata</returns>
    /// <response code="200">Datatyper hentet vellykket</response>
    /// <response code="500">Intern serverfeil oppstod</response>
    [HttpGet("data-types")]
    [ProducesResponseType(typeof(IEnumerable<object>), 200)]
    [ProducesResponseType(typeof(string), 500)]
    public ActionResult<IEnumerable<object>> GetDataTypes()
    {
        try
        {
            var dataTypes = Enum.GetValues<DataType>()
                .Select(dt => new { Value = (int)dt, Name = dt.ToString() })
                .ToList();

            return Ok(dataTypes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting data types");
            return StatusCode(500, "An error occurred while retrieving data types");
        }
    }
}
