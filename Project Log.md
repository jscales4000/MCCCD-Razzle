# MCCCD Razzle — Project Log

---

## v0.8.0 — Source-First Consolidation + Merge to main (2026-06-24)
**Agent:** Claude Code (claude-opus-4-8, 1M)
**Session type:** Feature decision + consolidation + tap-highlight fix + merge
**Branch:** `feat/home-source-select-toggle` @ `287ac31` → **merged to `main`**
**FRED project:** MCCCD-AA140 Touchpanel (`c1937681-e57d-4354-aa58-a5b0f6e9ca23`)
**FRED status:** healthy.

### Summary
Jordan picked **Workflow B (source-first "paint")** after the on-glass A/B. Made
source-first the **sole** Home workflow, commented out the destination-first
(Workflow A) code for now (production deletes it), removed the A/B toggle, fixed
the CH5 tap-highlight on the new controls, deployed to `.80`, and merged the
branch to `main`.

### Done
- **Source-first is now the only workflow.** Removed the toggle from `Home.svelte`;
  pinned `homeRouteMode` to `'source'` in `session.ts`. All Workflow-A paths
  commented + tagged `RETIRED; delete in production` (handlers, deriveds, onMount
  reset, toggle markup + CSS, A caption, `targeted`-chip logic, reduced-motion
  rule). Source-tap now *arms*; aria-label updated.
  - `homeRouteMode` deliberately **kept as a store** (not deleted): `router.ts`'s
    module-level click-outside-disarm guard reads `$homeRouteMode === 'source'`
    to no-op on Home. Production cleanup drops the store **and** that guard check
    together.
- **Tap-highlight "blue overlay on press" fixed** (FRED `66d80765`): shared rule
  in `src/global.css` gives `.hero-card`/`.disp-chip`/`.send-all`
  `-webkit-tap-highlight-color: transparent` + `user-select:none`. `.mode-seg`
  excluded (toggle removed).
- **Verify:** `svelte-check` clean except the 2 known pre-existing problems
  (`MicVolumeModal:64`, `ConfirmShutdownModal:29`). Built + **deployed to
  tabletop `.80`** (PROJECTLOAD OK).
- **Merged** `feat/home-source-select-toggle` → `main`.

### Commit
- `287ac31` feat(home): make source-first the sole workflow; comment out destination-first.

### FRED
- Handoff doc `2a9138d2` created.
- Tasks: `66d80765` todo → **review** (tap-highlight); `d084d25c` review, updated
  with the source-first decision; `391d7a70` **created** (button-press latency —
  next work).

### Next
- **Button-press latency** (`391d7a70`): branch `perf/button-press-latency` off
  `main`, adversarial-dev a fix for the on-glass tap lag.
- Jordan: on-glass confirm source-first-only build + tap-highlight gone on `.80`.
- Wall `.78` deploy when it returns.

---

## v0.7.0 — Home Source-Select Workflow Toggle + Backup (2026-06-22)
**Agent:** Claude Code (claude-opus-4-8, 1M)
**Session type:** Feature (Svelte panel) — brainstorm → spec → plan → subagent-driven build → debug
**Branch:** `feat/home-source-select-toggle` (pushed to `origin`)
**FRED project:** MCCCD-AA140 Touchpanel (`c1937681-e57d-4354-aa58-a5b0f6e9ca23`)

> **FRED status:** FRED was intermittently **unreachable/degraded** this session
> (`api_service: false`; Tailscale/Mac-Boogie services down). Live FRED catch-up
> + handoff were done from the repo's offline copies. A FRED handoff doc is
> **deferred** until FRED is back online.

### Summary
Built a live A/B toggle on the Home page to evaluate two source-selection
workflows on glass, then deployed both halves of the system and backed
everything up with explicit, named, revertable restore points.

### Feature — source-select workflow toggle (commits `549fc32`..`91a8bed`)
A toggle above the source row flips Home between two workflows:
- **A — Destination-first (unchanged):** narrow display chips → tap a source →
  routes to the targeted set → grouping resets. The original "multi-select"
  behavior, byte-for-byte.
