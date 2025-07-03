using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SP.RBAC.API.Data;
using SP.RBAC.API.DTOs;
using SP.RBAC.API.Entities;
using AutoMapper;
using Swashbuckle.AspNetCore.Annotations;

namespace SP.RBAC.API.Controllers;

/// <summary>
/// Systemrelasjon API - Administrerer relasjoner og integrasjoner mellom eksterne systemer
/// </summary>
/// <remarks>
/// Dette API-et håndterer opprettelse, administrasjon og sporing av relasjoner mellom ulik    /// <summary>
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
    [HttpPatch("{id:guid}/activate")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(string), 400)]
    [ProducesResponseType(typeof(string), 404)]
    [ProducesResponseType(typeof(string), 500)]tegrasjonssystemer.
/// Systemrelasjoner definerer hvordan data flyter mellom systemer, integrasjonsmetoder og synkroniseringsfrekvenser.
/// 
/// **Hovedfunksjoner:**
/// - Definere datalagsrelasjoner mellom systemer (HR → EMR, CRM → Billing, etc.)
/// - Konfigurere integrasjonsmetoder (REST API, Database sync, File transfer, Message queue)
/// - Administrere dataflyt-retninger (unidirectional, bidirectional)
/// - Spore systemarkitektur og avhengigheter
/// - Overvåke integrasjonsstatus og ytelse
/// 
/// **Relasjonstyper:**
/// - **DataSync**: Kontinuerlig datasynkronisering mellom systemer
/// - **EventDriven**: Event-basert kommunikasjon og datadeling
/// - **BatchTransfer**: Planlagt batch-overføring av data
/// - **APIIntegration**: Real-time API-integrasjon
/// - **DatabaseLink**: Direkte database-kobling mellom systemer
/// 
/// **Vanlige bruksscenarioer:**
/// - HR-system synkroniserer ansattdata til EMR for tilgangskontroll
/// - CRM-system sender kundedata til billing-system for fakturering
/// - EMR-system rapporterer pasientaktivitet til compliance-system
/// - Identity Provider distribuerer brukerrettigheter til alle tilkoblede systemer
/// 
/// **Eksempel systemrelasjon:**
/// ```json
/// {
///   "sourceSystemId": "hr-system-guid",
///   "targetSystemId": "emr-system-guid", 
///   "relationshipType": "DataSync",
///   "description": "Synkroniserer ansattinformasjon for tilgangskontroll",
///   "dataFlow": "SourceToTarget",
///   "integrationMethod": "REST_API",
///   "frequency": "Real-time"
/// }
/// ```
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

    public SystemRelationshipController(
        RbacDbContext context, 
        IMapper mapper, 
        ILogger<SystemRelationshipController> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Hent alle systemrelasjoner med avansert filtrering og paginering
    /// </summary>
    /// <remarks>
    /// Returnerer en paginert liste over systemrelasjoner med omfattende filtreringsalternativer.
    /// Resultatet inkluderer detaljer om kildesystem, målsystem, relasjonstype og integrasjonskonfigurasjon.
    /// 
    /// **Filtreringsalternativer:**
    /// - **sourceSystemId**: Filtrer på spesifikt kildesystem
    /// - **targetSystemId**: Filtrer på spesifikt målsystem  
    /// - **relationshipType**: Filtrer på relasjonstype (DataSync, EventDriven, etc.)
    /// - **isActive**: Filtrer på aktive/inaktive relasjoner
    /// 
    /// **Sortering**: Resultater sorteres først etter kildesystemets navn, deretter målsystemets navn
    /// 
    /// **Eksempel bruk:**
    /// - `GET /api/systemrelationship?sourceSystemId=hr-guid&amp;relationshipType=DataSync`
    /// - `GET /api/systemrelationship?isActive=true&amp;pageSize=20`
    /// 
    /// **Ytelsesnotater:**
    /// - Inkluderer relaterte systemdata for komplett kontekst
    /// - Store datasett pagineres automatisk for optimal ytelse
    /// - Vurder mindre sidestørrelser for systemer med mange relasjoner
    /// </remarks>
    /// <param name="request">Søke- og filtreringsparametere for systemrelasjoner</param>
    /// <returns>Paginert liste over systemrelasjoner med metadata og relaterte systemdetaljer</returns>
    /// <response code="200">Systemrelasjoner hentet vellykket</response>
    /// <response code="400">Ugyldige paginerings- eller filtreringsparametere</response>
    /// <response code="500">Intern serverfeil oppstod</response>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponseDto<SystemRelationshipDto>), 200)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(typeof(string), 500)]
    public async Task<ActionResult<PaginatedResponseDto<SystemRelationshipDto>>> GetSystemRelationships(
        [FromQuery] SystemRelationshipSearchRequest request)
    {
        var query = _context.SystemRelationships
            .Include(r => r.SourceSystem)
            .Include(r => r.TargetSystem)
            .AsQueryable();

        // Apply filters
        if (request.SourceSystemId.HasValue)
        {
            query = query.Where(r => r.SourceSystemId == request.SourceSystemId.Value);
        }

        if (request.TargetSystemId.HasValue)
        {
            query = query.Where(r => r.TargetSystemId == request.TargetSystemId.Value);
        }

        if (request.RelationshipType.HasValue)
        {
            query = query.Where(r => r.RelationshipType == request.RelationshipType.Value);
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(r => r.IsActive == request.IsActive.Value);
        }

        // Get total count
        var totalCount = await query.CountAsync();

        // Apply pagination
        var relationships = await query
            .OrderBy(r => r.SourceSystem!.Name)
            .ThenBy(r => r.TargetSystem!.Name)
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
    /// <returns>Fullstendig systemrelasjon med alle relaterte detaljer</returns>
    /// <response code="200">Systemrelasjon hentet vellykket</response>
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
            .Include(r => r.RelatedDocuments)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (relationship == null)
        {
            _logger.LogWarning("System relationship with ID {Id} not found", id);
            return NotFound($"System relationship with ID {id} not found");
        }

        var relationshipDto = _mapper.Map<SystemRelationshipDto>(relationship);
        return Ok(relationshipDto);
    }

    /// <summary>
    /// Opprett en ny systemrelasjon mellom integrasjonssystemer
    /// </summary>
    /// <remarks>
    /// Oppretter en ny relasjon som definerer hvordan data flyter mellom to integrasjonssystemer.
    /// Systemrelasjonen etablerer integrasjonskonfigurasjon, dataflyt-retning og synkroniseringsparametere.
    /// 
    /// **Forutsetninger:**
    /// - Både kilde- og målsystem må eksistere og være aktive
    /// - Relasjonsnavn må være unikt mellom de spesifiserte systemene
    /// - Relasjonstype må være støttet (DataSync, EventDriven, BatchTransfer, etc.)
    /// - Integrasjonsmetode må være kompatibel med begge systemer
    /// 
    /// **Valideringsregler:**
    /// - Kilde- og målsystem ID må referere til eksisterende integrasjonssystemer
    /// - Kan ikke opprette duplikat aktive relasjoner mellom samme systemer med samme type
    /// - Dataflyt-retning må være gyldig (SourceToTarget, TargetToSource, Bidirectional)
    /// - Frekvens må være støttet (Real-time, Hourly, Daily, Weekly, etc.)
    /// 
    /// **Anbefalte praksis:**
    /// - Bruk beskrivende relasjonstyper for forståelse
    /// - Spesifiser klare dataflyt-retninger for å unngå konflikter
    /// - Konfigurer appropriate frekvenser basert på datakritikalitet
    /// - Dokumenter integrasjonsavhengigheter grundig
    /// 
    /// **Eksempel request:**
    /// ```json
    /// {
    ///   "sourceSystemId": "hr-system-guid",
    ///   "targetSystemId": "emr-system-guid",
    ///   "relationshipType": "DataSync", 
    ///   "description": "Synkroniserer ansattinformasjon for EMR tilgangskontroll",
    ///   "dataFlow": "SourceToTarget",
    ///   "integrationMethod": "REST_API",
    ///   "frequency": "Real-time"
    /// }
    /// ```
    /// </remarks>
    /// <param name="request">Systemrelasjon data for opprettelse</param>
    /// <returns>Opprettet systemrelasjon med generert ID og metadata</returns>
    /// <response code="201">Systemrelasjon opprettet vellykket</response>
    /// <response code="400">Ugyldig forespørseldata eller system ikke funnet</response>
    /// <response code="409">Aktiv relasjon av samme type eksisterer allerede mellom disse systemene</response>
    /// <response code="500">Intern serverfeil oppstod</response>
    [HttpPost]
    [ProducesResponseType(typeof(SystemRelationshipDto), 201)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(typeof(string), 409)]
    [ProducesResponseType(typeof(string), 500)]
    public async Task<ActionResult<SystemRelationshipDto>> CreateSystemRelationship(
        CreateSystemRelationshipRequest request)
    {
        // Validate that source and target systems exist
        var sourceSystemExists = await _context.IntegrationSystems
            .AnyAsync(s => s.Id == request.SourceSystemId);
        if (!sourceSystemExists)
        {
            return BadRequest($"Source system with ID {request.SourceSystemId} not found");
        }

        var targetSystemExists = await _context.IntegrationSystems
            .AnyAsync(s => s.Id == request.TargetSystemId);
        if (!targetSystemExists)
        {
            return BadRequest($"Target system with ID {request.TargetSystemId} not found");
        }

        // Check for duplicate relationship
        var existingRelationship = await _context.SystemRelationships
            .FirstOrDefaultAsync(r => 
                r.SourceSystemId == request.SourceSystemId &&
                r.TargetSystemId == request.TargetSystemId &&
                r.RelationshipType == request.RelationshipType &&
                r.IsActive);

        if (existingRelationship != null)
        {
            return BadRequest($"Active relationship of type '{request.RelationshipType}' already exists between these systems");
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
    /// <param name="request">Oppdaterte systemrelasjon data</param>
    /// <returns>Oppdatert systemrelasjon med ny konfigurasjon</returns>
    /// <response code="200">Systemrelasjon oppdatert vellykket</response>
    /// <response code="400">Ugyldig forespørseldata eller konfigurasjonsfeil</response>
    /// <response code="404">Systemrelasjon ikke funnet med angitt ID</response>
    /// <response code="409">Konflikt med eksisterende relasjoner eller aktive integrasjoner</response>
    /// <response code="500">Intern serverfeil oppstod</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(SystemRelationshipDto), 200)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(typeof(string), 404)]
    [ProducesResponseType(typeof(string), 409)]
    [ProducesResponseType(typeof(string), 500)]
    public async Task<ActionResult<SystemRelationshipDto>> UpdateSystemRelationship(
        Guid id, 
        UpdateSystemRelationshipRequest request)
    {
        var relationship = await _context.SystemRelationships
            .FirstOrDefaultAsync(r => r.Id == id);

        if (relationship == null)
        {
            return NotFound($"System relationship with ID {id} not found");
        }

        // Update fields
        if (!string.IsNullOrWhiteSpace(request.Description))
        {
            relationship.UpdateDescription(request.Description);
        }

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
    /// Reactivates a deactivated system relationship
    /// </summary>
    [HttpPost("{id:guid}/activate")]
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
    /// Gets all relationships for a specific system (both source and target)
    /// </summary>
    [HttpGet("by-system/{systemId:guid}")]
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
            .Where(r => r.SourceSystemId == systemId && r.IsActive)
            .OrderBy(r => r.TargetSystem!.Name)
            .ToListAsync();

        var targetRelationships = await _context.SystemRelationships
            .Include(r => r.SourceSystem)
            .Where(r => r.TargetSystemId == systemId && r.IsActive)
            .OrderBy(r => r.SourceSystem!.Name)
            .ToListAsync();

        var result = new SystemRelationshipsDto
        {
            SystemId = systemId,
            OutgoingRelationships = _mapper.Map<List<SystemRelationshipDto>>(sourceRelationships),
            IncomingRelationships = _mapper.Map<List<SystemRelationshipDto>>(targetRelationships)
        };

        return Ok(result);
    }

    /// <summary>
    /// Gets system architecture overview showing all relationships
    /// </summary>
    [HttpGet("architecture")]
    public async Task<ActionResult<IntegrationArchitectureDto>> GetIntegrationArchitecture()
    {
        var systems = await _context.IntegrationSystems
            .Where(s => s.IsActive)
            .Select(s => new IntegrationSystemSummaryDto
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                SystemType = s.SystemType,
                Status = s.Status
            })
            .ToListAsync();

        var relationships = await _context.SystemRelationships
            .Include(r => r.SourceSystem)
            .Include(r => r.TargetSystem)
            .Where(r => r.IsActive)
            .ToListAsync();

        var relationshipDtos = _mapper.Map<List<SystemRelationshipDto>>(relationships);

        var architecture = new IntegrationArchitectureDto
        {
            Systems = systems,
            Relationships = relationshipDtos,
            GeneratedAt = DateTime.UtcNow
        };

        _logger.LogInformation("Generated integration architecture overview with {SystemCount} systems and {RelationshipCount} relationships", 
            systems.Count, relationships.Count);

        return Ok(architecture);
    }

    /// <summary>
    /// Gets relationship statistics by type
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult<RelationshipStatisticsDto>> GetRelationshipStatistics()
    {
        var totalRelationships = await _context.SystemRelationships.CountAsync();
        var activeRelationships = await _context.SystemRelationships.CountAsync(r => r.IsActive);

        var relationshipsByType = await _context.SystemRelationships
            .Where(r => r.IsActive)
            .GroupBy(r => r.RelationshipType)
            .Select(g => new RelationshipTypeCountDto
            {
                RelationshipType = g.Key,
                Count = g.Count()
            })
            .ToListAsync();

        var systemsWithRelationships = await _context.SystemRelationships
            .Where(r => r.IsActive)
            .SelectMany(r => new[] { r.SourceSystemId, r.TargetSystemId })
            .Distinct()
            .CountAsync();

        var statistics = new RelationshipStatisticsDto
        {
            TotalRelationships = totalRelationships,
            ActiveRelationships = activeRelationships,
            InactiveRelationships = totalRelationships - activeRelationships,
            RelationshipsByType = relationshipsByType,
            SystemsWithRelationships = systemsWithRelationships
        };

        return Ok(statistics);
    }
}
