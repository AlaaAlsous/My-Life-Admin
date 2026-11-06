# MyLife Admin

MyLife Admin är en webbapp för att samla vardagsadministration på ett ställe. Håll koll på händelser och påminnelser, hantera dokument och få en överblick via dashboarden. Appen består av en Angular-frontend och ett .NET API.

## Funktionalitet

- Skapa ett konto och logga in för att komma åt dina personliga sidor.
- Se nyligen tillagda dokument, kommande händelser och påminnelser som behöver uppmärksamhet på dashboarden.
- Ladda upp, sök efter och visa dokument. Lägg till utgångsdatum som kan följas i kalendern.
- Håll koll på viktiga datum i kalendern och hantera påminnelser, inklusive e-postaviseringar.
- Chatta med AI-assistenten om du har konfigurerat Azure OpenAI.

## Kom igång

Du behöver .NET 10 SDK och Node.js 20 eller senare.

1. Starta API:t i en terminal:

   ```powershell
   cd My-Life-Admin-Backend
   $env:Jwt__SigningKey = "byt-till-en-lang-slumpmassig-nyckel"
   dotnet run
   ```

   API:t startar på `http://localhost:5076`. I utvecklingsläge används SQLite och databasmigreringar körs automatiskt. JWT-nyckeln är lokal konfiguration och får inte checkas in i källkod.

2. Starta webbappen i en annan terminal:

   ```powershell
   cd My-Life-Admin-Frontend
   npm install
   npm start
   ```

   Öppna `http://localhost:4200`.

## JWT-konfiguration

API:t kräver en signeringsnyckel för JWT. Konfigurera den med miljövariabeln `Jwt__SigningKey` som ovan. Övriga inställningar kan anges med `Jwt__Issuer`, `Jwt__Audience` och `Jwt__ExpiresMinutes`.

## AI-assistent (valfri)

Chatten (`POST /api/chat`) använder Azure OpenAI när alla tre inställningarna är angivna. Om alla är tomma svarar API:t med en platshållartext; om bara några är angivna startar inte API:t.

Hämta `Endpoint` och `ApiKey` från Azure OpenAI-resursens **Keys and Endpoint**. `Deployment` är namnet på modellens deployment, inte modellnamnet. Spara värden lokalt med user-secrets från backendmappen:

```powershell
dotnet user-secrets set "Ai:Endpoint" "https://din-resurs.openai.azure.com/"
dotnet user-secrets set "Ai:ApiKey" "DIN_OPENAI_NYCKEL"
dotnet user-secrets set "Ai:Deployment" "DIN_DEPLOYMENT"
```

`Ai__SystemPrompt`, `Ai__MaxHistoryMessages` och `Ai__MaxOutputTokens` är valfria inställningar. Dokumentanalys kan också konfigureras med `DocumentIntelligence:Endpoint` och `DocumentIntelligence:ApiKey` via user-secrets.