- **B — Source-first "paint" (new):** tap a source to **arm** it (persistent) →
  tap display chips to route the armed source **immediately** (live feedback per
  tap) → **Send to All** shortcut → persists until a different source is armed.

Pure panel-side — rides existing `Display{N}Source` set + `Display{N}SourceFb`
feedback; no contract/processor change. Built with brainstorming → writing-plans
→ subagent-driven-development (7 tasks, per-task spec+quality review, opus
whole-branch review, one fix wave). State in `lib/stores/session.ts`
(`homeRouteMode`) + `lib/stores/router.ts` (`armForPaint`, `routeArmedToAll`,
reuses `armedSource`/`routeSource`); UI + animations in `pages/Home.svelte`.
`svelte-check` clean except the known pre-existing `MicVolumeModal.svelte:64`.

### On-glass bug fixed (commit `91a8bed`)
Source-first arm self-destructed: the module-level click-outside-disarm listener
in `router.ts` (an Advanced-Routing affordance keyed to `.chip`/`.tile`)
disarmed `armedSource` on the very tap that armed it (Home uses `.hero-card`/
`.disp-chip`) — "Send to All flashed then vanished," and chip painting dropped
the arm after one tap. Root-caused via static event-flow trace; fixed by guarding
the listener to no-op while `homeRouteMode === 'source'`. Advanced Routing
(default destination mode) unaffected.

### Deploy state
- Earlier this session: **processor `.cpz` rebuilt + redeployed** to RMC4 `.198`
  (slot 01, `PROGLOAD` OK; CWS debug live) and **panel redeployed** to TS-1070
  `.80`. The toggle feature is deployed to `.80` (`PROJECTLOAD` OK).
- **TSW-1070 wall `.78`: OFFLINE** all session — run `npm run deploy:wall` when
  it returns.

### Backup & revert (this entry's purpose)
Two annotated restore-point tags, both pushed to `origin`:
- **`pre-source-select-toggle-baseline` → `1879f0b`** — the **pre-toggle**
  (original multi-select / destination-first-only) state, = v0.6.0. Also the tip
  of `origin/main`.
- **`v-2026-06-22-source-select-toggle` → `91a8bed`** — the **current** state
  (toggle feature + bug fix), tip of `feat/home-source-select-toggle`.

**To revert the panel to pre-toggle behavior:**
`git checkout pre-source-select-toggle-baseline` (or `1879f0b`) →
`cd MCCCD-AA140 && npm run build && npm run deploy:both`. `main` is unchanged, so
the baseline is always reachable. The feature is isolated on its branch and not
merged to `main`.

### Open / next
- Decide finish path for the branch (merge / PR / keep) — was mid-decision.
- Tap-highlight ("transparent blue overlay on press"): the new controls
  (`.mode-seg`, `.send-all`, `.hero-card`, `.disp-chip`) don't inherit the
  `-webkit-tap-highlight-color: transparent` base rule used in `global.css`;
  documented follow-up to add it.
- Write the FRED handoff once FRED is reachable.

---

## v0.6.0 — UI Polish Pass + Full GitHub Backup (2026-06-13)
**Agent:** Claude Code (claude-opus-4-8, 1M)
**Session type:** UI polish / Crestron CH5 Svelte + repo backup
**Branch:** `main` (pushed to `origin/main`)
**FRED project:** MCCCD-AA140 Touchpanel (`c1937681-e57d-4354-aa58-a5b0f6e9ca23`)

> **Log gap note:** This log was last updated at v0.5.0 (2026-05-01). The
> intervening month of work — name-based contract fix, device-integration
> (USB-SW-400 host switch, D5 signage, debug ping), cameras v2 (multicam
> switch, framing/tracking, live VISCA coords, zones/profiles), projector
> screen relay, User/Technician PIN view modes, the Home display-select strip
> + Control-Source flag + realistic room map, and the IP address plan — landed
> in git history and FRED tasks but was never logged here. This entry resumes
> the log at the current HEAD and does not attempt to restate that history;
> see `git log` and FRED for the detail.

