# Mail-sending system — implementation plan

## Context

Bluewater currently has no way to send email at all: no SMTP/mail-provider integration, no background job runner, no template/layout concept. Two needs exist:

1. **Mailing tool** — admins compose and send bulk mail (newsletters/announcements) to member groups, with click/open tracking and stats.
2. **Transactional mail** — other app flows (e.g. member password reset, new member) need to send one-off emails (welcome mail, notifications) with attachments and merge variables, without any of the bulk-mail machinery.

Both need to share the same underlying plumbing: multipart (HTML+plaintext) message building, a merge-token/placeholder substitution engine, configurable senders, and asynchronous delivery via background jobs — so this plan builds that shared foundation first (Phases 1–3) and the bulk mailing tool on top of it (Phases 4–5).

Reference existing patterns to imitate throughout: **News** feature (`NewsPost`/`NewsService`/`NewsController`/`NewsServiceTests`) for a simple CRUD vertical slice, and `MemberClusterService`/`MemberCluster` for a criteria-resolvable grouping concept — mailings reuse `IMemberClusterService.ResolveMembersAsync` directly for cluster targeting rather than reimplementing resolution.

## Decisions locked in

- **Background jobs:** Hangfire, using the existing Postgres database (`Hangfire.PostgreSql`). No Quartz.
- **Transport:** SMTP via MailKit. **Multiple independent SMTP accounts** configured in `appsettings` (each sender has its own host/port/credentials) — not one relay with multiple From identities.
- **Placeholders:** first name, full name, email, formal salutation (= `"Dear {FullName}"`, no gendered logic), address block and custom ones for the transactional mails.
- **Templates/Layouts:** brand-new entities, admin CRUD UI, permission-gated.
- **Click/open tracking:** new anonymous endpoints on the existing API (no separate public domain).
- **Attachments:** reuse existing `StoredFile` + `IFileStorageService`.
- **Mailing targets:** multiple `MemberCluster`s **and** multiple `UserGroupInstance`s (current season), unioned and deduplicated.
- **Scope:** full stack, both paths, sequenced backend-slice → frontend-slice per phase.
- **Proof send:** renders with generic/sample placeholder data (not the admin's own data) — shows what a real recipient would see.
- **Re-send:** a mailing is always re-runnable; `SendAsync` can be called again after `Sent` and only emails newly-resolved recipients (per-recipient `Sent` flag gates duplicates).
- **Click stats:** counter + first-click timestamp per recipient-link, no full click-event log.
- **New dependencies approved:** MailKit, Hangfire.PostgreSql (+ Hangfire.AspNetCore), Markdig (server-side markdown), AngleSharp (HTML link rewriting).

---

## Phase 1 — Foundations: senders, MailKit transport, Hangfire wiring

No domain entities. A low-level transport that can send one already-built multipart message through a named sender, plus Hangfire able to enqueue/execute jobs against the existing Postgres DB. Proven with a manual smoke test (send via Hangfire dashboard "trigger" against a local SMTP catcher, e.g. smtp4dev/Mailhog — do **not** point dev config at a real mailbox).

**Config**
- `backend/src/Bluewater.Infra/Options/MailSenderOptions.cs` — `Key`, `DisplayName`, `FromAddress`, `ReplyToAddress`, `SmtpHost`, `SmtpPort`, `Username`, `Password`, `UseSsl`.
- `backend/src/Bluewater.Infra/Options/MailOptions.cs` — `List<MailSenderOptions> Senders`, `PublicBaseUrl` (for tracked-link/pixel URLs, used from Phase 4).
- Bind in `WebApplicationBuilderExtensions.AddBluewater()`: `builder.Services.AddOptions<MailOptions>().Bind(builder.Configuration.GetSection("Mail")).ValidateOnStart();`
- `appsettings.json` gets an empty `"Mail": { "Senders": [] }` shape; real sender credentials go in `appsettings.Development.json` (matching how `Jwt`/`Database` are split today).

**Transport**
- Add `MailKit` to `Bluewater.Infra.csproj`.
- `Bluewater.Domain/Models/Mail/MailMessageEnvelope.cs` + `MailAttachment.cs` — plain non-persisted transport DTOs (`ToAddresses`, `CcAddresses`, `BccAddresses`, `ReplyToOverride`, `Subject`, `HtmlBody`, `PlainTextBody`, `Attachments`).
- `Bluewater.Infra/Services/Abstractions/IMailTransportService.cs` / `MailTransportService.cs` — resolves a `MailSenderOptions` by key, builds a `MimeMessage` via `BodyBuilder`, opens a new MailKit `SmtpClient` per send (connect/authenticate/send/disconnect), throws `InvalidOperationException` if the sender key is unconfigured. Register `AddScoped<IMailTransportService, MailTransportService>()`.

**Hangfire**
- Add `Hangfire.AspNetCore` + `Hangfire.PostgreSql` to `Bluewater.Api.csproj`, `Hangfire.Core` to `Bluewater.Core.csproj` (Core services enqueue via `IBackgroundJobClient`).
- Extract the Npgsql connection-string builder already used by `AddDatabase()` into a shared helper so Hangfire's `UsePostgreSqlStorage` and EF Core's `AddDbContext` build an identical connection string.
- Wire `AddHangfire(...)` + `AddHangfireServer()` in `AddBluewater()`; `app.MapHangfireDashboard("/hangfire", ...)` in `Program.cs` after `UseAuthorization()`.
- `Bluewater.Api/Authorization/HangfireAdminAuthorizationFilter.cs` (`IDashboardAuthorizationFilter`) gates the dashboard on `AdminModifySettings` — **this must be wired in this phase, not deferred**, and only exposed when `app.Environment.IsDevelopment()` in addition to auth, unless the user later confirms it should be reachable in production too.
- No new EF migration needed for Hangfire itself — `UsePostgreSqlStorage` self-migrates its own `hangfire.*` schema on first run, independent of `context.Database.Migrate()`.

**Tests:** MailKit has no in-memory fake; this phase's coverage is manual verification against a local SMTP catcher rather than an automated test — call this out as a known, acceptable gap.

**Critical files:** `Bluewater.Infra/Options/MailOptions.cs`, `Bluewater.Infra/Services/MailTransportService.cs`, `Bluewater.Api/Extensions/WebApplicationBuilderExtensions.cs`, `Bluewater.Api/Program.cs`, `appsettings.Development.json`.

---

## Phase 2 — Template/Layout domain + admin CRUD (backend + frontend)

**Domain** (`Bluewater.Domain/Models/Mail/`)
- `MailLayout : IAuditable` — `Name`, `HeaderHtml`, `FooterHtml`, `IsDefault` (body is injected between header/footer at render time).
- `MailTemplateKind` enum — `Mailing = 1`, `Transactional = 2`.
- `MailTemplate : IAuditable` — `Name`, `Kind`, `SubjectTemplate`, `BodyMarkdown`, `DefaultLayoutId` (FK, `SetNull` on delete), `DefaultSenderKey`.

**Migration:** `dotnet ef migrations add AddMailTemplatesAndLayouts --project src/Bluewater.Infra --startup-project src/Bluewater.Api`. Add `DbSet`s + `OnModelCreating` FK config to `BluewaterContext`. `IAuditable` audit-stamping applies automatically.

**Merge-token engine (shared — build once here, reused unchanged by Phases 3 & 4):**
- `Bluewater.Core/Services/Mail/MergeTokenContext.cs` — `record MergeTokenContext(IReadOnlyDictionary<string,string> Values)` + a `With(...)` helper to merge extra caller-supplied key/values (e.g. a generated password for a welcome mail).
- `Bluewater.Core/Services/Mail/IMergeTokenRenderer.cs` / `MergeTokenRenderer.cs` — regex-based `{{Token}}` substitution; unknown tokens are left **literally in place** (not blanked) so bugs are visible.
- `Bluewater.Core/Services/Mail/BlueUserMergeTokenFactory.cs` — the one place that knows how to turn a `BlueUser` into the standard token set: `FirstName`, `FullName`, `Email`, `FormalSalutation` (`"Dear {FullName}"`), `AddressBlock`. Every real-`BlueUser` recipient in both mail paths goes through this factory.
- Rendering order for a (template, layout, context) triple: **substitute tokens into the markdown source first, then convert to HTML** (so `**{{FullName}}**` bolds correctly), then wrap with layout header/footer (which may also contain tokens), then derive the plaintext fallback from the token-substituted markdown.
- Add `Markdig` to `Bluewater.Core` (or `Bluewater.Infra`) for server-side markdown→HTML — the frontend's `marked`-based renderer is JS-only and can't be reused server-side. Derive the plaintext fallback via Markdig's AST walker (extract text nodes) rather than regex-stripping HTML tags.

**Core services:** `MailLayoutDto`/`UpsertMailLayoutRequest`, `MailTemplateDto`/`UpsertMailTemplateRequest`/`MailTemplatePreviewRequest`/`MailTemplatePreviewDto` in `Bluewater.Core/Dto/Mail/`. `IMailLayoutService`/`MailLayoutService`, `IMailTemplateService`/`MailTemplateService` (CRUD mirroring `MemberClusterService`, plus `PreviewAsync(templateId, request)` rendering with sample values like "Jane Doe"). Validators: `UpsertMailLayoutRequestValidator`, `UpsertMailTemplateRequestValidator` (required fields, length limits, `DefaultLayoutId` existence via `MustAsync`).

**Controllers:** `MailLayoutsController` (`/api/maillayouts`), `MailTemplatesController` (`/api/mailtemplates` + `POST /{id}/preview`) — full CRUD, all gated on a single new permission `BluePermission.AdminModifyMailTemplates = 29` (covers read+write, consistent with the admin-only nature of this area — no separate view permission, matching the `ClustersModify`-style consolidated pattern rather than the `NewsView`/`NewsModify` split).

**Frontend** (after `pnpm generate:api`): `frontend/src/routes/tools/mail/templates/` and `.../layouts/` (list/new/edit, mirroring `routes/news/` and `routes/tools/groups/` structure), `frontend/src/lib/components/MailTemplateForm.svelte` (plain `<textarea>` for `BodyMarkdown`, live preview via a **debounced call to the server preview endpoint** rather than a second client-side `marked` render — avoids the two-renderer-diverge bug class). Add a nav entry alongside the other `routes/tools/*` admin tools.

**Tests:** `MailLayoutServiceTests.cs`, `MailTemplateServiceTests.cs` (CRUD/not-found/validation), `MergeTokenRendererTests.cs` (pure-logic, no DB: known/unknown/multiple tokens).

**Critical files:** `Bluewater.Domain/Models/Mail/MailTemplate.cs`, `MailLayout.cs`; `Bluewater.Core/Services/Mail/MergeTokenRenderer.cs`, `BlueUserMergeTokenFactory.cs`; `Bluewater.Core/Services/MailTemplateService.cs`; `Bluewater.Infra/Context/BluewaterContext.cs`; `Bluewater.Domain/Models/Groups/BluePermission.cs`; `frontend/src/lib/components/MailTemplateForm.svelte`.

---

## Phase 3 — Transactional mail path

**Core `MailService`** — the shared entry point named in the spec. Not exposed as its own controller (it's called in-process by other services); if an admin-triggerable ad-hoc send is wanted later, that's a thin controller wrapping the same method.

- `Bluewater.Core/Dto/Mail/SendTransactionalMailRequest.cs` (`TemplateId?`, `SubjectOverride?`, `BodyMarkdownOverride?`, `SenderKey`, `List<TransactionalRecipient> Recipients`, `Cc`, `Bcc`, `ReplyToOverride?`, `AttachmentStoredFileIds`); `TransactionalRecipient(Email, DisplayName?, MergeValues, UserId?)`.
- `IMailService.SendTransactionalAsync(request)`: per recipient, resolves template+layout or uses the override content, builds a `MergeTokenContext` from `MergeValues` merged with `BlueUserMergeTokenFactory.ForUser(...)` when `UserId` is set, renders subject/HTML/plaintext via Phase 2's pipeline, **then** enqueues one Hangfire job per recipient with the fully-rendered content as plain serializable data (not entity references — Hangfire jobs execute later against a fresh scope, so rendering must happen before enqueueing, inside `MailService`, which still has live DB access).
- `Bluewater.Core/Services/Mail/TransactionalMailJob.cs` — "dumb" job: given rendered subject/HTML/plaintext + sender key + `AttachmentStoredFileIds`, loads attachment bytes via `IFileStorageService` at execution time, builds a `MailMessageEnvelope`, calls `IMailTransportService.SendAsync`.
- Validator: `SendTransactionalMailRequestValidator` — ≥1 recipient, valid emails, `TemplateId` XOR override content present, attachment ids exist (`MustAsync`).

**First real consumer:** `UserService.CreateAsync` sends a welcome mail after `_userManager.CreateAsync` succeeds, using a seeded `MailTemplate` (`Kind = Transactional`) looked up by a well-known name. If that template is missing, **log a warning and no-op** rather than blocking user creation — the admin can create the template via Phase 2's UI at any time.

**Tests:** `SqliteServiceTestBase` needs a trivial hand-rolled fake `IBackgroundJobClient` (Hangfire's interface is small; a fake recording calls is enough, no mocking library — consistent with repo convention) registered in its `ServiceCollection`, since Hangfire's real client needs `JobStorage.Current` wired to Postgres. `MailServiceTests.cs` (template/override resolution, validation, per-recipient enqueue with correct rendered content asserted against the fake), updated `UserServiceTests.cs` (welcome-mail job gets enqueued on create).

**Frontend:** none directly — likely no controller/DTO surface change at all in this phase (transactional mail has no direct UI); re-run `pnpm generate:api` only if this phase happens to touch any existing controller-facing DTO.

**Critical files:** `Bluewater.Core/Services/MailService.cs`, `Bluewater.Core/Services/Mail/TransactionalMailJob.cs`, `Bluewater.Core/Services/UserService.cs`, `Bluewater.Tests/TestSupport/SqliteServiceTestBase.cs`, `Bluewater.Core/Validators/SendTransactionalMailRequestValidator.cs`.

---

## Phase 4 — Mailing tool: domain, targeting, proof/send, tracking, stats

**Domain** (`Bluewater.Domain/Models/Mail/`)
- `MailingStatus` enum — `Draft = 1`, `Sending = 2`, `Sent = 3`.
- `Mailing : IAuditable` — `Subject`, `BodyMarkdown`, `SenderKey`, `TemplateId?`, `LayoutId?`, `Status`, `ProofSendCount`, `SentAt?`, collections of targets/recipients.
- `MailingTargetCluster` / `MailingTargetGroupInstance` — composite-key join entities (`IAuditableRelation`, mirroring `SignupMemberCluster`'s pattern) linking a `Mailing` to a `MemberCluster` / `UserGroupInstance`. Each gets a `DateTime? LastSentAt`, updated on every `SendAsync` run — informational only, since re-send gating happens at the recipient level, not here.
- `MailingRecipient : IAuditable` — `MailingId`, `UserId?`, `Email`, `FullName`, `Sent`, `SentAt?`, `Opened`, `FirstOpenedAt?`, `OpenCount`, `TrackingToken` (unique), plus **denormalized rendered content** (`RenderedSubject`, `RenderedHtmlBody`, `RenderedPlainTextBody`, nullable until send-prep) so the send job stays "dumb"/idempotent and doesn't re-run the rendering pipeline on retry.
- `MailingRecipientLink : IAuditable` — `MailingRecipientId`, `OriginalUrl`, `Token` (unique, indexed), `ClickCount`, `FirstClickedAt?`. One row per (recipient × distinct URL in the body), generated at send time — no separate `MailingLink` master table; per-link mailing stats aggregate by grouping on `OriginalUrl` across all of a mailing's recipients. Trades minor storage redundancy for a much simpler write/read path — acceptable given expected recipient-list sizes (hundreds, not millions).

**Migration:** `dotnet ef migrations add AddMailingTool --project src/Bluewater.Infra --startup-project src/Bluewater.Api`. Unique index on `(MailingId, Email)` for `MailingRecipient` (dedup — spec: "skip already-recorded recipients"), unique index on `MailingRecipient.TrackingToken` and `MailingRecipientLink.Token`.

**Targeting resolution:** `IMailingTargetResolverService.ResolveRecipientsAsync(mailingId)` — unions `IMemberClusterService.ResolveMembersAsync(clusterId)` for each target cluster with a direct `UserGroupInstanceMembers` query for each target instance, `DistinctBy(UserId)`, filters out blank emails. Reuses `ClusterMemberDto` — no new DTO.

**`MailingService`** (mirrors `MemberClusterService`'s add/remove-criterion shape for targets):
- `CreateAsync`/`UpdateAsync`/`GetAsync`/`ListAsync`/`DeleteAsync` — edits rejected once `Status != Draft` (`BlueValidationException`).
- `AddTargetClusterAsync`/`RemoveTargetClusterAsync`, `AddTargetGroupInstanceAsync`/`RemoveTargetGroupInstanceAsync`.
- `SendProofAsync(mailingId)` — renders with **generic sample data** (not the admin's own), sends to the calling admin's own email, increments `ProofSendCount`. No tracking-link rewriting (a proof isn't a real recipient and would skew stats).
- `SendAsync(mailingId)` — **always re-runnable**, even after `Status == Sent`:
  1. Resolve targets via the resolver service.
  2. Insert a new `MailingRecipient` (unsent) for anyone resolved but not already present (by email, case-insensitive) with a non-blank email.
  3. For every recipient with `Sent == false` (newly-added, or previously failed): render subject/HTML/plaintext via `BlueUserMergeTokenFactory.ForUser(...)`, rewrite `<a href>` targets in the HTML into `{PublicBaseUrl}/api/mail/r/{token}` redirects using **AngleSharp** (HTML parsing, not regex) — generating one `MailingRecipientLink` row per distinct href per recipient — append the open-tracking pixel before the closing body tag, store the rendered content on the `MailingRecipient` row, enqueue a `MailingRecipientSendJob`.
  4. Mark `Mailing.Status = Sent` once enqueuing completes (this means "send process was kicked off," not "100% delivered" — actual delivery status is visible via stats). Update `LastSentAt` on the targeted clusters/instances.
- `GetStatsAsync(mailingId)` — `sentCount`/`openedCount` from `Recipients`, per-link stats grouped by `OriginalUrl` summed across `MailingRecipientLink` rows.
- `Bluewater.Core/Services/Mail/MailingRecipientSendJob.cs` — loads a `MailingRecipient` by id, sends its pre-rendered content via `IMailTransportService`, sets `Sent = true`/`SentAt` and saves. Relies on Hangfire's `AspNetCoreJobActivator` providing a fresh scoped `BluewaterContext` per execution (standard `AddHangfire`+`AddHangfireServer` behavior) — confirm this works as expected during Phase 1's smoke test.

**Tracking controller** (anonymous, mirrors `HealthController`'s top-level placement): `Bluewater.Api/Controllers/MailTrackingController.cs`, route `api/mail`:
- `GET /r/{token}` → looks up `MailingRecipientLink` by token, increments `ClickCount`/sets `FirstClickedAt`, 302-redirects to `OriginalUrl`; unknown/expired token → redirect to a safe fallback (site homepage) rather than an error.
- `GET /p/{token}.gif` → looks up `MailingRecipient` by `TrackingToken`, sets `Opened = true`/`FirstOpenedAt`/increments `OpenCount`, returns a static 1×1 transparent GIF; unknown token silently no-ops (don't leak info from a public endpoint).
- Both actions return `IActionResult` (the documented exception to "actions return the plain type," same as `FilesController.Download`, since these stream a redirect/binary body).

**`MailingsController`** (`/api/mailings`, gated on new permission `BluePermission.AdminModifyMailings = 30`):
`GET/POST /`, `GET/PUT/DELETE /{id}`, `POST/DELETE /{id}/targets/clusters/{clusterId}`, `POST/DELETE /{id}/targets/groupinstances/{instanceId}`, `POST /{id}/preview` (sample-data render, no persistence, no tracking rewrite), `POST /{id}/proof`, `POST /{id}/send`, `GET /{id}/stats`, `GET /{id}/recipients` (paged). Reuse the **existing** `MemberClustersController` and `UserGroupInstancesController` list endpoints for the frontend's target picker rather than adding a redundant combined endpoint.

**Tests:** `MailingServiceTests.cs` (draft-edit rules, target add/remove, send recipient resolution/dedup/skip-no-email/re-run-picks-up-only-new, proof counter), `MailingTargetResolverServiceTests.cs` (cluster ∪ group-instance union + dedup), `MailTrackingServiceTests.cs` (click/open recording, unknown-token no-op).

**Critical files:** `Bluewater.Domain/Models/Mail/Mailing.cs`, `MailingRecipient.cs`, `MailingRecipientLink.cs`; `Bluewater.Core/Services/MailingService.cs`; `Bluewater.Core/Services/Mail/MailingTargetResolverService.cs`; `Bluewater.Api/Controllers/Api/MailingsController.cs`; `Bluewater.Api/Controllers/MailTrackingController.cs`; `Bluewater.Infra/Context/BluewaterContext.cs`.

---

## Phase 5 — Mailing tool frontend

Mirrors `routes/tools/groups/` (list + detail-with-sub-resources) and `NewsForm.svelte`'s compose pattern.

- `frontend/src/routes/tools/mail/mailings/+page.svelte` — list (status badge, sent count).
- `.../mailings/new/+page.svelte` — compose (create draft).
- `.../mailings/[id]/+page.svelte` — edit (if draft)/detail: target picker, proof/send actions, stats panel, recipient table.
- `frontend/src/lib/components/MailingForm.svelte` — subject input; sender `<select>` populated from a small new admin-only `GET /api/mail/senders` endpoint (lists configured `MailSenderOptions` keys/display names — senders live in server config, not the DB, so the picker needs this lookup); template `<select>` (`GET /api/mailtemplates?kind=Mailing`, pre-fills subject/body on pick); layout `<select>`; body `<textarea>` with a client-side `marked`-based preview acceptable here (pre-token-substitution editing aid — the authoritative check is the `/preview` endpoint).
- `frontend/src/lib/components/MailingTargetPicker.svelte` — multi-select over existing cluster/group-instance list endpoints, filtered to the current season for group instances (reuse whatever "current season" filtering `routes/tools/groups/instances` already does).
- `frontend/src/lib/components/MailingStats.svelte` — sent/opened counts, open rate, per-link click table.
- Proof button → `POST /{id}/proof`, shows updated `proofSendCount`. Send button → confirmation dialog before calling `POST /{id}/send`.

**Critical files:** `frontend/src/routes/tools/mail/mailings/[id]/+page.svelte`, `frontend/src/lib/components/MailingForm.svelte`, `MailingTargetPicker.svelte`, `MailingStats.svelte`.

---

## Cross-cutting notes

- **New permissions**, added in order to `Bluewater.Domain/Models/Groups/BluePermission.cs`: `AdminModifyMailTemplates = 29`, `AdminModifyMailings = 30`. Add both to the appropriate admin group's seed data in `BluewaterContextSeeder`.
- **After every phase that changes a controller/DTO**, run the root `CLAUDE.md`-mandated sequence: start the API in the background, `pnpm generate:api` in `frontend/`, fix resulting TypeScript errors, stop the API — before starting that phase's frontend slice.
- **New NuGet packages** to add (none currently used in this repo): `MailKit` (Phase 1), `Hangfire.AspNetCore` + `Hangfire.PostgreSql` (Phase 1), `Markdig` (Phase 2), `AngleSharp` (Phase 4). Pin exact versions at implementation time and sanity-check `Hangfire.PostgreSql`'s Npgsql version range against this repo's pinned Npgsql version (.NET 10 is very new).

## Verification

- **Phase 1:** trigger the smoke-test job from the Hangfire dashboard (`/hangfire`, behind auth) and confirm a message lands in the local SMTP catcher (smtp4dev/Mailhog).
- **Phase 2:** create a layout + template via the new admin UI, call the preview endpoint, confirm rendered HTML/plaintext match expectations; run `MergeTokenRendererTests`.
- **Phase 3:** trigger user creation in the running app (dev seed or signup flow) with a seeded welcome template, confirm the welcome mail arrives in the SMTP catcher with correct merge values and any attachment; run `dotnet test --filter MailServiceTests|UserServiceTests`.
- **Phase 4:** create a draft mailing targeting a test cluster/group instance, send a proof, confirm sample-data rendering; run `Send`, confirm recipients are created/deduplicated, click a tracked link and open the pixel URL manually, confirm `MailingRecipientLink`/`MailingRecipient` state updates and `GET /{id}/stats` reflects it; re-run `Send` after adding a new member to a targeted cluster and confirm only the new member gets a fresh send.
- **Phase 5:** drive the full compose → target → proof → send → stats flow in the browser against the local SMTP catcher.
