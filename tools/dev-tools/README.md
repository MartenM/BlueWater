# Dev tools

Local-only auxiliary services for development. Not part of the deployed stack (see `tools/docker-latest` for that).

## MailHog

A local SMTP catcher so mail sent by the API during development never leaves the machine.

```
cd tools/dev-tools
docker compose up -d
```

- SMTP endpoint: `localhost:1025` (no auth, no TLS) — matches the `Mail:Senders` entry already in `backend/src/Bluewater.Api/appsettings.Development.json`.
- Web UI: http://localhost:8025 — view/inspect every message the API sends while `dotnet run` is pointed at this catcher.

Stop with `docker compose down` (add `-v` to also drop any captured-message volume, though this image keeps messages in memory only).
