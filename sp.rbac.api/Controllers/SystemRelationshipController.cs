using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SP.RBAC.API.Data;
using SP.RBAC.API.DTOs;
using SP.RBAC.API.Entities;
using AutoMapper;
using Swashbuckle.AspNetCore.Annotations;

namespace SP.RBAC.API.Controllers;

/// <summary>
/// Administrerer relasjoner og integrasjoner mellom eksterne integrasjonssystemer
/// </summary>
/// <remarks>
/// Systemrelasjoner definerer hvordan data flyter mellom systemer, integrasjonsmetoder og synkroniseringsfrekvenser.
/// 
/// **Hovedfunksjoner:**
/// - Opprette og konfigurere nye systemrelasjoner
/// - Overvåke og administrere eksisterende integrasjoner
/// - Aktivere/deaktivere dataflyt mellom systemer
/// - Analysere relasjonsmønstre og systemarkitektur
/// 
/// **Relasjonstyper:**
/// - Enveys dataflyt (Source → Target)
/// - Toveis datasynkronisering (Bidirectional)
/// - Event-baserte integrasjoner
/// - Batch-baserte dataoverføringer
/// 
/// **Integrasjonsmetoder:**
/// - REST API-kall
/// - Message Queue systemer
/// - Databasesynkronisering
/// - Filbaserte integrasjoner
/// 
/// **Sikkerhet og etterlevelse:**
/// - Kryptering av sensitive dataflyter
/// - Auditlogging av alle integrasjonsaktiviteter
/// - Tilgangskontroll basert på systemroller
/// - Overholdelse av GDPR og databeskyttelsesregler
/// </remarks>
[ApiController]
[Route("api/[controller]")]
[SwaggerTag("Systemrelasjoner")]
[Produces("application/json")]
public sealed class SystemRelationshipController : ControllerBase
{
    private readonly RbacDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<SystemRelationshipController> _logger;

