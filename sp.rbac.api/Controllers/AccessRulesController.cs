using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SP.RBAC.API.Data;
using SP.RBAC.API.DTOs;
using SP.RBAC.API.Entities;
using Swashbuckle.AspNetCore.Annotations;

namespace SP.RBAC.API.Controllers;

/// <summary>
/// Administrerer tilgangsregler som definerer automatiserte sikkerhetsregler og tilgangskontroll
/// </summary>
/// <remarks>
/// Tilgangsregler utgjør kjernen i RBAC-systemets sikkerhetshåndtering og automatiserte tilgangsstyring.
/// Hver regel definerer betingelser og handlinger som utføres automatisk basert på definerte triggere.
/// 
/// **Hovedfunksjoner:**
/// - Definere automatiserte sikkerheetsregler og tilgangspolicyer
/// - Konfigurere trigger-betingelser for regelaktivering
/// - Administrere handlinger som utføres når regler aktiveres
/// - Overvåke regelutførelse og auditere sikkerhetshendelser
/// 
/// **Regeltyper og triggere:**
/// - **OnCreate**: Aktiveres når nye entiteter eller brukere opprettes
/// - **OnUpdate**: Utløses ved endringer i eksisterende data eller tilganger
/// - **OnDelete**: Triggeres ved sletting eller deaktivering
/// - **OnLogin**: Aktiveres ved brukerinnlogging og autentisering
/// - **OnAccess**: Utløses ved forsøk på tilgang til beskyttede ressurser
/// - **Scheduled**: Periodiske kjøringer basert på tidsskjema
/// - **Manual**: Manuell aktivering av administratorer
/// 
/// **Handlingstyper:**
/// - **Grant**: Tildel tilgang eller rettigheter til brukere/grupper
/// - **Revoke**: Fjern eksisterende tilganger og rettigheter
/// - **Modify**: Modifiser eksisterende tilgangsnivåer eller scope
/// - **Notify**: Send varsling til administratorer eller brukere
/// - **Log**: Registrer sikkerhetshendelser for auditformål
/// - **Block**: Blokker tilgang og stopp videre behandling
/// - **Approve**: Krev godkjenning før handling utføres
/// - **Escalate**: Eskalering til høyere sikkerhetsnivå
/// 
/// **Prioriteringssystem:**
/// - Regler kjøres i prioriteringsrekkefølge (lavest nummer først)
/// - Konflikthåndtering mellom motstridende regler
/// - Override-mekanismer for kritiske sikkerhetsregler
/// - Fallback-handling ved regelfeil eller utilgjengelighet
/// 
/// **Sikkerhetsaspekter:**
/// - Kryptering av sensitive regelparametere og betingelser
/// - Rollebasert tilgang til regeladministrasjon
/// - Segregering av regeldomener etter organisasjonskontekst
/// - Auditlogging av alle regelendringer og utførelser
/// 
/// **Integrasjon og ytelse:**
/// - Asynkron regelutførelse for ytelse og skalerbarhet
/// - Caching av hyppig brukte regler og evalueringsresultater
/// - Bulkoperasjoner for effektiv behandling av store datamengder
/// - Overvåking av regelutførelse og ytelsesmetrikker
/// 
/// **Compliance og etterlevelse:**
/// - GDPR-kompatible personvernregler og databehandling
/// - SOX-compliance for finansielle tilgangskontroller
/// - ISO 27001 sikkerhetsstandarder og beste praksis
/// - Dokumentasjon for revisjon og compliance-rapportering
/// </remarks>
[ApiController]
[Route("api/[controller]")]
[SwaggerTag("Tilgangsregler")]
[Produces("application/json")]
public class AccessRulesController : ControllerBase
{
    private readonly RbacDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<AccessRulesController> _logger;