### Summary
Acted on a five-item UI punch list, created a reusable room-map skill + FRED
persona, synced FRED task state, then backed the entire repository up to GitHub
so it can be picked up cleanly on another machine.

### UI changes (commit `9d25813`)
- **Home — routing clears the grouping.** `routeSourceToTargets()` now calls
  `resetTargetDisplays()` after publishing, so the flow loops: pick display
  chips → tap a source → it routes → grouping resets to All → pick again. The
  10s quiet-period timer is kept only for sets picked but never routed. (Per
  explicit user direction — overrides the earlier timer-keep design.)
- **Volume popup centered.** `VolumePopup.svelte` moved from bottom-right to
  horizontally centered above the footer (`left:50%` + `translateX(-50%)`,
  rise keyframe updated to keep the centering transform).
- **Room map rework** (`RoomPlan.svelte`): conference table + chairs removed,
  replaced by 3 rows of front-facing classroom seats in left/right banks with a
  center aisle; Cam1 moved fully inside the front wall; both cameras redrawn as
  aimed PTZ units (body + lens stub on the aim side + translucent FOV wedge —
  Cam1 looks back over the seats, Cam2 toward the podium); mic live-state now
  lights the mic's own edge (green border + glow) instead of an external dot +
  dashed halo; mics centered over the seating banks.
- **Cameras page resync** (`Cameras.svelte`, `PresetButton.svelte`): retired the
  legacy slate `.glass-card` + hardcoded-blue look. Now matches the app theme —
  routing-style compact header (back button / room / eyebrow / online pill),
  navy `.cam-card` surfaces, and flat amber-token buttons across camera select,
  framing toggles, segment switch, zone/profile radios, zoom, presets, and Send
  to VTC. Also fixed the long-standing `leaveCameras('settings')` type error
  (signature is now parameterless); `svelte-check` is down to the single known
  pre-existing `MicVolumeModal.svelte:64` error.

### Skill + persona (commits `9d25813`, `f01e1f0`)
- New project skill `.claude/skills/gui-room-map/SKILL.md` — two-layer
  touch/scene architecture, % geometry + collision checklist, architectural
  idioms (aimed PTZ cams, classroom seating, edge-lit mics), feedback-only state.
- New FRED library persona **"GUI Room-Map / RCP Builder"**
  (`86ddf28e-1020-4104-bd83-91fdd052b635`, division `crestron`), assigned to the
  project at priority 8. Offline reference copy at
  `MCCCD-AA140/docs/personas/2026-06-11-gui-room-map-persona-mod.md`.

### FRED task sync
- `c6d01695` (Cameras `'settings'` type error) → **review** (fixed this session).
- New review task `b4dbc7c0` covering this session's batch for on-glass verify.

### Deploy state
- **TS-1070 (.80):** deployed (`546 KB` .ch5z, PROJECTLOAD OK, UI restarted).
- **TSW-1070 (.78):** OFFLINE — run `npm run deploy:wall` when it returns.
- Processor contract unchanged (panel-only session).

### Backup / GitHub
- `main` pushed to `origin/main` (upstream set); `origin/main` was ~70 commits
  behind and is now current at `f01e1f0`.
- Feature branches pushed for complete backup: `feat/device-integration-usb-signage`,
  `feat/screen-relay-and-view-modes`, `fix/name-based-contract`, and
  `feat/drag-drop-router-mockup` (was ahead 8).
- Untracked Excel crash-recovery artifact `MCCCD_AA140_Equipment_List(AutoRecovered).xlsx`
  left out of the repo intentionally (not project source; the canonical
  equipment-list xlsx is already tracked).

### Pick-up notes for another machine
1. `git clone https://github.com/jscales4000/MCCCD-Razzle.git` → `git checkout main`.
2. FRED project ID lives in `./CLAUDE.md` and `.fred.json`; load tasks + personas
   at session start.
3. Panel build: `cd MCCCD-AA140 && npm install && node build.mjs` (never
   `vite build` directly — `#` in the path breaks Rollup). Deploy with
   `npm run deploy:both`.

---

