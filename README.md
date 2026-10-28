# net25-devops-grupp-9

## JWT setup (backend)

Backenden kräver en signeringsnyckel för JWT som **inte** får ligga i source control.

Sätt den via miljövariabel innan du startar API:t:

```powershell
$env:Jwt__SigningKey = "replace-with-a-long-random-secret"
```

Konfigurationsnycklar:

- `Jwt__Issuer`
- `Jwt__Audience`
- `Jwt__ExpiresMinutes`
- `Jwt__SigningKey`

## AI-assistent (backend)

Chatten (`POST /api/chat`) använder Azure OpenAI när `Endpoint`, `ApiKey` och `Deployment` är satta. Är alla tre tomma svarar API:t med en platshållartext, så att chatten går att använda innan Azure-resursen finns. Är bara några av dem satta startar inte API:t.

Värdena finns på Azure OpenAI-resursen under **Keys and Endpoint**. `Deployment` är namnet ni gav deploymenten av modellen (inte modellnamnet, t.ex. `gpt-4o-mini`).

Konfigurationsnycklar:

- `Ai__Endpoint`
- `Ai__ApiKey`
- `Ai__Deployment`
- `Ai__SystemPrompt` (valfri, instruktion till modellen)
- `Ai__MaxHistoryMessages` (valfri, antal tidigare meddelanden som skickas med, standard 20)
- `Ai__MaxOutputTokens` (valfri, max längd på svaret, standard 800)