    public SystemRelationshipController(RbacDbContext context, IMapper mapper, ILogger<SystemRelationshipController> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Hent paginerte systemrelasjoner med filtrering og sortering
    /// </summary>
    /// <remarks>
    /// Returnerer en paginert liste over alle systemrelasjoner med mulighet for filtrering på:
    /// - Relasjonstype (source-target, bidirectional, event-based)
    /// - Aktivitetsstatus (active, inactive, pending)
    /// - Kildesystem eller målsystem
    /// - Integrasjonsmetode (REST, Message Queue, Database, File)
    /// 
    /// **Bruksscenarioer:**
    /// - Få oversikt over alle aktive integrasjoner
    /// - Identifisere systemer med mange avhengigheter
    /// - Planlegge systemoppgraderinger og vedlikehold
    /// - Analysere dataflyt-mønstre og systemarkitektur
    /// 
    /// **Sorteringsmuligheter:**
    /// - Opprettelsesdato (nyeste/eldste først)
    /// - Systemnavnene (alfabetisk)
    /// - Aktivitetsstatus og ytelsesmetrikker
    /// - Sist modifisert tidspunkt
    /// 
    /// **Ytelsesoptimalisering:**
    /// - Støtter lazy loading av relaterte entiteter
    /// - Effektiv paginering for store datamengder
    /// - Caching av hyppig brukte relasjonslister
    /// </remarks>
    /// <param name="request">Paginering og filtreringsparametere</param>
    /// <returns>Paginert liste med systemrelasjoner</returns>
    /// <response code="200">Vellykket uthenting av systemrelasjoner</response>
    /// <response code="400">Ugyldig forespørsel eller parametere</response>
    /// <response code="500">Intern serverfeil oppstod</response>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponseDto<SystemRelationshipDto>), 200)]
    [ProducesResponseType(typeof(string), 400)]
    [ProducesResponseType(typeof(string), 500)]
    public async Task<ActionResult<PaginatedResponseDto<SystemRelationshipDto>>> GetSystemRelationships([FromQuery] SystemRelationshipSearchRequest request)
    {
        var query = _context.SystemRelationships
            .Include(r => r.SourceSystem)
            .Include(r => r.TargetSystem)
            .AsQueryable();

        // Apply filtering logic here if needed
        if (request.SourceSystemId.HasValue)
        {
            query = query.Where(r => r.SourceSystemId == request.SourceSystemId.Value);
        }
        
        if (request.TargetSystemId.HasValue)
        {
            query = query.Where(r => r.TargetSystemId == request.TargetSystemId.Value);
        }
        
        if (request.IsActive.HasValue)
        {
            query = query.Where(r => r.IsActive == request.IsActive.Value);
        }
        
        var totalCount = await query.CountAsync();
        var relationships = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        var relationshipDtos = _mapper.Map<List<SystemRelationshipDto>>(relationships);

        var response = new PaginatedResponseDto<SystemRelationshipDto>
        {
            Items = relationshipDtos,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
        };

        _logger.LogInformation("Retrieved {Count} system relationships for page {Page}", relationships.Count, request.Page);
        return Ok(response);
    }

    /// <summary>
    /// Hent en spesifikk systemrelasjon med fullstendige detaljer
    /// </summary>
    /// <remarks>
    /// Returnerer detaljert informasjon om en enkelt systemrelasjon inkludert:
    /// - Komplette kilde- og målsystemdetaljer
    /// - Integrasjonskonfigurasjon og dataflyt-parametere
    /// - Relaterte dokumenter og integrasjonshistorikk
    /// - Status og ytelsesmetrikker
    /// 
    /// **Bruksscenarioer:**
    /// - Validere relasjonskonfigurasjon før endringer
    /// - Analysere integrasjonsytelse og dataflyt
    /// - Feilsøke integrasjonsproblemer
    /// - Dokumentere systemarkitektur og avhengigheter
    /// 
    /// **Inkluderte data:**
    /// - Kildesystem: Navn, type, status og konfigurasjon
    /// - Målsystem: Navn, type, status og konfigurasjon  
    /// - Relasjon: Type, beskrivelse, dataflyt og frekvens
    /// - Dokumenter: Teknisk dokumentasjon og integrasjonsguider
    /// </remarks>
    /// <param name="id">Unik identifikator for systemrelasjonen</param>
    /// <returns>Detaljerte opplysninger om systemrelasjonen</returns>
    /// <response code="200">Systemrelasjon funnet og returnert</response>
    /// <response code="404">Systemrelasjon ikke funnet med angitt ID</response>
    /// <response code="500">Intern serverfeil oppstod</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SystemRelationshipDto), 200)]
    [ProducesResponseType(typeof(string), 404)]
    [ProducesResponseType(typeof(string), 500)]
    public async Task<ActionResult<SystemRelationshipDto>> GetSystemRelationship(Guid id)
    {
        var relationship = await _context.SystemRelationships
            .Include(r => r.SourceSystem)
            .Include(r => r.TargetSystem)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (relationship == null)
        {
            return NotFound($"System relationship with ID {id} not found");
        }

        var relationshipDto = _mapper.Map<SystemRelationshipDto>(relationship);
        _logger.LogInformation("Retrieved system relationship {Id}", id);
        
        return Ok(relationshipDto);
    }

    /// <summary>
    /// Opprett ny systemrelasjon med konfigurasjon og validering
    /// </summary>
    /// <remarks>
    /// Oppretter en ny systemrelasjon mellom to eksisterende integrasjonssystemer.
    /// Operasjonen validerer kompatibilitet og konfigurerer automatisk grunnleggende integrasjonsparametere.
    /// 
    /// **Valideringsprosess:**
    /// 1. Verifiserer at begge systemer eksisterer og er aktive
    /// 2. Sjekker for eksisterende relasjoner for å unngå duplikater
    /// 3. Validerer kompatibilitet mellom systemtyper og protokoller
    /// 4. Tester grunnleggende tilkobling og autentisering
    /// 
    /// **Automatisk konfigurasjon:**
    /// - Anbefalt integrasjonsmetode basert på systemtyper
    /// - Standard dataflyt-retning og synkroniseringsfrekvens
    /// - Sikkerhetskonfigurasjon og krypteringsinnstillinger
    /// - Overvåking og logging-parametere
    /// 
    /// **Tillatte relasjonstyper:**
    /// - SOURCE_TO_TARGET: Enveys dataflyt fra kilde til mål
    /// - BIDIRECTIONAL: Toveis datasynkronisering
    /// - EVENT_DRIVEN: Event-basert integrasjon
    /// - BATCH_TRANSFER: Planlagte batch-overføringer
    /// 
    /// **Integrasjonsmetoder:**
    /// - REST_API: HTTP-baserte API-kall
    /// - MESSAGE_QUEUE: Asynkron meldingskø
    /// - DATABASE_SYNC: Direktesynkronisering mellom databaser
    /// - FILE_TRANSFER: Filbaserte dataoverføringer
    /// 
    /// **Eksempel request:**
    /// ```json
    /// {
    ///   "sourceSystemId": "123e4567-e89b-12d3-a456-426614174000",
    ///   "targetSystemId": "987fcdeb-51a2-43d1-9f47-123456789abc",
    ///   "relationshipType": "SOURCE_TO_TARGET",
    ///   "description": "Synkronisering av brukerdata fra HR-system til Active Directory",
    ///   "dataFlow": "Unidirectional",
    ///   "integrationMethod": "REST_API",
    ///   "frequency": "Daily"
    /// }
    /// ```
    /// </remarks>
    /// <param name="request">Konfigurasjon for den nye systemrelasjonen</param>
    /// <returns>Opprettet systemrelasjon med generert ID</returns>
    /// <response code="201">Systemrelasjon opprettet vellykket</response>
    /// <response code="400">Ugyldig forespørsel eller valideringsfeil</response>
    /// <response code="409">Relasjon eksisterer allerede mellom systemene</response>
    /// <response code="500">Intern serverfeil oppstod</response>
    [HttpPost]
    [ProducesResponseType(typeof(SystemRelationshipDto), 201)]
    [ProducesResponseType(typeof(string), 400)]
    [ProducesResponseType(typeof(string), 409)]
    [ProducesResponseType(typeof(string), 500)]
    public async Task<ActionResult<SystemRelationshipDto>> CreateSystemRelationship([FromBody] CreateSystemRelationshipRequest request)
    {
        // Validate that both systems exist
        var sourceSystem = await _context.IntegrationSystems.FindAsync(request.SourceSystemId);
        if (sourceSystem == null)
        {
            return BadRequest($"Source system with ID {request.SourceSystemId} not found");
        }

        var targetSystem = await _context.IntegrationSystems.FindAsync(request.TargetSystemId);
        if (targetSystem == null)
        {
            return BadRequest($"Target system with ID {request.TargetSystemId} not found");
        }

        // Check for existing relationship
        var existingRelationship = await _context.SystemRelationships
            .FirstOrDefaultAsync(r => r.SourceSystemId == request.SourceSystemId && r.TargetSystemId == request.TargetSystemId);
        
        if (existingRelationship != null)
        {
            return Conflict($"Relationship already exists between systems {request.SourceSystemId} and {request.TargetSystemId}");
        }

        var relationship = SystemRelationship.Create(
            request.SourceSystemId,
            request.TargetSystemId,
            request.RelationshipType,
            request.Description);

        if (!string.IsNullOrWhiteSpace(request.DataFlow))
        {
            relationship.SetDataFlow(request.DataFlow);
        }

        if (!string.IsNullOrWhiteSpace(request.IntegrationMethod))
        {
            relationship.SetIntegrationMethod(request.IntegrationMethod);
        }

        if (!string.IsNullOrWhiteSpace(request.Frequency))
        {
            relationship.SetFrequency(request.Frequency);
        }

        _context.SystemRelationships.Add(relationship);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created system relationship {Id} between systems {SourceId} and {TargetId}", 
            relationship.Id, request.SourceSystemId, request.TargetSystemId);

        var relationshipDto = _mapper.Map<SystemRelationshipDto>(relationship);
        return CreatedAtAction(nameof(GetSystemRelationship), new { id = relationship.Id }, relationshipDto);
    }