## v0.5.0 — CH5 Page-Type Mockup Sprint (2026-05-01)
**Agent:** Windsurf Cascade
**Session type:** Design / CH5 UX Exploration
**Branch:** uncommitted (mockups only, no Svelte changes)

### Summary
User requested a deep BMAD yolo sprint exploring entirely new page types and UI states that would exist inside a real CH5 project — going beyond the existing Home/Cameras/Settings pages to include states emulating CH5 presets and standard AV room patterns.

### New Mockups Delivered (12–17)
| # | File | Description |
|---|------|-------------|
| 12 | `12-splash-system-off.html` | Idle/boot splash — 88px room name hero, pulsing power-on CTA with ring animation, layered ambient glows, grid texture, bottom status strip |
| 13 | `13-audio-mixer.html` | Full 5-channel broadcast mixer — stereo VU bars, vertical faders, trim sliders, per-channel mute, master fader + output select, preset scenes |
| 14 | `14-display-routing.html` | CHV-style source→display matrix — source list with route badges, 3 display cells with screen mockups, drag-to-route, power/audio/mirror controls, mode selector |
| 15 | `15-shutdown-modal.html` | Occupancy-triggered shutdown modal — animated SVG countdown ring, danger stripe, backdrop blur, shutdown checklist, vacancy context bar |
| 16 | `16-room-schedule.html` | Room booking / calendar — live session progress bar, extend/end actions, attendee avatars, pixel-accurate hour-axis timeline with free/booked blocks and "now" line |
| 17 | `17-tech-support.html` | Tech support dashboard — 3-col layout: live device diagnostics (10 devices), issue-guided quick-fix actions, QR help desk + emergency call button |

### Design Decisions
- All new mockups use the established MCCCD orange palette (`#f5a623`, `#0d1b2e`)
- Each mockup is a fully self-contained `1280×800` HTML/CSS file — no dependencies
- Font/icon sizing varied per page: mixer uses compact 11–13px labels; splash uses 88px hero; routing uses 13–14px comfortable readable sizes
- `index.html` updated: count bumped to 17, subtitle revised, 6 new cards added

### Open Opportunities
- Mockup 13 (mixer) could become a real `AudioMixer.svelte` page with SIMPL+ fader signal bindings
- Mockup 14 (routing) maps cleanly to a `DisplayMatrix.svelte` driven by analog routing signals
- Mockup 16 (schedule) could integrate with Exchange/Google Calendar via a web service join in SIMPL#
- Mockup 15 countdown modal already mirrors `ConfirmShutdownModal.svelte` — SVG ring variant could replace the current bar

---

## v0.4.0 — MCCCD Orange Theme Integration (2026-05-01)
**Agent:** Windsurf Cascade
**Session type:** Design / Color Scheme Integration
**Branch:** uncommitted (mockups + global.css only, no Svelte component changes)

### Summary
User provided a photo of an existing MCCCD panel in another campus room running an amber/orange color scheme (`#f5a623` accent, `#0d1b2e` navy bg). Integrated that palette across the entire project:

### global.css changes
- Added `--color-accent-orange` family of vars to `:root` (7 new variables)
- Added `.theme-orange` override class — applying `class="theme-orange"` to `#app` in App.svelte switches the entire panel accent from cyan to amber with zero component edits
- Replaced all hardcoded `rgba(56,189,248,...)` values in `.btn`, `.btn.active`, `.btn.primary`, `.icon-btn.active` with `color-mix(in srgb, var(--color-accent) %, transparent)` — fully theme-aware

### Mockup updates
- All 10 existing mockups: `--accent` swapped `#38bdf8` → `#f5a623`, `rgba(56,189,248,...)` → `rgba(245,166,35,...)`, bg `#0b1220` → `#0d1b2e`
- New `11-orange-theme.html`: Full Synthesis layout in amber, includes a **palette comparison strip** at the bottom (amber swatches vs cyan swatches side by side)
- `index.html`: all cyan vars updated to orange, subtitle updated to "11 concepts", mockup 11 card added

