# Sign-up List Feature — Functional Specification

> Written from source-code analysis of the existing GYAS site implementation.
> Intended for use by an implementing team rebuilding the application.

---

## 1. Overview

The sign-up list feature lets administrators create registration forms for activities or events. Members browse available sign-ups, register themselves (and optionally others), fill in custom fields, and can later update or cancel their registration. Administrators control access, capacity, deadlines, and form structure.

---

## 2. Core Concepts

### 2.1 Signup

A signup is a registration list for a single activity. It has:

 - id
 - title
 - description
 - category_id
 - end_date (deadline)
 - allow_multiple (if multiple responses by the same user are allowed)
 - max_signups (nullable, can be unlimted)
 - max_waitlist (nullable, can be unlimted)
 - hide_signups (hides indvidivual repsonses in the public facing part)
 - anonymous (if enabled, show responses but not by who they were submitted)

A signup disappears from the public listing x days after `end_date`. Configured using appSettings. But should still be visible in a list of signups for a user (to see what signups they signed up for).

### 2.2 SignupResponse

A single registration by one member on a signup

- id
- signup_id
- user_id
- reservation (entries added by admins that are marked as reservations)

Entries are ordered by: reservation entries first, then chronologically by the date they were created.

### 2.3 Custom Input Field

A signup can have zero or more input fields for the responded to input more information. Each field has:

 - id
 - title
 - note (additional label)
 - type (enum)
 - options (json)
 - visible (if this entry is visible in the public list of signups on a signup)
 - sort order (used to sort on display)

Types are as following:
 - Checkbox
 - Checkbox list
 - Radio list
 - Textbox
 - Textarea (multiline version of textbox)
 - Other member (used to select an other member that has atleast one active group this season)

Inputs marked `public=false` are only visible to admins and to the registrant themselves.

### 2.4 SignupAccessGroup (Clusters)
Cluster of users. For functionality like signups and mail list we need to be able to define a set of users. A cluster is a group of members spanning serveral groups or categories, or other requirements. In a nutshell it's a method to select a certain set of users. If a member is inside a cluster can be defined by:
 - a Group category (are they part of a group in the category)
   - (and optionally) the role within that group category (do they have a certain role within a group of this category)
 - A single group (Single group, like ELJD)
 - Membership status (to be implemented later)

 How would you implement this feature? Store the clusters, or let users define them themself when creating a signup / mail list. Premade clusters could be useful (Active Members, Active Rowers, Active coaches, etc). Should cluster membership be computed on demand and then stored (for chacing)?


### 2.7 Category

Signups are grouped into categories for display. Each category has a title and an optional `hidden` flag. Hidden categories are only visible to admins. Categories have a `sort_order` controlling their display sequence.

---

## 3. Input Field Types

| Type | Description | Stored value |
|---|---|---|
| `checkbox` | Single yes/no toggle | Integer: 0 or 1 |
| `radiobutton` | Mutually exclusive choice from a list | Integer index into options array |
| `dropdown` | Same as radiobutton, but rendered as a `<select>` | Integer index |
| `checkbox_group` | Multiple choices from a list | Serialized array of selected indices |
| `multiselect` | Same as checkbox_group, rendered as a multi-select box | Serialized array |
| `radiobutton_limited` | Radio buttons where each option has a per-option capacity cap; options are disabled when full | String (option key) |
| `checkbox_matrix` | 2-D grid: vertical rows × horizontal columns, multiple cells selectable per row | Serialized 2-D array |
| `radiobutton_matrix` / `radiobutton_group` | 2-D grid, only one column selectable per row | Serialized 2-D array |
| `textarea` | Long free-form text | String |
| `textfield` | Short free-form text (max 255 chars) | String |
| `numberfield` | Numeric value with optional default, min, max, and +/- buttons | Integer/float as string |
| `selectuser` | Autocomplete picker for a single registered member | User ID (integer) |
| `selectteam` | Autocomplete picker for multiple members (a team) | Serialized array of user IDs |
| `timerange` | Two-value time selector (from hour to hour), constrained within admin-defined limits | Serialized `{begin, end}` |

> **Recommendation for the rebuild:** Model these as a polymorphic `FieldDefinition` entity with a `type` discriminator and a JSON `config` column, rather than serialized PHP data. This avoids deserialization coupling and makes querying and validation much easier.

---

## 4. Capacity and Waitlist Logic

When `has_maximum=true`:

```
Total spots = maximum + (has_spares ? spares : unlimited)
```

- Entries 1 … maximum → type `Ingeschreven`
- Entries maximum+1 … maximum+spares → type `Reserve`
- When all spots including spares are taken, new registrations are blocked

