# Bluewater Style Guide

This guide defines how Bluewater should look and feel. It exists because this frontend will be
deployed for multiple rowing associations, starting with **A.G.S.R. Gyas** (gyas.nl) — so brand
values must stay swappable, not hardcoded into components.

## Theming model

All brand-specific values are defined once, as Tailwind v4 theme tokens in
`src/routes/layout.css`:

```css
@theme {
	--color-primary: #ed8b00;
	--color-primary-hover: #c97600;
	--color-primary-content: #ffffff;
	--font-sans: 'Inter Variable', system-ui, sans-serif;
}
```

To retheme for a different association: change these CSS variables and swap the logo file under
`static/images/` (currently `gyas-groningen.svg`). No component code should need to change.
Components must always reference the token (`bg-primary`, `text-primary-content`, etc.), never a
literal hex color.

This is build-time theming: each association gets its own deployment with its own values. If we
later need one deployment to serve multiple associations at runtime, revisit this — don't
preemptively build that now.

## Color

- **Primary**: `--color-primary` (`#ed8b00`, orange) — buttons, links, active/selected states,
  focus rings, small accents. This is the one color most likely to change per deployment.
- **Background**: white. No dark mode.
- **Neutrals**: Tailwind's default `gray` scale for body text, borders, and surfaces.
- **Status colors** (errors, success, warnings on forms/bookings): use Tailwind's default
  `red`/`green`/`amber` utilities directly. We don't have enough of these flows built yet to
  justify defining a dedicated semantic palette — revisit once patterns repeat.

### Contrast rule (important)

`#ed8b00` on white is roughly 2.4:1 — it **fails WCAG AA** for text (needs 4.5:1 for normal text,
3:1 for large text/icons).

- ✅ White text/icons on an orange background (buttons, badges).
- ✅ Orange as a border, underline, or decorative accent.
- ❌ Orange text or small orange icons directly on a white background.

If something needs orange-ish emphasis directly on white, use `--color-primary-hover`
(`#c97600`, darker) and still check contrast — don't reach for `--color-primary` as a text color.

## Typography

- **Font**: Inter (variable), self-hosted via `@fontsource-variable/inter` — no external font
  request. Falls back to the OS UI font stack.
- Replaces the old site's Calibri/Arial/Verdana stack with something built for on-screen UI
  density, while staying just as neutral/functional.
- Use Tailwind's default type scale (`text-sm`, `text-base`, `text-lg`, `text-xl`, `text-2xl`,
  …). Don't introduce a custom scale.

## Layout & density

The site spans two contexts with different density needs — don't force one density everywhere:

- **Guest/marketing pages** (home, about, join): more generous spacing, hero sections, card
  grids for news/events — similar to gyas.nl.
- **Member/functional pages** (schedules, bookings, rosters): favor information density —
  tables and compact lists. Not spreadsheet-dense, but don't pad these out with whitespace just
  for visual consistency with marketing pages.

Use cards for content that's meant to be browsed (news, events, listings) and
tables/lists for content that's meant to be scanned or compared (schedules, rosters, bookings).

## Navigation

Single top nav bar, used consistently across guest and member areas — not the old site's
left-sidebar member portal. Group related member-area pages under top-level nav items with
dropdowns rather than introducing a separate nested sidebar.

## Components

- **Shape**: small border radius (`rounded-md`) on buttons, cards, and inputs. Not sharp
  corners, not pill-shaped.
- **Buttons**:
  - Primary action: solid `bg-primary`, `text-primary-content`, `rounded-md`. One primary button
    per view/section — don't let multiple solid-orange buttons compete for attention.
  - Secondary/tertiary actions: neutral gray outline or ghost style.
- **Cards**: white surface, subtle border or `shadow-sm`, `rounded-md`.
- **Forms**: built on `@tailwindcss/forms` base styles; focus rings use `--color-primary`.

## Icons

[Lucide](https://lucide.dev) via `@lucide/svelte` — a consistent stroke-based icon set. Default
icon size should match adjacent text size; avoid mixing in icons from other sets.

## Logo

Reference logo lives at `static/images/gyas-groningen.svg`. Other associations' logos replace
this file (or the reference to it) per deployment — keep the same aspect-ratio expectations
(used in the nav header) when swapping.