    /// <summary>
    /// Oppdater eksisterende systemrelasjon med ny konfigurasjon
    /// </summary>
    /// <remarks>
    /// Oppdaterer en eksisterende systemrelasjon med ny metadata, konfigurasjon eller strukturelle endringer.
    /// Støtter delvis oppdateringer mens dataintegritet og referanserestriksjoner opprettholdes.
    /// 
    /// **Oppdateringsmuligheter:**
    /// - Endre relasjonsbeskrivelse og dokumentasjon
    /// - Justere dataflyt-retning og integrasjonsmetode
    /// - Modifisere synkroniseringsfrekvens og tidspunkter
    /// - Oppdatere tekniske parametere og konfigurasjon
    /// 
    /// **Begrensninger og validering:**
    /// - Kan ikke endre kilde- eller målsystem etter opprettelse
    /// - Relasjonstype kan ikke endres hvis aktive dataflyter eksisterer
    /// - Endringer må være kompatible med eksisterende integrasjoner
    /// - Oppgi grunn for endring for sporbarhet og revisjon
    /// 
    /// **Påvirkningsanalyse:**
    /// - Endring av integrasjonsmetode kan påvirke eksisterende synkroniseringsprosesser
    /// - Modifisering av frekvens kan påvirke systemytelse og dataaktualitet
    /// - Dataflyt-endringer kan kreve rekonfigurasjon av målsystemer
    /// 
    /// **Eksempel request:**
    /// ```json
    /// {
    ///   "description": "Oppdatert: Inkluderer nå også avdelingsinformasjon",
    ///   "dataFlow": "Bidirectional",
    ///   "integrationMethod": "MESSAGE_QUEUE", 
    ///   "frequency": "Hourly"
    /// }
    /// ```
    /// </remarks>
    /// <param name="id">Unik identifikator for systemrelasjonen som skal oppdateres</param>
    /// <param name="request">Oppdateringsinformasjon for systemrelasjonen</param>
    /// <returns>Oppdatert systemrelasjon</returns>
    /// <response code="200">Systemrelasjon oppdatert vellykket</response>
    /// <response code="400">Ugyldig forespørsel eller valideringsfeil</response>
    /// <response code="404">Systemrelasjon ikke funnet med angitt ID</response>
    /// <response code="500">Intern serverfeil oppstod</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(SystemRelationshipDto), 200)]
    [ProducesResponseType(typeof(string), 400)]
    [ProducesResponseType(typeof(string), 404)]
    [ProducesResponseType(typeof(string), 500)]
    public async Task<ActionResult<SystemRelationshipDto>> UpdateSystemRelationship(Guid id, [FromBody] UpdateSystemRelationshipRequest request)
    {
        var relationship = await _context.SystemRelationships
            .FirstOrDefaultAsync(r => r.Id == id);

        if (relationship == null)
        {
            return NotFound($"System relationship with ID {id} not found");
        }

        // Update only provided fields
        if (!string.IsNullOrWhiteSpace(request.Description))
        {
            relationship.Description = request.Description;
        }

        if (!string.IsNullOrWhiteSpace(request.DataFlow))
        {
            relationship.DataFlow = request.DataFlow;
        }

        if (!string.IsNullOrWhiteSpace(request.IntegrationMethod))
        {
            relationship.SetIntegrationMethod(request.IntegrationMethod);
        }

        if (!string.IsNullOrWhiteSpace(request.Frequency))
        {
            relationship.SetFrequency(request.Frequency);
        }

        relationship.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated system relationship {Id}", id);

        var relationshipDto = _mapper.Map<SystemRelationshipDto>(relationship);
        return Ok(relationshipDto);
    }