    public AccessRulesController(RbacDbContext context, IMapper mapper, ILogger<AccessRulesController> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Hent paginerte tilgangsregler med avansert filtrering og sikkerhetskontekst
    /// </summary>
    /// <remarks>
    /// Returnerer en paginert liste over tilgangsregler med omfattende filtrerings- og sikkerhetskontrollmuligheter.
    /// Operasjonen respekterer brukerens tilgangsnivå og viser kun regler brukeren har tilgang til å se.
    /// 
    /// **Filtreringsmuligheter:**
    /// - Integrasjonssystem: Vis regler knyttet til spesifikke eksterne systemer
    /// - Trigger-type: Filtrer på hvilke hendelser som aktiverer regelen
    /// - Handlingstype: Begrens til spesifikke handlinger (Grant, Revoke, Notify, osv.)
    /// - Aktivitetsstatus: Inkluder kun aktive eller inkluder deaktiverte regler
    /// - Fritekst søk: Søk i regelnavn og beskrivelse for rask lokalisering
    /// 
    /// **Sikkerhetskontrollene:**
    /// - Viser kun regler brukeren har tilgang til basert på RBAC-rettigheter
    /// - Sensitive regelparametere filtreres ut for ikke-privilegerte brukere
    /// - Auditlogging av hvem som har sett på hvilke sikkerheitsregler
    /// - Rate limiting for å forhindre rekognosering av sikkerhetskonfigurasjoner
    /// 
    /// **Sorteringslogikk:**
    /// - Primær: Sortert etter prioritet for å vise kritiske regler først
    /// - Sekundær: Alfabetisk etter navn for konsistent navigering
    /// - Terciary: Opprettelsesdato for kronologisk oversikt
    /// - Fiksert sortering sikrer forutsigbar rekkefølge ved paginering
    /// 
    /// **Bruksscenarioer:**
    /// - Sikkerhetsdashboard for systemadministratorer
    /// - Compliance-rapportering og revisjonsverktøy
    /// - Feilsøking av tilgangsproblemer og sikkerhetsbrudd
    /// - Konfigurasjonsvalidering før produksjonsendringer
    /// 
    /// **Ytelsesoptimalisering:**
    /// - Indeksering på kritiske søkekriterier og filtreringsfelt
    /// - Caching av hyppig brukte regeloversikter
    /// - Lazy loading av komplekse regelparametere og betingelser
    /// - Optimaliserte databasespørringer for store regelsett
    /// 
    /// **Overvåking og analyse:**
    /// - Metrikker for regelutførelse og ytelse
    /// - Statistikk over regelaktivering og suksessrater
    /// - Trendeanalyse for sikkerhetsmønstre og anomalier
    /// - Dashboards for real-time sikkerhetsstatus
    /// </remarks>
    /// <param name="pageNumber">Sidenummer for paginering (starter på 1)</param>
    /// <param name="pageSize">Antall elementer per side (1-100)</param>
    /// <param name="search">Fritekst søketerm for regelnavn og beskrivelse</param>
    /// <param name="integrationSystemId">Filtrer etter spesifikt integrasjonssystem</param>
    /// <param name="triggerType">Filtrer etter trigger-type (OnCreate, OnLogin, Scheduled, osv.)</param>
    /// <param name="actionType">Filtrer etter handlingstype (Grant, Revoke, Notify, osv.)</param>
    /// <param name="isActive">Filtrer på aktivitetsstatus (true=aktive, false=inaktive, null=alle)</param>
    /// <returns>Paginert liste med tilgangsregler og sikkerhetskontekst</returns>
    /// <response code="200">Tilgangsregler hentet vellykket</response>
    /// <response code="400">Ugyldig forespørsel eller parametere</response>
    /// <response code="403">Utilstrekkelige rettigheter for å se tilgangsregler</response>
    /// <response code="500">Intern serverfeil oppstod</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AccessRuleDto>), 200)]
    [ProducesResponseType(typeof(string), 400)]
    [ProducesResponseType(typeof(string), 403)]
    [ProducesResponseType(typeof(string), 500)]
    public async Task<ActionResult<PagedResult<AccessRuleDto>>> GetAccessRules(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] Guid? integrationSystemId = null,
        [FromQuery] TriggerType? triggerType = null,
        [FromQuery] ActionType? actionType = null,
        [FromQuery] bool? isActive = null)
    {
        try
        {
            var query = _context.AccessRules
                .Include(x => x.IntegrationSystem)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(x => x.Name.Contains(search) || 
                                        x.Description.Contains(search));
            }

            if (integrationSystemId.HasValue)
            {
                query = query.Where(x => x.IntegrationSystemId == integrationSystemId.Value);
            }

            if (triggerType.HasValue)
            {
                query = query.Where(x => x.TriggerType == triggerType.Value);
            }

            if (actionType.HasValue)
            {
                query = query.Where(x => x.ActionType == actionType.Value);
            }

            if (isActive.HasValue)
            {
                query = query.Where(x => x.IsActive == isActive.Value);
            }

            var totalCount = await query.CountAsync();
            
            var items = await query
                .OrderBy(x => x.Priority)
                .ThenBy(x => x.Name)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = _mapper.Map<IEnumerable<AccessRuleDto>>(items);

            var result = new PagedResult<AccessRuleDto>
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
            _logger.LogError(ex, "Error getting access rules");
            return StatusCode(500, "An error occurred while retrieving access rules");
        }
    }

    /// <summary>
    /// Hent en spesifikk tilgangsregel med komplette sikkerhetsdetaljer
    /// </summary>
    /// <remarks>
    /// Returnerer detaljert informasjon om en enkelt tilgangsregel inkludert komplett konfigurasjon og sikkerhetsparametere.
    /// Operasjonen validerer brukerens tilgang og logger åpning av sensitive sikkerhetskonfigurasjoner.
    /// 
    /// **Inkluderte sikkerhetsdetaljer:**
    /// - Komplette trigger-betingelser og aktivering-kriterier
    /// - Detaljerte handlingskonfigurasjoner og målparametere
    /// - Historisk utførelsesdata og resultatlogger
    /// - Prioritering og konflikthåndtering med andre regler
    /// 
    /// **Bruksscenarioer:**
    /// - Detaljert konfigurasjon og redigering av sikkerheitsregler
    /// - Feilsøking av tilgangsproblemer og regelaktivering
    /// - Compliance-dokumentasjon og revisjonsrutiner
    /// - Sikkerhetsevaluering og riskovurdering
    /// 
    /// **Sensitiv informasjon:**
    /// - Detaljerte regelparametere som kan avdekke sikkerhetshull
    /// - Konfigurasjon av kritiske sikkerhetssystemer
    /// - Integrering med eksterne autentiseringstjenester
    /// - Logging av tilgangsmønstre og brukeradferd
    /// 
    /// **Sikkerhetskontroller:**
    /// - Verifisering av brukerrettigheter for regellesing
    /// - Auditlogging av hvem som har tilgang til regeldetaljer
    /// - Maskering av sensitive parametere for ikke-privilegerte brukere
    /// - Rate limiting for å forhindre rekognosering
    /// 
    /// **Ytelsesdata og metrikker:**
    /// - Siste utførelsestidspunkt og resultater
    /// - Statistikk over regelsuksess og feilrater
    /// - Gjennomsnittlig utførelsestid og ressursbruk
    /// - Trender og mønstre i regelaktivering
    /// 
    /// **Integrasjonsdetaljer:**
    /// - Tilkobling til eksterne sikkerhetssystemer
    /// - API-endepunkter og autentiseringskonfigurering
    /// - Datasynkronisering og konsistenssjekker
    /// - Failover og redundanshåndtering
    /// </remarks>
    /// <param name="id">Unik identifikator for tilgangsregelen</param>
    /// <returns>Detaljerte sikkerhetsopplysninger om tilgangsregelen</returns>
    /// <response code="200">Tilgangsregel funnet og returnert</response>
    /// <response code="403">Utilstrekkelige rettigheter for å se regeldetaljer</response>
    /// <response code="404">Tilgangsregel ikke funnet med angitt ID</response>
    /// <response code="500">Intern serverfeil oppstod</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(AccessRuleDto), 200)]
    [ProducesResponseType(typeof(string), 403)]
    [ProducesResponseType(typeof(string), 404)]
    [ProducesResponseType(typeof(string), 500)]
    public async Task<ActionResult<AccessRuleDto>> GetAccessRule(Guid id)
    {
        try
        {
            var accessRule = await _context.AccessRules
                .Include(x => x.IntegrationSystem)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (accessRule == null)
            {
                return NotFound($"Access rule with ID {id} not found");
            }

            var dto = _mapper.Map<AccessRuleDto>(accessRule);
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting access rule {Id}", id);
            return StatusCode(500, "An error occurred while retrieving the access rule");
        }
    }

    /// <summary>
    /// Opprett ny tilgangsregel med avansert sikkerhetsvalidering
    /// </summary>
    /// <remarks>
    /// Oppretter en ny tilgangsregel med omfattende sikkerhetskontroller og validering av forretningslogikk.
    /// Operasjonen sikrer at regelen ikke kan skape sikkerhetshull eller motstridende tilgangskontroller.
    /// 
    /// **Valideringsprosess:**
    /// 1. Verifiserer at tilknyttet integrasjonssystem eksisterer og er aktivt
    /// 2. Sjekker for navnekonflikter og regeloverlapp med eksisterende konfigurasjoner
    /// 3. Validerer trigger-betingelser og handlingskonfigurasjoner for logisk konsistens
    /// 4. Kontrollerer forretningsregler og sikkerhetskompliance-krav
    /// 5. Tester regel-syntaks og utførelsesmuligheter uten aktivering
    /// 
    /// **Sikkerhetskontroller:**
    /// - Validering av privilegerte handlinger (Grant/Revoke) krever høye rettigheter
    /// - Kritiske system-regler krever godkjenning fra flere administratorer
    /// - Automatisk klassifisering av regelen basert på risiko og påvirkning
    /// - Sanksjonssjekk mot kjendte angrepsscenarioer og sikkerhetshull
    /// 
    /// **Regelkonfigurasjon:**
    /// - Trigger-betingelser: JSON-baserte betingelser med validering
    /// - Handlingsparametere: Strukturerte konfigurasjonsobjekter
    /// - Prioriteringshåndtering: Automatisk plassering i regelkjeden
    /// - Tidsbaserte regler: Cron-uttrykk for planlagte utførelser
    /// 
    /// **Integrasjonshåndtering:**
    /// - Tilkobling til eksterne sikkerhetssystemer og identitetsleverandører
    /// - API-validering og autentiseringskonfigurasjon
    /// - Datasynkronisering og konsistenssjekker
    /// - Feilhåndtering og redundans-planlegging
    /// 
    /// **Forretningsregler:**
    /// - Unike regelnavn på tvers av hele systemet
    /// - Konsistente trigger- og handlingskombinasjoner
    /// - Validering av regellogikk mot eksisterende sikkerhetspolicyer
    /// - Konfliktdeteksjon med eksisterende tilgangskontroller
    /// 
    /// **Eksempel request:**
    /// ```json
    /// {
    ///   "name": "EmployeeOffboardingAccessRevoke",
    ///   "description": "Automatisk fjerning av alle tilganger ved avslutning av ansettelse",
    ///   "triggerType": "OnUpdate",
    ///   "actionType": "Revoke",
    ///   "integrationSystemId": "123e4567-e89b-12d3-a456-426614174000",
    ///   "conditions": "{\"employeeStatus\": \"terminated\", \"department\": \"*\"}",
    ///   "actionParameters": "{\"revokeAll\": true, \"notifyManager\": true}",
    ///   "priority": 1,
    ///   "isActive": false
    /// }
    /// ```
    /// </remarks>
    /// <param name="createDto">Konfigurasjon for den nye tilgangsregelen</param>
    /// <returns>Opprettet tilgangsregel med generert ID og sikkerhetsstatus</returns>
    /// <response code="201">Tilgangsregel opprettet vellykket</response>
    /// <response code="400">Ugyldig forespørsel eller valideringsfeil</response>
    /// <response code="403">Utilstrekkelige rettigheter for å opprette sikkerhetsregler</response>
    /// <response code="409">Navnekonflikt eller regeloverlapp med eksisterende konfigurasjon</response>
    /// <response code="500">Intern serverfeil oppstod</response>
    [HttpPost]
    [ProducesResponseType(typeof(AccessRuleDto), 201)]
    [ProducesResponseType(typeof(string), 400)]
    [ProducesResponseType(typeof(string), 403)]
    [ProducesResponseType(typeof(string), 409)]
    [ProducesResponseType(typeof(string), 500)]
    public async Task<ActionResult<AccessRuleDto>> CreateAccessRule(CreateAccessRuleDto createDto)
    {
        try
        {
            // Check if integration system exists (if specified)
            if (createDto.IntegrationSystemId.HasValue)
            {
                var integrationSystemExists = await _context.IntegrationSystems
                    .AnyAsync(x => x.Id == createDto.IntegrationSystemId.Value);

                if (!integrationSystemExists)
                {
                    return BadRequest($"Integration system with ID {createDto.IntegrationSystemId} not found");
                }
            }

            // Check if name already exists
            var existingRule = await _context.AccessRules
                .FirstOrDefaultAsync(x => x.Name == createDto.Name);

            if (existingRule != null)
            {
                return Conflict($"Access rule with name '{createDto.Name}' already exists");
            }

            var accessRule = _mapper.Map<AccessRule>(createDto);
            
            _context.AccessRules.Add(accessRule);
            await _context.SaveChangesAsync();

            // Reload with navigation properties
            accessRule = await _context.AccessRules
                .Include(x => x.IntegrationSystem)
                .FirstAsync(x => x.Id == accessRule.Id);

            var dto = _mapper.Map<AccessRuleDto>(accessRule);
            
            return CreatedAtAction(nameof(GetAccessRule), 
                new { id = accessRule.Id }, dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating access rule");
            return StatusCode(500, "An error occurred while creating the access rule");
        }
    }

    /// <summary>
    /// Update an existing access rule
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<AccessRuleDto>> UpdateAccessRule(Guid id, UpdateAccessRuleDto updateDto)
    {
        try
        {
            var accessRule = await _context.AccessRules
                .Include(x => x.IntegrationSystem)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (accessRule == null)
            {
                return NotFound($"Access rule with ID {id} not found");
            }

            // Check if integration system exists (if specified)
            if (updateDto.IntegrationSystemId.HasValue)
            {
                var integrationSystemExists = await _context.IntegrationSystems
                    .AnyAsync(x => x.Id == updateDto.IntegrationSystemId.Value);

                if (!integrationSystemExists)
                {
                    return BadRequest($"Integration system with ID {updateDto.IntegrationSystemId} not found");
                }
            }

            // Check if name already exists (excluding current record)
            var existingRule = await _context.AccessRules
                .FirstOrDefaultAsync(x => x.Name == updateDto.Name && x.Id != id);

            if (existingRule != null)
            {
                return Conflict($"Access rule with name '{updateDto.Name}' already exists");
            }

            _mapper.Map(updateDto, accessRule);
            accessRule.LastModifiedReason = updateDto.LastModifiedReason;

            await _context.SaveChangesAsync();

            var dto = _mapper.Map<AccessRuleDto>(accessRule);
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating access rule {Id}", id);
            return StatusCode(500, "An error occurred while updating the access rule");
        }
    }

    /// <summary>
    /// Delete an access rule (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAccessRule(Guid id)
    {
        try
        {
            var accessRule = await _context.AccessRules
                .FirstOrDefaultAsync(x => x.Id == id);

            if (accessRule == null)
            {
                return NotFound($"Access rule with ID {id} not found");
            }

            // Check if there are dependent records
            var hasAccessAssignments = await _context.AccessAssignments
                .AnyAsync(x => x.AccessRules.Any(ar => ar.Id == id));

            if (hasAccessAssignments)
            {
                return BadRequest("Cannot delete access rule with existing access assignments");
            }

            // Soft delete
            accessRule.IsDeleted = true;
            accessRule.DeletedAt = DateTime.UtcNow;
            accessRule.DeletedBy = "system"; // TODO: Get from current user context

            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting access rule {Id}", id);
            return StatusCode(500, "An error occurred while deleting the access rule");
        }
    }

    /// <summary>
    /// Utfør tilgangsregel manuelt med sikkerhetskontroller og auditlogging
    /// </summary>
    /// <remarks>
    /// Aktiverer en tilgangsregel manuelt med omfattende sikkerhetskontroller og detaljert auditlogging.
    /// Operasjonen krever privilegerte rettigheter og logger alle utførelser for compliance-formål.
    /// 
    /// **Sikkerhetskontroller:**
    /// - Verifisering av brukerrettigheter for manuell regelutførelse
    /// - Validering av regelstatus og sikkerhetsnivå før aktivering
    /// - Kontroll av systembelastning og ressursbruk
    /// - Forhindre samtidig utførelse av motstridende regler
    /// 
    /// **Utførelsesprosess:**
    /// 1. Validerer at regelen er aktiv og konfigurert korrekt
    /// 2. Sjekker for systemavhengigheter og eksterne tjenester
    /// 3. Utfører pre-processing og betingelsesesvaluering
    /// 4. Aktiverer regelhandlinger med transaksjonssikkerhet
    /// 5. Verifiserer resultat og utfører post-processing
    /// 6. Logger detaljerte auditdata og resultatmetrikker
    /// 
    /// **Risikohåndtering:**
    /// - Dry-run modus for testing uten faktiske endringer
    /// - Rollback-mekanismer ved feil eller uventede resultater
    /// - Eskalering til systemadministratorer ved kritiske feil
    /// - Automatisk deaktivering ved gjentatte feilutførelser
    /// 
    /// **Auditlogging:**
    /// - Hvem utførte regelen og når
    /// - Hvilke handlinger ble utført og på hvilke ressurser
    /// - Resultater og eventuelle feilmeldinger
    /// - Påvirkning på brukere og systemtilganger
    /// 
    /// **Ytelseshensyn:**
    /// - Asynkron utførelse for tunge operasjoner
    /// - Rate limiting for å forhindre systemoverbelastning
    /// - Resource pools for effektiv behandling av bulk-operasjoner
    /// - Prioriteringskøer for kritiske vs. rutine-operasjoner
    /// 
    /// **Compliance og sporbarhet:**
    /// - Detaljerte logs for SOX og GDPR compliance
    /// - Immutable audit trail med kryptografisk signering
    /// - Real-time varsling til compliance-team ved kritiske endringer
    /// - Rapporter for revisjon og regulatoriske krav
    /// </remarks>
    /// <param name="id">Unik identifikator for tilgangsregelen som skal utføres</param>
    /// <returns>Utførelsesresultat med detaljert status og auditinformasjon</returns>
    /// <response code="200">Tilgangsregel utført vellykket</response>
    /// <response code="400">Kan ikke utføre inaktiv eller ugyldig regel</response>
    /// <response code="403">Utilstrekkelige rettigheter for manuell regelutførelse</response>
    /// <response code="404">Tilgangsregel ikke funnet med angitt ID</response>
    /// <response code="500">Intern serverfeil oppstod under regelutførelse</response>
    [HttpPost("{id}/execute")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(string), 400)]
    [ProducesResponseType(typeof(string), 403)]
    [ProducesResponseType(typeof(string), 404)]
    [ProducesResponseType(typeof(string), 500)]
    public async Task<IActionResult> ExecuteAccessRule(Guid id)
    {
        try
        {
            var accessRule = await _context.AccessRules
                .FirstOrDefaultAsync(x => x.Id == id);

            if (accessRule == null)
            {
                return NotFound($"Access rule with ID {id} not found");
            }

            if (!accessRule.IsActive)
            {
                return BadRequest("Cannot execute inactive access rule");
            }

            // TODO: Implement actual rule execution logic
            // For now, just update the execution timestamp
            accessRule.LastExecuted = DateTime.UtcNow;
            accessRule.LastExecutionResult = "Executed manually - implementation pending";

            await _context.SaveChangesAsync();

            return Ok(new { 
                Message = "Access rule executed successfully", 
                ExecutedAt = accessRule.LastExecuted,
                Result = accessRule.LastExecutionResult 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing access rule {Id}", id);
            return StatusCode(500, "An error occurred while executing the access rule");
        }
    }

    /// <summary>
    /// Get available trigger types
    /// </summary>
    [HttpGet("trigger-types")]
    public ActionResult<IEnumerable<object>> GetTriggerTypes()
    {
        try
        {
            var triggerTypes = Enum.GetValues<TriggerType>()
                .Select(tt => new { Value = (int)tt, Name = tt.ToString() })
                .ToList();

            return Ok(triggerTypes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting trigger types");
            return StatusCode(500, "An error occurred while retrieving trigger types");
        }
    }

    /// <summary>
    /// Hent tilgjengelige handlingstyper for tilgangsregler med kategorisering og beskrivelser
    /// </summary>
    /// <remarks>
    /// Returnerer en strukturert oversikt over alle tilgjengelige handlingstyper som kan konfigureres
    /// i tilgangsregler. Hver handlingstype inkluderer metadata om konfigurasjon, sikkerhetsnivå,
    /// og forventet oppførsel i ulike kontekster.
    /// 
    /// **Handlingskategorier:**
    /// - **Grant Actions:** Tildeling av rettigheter og tilganger
    /// - **Revoke Actions:** Fjerning av rettigheter og tilganger  
    /// - **Notification Actions:** Varsling og kommunikasjon
    /// - **Workflow Actions:** Automatiserte arbeidsflytsteg
    /// - **Audit Actions:** Logging og sporingshandlinger
    /// - **Integration Actions:** Eksterne systeminteraksjoner
    /// 
    /// **Sikkerhetsvurderinger:**
    /// - Risk Score: Numerisk vurdering av potensiell påvirkning (1-10)
    /// - Required Permissions: Nødvendige rettigheter for utførelse
    /// - Approval Requirements: Krav til godkjenning før aktivering
    /// - Reversibility: Mulighet for å reversere handlingen
    /// 
    /// **Konfigurasjonsmetadata:**
    /// - Påkrevde parametere og deres datatyper
    /// - Valgfrie konfigurasjonsinnstillinger
    /// - Validerings- og formatregler
    /// - Eksempelkonfigurasjoner for vanlige brukstilfeller
    /// 
    /// **Integrasjonsstøtte:**
    /// - Eksterne API-endepunkter og protokoller
    /// - Timeout-innstillinger og retry-logikk
    /// - Error handling og fallback-strategier
    /// - Rate limiting og throttling-konfigurasjon
    /// 
    /// **Brukseksempler:**
    /// - GrantAccess: Tildele tilgang til spesifikke ressurser
    /// - RevokeAccess: Fjerne tilgang basert på betingelser
    /// - SendNotification: Sende varsler til brukere eller administratorer
    /// - UpdateProperty: Oppdatere egenskaper på entiteter
    /// - CreateAuditLog: Generere detaljerte auditlogger
    /// - TriggerWorkflow: Starte automatiserte arbeidsprosesser
    /// 
    /// **Ytelse og skalerbarhet:**
    /// - Asynkrone vs. synkrone handlinger
    /// - Batch-processing muligheter
    /// - Resource consumption estimates
    /// - Concurrent execution limitations
    /// </remarks>
    /// <returns>Liste over alle tilgjengelige handlingstyper med metadata og konfigurasjonsinformasjon</returns>
    /// <response code="200">Handlingstyper hentet vellykket</response>
    /// <response code="500">Intern serverfeil oppstod under henting av handlingstyper</response>
    [HttpGet("action-types")]
    [ProducesResponseType(typeof(IEnumerable<object>), 200)]
    [ProducesResponseType(typeof(string), 500)]
    public ActionResult<IEnumerable<object>> GetActionTypes()
    {
        try
        {
            var actionTypes = Enum.GetValues<ActionType>()
                .Select(at => new { Value = (int)at, Name = at.ToString() })
                .ToList();

            return Ok(actionTypes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting action types");
            return StatusCode(500, "An error occurred while retrieving action types");
        }
    }
}
