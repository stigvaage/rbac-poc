# 🚀 SP.RBAC.API - Developer Quick Start

[![Status](https://img.shields.io/badge/Status-Aktiv%20utvikling-green.svg)](PROGRESS.md)
[![API](https://img.shields.io/badge/API-http%3A%2F%2Flocalhost%3A5110%2Fswagger-brightgreen.svg)](http://localhost:5110/swagger)

## ⚡ Hurtigstart (5 minutter)

### 1. 📥 Hent og kjør

```bash
# Klon repository
git clone https://github.com/stigvaage/sp-rbac-poc.git
cd sp-rbac-poc

# Kjør applikasjonen
cd sp.rbac.api
dotnet run --urls="http://localhost:5110"
```

### 2. 🎯 Test API-et

- **Swagger UI**: [http://localhost:5110/swagger](http://localhost:5110/swagger)
- **Health Check**: [http://localhost:5110/health](http://localhost:5110/health)
- **API Base**: [http://localhost:5110/api](http://localhost:5110/api)

### 3. 🔍 Utforsk seed data

API-et initialiseres med testdata:
- 3 integrasjonssystemer (HR, EMR, CRM)
- 5 entitetsdefinisjoner med egenskapsdefinisjoner
- 6 entitetsinstanser med egenskapsverdier
- Eksempel tilgangsregler og tildelinger

## 🏗️ Arkitekturoversikt

```
┌─────────────────────────────────────────────────────────────┐
│                    SP.RBAC.API                              │
├─────────────────────────────────────────────────────────────┤
│  Controllers (12)    │  DTOs (24+)     │  Entities (8)      │
│  ├─ Integration      │  ├─ Input       │  ├─ Integration    │
│  ├─ Entity          │  ├─ Output      │  ├─ Entity        │
│  ├─ Property        │  └─ Paged       │  ├─ Property      │
│  ├─ Access          │                 │  └─ Access        │
│  └─ Sync            │                 │                   │
├─────────────────────────────────────────────────────────────┤
│  Data Layer                                                 │
│  ├─ RbacDbContext (EF Core 9.0)                            │
│  ├─ AutoMapper Profiles                                    │
│  └─ In-Memory Database (default)                           │
└─────────────────────────────────────────────────────────────┘
```

## 📚 Viktige konsepter

### 🔗 Entity-Attribute-Value (EAV) Pattern
```csharp
EntityDefinition -> PropertyDefinition -> PropertyValue
     │                    │                    │
     │                    │                    └─ Faktiske verdier
     │                    └─ Attributt metadata og validering  
     └─ Entitetsstruktur og konfigurasjon
```

### 🎯 Tilgangsstyring
```csharp
AccessRule (Triggere/Betingelser) -> AccessAssignment (Bruker<->Rolle)
```

### 🔄 Synkronisering
```csharp
IntegrationSystem -> EntityInstance -> PropertyValue
                         │
                         └─ SyncLog (sporing og feilhåndtering)
```

## 🛠️ Utviklingsregler

### Clean Architecture 
- **Domain**: Entiteter og forretningslogikk (ingen eksterne avhengigheter)
- **Application**: Use cases og applikasjonslogikk (kun Domain-avhengigheter)
- **Infrastructure**: Database og eksterne tjenester
- **API**: HTTP-endepunkter og presentasjonslogikk

### DDD Prinsipper
- Factory methods for entitetsopprettelse
- Private konstruktører på aggregates
- Forretningsmetoder med intensjonsavsløring
- Sterkt typede ID-er (planlagt)

### Testing Strategy
- **Unit Tests**: Domain og Application (xUnit + FakeItEasy)
- **Integration Tests**: Infrastructure og API (Testcontainers)
- **Architecture Tests**: Håndheve designprinsipper (NetArchTest)

## 📋 Ofte brukte kommandoer

### Development
```bash
# Bygg prosjekt
dotnet build

# Kjør tester (når implementert)
dotnet test

# Kjør med watch (auto-reload)
dotnet watch run --urls="http://localhost:5110"

# Sjekk for compiler warnings
dotnet build --verbosity normal
```

### Database
```bash
# Lag EF Core migrasjon
dotnet ef migrations add <MigrationName>

# Oppdater database
dotnet ef database update

# Fjern siste migrasjon
dotnet ef migrations remove
```

### Documentation
```bash
# Generer XML dokumentasjon (automatisk i build)
dotnet build

# Sjekk Markdown lint
markdownlint *.md

# Format kode
dotnet format
```

## 🧪 Testing med Swagger

### Vanlige workflows:

1. **Opprett integrasjonssystem** → **Opprett entitetsdefinisjon** → **Opprett egenskapsdefinisjoner**
2. **Opprett entitetsinstans** → **Legg til egenskapsverdier**
3. **Konfigurer tilgangsregel** → **Test automatisk tilgangstildeling**

### JSON-eksempler:

**Nytt integrasjonssystem:**
```json
{
  "name": "TestSystem",
  "displayName": "Test System",
  "systemType": "HR",
  "connectionString": "test-connection",
  "authenticationType": 0,
  "isActive": true
}
```

**Ny entitetsdefinisjon:**
```json
{
  "integrationSystemId": "your-system-id",
  "name": "Employee",
  "displayName": "Ansatt",
  "tableName": "employees",
  "primaryKeyField": "emp_id"
}
```

## 🎯 Neste steg for bidragsytere

### 🔴 Høy prioritet (påkrevd)
1. **Implementer testing** - xUnit unit tests og integrasjonstester
2. **Fullfør norsk dokumentasjon** - 45 API-metoder gjenstår
3. **Sikkerhet** - JWT autentisering og autorisasjon

### 🟡 Medium prioritet
4. **DDD refactoring** - Sterkt typede ID-er og factory methods
5. **Advanced features** - Godkjenningsarbeidsflyt og varsling
6. **Performance** - Database optimalisering og caching

### 🟢 Lav prioritet  
7. **Enterprise features** - Multi-tenancy og GDPR compliance
8. **DevOps** - CI/CD pipeline og containerisering

## 📖 Nyttige lenker

- 📊 **[PROGRESS.md](PROGRESS.md)** - Detaljert fremdriftssporing
- 📝 **[CHANGELOG.md](CHANGELOG.md)** - Versjonhistorikk og endringer
- 🏗️ **[.github/instructions/](/.github/instructions/)** - Utviklingsinstruksjoner
- 🤖 **[.github/copilot-instructions.md](/.github/copilot-instructions.md)** - GitHub Copilot-konfigurasjon

## 🆘 Feilsøking

### Port i bruk
```bash
# Finn prosess som bruker port 5110
lsof -ti:5110

# Drep prosess
lsof -ti:5110 | xargs kill -9

# Eller bruk annen port
dotnet run --urls="http://localhost:5111"
```

### Database issues
```bash
# Reset in-memory database (restart app)
dotnet run

# Sjekk EF Core versjoner
dotnet list package | grep EntityFramework
```

### Swagger ikke tilgjengelig
1. Sjekk at app kjører på riktig port
2. Naviger til /swagger (ikke /swagger/index.html)
3. Sjekk console for errors

---

💡 **Tips**: Bruk GitHub Copilot Chat med `@workspace` for spørsmål om kodebasen!

🚀 **Kom i gang**: Start med å utforske Swagger UI og test noen API-kall!
