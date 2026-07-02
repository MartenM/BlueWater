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

**After any backend contract change, automatically:**
1. Start the backend in the background (PowerShell):
   ```powershell
   $api = Start-Process dotnet -ArgumentList "run","--project","src/Bluewater.Api" -PassThru -NoNewWindow
   Start-Sleep 12
   ```
2. Run `pnpm generate:api` in `frontend/`
3. Fix any TypeScript errors in the frontend call sites that arise from the regenerated client
4. Stop the background process: `Stop-Process -Id $api.Id`

Do not skip this sequence or leave it as a manual step for the user. If the backend fails to start (e.g. Postgres is not running), surface the error clearly rather than silently leaving the frontend client stale.

## Working across both sides

- For a full-stack change, work sequentially in this one session and plan for it as a sequence of phases: backend piece first (with its own tests, per `backend/CLAUDE.md`'s testing conventions), then `generate:api`, then the frontend piece. A subagent for the frontend half would have to re-derive what changed and why on the backend side — context this session already has for free — so direct work is the cheaper default. "Direct work" means direct in *scope* (this session drives it, no fresh subagent), not necessarily inline in the main thread — see below.
- **Fork per phase, not just per task.** Once a plan is approved, treat each of the following as its own fork rather than running it inline in the main thread: implementing the backend piece + its tests; implementing the frontend piece (routes/components) after `generate:api`; each individual debug loop (e.g. chasing an NSwag route collision or a schema-transformer bug) — these tend to be multi-cycle Read/Grep/Bash/restart loops that are pure noise once the root cause is found; reading a batch of existing reference components/files purely to absorb conventions before writing new code; driving manual/Playwright verification and reporting pass/fail (don't view raw screenshots in the main thread — have the fork look at them and report a summary). Reserve the main thread for: decisions, TaskCreate/TaskUpdate tracking, and reviewing the final diffs.
- Use a fresh subagent only for chunks genuinely independent of the rest of the task (e.g. an unrelated backend-only fix alongside an unrelated frontend-only one) — there a clean slate costs nothing extra, and it keeps that side's conventions out of the main session's context.
- Don't delegate trivial one-file edits.
