# Phase 1 Design — Debug Panel Live Log UX

**Date.** 2026-05-28
**Branch.** `worktree-debug-ui-buildout` (forked from `feat/drag-drop-router-mockup` @ `541fcf9`)
**Scope.** Replace the current freeform-div Live Log with a filterable, searchable, scroll-locked event browser. Pure frontend — no backend or `DebugTrace` changes.

## Problem

After Phase 0 (this session) wired `panel-cip` raw CIP signals into `DebugTrace`, the `/events` poll now streams 20–50+ events per second during slider drags. The current Live Log card is a freeform scrolling div with only "clear" and "pause" controls. With the flood live, it's unreadable — you can't isolate "what did Camera 1 do in the last 5 seconds" without scrolling through hundreds of `panel-cip` heartbeat lines.

## Goal

Make the Live Log usable as a diagnostic instrument:

- **Filter by event type** (sig_change / command / response / error / lifecycle / state_change)
- **Filter by device** (auto-populated from observed devices: panel-cip, panel, cam-1, cam-2, p300, mxa-a, mxa-b, system, …)
- **Search** the rendered event JSON by substring
- **Scroll-lock** so reviewing history doesn't get yanked by new events
- **Click row to expand** full JSON inline
- **Sticky header** keeps filter controls visible while scrolling
- **Counts** (total / filtered) shown in header
- **Copy** filtered view as JSON-lines to clipboard

## Out of scope (deferred)

- Correlation-id grouping (collapse command+response pairs into threaded rows) — Phase 5
- Persisting filter state across page reloads — Phase 6 polish, one localStorage key
- Time-range filtering — defer until use case demands it
- Server-side filtering (e.g., `events?type=command`) — keep server simple; all filtering client-side over the in-memory event array

## Architecture

### Single-page, no framework

The existing UI is vanilla HTML/JS/CSS with no framework. Keep that. Phase 1 adds:

- New DOM structure inside `#log-card` (chip rows, search input, scroll-lock toggle, copy button, compact row layout)
- New CSS rules for chips and compact rows
- New JS state: `eventsAll[]` (ring of all polled events), `typeFilters`, `deviceFilters`, `searchTerm`, `scrollLocked`, `deviceCounts`
- New JS functions: `renderLog()` (filter + render), `updateChips()` (recompute counts + populate device row), `onScrollEvent()`, `expandRow()`

### Filter logic

All filtering client-side. Each polled batch from `/events` appends to `eventsAll[]`. Render pass iterates `eventsAll` and tests each:

```
visible(event) =
    typeFilters.has(event.eventType)
    AND (deviceFilters.size == 0 OR deviceFilters.has(event.device || 'system'))
    AND (searchTerm == '' OR JSON.stringify(event).toLowerCase().includes(searchTerm.toLowerCase()))
```

Multi-select chips behave as OR within a row, AND across rows. An empty filter set on the device row means "all devices" (so a user who never touches the device row sees everything).

Type filter set is fixed at boot (six known types). Device filter set is populated dynamically — when a new device first appears in events, a chip is added (default: on). Chip ordering: device-row sorted alphabetically with `system` last.

### Search

Single input. 200ms debounce on keystroke. Lowercase substring match against `JSON.stringify(event)`. No regex, no field-specific search — substring is enough for diagnostic work and avoids a small-grammar parser.

### Scroll lock

Default off (auto-scroll to bottom on every render that adds visible rows). Toggle button at the top of the log body. When locked, new events still append to DOM but the scroll position is preserved. A "↓ jump to bottom" button appears that scrolls to bottom and re-engages auto-scroll. When the user manually scrolls up past a small threshold, auto-engage scroll-lock so they can read uninterrupted.

### Row layout

Four columns in a CSS grid: `timestamp | type | device | body`.

- `timestamp` — `HH:MM:SS.mmm`, monospace, muted color
- `type` — short label (`sig_change`, `command`, etc), color-coded per type
- `device` — string, muted
- `body` — one-line summary built per event type:
  - `sig_change`: `{signal} {signalType}={value}`
  - `command`: `method={method} · corr={correlationId}`
  - `response`: `raw={raw} · corr={correlationId}`
  - `error`: `message={message}`
  - `lifecycle`: `message={message} · ...` (key-value pairs joined)
  - `state_change`: `{property}={value}`

Click a row → toggle an inline detail row below with the full JSON, pretty-printed.

### Header & counts

Sticky `position: sticky; top: 0` inside the log card. Three lines:

1. Title + meta + actions: `Live Log · {totalEvents} events · {visibleEvents} visible · poll {ok/err} · clear · copy`
2. Type chip row
3. Device chip row + search + scroll-lock + jump-to-bottom

### Polling unchanged

The existing `fetch('./events?since=N')` poll runs unchanged. On each response, append new events to `eventsAll`, update device chip set if a new device appears, then `renderLog()`.

### Memory ceiling

Cap `eventsAll` to the last 5000 entries (5× the server ring) to avoid unbounded DOM growth. On overflow, drop oldest and re-render. Visible to user only as the "X events" count plateauing at 5000.

## Error handling

- Poll fetch failures: keep the existing "poll-status" pill behavior (red `error` pill). No change.
- `JSON.parse` of an entry failing: skip that entry, log to console. Already covered by `try/catch` in current code.
- `localStorage` access errors: N/A — Phase 1 has no persistence.
- Clipboard API failure on `copy`: fall back to a hidden textarea + `document.execCommand('copy')`, then show a brief toast.

## Testing

No formal test suite for the embedded resources. Manual verification per acceptance:

1. **Load the page after deploy.** Header chips show six types and the devices observed since boot. All chips default "on". Log shows live stream.
2. **Toggle off `panel-cip`.** Slider drags on the panel no longer add visible rows. Counts in `panel-cip` chip keep climbing (still received), but visible total stays small.
3. **Type "join-18494" in search.** Only events whose JSON contains that substring remain. Clear search → all return.
4. **Scroll up manually.** Auto-scroll lock engages; new events accumulate without yanking view. "↓ jump to bottom" button scrolls to bottom and resumes auto-scroll.
5. **Click an event row.** Inline JSON detail appears below. Click again → collapses.
6. **Click `copy`.** Clipboard contains the visible filtered rows as JSON-lines (one event per line).
7. **Stress test.** Drag a panel slider continuously for 30s with `panel-cip` chip off. Browser stays responsive; counts climb but visible rows stable; no scroll glitches.

## File touchpoints

- `MCCCD-AA140-SIMPL/debug-ui-src/debug.html` — replace `#log-card` markup with the new structure
- `MCCCD-AA140-SIMPL/debug-ui-src/debug.css` — add chip / row / sticky-header styles
- `MCCCD-AA140-SIMPL/debug-ui-src/debug.js` — replace the existing log append-on-poll path with the new render pipeline (state + filters + rendering)

No backend changes. No `.csproj` change (the embedded-resource manifest already covers these three files).

## Deploy

`dotnet build -c Release` then `PROC_HOST=192.168.2.198 python MCCCD-AA140-SIMPL/scripts/deploy.py …`. The cleanup handler from `checkpoint-program-status-cleanup` will Unregister the old `HttpCwsServer` cleanly during PROGLOAD, so no stale-cache reboot should be needed.

## Commit + tag

Single commit. Tag: `checkpoint-debug-log-ux`.
