# SP.RBAC.API - Rollebasert tilgangskontroll API

En omfattende .NET 9 Web API for håndtering av rollebasert tilgangskontroll med støtte for integrasjonssystemer, entitetsdefinisjoner, egenskapsdefinisjoner og entitetsinstanser.

## Funksjonalitet

- **Integrasjonssystemhåndtering**: CRUD-operasjoner for eksterne systemer (HR, EMR, CRM, etc.)
- **Entitetsdefinisjoner**: Definer datastrukturer for entiteter som brukere, roller, avdelinger
- **Egenskapsdefinisjoner**: Definer egenskaper/attributter for entiteter med ulike datatyper  
- **Entitetsinstanser**: Håndter faktiske entitetsposter med egenskapsverdier (EAV-mønster)
- **Tilgangsregler**: Forretningsregler for automatisk tilgangstildeling
- **Tilgangstildelinger**: Bruker-rolle-system kartlegging
- **Egenskapsverdier**: Verdier for entitetsegenskaper med historikk
- **Synkroniseringslogger**: Sporing av synkroniseringsaktiviteter
- **Pagineringssstøtte**: Alle liste-endepunkter støtter paginering
- **In-Memory Database**: Bruker Entity Framework Core med In-Memory database for enkel testing
- **AutoMapper-integrasjon**: Automatisk kartlegging mellom entiteter og DTOer
- **Swagger-dokumentasjon**: Interaktiv API-dokumentasjon på rot-URL
- **Eksempeldata**: Forhåndsinitialisert med eksempel integrasjonssystemer og entitetsdata

## Teknologistakk

- **.NET 9**: Nyeste .NET-rammeverk
- **ASP.NET Core Web API**: RESTful API-rammeverk
- **Entity Framework Core**: ORM med In-Memory og SQL Server-støtte
- **AutoMapper**: Objekt-til-objekt kartlegging
- **Swagger/OpenAPI**: API-dokumentasjon
- **CORS**: Cross-origin resource sharing aktivert

## Prosjektstruktur

```
SP.RBAC.API/
├── Controllers/                    # API-kontrollere
│   ├── IntegrationSystemsController.cs
│   ├── EntityDefinitionsController.cs
│   ├── PropertyDefinitionsController.cs
│   ├── EntityInstancesController.cs
│   ├── PropertyValuesController.cs
│   ├── AccessRulesController.cs
│   ├── AccessAssignmentsController.cs
│   └── SyncLogsController.cs
├── Data/                          # Datakontekst og kartlegging
│   ├── RbacDbContext.cs
│   └── MappingProfile.cs
├── DTOs/                          # Data Transfer Objects
│   ├── CommonDTOs.cs
│   ├── IntegrationSystemDTOs.cs
│   ├── EntityDefinitionDTOs.cs
│   ├── PropertyDefinitionDTOs.cs
│   ├── EntityInstanceDTOs.cs
│   ├── PropertyValueDTOs.cs
│   ├── AccessRuleDTOs.cs
│   ├── AccessAssignmentDTOs.cs
│   └── SyncLogDTOs.cs
├── Entities/                      # Domeneentiteter
│   ├── BaseEntity.cs
│   ├── BaseAuditableEntity.cs
│   ├── Enums.cs
│   ├── IntegrationSystem.cs
│   ├── EntityDefinition.cs
│   ├── PropertyDefinition.cs
│   ├── EntityInstance.cs
│   ├── PropertyValue.cs
│   ├── AccessRule.cs
│   ├── AccessAssignment.cs
│   └── SyncLog.cs
└── Program.cs                     # Applikasjonskonfigurasjon og oppstart
```

## API-endepunkter

### Integrasjonssystemer

- `GET /api/IntegrationSystems` - List integrasjonssystemer med paginering
- `GET /api/IntegrationSystems/{id}` - Hent spesifikt integrasjonssystem
- `POST /api/IntegrationSystems` - Opprett nytt integrasjonssystem
- `PUT /api/IntegrationSystems/{id}` - Oppdater integrasjonssystem
- `DELETE /api/IntegrationSystems/{id}` - Slett integrasjonssystem (soft delete)
- `POST /api/IntegrationSystems/{id}/test-connection` - Test systemtilkobling

### Entitetsdefinisjoner

