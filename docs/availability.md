Availability Planner

This is a separate, lighter-weight feature from the Outing Planner it lets team members mark, on a weekly grid, when they're generally around to row — independent of any specific scheduled outing.

Access: Reached via a "Beschikbaarheid" (Availability). A user picks one of the teams ("ploegen") they belong to from a sidebar list (grouped by season); their last-picked team is remembered per user.

Main view (per group instance with outing planner permission): A week-by-week grid (with next/previous week navigation), one table per day, one row per team member (grouped/labeled by role — e.g. rower, cox, coach). Each row is a horizontal timeline (roughly 6:00–23:00, configurable in the frontend) where the member's available time blocks are drawn as colored bars.

Setting your own availability:
- Only your own row is editable
- Click-and-drag on your row's timeline to draw a new availability block.
- Drag a block's left/right edge to resize it, drag the middle to move it.
- Double-click a block (or tap, on mobile) to open a small popup with start/end time dropdowns (15-minute increments) and a delete button.
- Right-click a block to delete it directly.
- Overlapping/adjacent blocks on the same row automatically merge into one; blocks snap to a 15-minute grid.
- Changes save automatically per day (no explicit "save" button — edits are pushed to the server as you edit that day's row).
- availability is saved per user and is shared across all groups (so just saved under a users id, not group instance + userid)

Additionally, create a page where the user can add their availabiltiy independend of any group instance.

Purpose: This is a planning aid for the outing planner (can be a coach, rower, anyone) — it doesn't itself create outings or assign roles, it just shows a rolling picture of "who tends to be free when" so training times can be chosen sensibly. It's distinct from per-outing role selection (rower/cox/coach/unavailable) in the Outing Planner, which is about a specific, already-scheduled session.