When a member cancels, entries shift up automatically in display order (they are sorted by `entered_date`), but the system does **not** automatically promote reserve entries to confirmed — promotion happens through the natural sort on next display. 

> **Improvement:** Implement an explicit promotion step: when an entry is deleted and `nr_entries` drops below `maximum`, automatically notify the first reserve entry holder and mark their entry as confirmed.

The `reservation` flag on an entry is a manual admin override (separate from the automatic reserve queue) used to mark entries that were pre-allocated outside the normal flow.

---

## 5. Entry Lifecycle

### 5.1 Allowed to register?

All of the following must be true:
1. The signup has not passed `end_date`
2. The maximum + spares has not been reached
3. The current user is in at least one access group
4. If `multiple=false`: the user does not already have an entry
5. If `other=false`: the registrant must be the current user

### 5.2 Allowed to update an entry?

1. `updateable=true` on the signup
2. `end_date` has not passed
3. Current user is the registrant (`user_id`) **or** the creator (`entered_user`)
4. The signup has custom inputs (no point showing an empty form)

### 5.3 Allowed to delete an entry?

1. `deleteable=true` on the signup
2. `end_date` has not passed
3. Current user is the registrant or the creator

### 5.4 Registering for someone else

If `other=true`, the registration form includes an autocomplete user-search field. The selected member becomes `user_id` (the registrant), while the logged-in user becomes `entered_user` (the creator). Both fields are tracked separately in the entry.

---

## 6. User-Facing Flows

### 6.1 Browse signups

- Page shows all accessible, non-hidden signups grouped by category
- Each row shows: title (link), end date, current count, maximum (or "unlimited"), and a status icon (registered / not registered / deadline passed)
- Past signups (beyond grace period) are hidden by default; optionally shown collapsed

### 6.2 View a signup

Shows:
- Title and formatted description
- Entry list, grouped by type ("Ingeschreven", "Reserve", "Gereserveerd")
- Entry numbers
- Member name (or "anonymous" if `anonymous=true`)
- Public custom field values
- Edit link per entry (only shown to the entry owner or creator)
- Sidebar: entry count, maximum, deadline, and action buttons

### 6.3 Register / Edit entry

Form contains:
- Name field (disabled unless `other=true` for the current user)
- All custom inputs defined on the signup
- Submit button ("Toevoegen" or "Wijzigen")
- Delete button for existing entries ("Verwijderen", with confirmation dialog)

After submit: redirect back to the signup detail view.

---

## 7. Admin Flows

### 7.1 Create a signup (wizard)

Multi-step form: **Init → General → Groups → Options → Inputs → Summary → Finish**

**Step: Init** — choose starting point:
- Blank signup
- Copy from an existing template (inputs, groups, options all pre-filled)
- Copy from an existing signup (same as template but from a real past event)

**Step: General**
- Title, description, category, linked agenda item, end date + time, visibility (hidden/visible)

**Step: Groups**
- Add/remove access groups (cluster references)
- Optionally limit a group to a specific season

**Step: Options**
- Set all signup option flags (capacity, reserve, updateable, deleteable, etc.)

**Step: Inputs**
- Add/remove/reorder custom input fields
- For each field: set title, type, options, note, public flag
- Field type can be changed; options are preserved for compatible type changes, reset for incompatible ones

**Step: Summary**
- Read-only review of all settings

**Finish** — signup is persisted. If not hidden, a push notification is sent to all members.

### 7.2 Edit an existing signup

Same sections as creation (general, groups, options, inputs, entries), each editable independently. Changes to options that remove fields already filled in by members must be handled carefully (do not silently discard entry data).

> **Improvement:** When deleting a custom input field that already has entry data, warn the admin explicitly and require confirmation, rather than silently soft-deleting it.

### 7.3 Manage entries (admin)

- Full entry list including non-public fields, entered_date, entered_user
- Sortable by any column (sort state persisted in session)
- Add entry for any member (`other` restriction does not apply to admins)
- Edit or delete any entry regardless of `updateable`/`deleteable` flags
- Mark an entry as reservation
- Print view and Excel export

### 7.4 Manage templates

Same wizard as signup creation but without `end_date` or visibility. Templates do not appear in member-facing pages. Admins can create, edit, and delete templates. Templates are grouped by category.

### 7.5 Manage categories

Create, rename, reorder, and delete categories. Hidden categories are usable but not shown to members.

---

## 8. API Endpoints

The feature exposes a REST-style JSON API.

### `GET /api/signup`

Returns a list of all accessible signups.