- `GET /api/EntityDefinitions` - List entitetsdefinisjoner med paginering
- `GET /api/EntityDefinitions/{id}` - Hent spesifikk entitetsdefinisjon
- `POST /api/EntityDefinitions` - Opprett ny entitetsdefinisjon
- `PUT /api/EntityDefinitions/{id}` - Oppdater entitetsdefinisjon
- `DELETE /api/EntityDefinitions/{id}` - Slett entitetsdefinisjon (soft delete)
- `GET /api/EntityDefinitions/{id}/property-definitions` - Hent egenskapsdefinisjoner for entitet

### Egenskapsdefinisjoner

- `GET /api/PropertyDefinitions` - List egenskapsdefinisjoner med paginering
- `GET /api/PropertyDefinitions/{id}` - Hent spesifikk egenskapsdefinisjon
- `POST /api/PropertyDefinitions` - Opprett ny egenskapsdefinisjon
- `PUT /api/PropertyDefinitions/{id}` - Oppdater egenskapsdefinisjon
- `DELETE /api/PropertyDefinitions/{id}` - Slett egenskapsdefinisjon (soft delete)
- `GET /api/PropertyDefinitions/data-types` - Hent tilgjengelige datatyper

### Entitetsinstanser

- `GET /api/EntityInstances` - List entitetsinstanser med paginering
- `GET /api/EntityInstances/{id}` - Hent spesifikk entitetsinstans
- `POST /api/EntityInstances` - Opprett ny entitetsinstans
- `PUT /api/EntityInstances/{id}` - Oppdater entitetsinstans
- `DELETE /api/EntityInstances/{id}` - Slett entitetsinstans (soft delete)

### Egenskapsverdier

- `GET /api/PropertyValues` - List egenskapsverdier med paginering
- `GET /api/PropertyValues/{id}` - Hent spesifikk egenskapsverdi
- `POST /api/PropertyValues` - Opprett ny egenskapsverdi
- `PUT /api/PropertyValues/{id}` - Oppdater egenskapsverdi
- `GET /api/PropertyValues/entity-instance/{entityInstanceId}` - Hent verdier for entitetsinstans
- `GET /api/PropertyValues/property-definition/{propertyDefinitionId}` - Hent verdier for egenskapsdefinisjon
- `GET /api/PropertyValues/entity-instance/{entityInstanceId}/history` - Hent historikk for entitetsinstans

### Tilgangsregler

- `GET /api/AccessRules` - List tilgangsregler med paginering
- `GET /api/AccessRules/{id}` - Hent spesifikk tilgangsregel
- `POST /api/AccessRules` - Opprett ny tilgangsregel
- `PUT /api/AccessRules/{id}` - Oppdater tilgangsregel
- `POST /api/AccessRules/{id}/execute` - Utfør tilgangsregel manuelt
- `GET /api/AccessRules/trigger-types` - Hent tilgjengelige triggertyper
- `GET /api/AccessRules/action-types` - Hent tilgjengelige aksjonstyper

### Tilgangstildelinger

- `GET /api/AccessAssignments` - List tilgangstildelinger med paginering
- `GET /api/AccessAssignments/{id}` - Hent spesifikk tilgangstildeling
- `POST /api/AccessAssignments` - Opprett ny tilgangstildeling
- `PUT /api/AccessAssignments/{id}` - Oppdater tilgangstildeling
- `GET /api/AccessAssignments/user/{userId}` - Hent tildelinger for bruker
- `GET /api/AccessAssignments/system/{targetSystemId}` - Hent tildelinger for system
- `GET /api/AccessAssignments/assignment-types` - Hent tilgjengelige tildelingstyper

### Synkroniseringslogger

- `GET /api/SyncLogs` - List synkroniseringslogger med paginering
- `GET /api/SyncLogs/{id}` - Hent spesifikk synkroniseringslogg
- `POST /api/SyncLogs` - Opprett ny synkroniseringslogg
- `PUT /api/SyncLogs/{id}` - Oppdater synkroniseringslogg
- `GET /api/SyncLogs/integration-system/{integrationSystemId}` - Hent logger for integrasjonssystem
- `GET /api/SyncLogs/sync-statuses` - Hent tilgjengelige synkroniseringsstatuser
- `GET /api/SyncLogs/summary` - Hent sammendrag av synkroniseringsaktivitet
- `GET /api/SyncLogs/failed` - Hent feilede synkroniseringer

## Datamodell

### Kjerneentiteter

1. **IntegrationSystem**: Eksterne systemer å integrere med
   - Håndterer systemkonfigurasjon, tilkoblingsstrenger og autentiseringstyper
   - Støtter Database, LDAP, OAuth2, SAML, JWT, ApiKey autentisering