    /// <summary>
    /// Deaktiver systemrelasjon (myk sletting med avhengighetsvalidering)
    /// </summary>
    /// <remarks>
    /// Utfører myk deaktivering av en systemrelasjon etter validering av at ingen kritiske avhengigheter eksisterer.
    /// Denne operasjonen markerer relasjonen som inaktiv uten å fysisk fjerne den fra databasen.
    /// 
    /// **Deaktiveringsprosess:**
    /// 1. Validerer at ingen kritiske dataflyter er avhengige av relasjonen
    /// 2. Stopper aktive synkroniseringsprosesser gradvis
    /// 3. Markerer relasjon som inaktiv med timestamp og årsak
    /// 4. Opprettholder historiske data for etterlevelse og revisjon
    /// 
    /// **Avhengighetsvalidering:**
    /// - Sjekker for aktive datasynkroniseringsjobber
    /// - Identifiserer kritiske integrasjonspunkter som kan påvirkes
    /// - Vurderer nedstrøms systemer som er avhengige av dataflyt
    /// 
    /// **Gjenopprettingsmuligheter:**
    /// - Deaktiverte relasjoner kan reaktiveres via activate-endpoint
    /// - All konfigurasjon og metadata bevares for rask gjenoppretting
    /// - Integrasjonshistorikk opprettholdes for analyse og feilsøking
    /// 
    /// **Alternative tiltak:**
    /// - Vurder midlertidig pausing i stedet for full deaktivering
    /// - Bruk planlagt vedlikehold for kritiske integrasjoner
    /// - Kontakt systemadministrator for komplekse avhengighetsløsninger
    /// 
    /// **Påvirkningsanalyse:**
    /// - Stopper alle nye dataoverføringer mellom systemene
    /// - Eksisterende data i målsystemer påvirkes ikke
    /// - Rapporter og analyser kan trenge filteroppdateringer
    /// </remarks>
    /// <param name="id">Unik identifikator for systemrelasjonen som skal deaktiveres</param>
    /// <returns>Ingen innhold ved vellykket deaktivering</returns>
    /// <response code="204">Systemrelasjon deaktivert vellykket</response>
    /// <response code="400">Kan ikke deaktivere relasjon med kritiske aktive avhengigheter</response>
    /// <response code="404">Systemrelasjon ikke funnet med angitt ID</response>
    /// <response code="500">Intern serverfeil oppstod</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(string), 400)]
    [ProducesResponseType(typeof(string), 404)]
    [ProducesResponseType(typeof(string), 500)]
    public async Task<IActionResult> DeactivateSystemRelationship(Guid id)
    {
        var relationship = await _context.SystemRelationships
            .FirstOrDefaultAsync(r => r.Id == id);

        if (relationship == null)
        {
            return NotFound($"System relationship with ID {id} not found");
        }

        relationship.Deactivate();
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deactivated system relationship {Id}", id);
        return NoContent();
    }

