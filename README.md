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