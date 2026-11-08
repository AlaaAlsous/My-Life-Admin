# MyLife Admin

MyLife Admin är en webbapp för att samla vardagsadministration på ett ställe. Håll koll på dokument, viktiga datum och påminnelser, och få en överblick via dashboarden. Appen består av en **Angular-frontend** och ett **ASP.NET Core-API** (minimal API) med EF Core.

## Funktionalitet

- Skapa ett konto och logga in (JWT) för att komma åt dina personliga sidor.
- Dashboard med nyligen tillagda dokument, kommande händelser och senaste påminnelser.
- Ladda upp, sök, visa, redigera, ladda ner och radera dokument. Ett utgångsdatum skapar automatiskt en kalenderhändelse.
- Kalender med händelser; skapa och hantera påminnelser med e-postaviseringar.
- AI-chatt (global och per dokument) med webbsökning om Azure OpenAI är konfigurerat.
- Mörkt läge och e-postnotiser som sparas per användare.

## Teknik

- **Backend:** ASP.NET Core (minimal API), EF Core, SQLite (utveckling) / Azure SQL Server (produktion), JWT, xUnit.
- **Frontend:** Angular (standalone-komponenter), Tailwind CSS, Vitest.
- **E2E:** Playwright.
- **CI/CD:** GitHub Actions (`.github/workflows`).

## Kom igång (utveckling)

Du behöver **.NET 10 SDK** och **Node.js 20+**.

### 1. Starta API:t

```powershell
cd My-Life-Admin-Backend
$env:Jwt__SigningKey = "byt-till-en-lang-slumpmassig-hemlighet"
dotnet run
```

API:t startar på `http://localhost:5076`. I utvecklingsläge används SQLite (`Data/mylife.db`) och databasmigreringar körs automatiskt vid start.

> JWT-nyckeln är lokal hemlig konfiguration — checka aldrig in den i källkoden. Ange den med miljövariabeln `Jwt__SigningKey` (som ovan) eller med `dotnet user-secrets set "Jwt:SigningKey" "..."`.

### 2. Starta webbappen

Öppna en annan terminal:

```powershell
cd My-Life-Admin-Frontend
npm install
npm start
```

Öppna `http://localhost:4200`.

## Konfiguration (valfria integrationer)

Appen körs utan dessa, men vissa funktioner kräver externa tjänster. Spara värdena som **user-secrets** från backendmappen — de hålls utanför källkoden:

```powershell
cd My-Life-Admin-Backend
dotnet user-secrets init   # endast första gången
```

### AI-chatt (Azure OpenAI)

Chatten (`POST /api/chat`) använder Azure OpenAI när alla tre är angivna. Om alla är tomma svarar API:t med en platshållartext; om bara några är angivna startar inte API:t.

```powershell
dotnet user-secrets set "Ai:Endpoint" "https://din-resurs.openai.azure.com/openai/v1"
dotnet user-secrets set "Ai:ApiKey" "DIN_API_NYCKEL"
dotnet user-secrets set "Ai:Deployment" "DIN_DEPLOYMENT"
```

Hämta `Endpoint` och `ApiKey` från Azure OpenAI-resursens **Keys and Endpoint**. `Deployment` är deployment-namnet (inte modellnamnet). Valfritt: `Ai__SystemPrompt`, `Ai__MaxHistoryMessages` och `Ai__MaxOutputTokens`.

### Dokumentanalys (Azure Document Intelligence)

Ger automatisk textutvinning och förslag på metadata vid dokumentuppladdning.

```powershell
dotnet user-secrets set "DocumentIntelligence:Endpoint" "https://din-resurs.cognitiveservices.azure.com/"
dotnet user-secrets set "DocumentIntelligence:ApiKey" "DIN_API_NYCKEL"
```

### E-postpåminnelser (SMTP)

Skickar e-post när en påminnelse infaller (bakgrundstjänst). Konfigurera valfri SMTP-server — t.ex. Gmail med ett [applösenord](https://support.google.com/accounts/answer/185833):

```powershell
dotnet user-secrets set "Email:SmtpHost" "smtp.gmail.com"
dotnet user-secrets set "Email:SmtpPort" "587"
dotnet user-secrets set "Email:Username" "din-epost@gmail.com"
dotnet user-secrets set "Email:Password" "DITT_APPLOSENORD"
dotnet user-secrets set "Email:FromEmail" "din-epost@gmail.com"
```

## Tester

```powershell
# Backend (xUnit)
cd My-Life-Admin-Backend.Tests
dotnet test

# Frontend (Vitest)
cd My-Life-Admin-Frontend
npm test                 # engångskörning: npx ng test --watch=false

# E2E (Playwright) – kräver att backend (5076) och frontend (4200) körs
cd e2e
npm install
npx playwright install
npm test
```

## Projektstruktur

```text
net25-devops-grupp-9/
├── My-Life-Admin-Backend/          # ASP.NET Core-API (minimal API)
│   ├── Endpoints/                  # API-endpoints (auth, dokument, events, påminnelser, dashboard, chatt, inställningar)
│   ├── Services/                   # Affärslogik (auth, dokument, events, påminnelser, e-post, dashboard)
│   ├── Repositories/               # Datalager mot EF Core
│   ├── Models/                     # Entiteter (User, Document, Event, Reminder, ChatMessage …)
│   ├── DTOs/                       # Request- och response-objekt
│   ├── Options/                    # Konfigurationsklasser (Jwt, Ai, Email, DocumentIntelligence …)
│   ├── Migrations/                 # EF Core-migrationer (SQLite + SQL Server)
│   ├── Ai/                         # Azure OpenAI-klient (chatt med webbsökning)
│   └── Storage/                    # Fillagring (lokalt / Azure Blob Storage)
│
├── My-Life-Admin-Backend.Tests/    # xUnit-tester för backend
│
├── My-Life-Admin-Frontend/         # Angular-app (standalone-komponenter)
│   └── src/app/
│       ├── auth/                   # Inloggnings-token, guard och HTTP-interceptor
│       ├── dashboard/              # Översiktssidan
│       ├── documents/              # Dokumentlista, dokumentvy och uppladdning
│       ├── events/                 # Kalender, händelser och påminnelser
│       ├── settings/               # Inställningar (tema, e-postnotiser)
│       ├── chat/                   # AI-chattwidget
│       ├── layout/                 # Sidhuvud, sidfot och mobilmeny
│       ├── landing/                # Landningssida
│       ├── login/ + register/      # Inloggnings- och registreringsdialoger
│       └── shared/                 # Delat (tema, bekräftelsedialog, laddnings-/tomma tillstånd)
│
├── e2e/                            # Playwright E2E-tester
└── .github/workflows/              # CI/CD-pipelines
```

