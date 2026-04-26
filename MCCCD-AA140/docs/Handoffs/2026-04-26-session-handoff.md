# Session Handoff — MCCCD-AA140 Touchpanel

**Date:** 2026-04-26
**Author of this handoff:** Claude (Opus 4.7)
**Session driver:** Jordan Scales
**Repo state:** clean working tree, all changes committed on `main`
**Total commits this session:** 16 (run `git log --oneline` to see)

---

## TL;DR — Where we are

The **frontend is production-ready** (style #2 Signal Tile applied, all behaviors wired). The **`.cce` contract has 76 entries** and is built once at v1.0 in Crestron Contract Editor, but needs a **rebuild for v1.1** (42 new entries added since first build). The **SIMPL# Pro project files are pre-staged at `MCCCD-AA140-SIMPL/`** but blocked on user creating the Visual Studio project skeleton + dropping the regenerated `.g.cs` from Contract Editor.

What you can do **right now** (before any user action):
- Open `http://localhost:5176/` (Vite dev server may still be running) to see the live UI
- Open `button-mockups.html` in the project root to compare all 10 button styles
- Flash `MCCCD-AA140/output/MCCCD-AA140.ch5z` (420 KB, built with Style 2) to a TS-1070 or TSW-1070 for visual smoke-test (panel will show "Offline" status pill since no processor responds yet — that's expected)

---

## Project & FRED Resources

**Archon project ID:** `c1937681-e57d-4354-aa58-a5b0f6e9ca23` (title: "MCCCD-AA140 Touchpanel")

**Personas assigned (6):**
- Crestron CH5 Extended Developer (`1a965715-...`)  — primary frontend
- Crestron SIMPL# Engineer (`d17da25e-...`)  — primary backend
- Crestron UX Master (`4396b575-...`)
- CH5 Video Integration Specialist (`f61640cf-...`)  — ch5-video + RTSP wiring
- device-api-specialist (`7aa52f65-...`)  — 1Beyond REST + Q-SYS named-controls
- Crestron CWS & WebSocket Protocol Engineer (`14af1e95-...`)  — deferred (only if SIMPL# Debug Tool wired in)

**FRED project documents:**
| ID | Type | Title |
|---|---|---|
| `4fa4406b-3e75-41b3-b62d-db74975ed253` | guide | Crestron Contract Editor — Hand-Authoring Constraints & Lessons |
| `a33188a5-f483-44ca-a695-d5f9c8b4b2ea` | design | AA140 Design Spec |
| `611b348b-71bd-4332-9c2c-6ea88dd5a58e` | spec | AA140 Implementation Plan (18 tasks across 7 phases) |
| `62a0f156-a106-4f6a-a231-532d74a932ec` | note | AA140 Button Style Mockups (10 options + applied pick) |

Search any of them via `mcp__fred__find_documents(project_id="c1937681-...", query="...")` or globally via `mcp__fred__rag_search_knowledge_base(query="...", search_scope="deep")`.

**Pending Archon task to action:** `2c98e0d4-ebb4-4d07-ad66-cfd0242987d7` — "Build Crestron Contract persona (must-haves / must-nots authority)". Source content is the Lessons Learned doc; create as a library persona via `manage_persona(action="create", division="crestron", is_library=true, ...)`.

---

## Hardware Inventory (quick reference)

| Device | Model | IPID | IP | Notes |
|---|---|---|---|---|
| Touchpanel | TS-1070 | `0x03` | `192.168.2.53` | Tabletop |
| Touchpanel | TSW-1070 | `0x04` | `192.168.2.123` | Wall |
| Soft-panel | WebXPanel | `0x05` | n/a | Tech / remote |
| Processor | RMC4 | — | `192.168.1.191` | 4-Series |
| Encoder | NVX E30 (Room PC) | `0x11` | TBD | Source 1 |
| Encoder | NVX E30 (Ext PC) | `0x12` | TBD | Source 2 |
| Encoder | NVX E30 (AirMedia) | `0x13` | TBD | Source 3 |
| Encoder | NVX-384 (Laptop combo) | `0x14` | TBD | Source 4 (HDMI + USB-C, internal autoswitch) |
| Decoder | NVX D200 (D1) | `0x21` | TBD | |
| Decoder | NVX D200 (D2) | `0x22` | TBD | |
| Decoder | NVX D200 (D3) | `0x23` | TBD | |
| DSP | Q-SYS Nano | `0x31` | TBD | All audio routing |
| Mic | Lavalier | — | wired | On Home |
| Mic | Handheld | — | wired | On Home |
| Mic | Sennheiser TCCM Ceiling 1 | — | Dante | Settings only |
| Mic | Sennheiser TCCM Ceiling 2 | — | Dante | Settings only |
| Mic | Sennheiser TCCM Ceiling 3 | — | Dante | Settings only |
| Camera | 1Beyond i20 (Front) | — | TBD | RTSP to ch5-video |
| Camera | 1Beyond i12 (Back-Left) | — | TBD | |
| Camera | 1Beyond i12 (Back-Right) | — | TBD | |
| Sensor | PoE Occupancy | `0x41` | TBD | Auto-on / vacancy shutdown |

VLAN: panels on `192.168.2.x`, processor on `192.168.1.x`. Confirm L3 routing + no firewall block on CIP (port 41794).

---

## File Layout

```
c:/Users/scale/CascadeProjects/Archon-Tests/MCCCD Razzle/   <-- git root, .gitignore here
├── .gitignore
├── Claude.md                          (empty placeholder)
├── button-mockups.html                ★ standalone 10-style viewer
├── MCCCD-AA140/                        ← CH5-Svelte panel project
│   ├── .gitignore                      (panel-specific: node_modules, dist, output)
│   ├── package.json
│   ├── vite.config.ts
│   ├── build.mjs
│   ├── tsconfig.json
│   ├── svelte.config.js
│   ├── index.html
│   ├── contracts/
│   │   └── MCCCD-AA140.cce             ← contract source-of-truth (76 entries, ASCII-only)
│   ├── public/
│   │   ├── ch5-components.js           (runtime, scaffold)
│   │   ├── cr-com-lib.js               (runtime, scaffold)
│   │   ├── config.json                 (panel host config)
│   │   └── config/                     ← .cse2j + .chd land here after Contract Editor build
│   ├── docs/
│   │   ├── superpowers/
│   │   │   ├── specs/2026-04-26-mcccd-aa140-design.md      ← design spec (v1.1)
│   │   │   └── plans/2026-04-26-mcccd-aa140.md             ← 18-task implementation plan
│   │   ├── Lessons-Learned/
│   │   │   ├── Crestron-Contract-Editor-Constraints.md    ← must-haves / must-nots
│   │   │   └── Button-Style-Mockups.md                    ← 10 styles + applied pick
│   │   ├── Handoffs/
│   │   │   └── 2026-04-26-session-handoff.md              ← THIS FILE
│   │   └── SIGNAL-MAP.md                                   (scaffold — not yet authored)
│   ├── layouts/                        ← 8 reference layouts from scaffold (not loaded; reference only)
│   ├── scripts/
│   │   └── validate.mjs                (sanity check)
│   ├── src/
│   │   ├── main.ts                     (mount, initSignals())
│   │   ├── App.svelte                  ← thin Home / Cameras / Settings router
│   │   ├── global.css                  ← Signal Tile button system (#2)
│   │   ├── components/
│   │   │   ├── DisplayTile.svelte      (4-source picker + power dot + audio + mirror)
│   │   │   ├── ConfirmShutdownModal.svelte  (30s countdown)
│   │   │   ├── PresetButton.svelte     (tap=recall, hold-3s=save with progress ring)
│   │   │   └── MicChannel.svelte       (5-mic settings row)
│   │   ├── pages/
│   │   │   ├── Home.svelte
│   │   │   ├── Cameras.svelte
│   │   │   └── Settings.svelte
│   │   └── lib/
│   │       ├── CrComLib.ts             (typed wrapper: publishDigital/Analog, pulseDigital, subscribe*)
│   │       ├── contract.ts             ← SIGNALS object (mirrors .cce, hand-maintained)
│   │       ├── cameras.ts              (3-camera registry + RTSP helper)
│   │       └── stores/
│   │           ├── page.ts             ('home' | 'cameras' | 'settings' router store)
│   │           └── signals.ts          (~30 typed feedback stores + initSignals subscriber)
│   ├── output/
│   │   └── MCCCD-AA140.ch5z            ← latest archive (Style 2 applied, 420 KB)
│   ├── dist/                           (vite build output, regenerated by npm run build)
│   ├── toDo/
│   │   └── PROJECT-LOG.md              (timestamped activity log)
│   └── TEMPLATE-README.md              (scaffold template docs)
└── MCCCD-AA140-SIMPL/                  ← SIMPL# Pro backend (pre-staged, awaiting VS bootstrap)
    ├── README.md                       (USER ACTION instructions)
    └── MCCCD-AA140/                    (project subfolder)
        ├── ControlSystem.cs            (entry point — registers panels, instantiates services)
        ├── NvxRoutingService.cs        (source/display routing, mirror-to-D3)
        ├── QsysAudioService.cs         (PA module wiring + ceiling mics + 10 trim/lineout)
        ├── CameraService.cs            (1Beyond REST: PTZ, zoom, presets, VTC, tracking)
        ├── OccupancyController.cs      (vacancy state machine + 30-min timer)
        ├── SystemPowerController.cs    (PowerUp/Down sequences + SystemPowerFb publish)
        └── Generated/
            └── README.md               (instructions for where .g.cs lands after Contract Editor build)
```

---

## How to Run

All commands assume `cd "c:/Users/scale/CascadeProjects/Archon-Tests/MCCCD Razzle/MCCCD-AA140"` unless noted.

```bash
# Type-check (svelte-check)
npm run check
# Expected: 0 errors, 1 informational warning (ConfirmShutdownModal $effect prop reference — non-blocking)

# Sanity-validate scaffold structure
npm run validate

# Local dev server with hot reload
npm run dev
# Opens http://localhost:5173/ (or 5174/5175/5176 if previous ones are taken).
# In browser, the panel renders but signals don't function (no CrComLib runtime).
# Buttons click, modal opens, preset hold animates, but feedback values stay at defaults.

# Production build (vite build via build.mjs — handles `#` in path + panel-safe HTML rewrite)
npm run build
# Output: dist/index.html + dist/assets/*.css + dist/assets/*.js

# Build .ch5z archive for panel deploy
npm run archive
# Output: output/MCCCD-AA140.ch5z (~420 KB)

# Deploy to a panel via ch5-cli (script currently targets the processor IP — wrong)
npm run deploy
# To deploy to TS-1070: edit package.json deploy script's -H to 192.168.2.53
# To deploy to TSW-1070: same but 192.168.2.123
# Or call ch5-cli directly:
#   npx ch5-cli deploy -H 192.168.2.53 -t touchscreen -u admin -i output/MCCCD-AA140.ch5z -p

# verify-signals — grep for SIGNALS usage in src/
npm run verify-signals
```

**Manual Crestron Toolbox flash:** Address Book → 192.168.2.53 → Tools → Application Loader → browse to `output/MCCCD-AA140.ch5z` → Send → reboot.

---

## USER ACTIONS Pending (in order)

### 1. Crestron Contract Editor REBUILD (Phase 4 — Archon task `f40d46c8-...`)

The `.cce` was built once at v1.0 (33 entries). v1.1 grew it to 76 entries. **It needs to be rebuilt** to regenerate `.cse2j` + `.chd` + `.g.cs` matching the new surface.

```
1. Open contracts/MCCCD-AA140.cce in Crestron Contract Editor (Windows GUI tool, ships with Toolbox).
2. Click Build.
3. Drop regenerated .cse2j + .chd into MCCCD-AA140/public/config/.
4. Drop regenerated MCCCD_AA140.g.cs into MCCCD-AA140-SIMPL/MCCCD-AA140/Generated/ (replacing previous).
5. Run npm run validate from MCCCD-AA140/ — should pass.
```

**If Contract Editor rejects the .cce:** see `docs/Lessons-Learned/Crestron-Contract-Editor-Constraints.md`. Most likely culprit if it ever happens again: a non-ASCII character snuck into a `notes` or `description` field. The .cce in this repo is ASCII-only — verify with `grep -P '[^\x00-\x7F]' MCCCD-AA140/contracts/MCCCD-AA140.cce` (should return nothing).

### 2. Visual Studio SIMPL# Pro Project Bootstrap (Phase 5 — Archon task `0945a771-...`)

```
1. Visual Studio → File → New → Project → Crestron → SIMPL# Pro → 4-Series Application.
2. Name: MCCCD-AA140
3. Location: c:\Users\scale\CascadeProjects\Archon-Tests\MCCCD Razzle\MCCCD-AA140-SIMPL
4. Replace auto-generated ControlSystem.cs with the version pre-staged at MCCCD-AA140-SIMPL/MCCCD-AA140/ControlSystem.cs.
5. Add the 5 service files (NvxRouting/QsysAudio/Camera/Occupancy/SystemPower) via "Add Existing Item".
6. Add MCCCD_AA140.g.cs (from Contract Editor build) to the project.
7. Add references:
   - Crestron Q-SYS PA Module (search Crestron Modules library for "qsys core").
   - PoE occupancy sensor driver (e.g. GLS-OIR-CN class for the installed sensor model).
8. Build → Build Solution. Expected: 0 errors after refs + .g.cs are in place.
   - Several // TODO field-config calls remain commented out — uncomment as Q-SYS named-controls + 1Beyond REST endpoints solidify.
```

### 3. Field Configuration (~2 hours)

Concrete values to fill in:

| What | Where | Current placeholder |
|---|---|---|
| NVX device IPs | (TBD; SIMPL# uses IPIDs) | — |
| Q-SYS DSP IP | (TBD) | — |
| 1Beyond camera IPs (3) | `MCCCD-AA140/src/lib/cameras.ts` `_camIps` array (panel-side); `MCCCD-AA140-SIMPL/MCCCD-AA140/CameraService.cs` `_camIps` array (backend) | `0.0.0.0` |
| Occupancy sensor IP / IPID | (already 0x41; IP TBD) | — |
| 1Beyond REST endpoints + auth | `CameraService.cs` HttpFireAndForget URLs | placeholder `/cgi-bin/ptz`, `/cgi-bin/preset`, `/cgi-bin/tracking`, `/cgi-bin/vtc-ingest` |
| Q-SYS named-control names | `QsysAudioService.cs` (e.g. `"MicLav.gain"`, `"ProgramRouter.input"`) | placeholder strings |
| NVX SDK class names | `NvxRoutingService.cs` | `DmNvx351`, `DmNvx384`, `DmNvxD30` (placeholders — actual SDK class names vary by version) |
| Soft-shutdown delay | `OccupancyController.cs` `SHUTDOWN_DELAY_MIN` | 30 (confirm) |
| Mic types (Lav/Handheld) | spec only — wired into Q-SYS | wired/wireless? |

### 4. Deploy + Smoke Test (Phases 6 — Archon task `184a941a-...`)

9-scenario smoke test from Plan Task 18:
1. Power-up (button → modal-skip since system off → pulse → displays come on, audio unmute, D3 mirrors D2)
2. Source routing (each source button on each tile → display switches within ~1s)
3. NVX-384 autoswitch (plug HDMI only → press Laptop → encoder picks HDMI; plug USB-C → encoder follows)
4. Mirror-to-D3 (set D1 to Ext PC → tap mirror → D3 jumps; change D1 source → D3 stays)
5. Audio-follows-display (toggle speaker icon on D1 → audio routes; toggle D2 → swap)
6. Mic mute (Lav, Handheld on Home; Ceiling 1/2/3 on Settings)
7. Cameras (selector → ch5-video stream; PTZ overlay press-hold; zoom in/out press-hold; preset tap=recall, hold-3s=save with progress ring; Send-to-VTC; tracking modes)
8. Occupancy (leave room empty → 30 min → soft shutdown countdown → power off; re-enter cancels)
9. Settings page (5-mic level meters animate; trim and lineout sliders publish to Q-SYS; ceiling mute toggles work)

Document any failures back into `MCCCD-AA140/toDo/PROJECT-LOG.md`.

---

## Known Risks / Gotchas

1. **Contract Editor regenerates IDs at Build time.** Hand-written IDs (`_a001`-`_a037`, `_b001`-`_b038`) get replaced. The generated `.g.cs` uses C# property names matching `name` field in the `.cce`, so as long as those don't change, code compiles.

2. **NVX SDK class names** (`DmNvx351`, `DmNvx384`, `DmNvxD30`) are placeholders. The actual class names in the installed Crestron SDK may differ. Visual Studio will surface unresolved type errors quickly. Coordinate with the Crestron SIMPL# Engineer persona.

3. **MainContract constructor signature** varies by Contract Editor version. The pre-staged `ControlSystem.cs` calls `new MainContract(_tsTabletop, _tswWall)` (multi-panel). Some Contract Editor versions generate a single-panel ctor + an `AddDevice()` method instead. Adjust to match generated `.g.cs`.

4. **`*Fb.OnUShortChange` / `OnAnalogChange` event accessor names** vary across Contract Editor versions. Some files have these stubbed-out (e.g. SystemPowerController's last-source snapshot). Wire them to whatever the generated `.g.cs` exposes.

5. **`prefers-reduced-motion`** — all CSS transitions clamp to 0ms when set. Verified honored across button states, modal animations, level meters, progress ring.

6. **`ch5-video`** is a runtime custom element (not a Svelte component). Won't render in browser dev mode (you'll see an empty `<ch5-video>` box). Renders correctly on actual panel hardware where the CrComLib runtime registers it.

7. **CIP port 41794** — must be open between panels (192.168.2.x) and processor (192.168.1.x). If panels show "Offline" indefinitely after deploy, check VLAN routing and firewall.

8. **Two-place contract maintenance** — every signal in `contracts/MCCCD-AA140.cce` MUST also be in `src/lib/contract.ts` `SIGNALS` object. Drift = silent join failures. Both currently in sync at 76 entries.

9. **The `.ch5z` in `output/`** corresponds to v1.1 frontend (Style 2 + behaviors) but expects the v1.1 contract. If the panel is flashed with this `.ch5z` against an unbuilt-v1.1 processor, signals like `MicCeilingNMute`, `ZoomIn/Out`, `SystemPowerFb` will silently fail to publish/subscribe (because the panel-side `.cse2j` references joins that aren't in the processor's old `.cpz`). Always rebuild + redeploy BOTH sides together.

10. **Power button confirmation modal** — only triggers if `systemPowerFb` is true. In dev mode (no processor), `systemPowerFb` defaults to false, so the modal never shows. To preview the modal in browser, temporarily edit `signals.ts` and set the initial value of `systemPowerFb` to `true`.

---

## Lessons Learned

Captured in detail at `MCCCD-AA140/docs/Lessons-Learned/Crestron-Contract-Editor-Constraints.md` (also pushed to FRED as guide doc `4fa4406b-...`). Key points:

1. **`Errors.Contract.min`** triggers on description / notes fields when too long OR contain non-ASCII. Trim to ≤ 60 chars, strip em-dashes (—) and en-dashes (–) preemptively.
2. **Never hand-author** `.cse2j`, `.chd`, `.g.cs`. Always edit the `.cce` and let Contract Editor build the rest. Hand-written `.cse2j` silently crashes CrComLib.
3. **Component / signal names must be valid C# identifiers** (PascalCase, no spaces, no special chars).
4. **siblingId pairings** must be mutual for paired command/feedback. Pulse commands without feedback have empty-string siblingId.
5. **Two-place contract sync** — every new signal must land in both `.cce` and `contract.ts` `SIGNALS`.

Archon task `2c98e0d4-...` is open to build a "Crestron Contract Engineer" library persona that bakes these rules into a reusable agent for future projects.

---

## Recent Git History

```
e11dde6 feat(style): swap from #3 Hairline Schematic to #2 Signal Tile per user pick
6d2e129 feat(simpl#): v1.1 — wire ceiling mics, trims, zoom, SystemPowerFb, drop NvxAutoSwitchSrc
0c236f8 feat(v1.1): contract + style + behavior changes (sources 4, ceiling mics, power modal, preset hold, zoom, settings)
43d9800 docs(spec): v2 — 4 sources, ceiling mics, Settings page, power modal, preset hold, camera zoom, DisplayPowerFb
ccd16b7 docs(lessons-learned): Crestron Contract Editor hand-authoring constraints
b8a8123 fix(contract): trim description + strip non-ASCII em-dashes for Contract Editor
79a037b docs(log): full session log + USER ACTION list for unblocking next phases
8fb7e4c feat(simpl#): pre-stage SIMPL# Pro project (Tasks 10-15)
b2fa48a feat(contract): populate .cce with full AA140 signal map; drop legacy placeholder refs
aeb0e6c feat(cameras): full cameras page (selector, ch5-video preview, PTZ overlay, presets, VTC, tracking)
e9391ba feat(home): full home page (3 display tiles + audio + mirror + mics + occupancy + power)
f990499 feat(home): add DisplayTile component (5 sources, audio toggle, mirror, autoswitch badge)
e52a531 feat(router): add page router + Home/Cameras stubs
6a628bc feat(stores): add feedback stores + subscriptions for AA140 signals
4b8feb2 feat(contract): add AA140-specific signals (mirror, mics, occupancy, camera, autoswitch)
8517b7a chore: initial ch5-svelte-v2 scaffold for AA140
```

---

## Three Possible Entry Points for the Next Session

### A. Continue toward smoke test on real hardware
The fastest path to a working room. Follow USER ACTIONS 1 → 2 → 3 → 4 above. Most of the remaining work is hands-on with Crestron Toolbox and Visual Studio; it's not generative AI work.

### B. Build the Crestron Contract Engineer persona
Open Archon task `2c98e0d4-ebb4-4d07-ad66-cfd0242987d7`. Use `mcp__fred__manage_persona(action="create", division="crestron", is_library=true, ...)` with `persona_content` distilled from the Lessons Learned doc. Acceptance: `find_personas(division="crestron")` returns the new entry; `route_to_persona(task_description="edit .cce contract file")` ranks it top.

### C. Sweat the v2 followups list
From the spec section 13 + Plan Phase 7:
- VU meter peak-hold + spectrum analyzer on Settings (currently single-bar amplitude)
- Advanced source/display matrix on Settings (the existing `App.multi-routing.svelte` reference layout)
- Mic gain in dB scale + assistive-listening output level
- Lighting / shade integration (if scope expands)
- VTC dialing UX (if no separate UC system provides it)
- Preset delete with confirmation modal
- SIMPL# Debug Tool instrumentation (uses the deferred CWS persona)

Each of these is a self-contained mini-feature.

---

## How to Resume

In a fresh session, start with:

```
Read this handoff: MCCCD-AA140/docs/Handoffs/2026-04-26-session-handoff.md
Read the design spec: MCCCD-AA140/docs/superpowers/specs/2026-04-26-mcccd-aa140-design.md
Run: git log --oneline -20
Then ask the user which entry point (A/B/C above) they want to work on.
```

The spec + this handoff capture every decision, deviation, and TODO. The git history captures every code change. Together they're the complete state — nothing else needs to be reconstructed from scratch.

---

**End of handoff.**