2. **EntityDefinition**: Definerer struktur av entiteter innenfor et system
   - Knyttet til et integrasjonssystem
   - Inneholder tabellnavn, primærnøkkel og sorteringsrekkefølge

3. **PropertyDefinition**: Definerer egenskaper/attributter for entiteter
   - Støtter ulike datatyper og valideringsregler
   - Kan være påkrevd, unike eller ha standardverdier

4. **EntityInstance**: Faktiske instanser av entiteter
   - Representerer spesifikke poster i systemet
   - Knyttet til en entitetsdefinisjon

5. **PropertyValue**: Verdier for entitetsegenskaper (EAV-mønster)
   - Historikksporing av verdiendringer
   - Støtter alle definerte datatyper

6. **AccessRule**: Forretningsregler for tilgangstildeling
   - Automatiske triggere basert på egenskapsendringer
   - Konfigurerbare aksjoner for tilgangsstyring

7. **AccessAssignment**: Bruker-rolle-system kartlegging
   - Håndterer direkte, arvede, automatiske og midlertidige tildelinger
   - Sporer tildeling og utløpsdatoer

8. **SyncLog**: Synkroniseringsaktivitetslogger
   - Sporer alle synkroniseringsoperasjoner
   - Inneholder feilmeldinger og ytelsesstatistikk

### Støttede datatyper

- **Tekst**: String
- **Numerisk**: Integer, Decimal
- **Logisk**: Boolean
- **Temporal**: DateTime, Date, Time
- **Spesialisert**: Email, Phone, Url
- **Strukturert**: List, Json

### Autentiseringstyper

- **Database**: Tradisjonell database-autentisering
- **LDAP**: Active Directory integrasjon
- **OAuth2**: Moderne OAuth2-flow
- **SAML**: Enterprise Single Sign-On
- **JWT**: JSON Web Token autentisering
- **ApiKey**: API-nøkkel basert autentisering

### Tildelingstyper

- **Direct**: Direkte tildelt av administrator
- **Inherited**: Arvet fra gruppe eller organisasjonsstruktur
- **Automatic**: Automatisk tildelt basert på regler
- **Temporary**: Midlertidig tildeling med utløpsdato

### Triggertyper for tilgangsregler

- **PropertyChange**: Utløses ved egenskapsendringer
- **NewEntity**: Utløses ved opprettelse av ny entitet
- **EntityUpdate**: Utløses ved oppdatering av entitet
- **EntityDelete**: Utløses ved sletting av entitet
- **Schedule**: Tidsbaserte triggere
- **Manual**: Manuell utførelse

### Aksjonstyper for tilgangsregler

- **AssignRole**: Tildel rolle til bruker
- **RemoveRole**: Fjern rolle fra bruker
- **UpdateProperty**: Oppdater entitetsegenskap
- **CreateEntity**: Opprett ny entitet
- **DeleteEntity**: Slett entitet
- **SendNotification**: Send varsling

## Kom i gang

### Forutsetninger

- .NET 9 SDK
- IDE (Visual Studio, VS Code, JetBrains Rider)
- Valgfritt: SQL Server for produksjonsdatabase

### Kjøre applikasjonen

1. Klon eller naviger til prosjektmappen:

   ```bash
   cd SP.RBAC.API
   ```

2. Gjenopprett pakker:

   ```bash
   dotnet restore
   ```

3. Bygg prosjektet:

   ```bash
   dotnet build
   ```

4. Kjør applikasjonen:

   ```bash
   dotnet run
   ```

5. Åpne nettleseren og naviger til `http://localhost:5109` for å få tilgang til Swagger UI

### Eksempeldata

Applikasjonen initialiserer automatisk eksempeldata ved oppstart:

- **HR-system**: Personaladministrasjonssystem
- **EMR-system**: Elektroniske pasientjournaler
- **Brukerdefinisjon**: Med egenskaper som EmployeeId, FirstName, LastName, Email, Department
- **Eksempelbrukere**: John Doe og Jane Smith med egenskapsverdier
- **Tilgangsregler**: Automatiske regler for rolletildeling
- **Tilgangstildelinger**: Eksempel bruker-rolle kartlegginger

## API-brukseksempler

### Opprett integrasjonssystem

