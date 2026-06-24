# CLAUDE.md

This is a monorepo with two halves, each with its own `CLAUDE.md` — read the relevant one before working in that directory:

- `backend/` — .NET 10 API solution. See `backend/CLAUDE.md`.
- `frontend/` — SvelteKit app. See `frontend/CLAUDE.md`.

Quick command pointers (full detail in each side's `CLAUDE.md`):

```
cd backend && dotnet build/test/run     # see backend/CLAUDE.md
cd frontend && pnpm dev/build/test:e2e/generate:api   # see frontend/CLAUDE.md
```

## Cross-cutting contract

The frontend's API client (`frontend/src/lib/api/`) is generated from the backend's OpenAPI spec via NSwag (`pnpm generate:api` in `frontend/`), which requires the backend API running locally first. Any backend DTO/contract change (new/renamed field, new/changed endpoint) is not done until `generate:api` has been re-run and the frontend call sites updated to match the regenerated client.

## Working across both sides

- For a full-stack change, work sequentially in this one session: backend piece first (with its own tests, per `backend/CLAUDE.md`'s testing conventions), then `generate:api`, then the frontend piece. A subagent for the frontend half would have to re-derive what changed and why on the backend side — context this session already has for free — so direct work is the cheaper default.
- Use a `fork` (not a fresh subagent) when a step would otherwise dump a lot of noisy intermediate output (build logs, test runs, exploratory reads) into this context — it inherits full context for free and shares the prompt cache, but keeps its own tool output out of the main session.
- Use a fresh subagent only for chunks genuinely independent of the rest of the task (e.g. an unrelated backend-only fix alongside an unrelated frontend-only one) — there a clean slate costs nothing extra, and it keeps that side's conventions out of the main session's context.
- Don't delegate trivial one-file edits.