### Extracted palette from reference photo
| Token | Value | Source |
|-------|-------|--------|
| `--color-accent-orange` | `#f5a623` | Amber icon/text color |
| `--color-bg-orange` | `#0d1b2e` | Navy background |
| `--color-panel-strong-orange` | `rgba(15,30,53,.97)` | Card surface |
| `--color-border-orange` | `rgba(196,122,30,.28)` | Amber hairline border |

### Open
- User review needed: does the orange theme replace cyan entirely, or ship as a toggle?
- If replacing: delete cyan vars, set `--color-accent: #f5a623` in `:root` directly

---

## v0.3.0 — UI Mockup Sprint (2026-04-27)
**Agent:** Windsurf Cascade  
**Session type:** Design / Brainstorm  
**Branch:** uncommitted (mockups only, no source changes)

### Summary
Full BMAD yolo design sprint. Read all 6 Svelte source files + global CSS in full, spun up a 2-persona FRED crew (Crestron UX Master + Backend Architect), then executed 10 distinct HTML-only UI upgrade mockups for the AA140 panel at 1280×800. Same color scheme throughout. Lint warnings (empty CSS stubs) resolved.

### Deliverables
All files written to `mockups/`:

| # | File | Concept |
|---|------|---------|
| index | `index.html` | Gallery with iframe previews of all 10 mockups |
| 01 | `01-edge-rail.html` | Vertical left rail nav replaces header — frees tile height |
| 02 | `02-full-bleed-tiles.html` | Source buttons flush-fill tile cells with no wasted padding |
| 03 | `03-source-card-grid.html` | Source buttons upgraded to icon + label cards (monitor / wifi / laptop) |
| 04 | `04-dark-mode-commander.html` | Deeper blacks, 1px borders, check badge in active cell — broadcast/flight-deck feel |
| 05 | `05-ambient-glow.html` | Radial inner glow on active source, green ring on audio-active tile, pulsing online dot |
| 06 | `06-quick-tap-footer.html` | Footer expanded to 116px with 3 explicit zones: Power / Mics / Volume |
| 07 | `07-camera-page-upgrade.html` | Camera page: glass-morphic PTZ diamond overlay, LIVE dot, segmented tracking, large zoom split |
| 08 | `08-component-upgrades.html` | Before → After for 6 components: status pills, power btn, mic btn, occupancy, VU meter, source btn |
| 09 | `09-settings-upgrade.html` | Settings: left sidebar nav + MicChannels as horizontal full-width strips (all 5 fit without scroll) |
| 10 | `10-full-synthesis.html` | **Recommended** — best of all concepts combined |

### Design decisions
- Color scheme untouched: `#0b1220` bg / `#38bdf8` accent / `rgba(15,23,42,.95)` glass panels
- Touch targets maintained at ≥52px
- All mockups are pure HTML/CSS — no build step, no framework dependency
- FRED document created: "UI Mockup Session — 10 HTML Mockups (2026-04-27)"
- No Svelte source files modified

### Open
- User review + direction needed before any mockup concept is implemented in Svelte
- Pending from prior sessions: contract rebuild, SIMPL# Pro bootstrap, wall panel deploy, CamPlay=false on camera leave

---

## v0.2.0 — Camera Streaming + Deploy Automation (2026-04-26, Session 2)
**Ref:** `MCCCD-AA140/docs/Handoffs/2026-04-26-session-2-handoff.md`

- Style polish: 0.5px hairline borders + Signal Tile source buttons
- Deploy automation: `python scripts/deploy.py` (paramiko SFTP + PROJECTLOAD) ~9s end-to-end
- `ch5-video` working on TS-1070: 12 persona violations resolved across 5 deploy iterations
- Camera stream: `rtsp://admin:crestron@192.168.2.79:554/1.h264`

---

## v0.1.0 — Initial Scaffold + Signal Wiring (2026-04-26, Session 1)
**Ref:** `MCCCD-AA140/docs/Handoffs/2026-04-26-session-handoff.md`

- CH5-Svelte project scaffolded for AA140
- Signal contract wired: displays, sources, mics, cameras, occupancy
- Home, Cameras, Settings pages built
- Component library: DisplayTile, MicChannel, MicVolumeModal, ConfirmShutdownModal
