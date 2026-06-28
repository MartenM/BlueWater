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
 - allow_delete (if a user can delete it's own response)
 - allow_update (if a user is allowed to update the response)
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
 - numberfield with min max
 - Textarea (multiline version of textbox)
 - Other member (used to select an other member that has atleast one active group this season)

 
> **Recommendation:** Model these as a polymorphic `FieldDefinition` entity with a `type` discriminator and a JSON `config` column. This avoids deserialization coupling and makes querying and validation much easier.

Inputs marked `public=false` are only visible to admins and to the registrant themselves.


### 2.4 Signup Access (Clusters)
Accesses is defined using clusters. A signup links to one or more cluster. If a user in one the clusters he/she has access to the signup and can view, and respond in the public facing part.


### 2.7 SignupCategory

Signups are grouped into categories for display. Each category has a title and an optional `hidden` flag. Categories have a `sort_order` controlling their display sequence.


## 4. Capacity and Waitlist Logic

```
Total spots = max_signups + max_waitlist
If signups or waitlist is unlimted that means there is no max.
```

- Entries 1 … max_signups → type `Ingeschreven`
- Entries max_signups+1 … max_signups+max_waitlist → type `Wachtlijst`
- When all spots including spares are taken, new registrations are blocked (if there is max ofcourse)

When a member cancels, entries shift up automatically in display order (they are sorted by creation date).

The `reservation` flag on an entry is a manual admin override (separate from the automatic reserve queue) used to mark entries that were pre-allocated outside the normal flow. These should be on the top of the list when ordering the list (so order by reserved, then entry date)

---

## UI
There should be two frontend UI's for signsup. 
A member facing part where members can:
 - view available signups, 
 - view the signup
 - respond to a signup.
 - View which signups they responded to 
 
  The 2 permissions are used:
 - SignupView
 - signupRespond

And a admin page (tools), to create, edit and manage (view repsones, export to excel/csv, delete responses, mark response as reservation). It should use the following to permissions:
 - AdminSignupView
 - AdminSingupModify (Allow creating and managing signups created by the current user)
 - AdminSignupModifyOthers (Allow creating and manging all signups)

