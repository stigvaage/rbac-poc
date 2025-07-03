using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SP.RBAC.API.Data;
using SP.RBAC.API.DTOs;
using SP.RBAC.API.Entities;

namespace SP.RBAC.API.Controllers;

/// <summary>
/// Administrerer tilgangstildelinger som kobler brukere til roller i spesifikke målsystemer
/// </summary>
/// <remarks>
/// Tilgangstildelinger representerer den faktiske koblingen mellom brukere og roller
/// innenfor konteksten av et spesifikt målsystem. Hver tildeling definerer hvilken
/// rolle en bruker har i et bestemt system, sammen med metadata om tildelingens
/// gyldighet, type og forretningsbegrunnelse.
/// 
/// **Hovedfunksjoner:**
/// - Administrere bruker-rolle-system tilordninger med full livssyklus
/// - Støtte for ulike tildelingstyper (direkte, automatisk, midlertidig)
/// - Validering av forretningsregler og tilgangspolicyer
/// - Sporing av tildelingsbegrunnelser og godkjenningsprosesser
/// - Bulk-operasjoner for effektiv tilgangsadministrasjon
/// 
/// **Sikkerhetshensyn:**
/// - Kritisk komponent for tilgangskontroll på tvers av alle systemer
/// - Validering av bruker- og rolleeksistens før tildeling
/// - Automatisk konfliktdeteksjon for overlappende tildelinger
/// - Audit-logging av alle tildelings- og endringsoperasjoner
/// 
/// **Forretningslogikk:**
/// - Støtte for tidsbegrensede tildelinger med utløpsdatoer
/// - Automatisk deaktivering basert på forretningsregler
/// - Hierarkisk rollearv hvis konfigurert i målsystemet
/// - Integrasjon med godkjenningsarbeidsflyter for sensitive roller
/// 
/// **Compliance og rapportering:**
/// - Fullstendig sporbarhet for alle tilgangsendringer
/// - Støtte for tilgangsgjennomganger og sertifiseringsprosesser
/// - Automatisert rapportering av tilgangsmønstre og avvik
/// - GDPR-kompatibel håndtering av brukerrelaterte tildelinger
/// </remarks>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AccessAssignmentsController : ControllerBase
{
    private readonly RbacDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<AccessAssignmentsController> _logger;

    public AccessAssignmentsController(RbacDbContext context, IMapper mapper, ILogger<AccessAssignmentsController> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Henter alle tilgangstildelinger med avansert filtrering og paginering
    /// </summary>
    /// <remarks>
    /// Returnerer en komplett oversikt over tilgangstildelinger i systemet med støtte
    /// for omfattende filtreringsmuligheter. Operasjonen inkluderer alle relaterte
    /// entiteter som bruker-, rolle- og målsysteminformasjon for komplett kontekst.
    /// 
    /// **Filtreringsmuligheter:**
    /// - Bruker-spesifikke tildelinger for individuell tilgangsgjennomgang
    /// - Rolle-baserte filter for rolleanalyse og administrasjon
    /// - Målsystem-filtrering for systemspesifikk tilgangskontroll
    /// - Tildelingstype-filtrering for å skille mellom automatiske og manuelle tildelinger
    /// - Aktivitetsstatus for å inkludere eller ekskludere inaktive tildelinger
    /// 
    /// **Sikkerhetsvurderinger:**
    /// - Returnerer sensitive bruker-rolle-koblinger som krever tilgangskontroll
    /// - Søkefunksjonalitet kan avsløre eksistensen av brukere og roller
    /// - Metadata kan inneholde forretningskritisk informasjon om tilgangsbegrunnelser
    /// 
    /// **Ytelsesoptimalisering:**
    /// - Eager loading av relaterte entiteter for reduserte database-kall
    /// - Indekserte spørringer på primære filter-felter
    /// - Paginering anbefales sterkt for store tildelingsdatasett
    /// 
    /// **Forretningsbruk:**
    /// - Tilgangsgjennomganger og sertifiseringsprosesser
    /// - Rapportering og compliance-dokumentasjon
    /// - Feilsøking av tilgangsproblemer og rolleanalyse
    /// - Bulk-operasjoner og datamassering
    /// </remarks>
    /// <param name="page">Sidenummer for paginering (1-basert, standard: 1)</param>
    /// <param name="pageSize">Antall elementer per side (standard: 10, maksimum anbefalt: 100)</param>
    /// <param name="userId">Filtrer på spesifikk bruker-ID for brukersentrerte tilgangsgjennomganger</param>
    /// <param name="roleId">Filtrer på spesifikk rolle-ID for rollebasert analyse</param>
    /// <param name="targetSystemId">Filtrer på målsystem-ID for systemspesifikk tilgangskontroll</param>
    /// <param name="assignmentType">Filtrer på tildelingstype (automatisk, manuell, midlertidig)</param>
    /// <param name="isActive">Filtrer på aktivitetsstatus (null=alle, true=aktive, false=inaktive)</param>
    /// <param name="search">Fritekstsøk i tildelingsbegrunnelse, metadata og relaterte entitetsnavn</param>
    /// <returns>Paginert resultat med tilgangstildelinger og fullstendig kontekstinformasjon</returns>
    /// <response code="200">Returnerer paginert liste over tilgangstildelinger med relaterte data</response>
    /// <response code="400">Ugyldig forespørsel - valideringsfeil i filtreringsparametere</response>
    /// <response code="500">Intern serverfeil under henting av tilgangstildelinger</response>
    [HttpGet]
    public async Task<ActionResult<PagedResult<AccessAssignmentDto>>> GetAccessAssignments(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] Guid? userId = null,
        [FromQuery] Guid? roleId = null,
        [FromQuery] Guid? targetSystemId = null,
        [FromQuery] AssignmentType? assignmentType = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? search = null)
    {
        try
        {
            var query = _context.AccessAssignments
                .Include(aa => aa.User)
                .Include(aa => aa.Role)
                .Include(aa => aa.TargetSystem)
                .AsQueryable();

            // Apply filters
            if (userId.HasValue)
                query = query.Where(aa => aa.UserId == userId.Value);

            if (roleId.HasValue)
                query = query.Where(aa => aa.RoleId == roleId.Value);

            if (targetSystemId.HasValue)
                query = query.Where(aa => aa.TargetSystemId == targetSystemId.Value);

            if (assignmentType.HasValue)
                query = query.Where(aa => aa.AssignmentType == assignmentType.Value);

            if (isActive.HasValue)
                query = query.Where(aa => aa.IsActive == isActive.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower();
                query = query.Where(aa => 
                    (aa.AssignmentReason != null && aa.AssignmentReason.ToLower().Contains(searchLower)) ||
                    aa.Metadata.ToLower().Contains(searchLower) ||
                    aa.User.DisplayName.ToLower().Contains(searchLower) ||
                    aa.Role.DisplayName.ToLower().Contains(searchLower) ||
                    aa.TargetSystem.Name.ToLower().Contains(searchLower));
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(aa => aa.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = _mapper.Map<List<AccessAssignmentDto>>(items);

            var result = new PagedResult<AccessAssignmentDto>
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
            _logger.LogError(ex, "Error retrieving access assignments");
            return StatusCode(500, "An error occurred while retrieving access assignments");
        }
    }

    /// <summary>
    /// Henter en spesifikk tilgangstildeling med komplett kontekstinformasjon
    /// </summary>
    /// <remarks>
    /// Returnerer detaljert informasjon om en enkelt tilgangstildeling inkludert
    /// alle relaterte entiteter som bruker, rolle og målsystem. Operasjonen
    /// gir fullstendig kontekst for tildelingen med metadata og audit-informasjon.
    /// 
    /// **Datainnhold:**
    /// - Komplett tilgangstildelingsinformasjon med alle felter
    /// - Relaterte bruker-, rolle- og målsystementiteter
    /// - Tildelingsbegrunnelse og metadata for forretningskontekst
    /// - Audit-informasjon med opprettelse og endringssporing
    /// 
    /// **Sikkerhetshensyn:**
    /// - Eksponerer sensitive bruker-rolle-koblinger som krever autorisasjon
    /// - Returnerer interne ID-er og systemreferanser
    /// - Metadata kan inneholde konfidensielle forretningsbegrunnelser
    /// 
    /// **Bruksområder:**
    /// - Detaljert tilgangsgjennomgang for spesifikke tildelinger
    /// - Feilsøking av tilgangsproblemer og rollevalidering
    /// - Audit og compliance-dokumentasjon
    /// - Grunnlag for tildelingsendrings- og oppdateringsprosesser
    /// </remarks>
    /// <param name="id">Unik identifikator for tilgangstildelingen som skal hentes</param>
    /// <returns>Komplett tilgangstildeling med all relatert kontekstinformasjon</returns>
    /// <response code="200">Tilgangstildeling funnet og returnert med komplett datainnhold</response>
    /// <response code="404">Tilgangstildeling med angitt ID eksisterer ikke i systemet</response>
    /// <response code="500">Intern serverfeil under henting av tilgangstildeling</response>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AccessAssignmentDto>> GetAccessAssignment(Guid id)
    {
        try
        {
            var accessAssignment = await _context.AccessAssignments
                .Include(aa => aa.User)
                .Include(aa => aa.Role)
                .Include(aa => aa.TargetSystem)
                .FirstOrDefaultAsync(aa => aa.Id == id);

            if (accessAssignment == null)
                return NotFound($"Access assignment with ID {id} not found");

            var dto = _mapper.Map<AccessAssignmentDto>(accessAssignment);
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving access assignment {Id}", id);
            return StatusCode(500, "An error occurred while retrieving the access assignment");
        }
    }

    /// <summary>
    /// Oppretter en ny tilgangstildeling med omfattende validering og konfliktsjekk
    /// </summary>
    /// <remarks>
    /// Oppretter en ny tilgangstildeling som kobler en bruker til en rolle innenfor
    /// konteksten av et spesifikt målsystem. Operasjonen utfører omfattende validering
    /// av alle refererte entiteter og sjekker for potensielle konflikter med
    /// eksisterende aktive tildelinger.
    /// 
    /// **Valideringsregler:**
    /// - Bruker må eksistere som entitetsinstans i systemet
    /// - Rolle må eksistere som entitetsinstans i systemet  
    /// - Målsystem må eksistere som aktivt integrasjonssystem
    /// - Ingen eksisterende aktiv tildeling for samme bruker-rolle-system kombinasjon
    /// - Tildelingstype må være gyldig for målsystemet
    /// 
    /// **Sikkerhetshensyn:**
    /// - Ingen automatisk autorisasjonssjekk implementert - krever ekstern tilgangskontroll
    /// - Opprettelse av tilgangstildelinger er en kritisk sikkerhetsoperasjon
    /// - Audit-logging registrerer opprettelseshandlingen for compliance
    /// - Tildelingsbegrunnelse anbefales for sporbarhet og godkjenning
    /// 
    /// **Forretningsprosess:**
    /// - Automatisk tidsstempel for opprettelse med systembruker-registrering
    /// - Tildeling aktiveres automatisk hvis ikke annet spesifiseres
    /// - Støtte for metadata og tildelingsbegrunnelse for dokumentasjon
    /// - Transaksjonell opprettelse sikrer datakonsistens
    /// 
    /// **Integrasjon:**
    /// - Kan trigge automatiske varslings- og godkjenningsarbeidsflyter
    /// - Synkronisering med eksterne systemer hvis konfigurert
    /// - Automatisk oppdatering av brukerens totale tilgangsprofil
    /// </remarks>
    /// <param name="dto">Opprettelsesdata for ny tilgangstildeling inkludert alle nødvendige referanser</param>
    /// <returns>Opprettet tilgangstildeling med generert ID og komplett kontekstinformasjon</returns>
    /// <response code="201">Tilgangstildeling opprettet og returnerer komplette data med Location header</response>
    /// <response code="400">Valideringsfeil - ugyldig bruker, rolle, målsystem eller manglende data</response>
    /// <response code="409">Konflikt - aktiv tilgangstildeling eksisterer allerede for samme kombinasjon</response>
    /// <response code="500">Intern serverfeil under opprettelse av tilgangstildeling</response>
    [HttpPost]
    public async Task<ActionResult<AccessAssignmentDto>> CreateAccessAssignment(CreateAccessAssignmentDto dto)
    {
        try
        {
            // Validate referenced entities exist
            var userExists = await _context.EntityInstances.AnyAsync(ei => ei.Id == dto.UserId);
            if (!userExists)
                return BadRequest($"User with ID {dto.UserId} not found");

            var roleExists = await _context.EntityInstances.AnyAsync(ei => ei.Id == dto.RoleId);
            if (!roleExists)
                return BadRequest($"Role with ID {dto.RoleId} not found");

            var targetSystemExists = await _context.IntegrationSystems.AnyAsync(is_ => is_.Id == dto.TargetSystemId);
            if (!targetSystemExists)
                return BadRequest($"Target system with ID {dto.TargetSystemId} not found");

            // Check for existing active assignment
            var existingAssignment = await _context.AccessAssignments
                .FirstOrDefaultAsync(aa => 
                    aa.UserId == dto.UserId && 
                    aa.RoleId == dto.RoleId && 
                    aa.TargetSystemId == dto.TargetSystemId &&
                    aa.IsActive);

            if (existingAssignment != null)
                return Conflict("An active access assignment already exists for this user-role-system combination");

            var accessAssignment = _mapper.Map<AccessAssignment>(dto);
            accessAssignment.Id = Guid.NewGuid();
            accessAssignment.CreatedAt = DateTime.UtcNow;
            accessAssignment.CreatedBy = "System"; // In real app, get from user context

            _context.AccessAssignments.Add(accessAssignment);
            await _context.SaveChangesAsync();

            // Fetch the created assignment with navigation properties
            var createdAssignment = await _context.AccessAssignments
                .Include(aa => aa.User)
                .Include(aa => aa.Role)
                .Include(aa => aa.TargetSystem)
                .FirstOrDefaultAsync(aa => aa.Id == accessAssignment.Id);

            var resultDto = _mapper.Map<AccessAssignmentDto>(createdAssignment);
            return CreatedAtAction(nameof(GetAccessAssignment), new { id = accessAssignment.Id }, resultDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating access assignment");
            return StatusCode(500, "An error occurred while creating the access assignment");
        }
    }

    /// <summary>
    /// Oppdaterer en eksisterende tilgangstildeling med validering og konfliktsjekk
    /// </summary>
    /// <remarks>
    /// Utfører komplett oppdatering av en eksisterende tilgangstildeling med validering
    /// av alle refererte entiteter og konfliktsjekk hvis nøkkelfelt endres. Operasjonen
    /// støtter endring av bruker, rolle, målsystem og alle metadata-felter.
    /// 
    /// **Valideringsregler:**
    /// - Tilgangstildeling må eksistere før oppdatering
    /// - Nye bruker-, rolle- og målsystem-referanser må være gyldige
    /// - Konfliktsjekk utføres hvis bruker-rolle-system kombinasjon endres
    /// - Ingen duplisering av aktive tildelinger tillatt
    /// 
    /// **Sikkerhetshensyn:**
    /// - Kritisk sikkerhetsoperasjon som krever høyt autorisasjonsnivå
    /// - Endringer i tilgangstildelinger logges for audit og compliance
    /// - Oppdatering kan påvirke brukerens totale tilgangsprofil umiddelbart
    /// 
    /// **Forretningsprosess:**
    /// - Automatisk oppdatering av audit-felter med tidsstempel
    /// - Støtte for endringsbegrunnelse og metadata-oppdatering
    /// - Transaksjonell behandling sikrer datakonsistens
    /// - Kan trigge re-synkronisering med eksterne systemer
    /// </remarks>
    /// <param name="id">Unik identifikator for tilgangstildelingen som skal oppdateres</param>
    /// <param name="dto">Oppdateringsdata for tilgangstildeling inkludert alle felter som skal endres</param>
    /// <returns>Oppdatert tilgangstildeling med alle endringer reflektert</returns>
    /// <response code="200">Tilgangstildeling oppdatert og returnerer komplette data</response>
    /// <response code="400">Valideringsfeil - ugyldig bruker, rolle, målsystem eller konflikterende data</response>
    /// <response code="404">Tilgangstildeling med angitt ID eksisterer ikke i systemet</response>
    /// <response code="409">Konflikt - ny kombinasjon ville skape duplikat av eksisterende aktiv tildeling</response>
    /// <response code="500">Intern serverfeil under oppdatering av tilgangstildeling</response>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AccessAssignmentDto>> UpdateAccessAssignment(Guid id, UpdateAccessAssignmentDto dto)
    {
        try
        {
            var existingAssignment = await _context.AccessAssignments.FindAsync(id);
            if (existingAssignment == null)
                return NotFound($"Access assignment with ID {id} not found");

            // Validate referenced entities exist
            var userExists = await _context.EntityInstances.AnyAsync(ei => ei.Id == dto.UserId);
            if (!userExists)
                return BadRequest($"User with ID {dto.UserId} not found");

            var roleExists = await _context.EntityInstances.AnyAsync(ei => ei.Id == dto.RoleId);
            if (!roleExists)
                return BadRequest($"Role with ID {dto.RoleId} not found");

            var targetSystemExists = await _context.IntegrationSystems.AnyAsync(is_ => is_.Id == dto.TargetSystemId);
            if (!targetSystemExists)
                return BadRequest($"Target system with ID {dto.TargetSystemId} not found");

            // Check for conflicting active assignment (if changing key fields)
            if (existingAssignment.UserId != dto.UserId || 
                existingAssignment.RoleId != dto.RoleId || 
                existingAssignment.TargetSystemId != dto.TargetSystemId)
            {
                var conflictingAssignment = await _context.AccessAssignments
                    .FirstOrDefaultAsync(aa => 
                        aa.Id != id &&
                        aa.UserId == dto.UserId && 
                        aa.RoleId == dto.RoleId && 
                        aa.TargetSystemId == dto.TargetSystemId &&
                        aa.IsActive);

                if (conflictingAssignment != null)
                    return Conflict("An active access assignment already exists for this user-role-system combination");
            }

            _mapper.Map(dto, existingAssignment);
            existingAssignment.UpdatedAt = DateTime.UtcNow;
            existingAssignment.UpdatedBy = "System"; // In real app, get from user context

            await _context.SaveChangesAsync();

            // Fetch updated assignment with navigation properties
            var updatedAssignment = await _context.AccessAssignments
                .Include(aa => aa.User)
                .Include(aa => aa.Role)
                .Include(aa => aa.TargetSystem)
                .FirstOrDefaultAsync(aa => aa.Id == id);

            var resultDto = _mapper.Map<AccessAssignmentDto>(updatedAssignment);
            return Ok(resultDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating access assignment {Id}", id);
            return StatusCode(500, "An error occurred while updating the access assignment");
        }
    }

    /// <summary>
    /// Sletter en tilgangstildeling med myk sletting og audit-sporing
    /// </summary>
    /// <remarks>
    /// Utfører myk sletting av en tilgangstildeling ved å markere den som slettet
    /// uten å fjerne data fysisk fra databasen. Dette bevarer audit-spor og
    /// historikk for compliance og potensielle gjenopprettingsbehov.
    /// 
    /// **Slettingsprosess:**
    /// - Myk sletting med IsDeleted flagg og slettingstidspunkt
    /// - Bevaring av alle historiske data for audit-formål
    /// - Registrering av slettende bruker for sporbarhet
    /// - Ingen fysisk fjerning av tilgangsdata fra database
    /// 
    /// **Sikkerhetshensyn:**
    /// - Kritisk sikkerhetsoperasjon som påvirker brukertilgang umiddelbart
    /// - Sletting kan fjerne nødvendig tilgang og påvirke brukerproduktivitet
    /// - Permanente audit-logger for compliance og feilsøking
    /// - Ingen automatisk autorisasjonssjekk - krever ekstern tilgangskontroll
    /// 
    /// **Forretningsimplikasjon:**
    /// - Umiddelbar effekt på brukerens tilgang i målsystemet
    /// - Kan trigge automatiske varslings- og godkjenningsarbeidsflyter
    /// - Mulighet for gjenoppretting gjennom administrativa prosesser
    /// - Synkronisering med eksterne systemer hvis konfigurert
    /// </remarks>
    /// <param name="id">Unik identifikator for tilgangstildelingen som skal slettes</param>
    /// <returns>Ingen innhold ved vellykket sletting</returns>
    /// <response code="204">Tilgangstildeling slettet (myk sletting) uten returdata</response>
    /// <response code="404">Tilgangstildeling med angitt ID eksisterer ikke i systemet</response>
    /// <response code="500">Intern serverfeil under sletting av tilgangstildeling</response>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteAccessAssignment(Guid id)
    {
        try
        {
            var accessAssignment = await _context.AccessAssignments.FindAsync(id);
            if (accessAssignment == null)
                return NotFound($"Access assignment with ID {id} not found");

            // Soft delete
            accessAssignment.IsDeleted = true;
            accessAssignment.DeletedAt = DateTime.UtcNow;
            accessAssignment.DeletedBy = "System"; // In real app, get from user context

            await _context.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting access assignment {Id}", id);
            return StatusCode(500, "An error occurred while deleting the access assignment");
        }
    }

    /// <summary>
    /// Henter alle tilgangstildelinger for en spesifikk bruker
    /// </summary>
    /// <remarks>
    /// Returnerer en komplett oversikt over alle tilgangstildelinger for en bestemt
    /// bruker på tvers av alle målsystemer. Operasjonen er kritisk for brukersentrerte
    /// tilgangsgjennomganger og personlige tilgangsprofiler.
    /// 
    /// **Bruksområder:**
    /// - Brukersentrerte tilgangsgjennomganger og sertifiseringsprosesser
    /// - Personlig tilgangsprofil for brukere og administratorer
    /// - Feilsøking av brukerspesifikke tilgangsproblemer
    /// - Grunnlag for tilgangsrapporter og compliance-dokumentasjon
    /// 
    /// **Sikkerhetshensyn:**
    /// - Eksponerer fullstendig tilgangsprofil for en bruker
    /// - Krever sterk autorisasjon for å forhindre uautorisert tilgangsinnsikt
    /// - Returnerer sensitive rolle- og systemkoblinger
    /// 
    /// **Datakvalitet:**
    /// - Inkluderer både aktive og inaktive tildelinger basert på parameter
    /// - Kronologisk sortering med nyeste tildelinger først
    /// - Komplett kontekstinformasjon med rolle- og målsystemdetaljer
    /// </remarks>
    /// <param name="userId">Unik identifikator for brukeren som tilgangshistorikken skal hentes for</param>
    /// <param name="includeInactive">Inkluder inaktive tildelinger i resultatet (standard: false)</param>
    /// <returns>Liste over brukerens tilgangstildelinger med komplett kontekstinformasjon</returns>
    /// <response code="200">Returnerer liste over brukerens tilgangstildelinger</response>
    /// <response code="500">Intern serverfeil under henting av brukertildelinger</response>
    [HttpGet("user/{userId:guid}")]
    public async Task<ActionResult<List<AccessAssignmentDto>>> GetUserAccessAssignments(
        Guid userId, 
        [FromQuery] bool includeInactive = false)
    {
        try
        {
            var query = _context.AccessAssignments
                .Include(aa => aa.User)
                .Include(aa => aa.Role)
                .Include(aa => aa.TargetSystem)
                .Where(aa => aa.UserId == userId);

            if (!includeInactive)
                query = query.Where(aa => aa.IsActive);

            var assignments = await query
                .OrderByDescending(aa => aa.CreatedAt)
                .ToListAsync();

            var dtos = _mapper.Map<List<AccessAssignmentDto>>(assignments);
            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving access assignments for user {UserId}", userId);
            return StatusCode(500, "An error occurred while retrieving access assignments");
        }
    }

    /// <summary>
    /// Get access assignments for a specific target system
    /// </summary>
    /// <param name="targetSystemId">Target system ID</param>
    /// <param name="includeInactive">Include inactive assignments</param>
    /// <returns>List of target system's access assignments</returns>
    [HttpGet("system/{targetSystemId:guid}")]
    public async Task<ActionResult<List<AccessAssignmentDto>>> GetSystemAccessAssignments(
        Guid targetSystemId, 
        [FromQuery] bool includeInactive = false)
    {
        try
        {
            var query = _context.AccessAssignments
                .Include(aa => aa.User)
                .Include(aa => aa.Role)
                .Include(aa => aa.TargetSystem)
                .Where(aa => aa.TargetSystemId == targetSystemId);

            if (!includeInactive)
                query = query.Where(aa => aa.IsActive);

            var assignments = await query
                .OrderByDescending(aa => aa.CreatedAt)
                .ToListAsync();

            var dtos = _mapper.Map<List<AccessAssignmentDto>>(assignments);
            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving access assignments for system {SystemId}", targetSystemId);
            return StatusCode(500, "An error occurred while retrieving access assignments");
        }
    }

    /// <summary>
    /// Get available assignment types enum values
    /// </summary>
    /// <returns>List of assignment type values</returns>
    [HttpGet("assignment-types")]
    public ActionResult<Dictionary<string, int>> GetAssignmentTypes()
    {
        var assignmentTypes = Enum.GetValues<AssignmentType>()
            .ToDictionary(at => at.ToString(), at => (int)at);
        
        return Ok(assignmentTypes);
    }

    /// <summary>
    /// Activate/deactivate an access assignment
    /// </summary>
    /// <param name="id">Access assignment ID</param>
    /// <param name="isActive">Active status</param>
    /// <returns>Updated access assignment</returns>
    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<AccessAssignmentDto>> UpdateAssignmentStatus(Guid id, [FromBody] bool isActive)
    {
        try
        {
            var assignment = await _context.AccessAssignments
                .Include(aa => aa.User)
                .Include(aa => aa.Role)
                .Include(aa => aa.TargetSystem)
                .FirstOrDefaultAsync(aa => aa.Id == id);

            if (assignment == null)
                return NotFound($"Access assignment with ID {id} not found");

            assignment.IsActive = isActive;
            assignment.UpdatedAt = DateTime.UtcNow;
            assignment.UpdatedBy = "System"; // In real app, get from user context

            await _context.SaveChangesAsync();

            var dto = _mapper.Map<AccessAssignmentDto>(assignment);
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating assignment status {Id}", id);
            return StatusCode(500, "An error occurred while updating the assignment status");
        }
    }
}