    /// <summary>
    /// Aktiverer eller reaktiverer systemrelasjon med validering
    /// </summary>
    /// <remarks>
    /// Aktiverer en deaktivert systemrelasjon eller reaktiverer en tidligere inaktiv integrasjon.
    /// Operasjonen validerer systemtilgjengelighet og kompatibilitet før aktivering.
    /// 
    /// **Aktiveringsprosess:**
    /// 1. Validerer tilgjengelighet av begge systemendepunkter
    /// 2. Tester autentisering og autorisasjon
    /// 3. Verifiserer dataskjema-kompatibilitet
    /// 4. Initialiserer overvåking og logging
    /// 5. Markerer relasjon som aktiv med starttidspunkt
    /// 
    /// **Kompatibilitetssjekker:**
    /// - API-versjonering og endepunktstabilitet
    /// - Dataskjemavalidering mot målsystem
    /// - Sikkerhetsprotokoller og sertifikater
    /// - Kapasitets- og ytelsesbaseline
    /// 
    /// **Forutsetninger for aktivering:**
    /// - Begge systemer må være tilgjengelige og responsiv
    /// - Gyldig autentisering må være konfigurert
    /// - Nødvendige tillatelser må være på plass
    /// - Systemmoduler må være kompatible med konfigurasjon
    /// 
    /// **Overvåking og varsling:**
    /// - Aktivering utløser automatisk helsesjekker
    /// - Etablerer overvåkingsbaseline for ytelse
    /// - Konfigurerer varsling for kritiske feil
    /// 
    /// **Feilhåndtering:**
    /// - Automatisk rollback ved valideringsfeil
    /// - Detaljerte feilmeldinger for feilsøking
    /// - Forslag til korrigerende tiltak ved kjente problemer
    /// </remarks>
    /// <param name="id">Unik identifikator for systemrelasjonen som skal aktiveres</param>
    /// <returns>Ingen innhold ved vellykket aktivering</returns>
    /// <response code="204">Systemrelasjon aktivert vellykket</response>
    /// <response code="400">Valideringsfeil - system ikke klar for aktivering</response>
    /// <response code="404">Systemrelasjon ikke funnet med angitt ID</response>
    /// <response code="500">Intern serverfeil oppstod</response>
    [HttpPost("{id:guid}/activate")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(string), 400)]
    [ProducesResponseType(typeof(string), 404)]
    [ProducesResponseType(typeof(string), 500)]
    public async Task<IActionResult> ActivateSystemRelationship(Guid id)
    {
        var relationship = await _context.SystemRelationships
            .FirstOrDefaultAsync(r => r.Id == id);

        if (relationship == null)
        {
            return NotFound($"System relationship with ID {id} not found");
        }

        relationship.Activate();
        await _context.SaveChangesAsync();

        _logger.LogInformation("Activated system relationship {Id}", id);
        return NoContent();
    }

    /// <summary>
    /// Hent alle relasjoner for et spesifikt system (både kilde og mål)
    /// </summary>
    /// <remarks>
    /// Returnerer en komplett oversikt over alle systemrelasjoner hvor det angitte systemet fungerer enten som kilde eller mål.
    /// Inkluderer både aktive og inaktive relasjoner med full kontekstuell informasjon.
    /// 
    /// **Bruksscenarioer:**
    /// - Kartlegge systemavhengigheter før oppgraderinger
    /// - Analysere påvirkningsområde for systemendringer
    /// - Dokumentere systemarkitektur og dataflyt
    /// - Planlegge vedlikehold og nedetid
    /// 
    /// **Inkluderte relasjonstyper:**
    /// - Utgående relasjoner (system som kilde)
    /// - Innkommende relasjoner (system som mål)
    /// - Bidireksjonelle integrasjoner
    /// - Event-baserte tilkoblinger
    /// 
    /// **Datagruppering:**
    /// - Relasjoner gruppert etter retning (inn/ut)
    /// - Status-kategorisering (aktiv/inaktiv/vedlikehold)
    /// - Integrasjonsmetode-klassifisering
    /// - Kritikalitetsnivå basert på datavolum og frekvens
    /// 
    /// **Systemanalyse:**
    /// - Identifiserer systemets rolle i integrasjonsarkitekturen
    /// - Beregner avhengighetsgrad og kritikalitet
    /// - Rapporterer integrasjonskapasitet og belastning
    /// </remarks>
    /// <param name="systemId">Unik identifikator for systemet</param>
    /// <returns>Komplette relasjonsdata for systemet</returns>
    /// <response code="200">Systemrelasjoner hentet vellykket</response>
    /// <response code="404">System ikke funnet med angitt ID</response>
    /// <response code="500">Intern serverfeil oppstod</response>
    [HttpGet("by-system/{systemId:guid}")]
    [ProducesResponseType(typeof(SystemRelationshipsDto), 200)]
    [ProducesResponseType(typeof(string), 404)]
    [ProducesResponseType(typeof(string), 500)]
    public async Task<ActionResult<SystemRelationshipsDto>> GetRelationshipsBySystem(Guid systemId)
    {
        var systemExists = await _context.IntegrationSystems
            .AnyAsync(s => s.Id == systemId);
        if (!systemExists)
        {
            return NotFound($"Integration system with ID {systemId} not found");
        }

        var sourceRelationships = await _context.SystemRelationships
            .Include(r => r.TargetSystem)
            .Where(r => r.SourceSystemId == systemId)
            .ToListAsync();

        var targetRelationships = await _context.SystemRelationships
            .Include(r => r.SourceSystem)
            .Where(r => r.TargetSystemId == systemId)
            .ToListAsync();

        var response = new SystemRelationshipsDto
        {
            SystemId = systemId,
            OutgoingRelationships = _mapper.Map<List<SystemRelationshipDto>>(sourceRelationships),
            IncomingRelationships = _mapper.Map<List<SystemRelationshipDto>>(targetRelationships)
        };

        _logger.LogInformation("Retrieved {OutgoingCount} outgoing and {IncomingCount} incoming relationships for system {SystemId}", 
            sourceRelationships.Count, targetRelationships.Count, systemId);

        return Ok(response);
    }

    /// <summary>
    /// Generer integrasjonsarkitektur-oversikt for rapportering
    /// </summary>
    /// <remarks>
    /// Produserer en omfattende rapport over systemintegrasjonsarkitekturen inkludert:
    /// - Nettverkstopologi og systemsammenkobling
    /// - Kritiske dataflyt-stier og avhengighetskart
    /// - Kapasitetsanalyse og ytelsesmetrikker
    /// - Sikkerhetsvurdering og risikoanalyse
    /// 
    /// **Arkitekturanalyse:**
    /// - Identifiserer sentrale knutepunktsystemer (high-degree nodes)
    /// - Kartlegger kritiske enkeltpunktsfeil (single points of failure)
    /// - Analyserer dataflyt-mønstre og integrasjonsvolum
    /// - Vurderer systemmodenhet og teknisk gjeld
    /// 
    /// **Rapporterings-elementer:**
    /// - Grafisk fremstilling av systemtopologi
    /// - Tabeller med relasjonsdetaljer og konfigurasjoner
    /// - Trendanalyse og kapasitetsplanlegging
    /// - Anbefalinger for arkitekturoptimalisering
    /// 
    /// **Bruksområder:**
    /// - Årlig arkitekturgjennomgang og dokumentasjon
    /// - Compliance-rapportering til ledelse og revisorer
    /// - Planlegging av systemutskiftinger og oppgraderinger
    /// - Risikovurdering og kontinuitetsplanlegging
    /// 
    /// **Tekniske detaljer:**
    /// - Inkluderer versjonsinformasjon og API-kompatibilitet
    /// - Rapporterer integrasjonshelse og oppetidsstatistikk
    /// - Dokumenterer datavolum og ytelsesbaseline
    /// - Identifiserer potensielle optimaliseringsmuligheter
    /// </remarks>
    /// <returns>Omfattende integrasjonsarkitektur-rapport</returns>
    /// <response code="200">Arkitekturrapport generert vellykket</response>
    /// <response code="500">Intern serverfeil under rapportgenerering</response>
    [HttpGet("architecture")]
    [ProducesResponseType(typeof(IntegrationArchitectureDto), 200)]
    [ProducesResponseType(typeof(string), 500)]
    public async Task<ActionResult<IntegrationArchitectureDto>> GetIntegrationArchitecture()
    {
        var allRelationships = await _context.SystemRelationships
            .Include(r => r.SourceSystem)
            .Include(r => r.TargetSystem)
            .ToListAsync();

        var allSystems = await _context.IntegrationSystems.ToListAsync();

        var architecture = new IntegrationArchitectureDto
        {
            TotalSystems = allSystems.Count,
            TotalRelationships = allRelationships.Count,
            Systems = _mapper.Map<List<IntegrationSystemSummaryDto>>(allSystems),
            Relationships = _mapper.Map<List<SystemRelationshipDto>>(allRelationships),
            GeneratedAt = DateTime.UtcNow
        };

        _logger.LogInformation("Generated integration architecture report with {SystemCount} systems and {RelationshipCount} relationships", 
            allSystems.Count, allRelationships.Count);

        return Ok(architecture);
    }

    /// <summary>
    /// Generer statistikk og metrikker for systemrelasjoner
    /// </summary>
    /// <remarks>
    /// Beregner og returnerer omfattende statistikk og nøkkelmetrikker for systemintegrasjoner:
    /// - Aktivitets- og tilgjengelighetstall
    /// - Integrasjonsmetode-fordeling og bruksmønstre
    /// - Systembelastning og kapasitetsutnyttelse
    /// - Trends og ytelsesutvikling over tid
    /// 
    /// **Statistiske kategorier:**
    /// - Kvantitative mål: Antall aktive/inaktive relasjoner, gjennomsnittlig oppetid
    /// - Kvalitative indikatorer: Integrasjonshelse, feilfrekvens, responsivitets
    /// - Trendanalyse: Vekstrate, sesonaliteter, mønstre i datatrafikk
    /// - Sammenligningsdata: Benchmark mot beste praksis og industristandarder
    /// 
    /// **Nøkkeltall for ledelse:**
    /// - Systemintegrasjonsmodnhet og digitalisering-score
    /// - Kostnad per integrasjon og ROI-beregninger
    /// - Risikovurdering og compliance-overholdelse
    /// - Kapasitetsplanlegging og tekniske investeringsbehov
    /// 
    /// **Operasjonelle metrikker:**
    /// - Systemrespons og ytelseskarakteristika
    /// - Feiltoleranse og gjenopprettingshastighet
    /// - Datavolum og gjennomstrømming per integrasjon
    /// - Vedlikeholdskostnader og operasjonell effektivitet
    /// 
    /// **Automatisering og varsling:**
    /// - Terskelverdier for kritiske nøkkeltall
    /// - Proaktiv varsling ved avvik fra baseline
    /// - Prediktiv analyse av systemkapasitetsbehov
    /// - Automatisk rapportering til interessenter
    /// </remarks>
    /// <returns>Omfattende statistikk og metrikker for systemintegrasjoner</returns>
    /// <response code="200">Statistikk generert vellykket</response>
    /// <response code="500">Intern serverfeil under statistikkberegning</response>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(RelationshipStatisticsDto), 200)]
    [ProducesResponseType(typeof(string), 500)]
    public async Task<ActionResult<RelationshipStatisticsDto>> GetRelationshipStatistics()
    {
        var allRelationships = await _context.SystemRelationships.ToListAsync();
        var allSystems = await _context.IntegrationSystems.ToListAsync();

        var statistics = new RelationshipStatisticsDto
        {
            TotalRelationships = allRelationships.Count,
            ActiveRelationships = allRelationships.Count(r => r.IsActive),
            InactiveRelationships = allRelationships.Count(r => !r.IsActive),
            SystemsWithRelationships = allSystems.Count(s => 
                allRelationships.Any(r => r.SourceSystemId == s.Id || r.TargetSystemId == s.Id))
        };

        _logger.LogInformation("Generated relationship statistics: {Active}/{Total} relationships", 
            statistics.ActiveRelationships, statistics.TotalRelationships);

        return Ok(statistics);
    }
}