```bash
curl -X POST "http://localhost:5109/api/IntegrationSystems" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "CRM_System",
    "displayName": "Customer Relationship Management",
    "description": "Salgs- og kundebehandlingssystem",
    "systemType": "CRM",
    "systemVersion": "1.5.2",
    "connectionString": "Server=crm-db;Database=CRM;Trusted_Connection=true;",
    "authenticationType": 2,
    "isActive": true,
    "configuration": "{\"syncInterval\": \"weekly\"}"
  }'
```

### Hent integrasjonssystemer

```bash
curl "http://localhost:5109/api/IntegrationSystems?pageNumber=1&pageSize=10"
```

### Opprett entitetsdefinisjon

```bash
curl -X POST "http://localhost:5109/api/EntityDefinitions" \
  -H "Content-Type: application/json" \
  -d '{
    "integrationSystemId": "your-integration-system-id",
    "name": "Customer",
    "displayName": "Kundeinformasjon",
    "description": "Kundeinformasjon fra CRM",
    "tableName": "customers",
    "primaryKeyField": "customer_id",
    "isActive": true,
    "sortOrder": 1
  }'
```

### Opprett tilgangsregel

```bash
curl -X POST "http://localhost:5109/api/AccessRules" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Auto_Role_Assignment",
    "description": "Automatisk rolletildeling basert på avdeling",
    "triggerType": 0,
    "actionType": 0,
    "conditions": "{\"department\": \"IT\"}",
    "actions": "{\"assignRole\": \"ITAdmin\"}",
    "isActive": true,
    "priority": 10
  }'
```

## Konfigurasjon

### Database

Som standard bruker applikasjonen Entity Framework Core In-Memory database for enkel testing. For å bruke SQL Server:

1. Legg til tilkoblingsstreng i `appsettings.json`:

   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Database=RBAC;Trusted_Connection=true;"
     }
   }
   ```

2. Applikasjonen vil automatisk bruke SQL Server når en tilkoblingsstreng er oppgitt.

### CORS

CORS er aktivert med en "AllowAll"-policy for utvikling. Oppdater CORS-konfigurasjonen i `Program.cs` for produksjonsbruk.

### Logging

Applikasjonen bruker strukturert logging med støtte for:

- Console-logging for utvikling
- File-logging for produksjon
- Application Insights for Azure-deployment

## Funksjonalitet og egenskaper

- **Soft Delete**: Alle entiteter støtter soft delete (IsDeleted-flagg)
- **Audit Trail**: Spor opprettelse/modifikasjon timestamps og brukere
- **Optimistisk samtidighet**: Versjonskontroll med RowVersion
- **Validering**: Entitetsvalidering og forretningsregelshåndhevelse
- **Paginering**: Konsistent paginering på tvers av alle liste-endepunkter
- **Filtrering**: Søk og filtreringsmuligheter på liste-endepunkter
- **Feilhåndtering**: Omfattende feilhåndtering med passende HTTP-statuskoder
- **Logging**: Strukturert logging gjennom hele applikasjonen
- **Caching**: Redis-støtte for ytelsesoptimalisering
- **Validering**: Omfattende inputvalidering og forretningsregler

## Fremtidige forbedringer

- **Autentisering/Autorisasjon**: JWT-tokens og rollebasert tilgangskontroll
- **Ekte integrasjonskoblinger**: Direkte koblinger til HR/EMR/CRM-systemer
- **Bakgrunnssynkroniseringstjenester**: Automatiserte datasynkroniseringsjobber
- **Avansert tilgangsregelmotor**: Komplekse forretningsregler og workflows
- **Audit log-endepunkter**: Detaljert sporingslogging
- **Bulk-operasjoner**: Masseoppdateringer og -imports
- **Eksport/Import-funksjonalitet**: Datamigrasjon og backup
- **Sanntidsvarsler**: WebSocket-baserte oppdateringer
- **GraphQL API**: Fleksibel dataspørring
- **Mikroservicer-arkitektur**: Skalerbar distribuert arkitektur

## Bidrag

Dette er et demonstrasjonsprosjekt som viser:

- Clean architecture-prinsipper
- RESTful API-design
- Entity Framework Core beste praksis
- AutoMapper-bruk
- Omfattende CRUD-operasjoner
- Riktig feilhåndtering og validering
- Moderne .NET 9-funksjoner
- Skalerbar arkitekturdesign

Prosjektet fungerer som et fundament for å bygge mer komplekse rollebaserte tilgangskontrollsystemer med integrasjonsmuligheter.