```json
{
  "signups": [
    {
      "id": 1,
      "title": "...",
      "agenda_id": 42,
      "hidden": false,
      "end_date": 1700000000,
      "entered_date": 1699000000,
      "category": "...",
      "nr_entries": 12,
      "hasPassedEndDate": false,
      "user_has_entry": true,
      "options": {
        "anonymous": false,
        "has_maximum": true,
        "maximum": 20,
        "multiple": false,
        "other": false,
        "allows_spares": true
      }
    }
  ]
}
```

### `GET /api/signup?signup_id=X`

Returns the signup detail including entries and inputs.

```json
{
  "signup": {
    "id": 1,
    "title": "...",
    "description": "...",
    "category": "...",
    "nr_entries": 12,
    "end_date": 1700000000,
    "user_has_entry": true,
    "entryIsAllowed": false,
    "hasPassedEndDate": false,
    "options": { ... },
    "inputs": [
      {
        "id": 5,
        "title": "Diet preference",
        "type": "radiobutton",
        "note": "...",
        "public": true,
        "order": 1,
        "config": { "options": [{"text": "Meat", "value": "Meat"}, ...] }
      }
    ],
    "entries": [
      {
        "id": 101,
        "type": "Ingeschreven",
        "user": { "id": 42, "fullname": "...", "photo_url": "..." },
        "user_is_allowed": true,
        "entry_inputs": [
          { "id": 201, "input_id": 5, "text_value": "Meat" }
        ]
      }
    ]
  }
}
```

### `GET /api/signup?signup_id=X&update=entries[&entry_id=Y]`

Returns the entry form data for a new entry or for editing an existing one.

### `POST /api/signup?signup_id=X&update=entries`

Create or update an entry. Body:
```json
{
  "signup_id": 1,
  "entry": {
    "user_id": 42,
    "input": {
      "5": 1
    }
  }
}
```

> **Improvement:** The current API has no `POST /api/signup` for creating signups. Admin signup management should be added as a full CRUD API to allow mobile/app-based administration.

---

## 9. Push Notifications

When a new signup is created and `hidden=false`, a push notification is sent with payload:
```json
{
  "type": "signup",
  "signup_id": 1,
  "title": "..."
}
```

> **Improvement:** Also send a reminder notification N days before `end_date` to members who are in an access group but have not yet registered.

---

## 10. Data Model (Simplified)

```
signup_categories
  id, title, hidden, sort_order, active

signup  (both signups and templates; template=1 for templates)
  id, title, description, agenda_id, category_id, end_date, hidden, template, options, active
  entered_date, entered_user, removed_date, removed_user

signup_cluster_relations  (access groups)
  id, signup_id, cluster_id, season_id, active

signup_input_relations  (custom fields)
  id, signup_id, input_id, title, options, note, public, sort_order, active

signup_inputs  (field type registry — static, rarely changes)
  id, title, description, sort_order, active

signup_entries
  id, signup_id, user_id, reservation, active
  entered_date, entered_user, removed_date, removed_user

signup_entry_input  (field values per entry)
  id, entry_id, input_relation_id, value, active
  entered_date, entered_user
```

> **Improvement:** Replace serialized PHP values in `options` and `value` columns with JSON. This makes the data portable, queryable, and safe to deserialize in any language.

---

## 11. Key Business Rules Summary

1. A signup without access groups is inaccessible to all members.
2. Deadline enforcement is server-side; clients must not be trusted.
3. The `reservation` flag is admin-only; members cannot set it.
4. Anonymous signups still track the member internally — only the display name is hidden.
5. Templates are never visible to members, even if `hidden=false`.
6. Signups linked to agenda items appear in the agenda detail view in addition to the signup list.
7. After `end_date`, the signup remains readable (for N days) but accepts no new entries.
8. Entry input values for deleted entries are retained in the database (soft delete only).
9. The sort order of entries in the admin list is session-scoped, not persisted per user.

---

## 12. Recommended Improvements Not Yet Implemented

| # | Description |
|---|---|
| 1 | **Automatic reserve promotion** — notify and confirm first reserve entry when a spot opens up. |
| 2 | **Reminder push notification** — remind unregistered eligible members N days before `end_date`. |
| 3 | **Admin signup creation API** — enable managing signups through the API, not just web UI. |
| 4 | **Input deletion warning** — warn admins when deleting a field that already has member responses. |
| 5 | **JSON instead of serialized PHP** — store `options` and entry `value` fields as JSON for portability. |
| 6 | **Waitlist email** — send email confirmation when a reserve entry is automatically promoted. |
| 7 | **Entry history** — track edits to entries with timestamps (currently only creation is logged). |
| 8 | **Persistent sort preference** — persist the admin list sort order per user in the database. |
