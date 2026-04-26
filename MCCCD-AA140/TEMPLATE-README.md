# ch5-svelte-v2 Template

Reusable starting point for CH5-Svelte touchpanel projects. Adapted from the
Codex CodexClass reference panel (2026-04-23) and aligned with the FRED CH5
Contract Workflow Doctrine.

## Stack

- Svelte **5** (uses `mount(App, ...)` and `onclick` event syntax)
- Vite **6** with `vite-plugin-static-copy` and a `#`-in-path workaround in `build.mjs`
- TypeScript 5
- `@crestron/ch5-crcomlib` 2.17.x + `@crestron/ch5-webxpanel` 2.8.x runtime
- `@crestron/ch5-shell-utilities-cli` 2.17.x for archive/deploy

## What you get out of the box

- Typed CrComLib wrapper (`src/lib/CrComLib.ts`) — `publishDigital`, `subscribeAnalog`, `pulseDigital`, etc.
- Svelte store pattern (`src/lib/stores/signals.ts`) — feedback subscriptions wired in `initSignals()`
- Glass-card design system (`src/global.css`) — dark theme, cyan accent, 1280×800 base scale
- Resolution-aware preview dock for browser dev (Auto / TSW-770 / TSW-1070)
- Build pipeline with panel-safe HTML rewrite (`build.mjs`)
- ch5-cli archive + deploy scripts (`npm run archive`, `npm run deploy`)
- `validate.mjs` runtime sanity check
- Small-seed `.cce` ready for Contract Editor build (`contracts/__PROJECT_NAME__.cce`)

## What to customize per project

Replace every `__PROJECT_NAME__`, `__PANEL_HOST__`, and `__ROOM_NAME__` token,
then redesign the placeholder UI for your room.

| File | Replace |
|---|---|
| `package.json` | `__PROJECT_NAME__` × 4, `__PANEL_HOST__` × 1, `__ROOM_NAME__` × 1 |
| `index.html` | `__PROJECT_NAME__` × 1 |
| `public/config.json` | `__PANEL_HOST__` × 1, `ipId` if not `0x03` |
| `contracts/__PROJECT_NAME__.cce` | Rename file; update top-level `name`/`company`/`client`/`author`; rename placeholder commands/feedbacks; preserve siblingId pairing |
| `src/lib/contract.ts` | `ROOM_NAME` constant + every key in `CONTRACT` |
| `src/lib/stores/signals.ts` | Stores + `initSignals()` subscriptions + action helpers |
| `src/App.svelte` | Entire layout — the placeholder card is just a "hello world" proof of life |
| `docs/SIGNAL-MAP.md` | Replace `<ROOM_NAME>` placeholders, document every signal |
| `toDo/PROJECT-LOG.md` | Stamp first entry, update timestamps from `YYYY-MM-DD HH:MM` |

## Bootstrap a new project

```bash
# 1. Copy the template
rsync -a --exclude=node_modules --exclude=dist --exclude=output ch5-svelte-v2/ MyNewPanel/
cd MyNewPanel

# 2. Find/replace the parameter tokens
grep -rln "__PROJECT_NAME__\|__PANEL_HOST__\|__ROOM_NAME__" .   # see all references first
# then run sed (or your IDE's project-wide find/replace)

# 3. Rename the .cce
mv contracts/__PROJECT_NAME__.cce contracts/MyNewPanel.cce

# 4. Install + verify
npm install
npm run validate
npm run check

# 5. Develop locally
npm run dev
```

## Contract workflow (DOCTRINE — do not deviate)

1. The **`.cce`** in `contracts/` is the source of truth. Edit it by hand or via the agent.
2. **NEVER** hand-author `.cse2j`, `.chd`, or `.g.cs` — those are Contract Editor build outputs only. Hand-written `.cse2j` silently crashes CrComLib.
3. Workflow: edit `.cce` → open in Crestron Contract Editor → click Build → drop the generated `.cse2j` + `.chd` into `public/config/` (for SIMPL Windows) or import `.g.cs` files into your SIMPL# Pro project.
4. After **any** contract change, rebuild + redeploy **both** the `.cpz` (processor) and the `.ch5z` (panel). Mismatched sides mean stale joins on the panel.
5. Signal name format in code: `${ROOM_NAME}.SignalName` — never raw join numbers.

See FRED doc `b9d287cb-6bce-4911-8049-65aa6ef7f77d` (CH5 Contract Workflow Doctrine) for the full lifecycle.

## Build & deploy

```bash
npm run validate         # sanity-check the project structure
npm run check            # svelte-check (type + binding errors)
npm run dev              # local dev server with hot reload + preview dock
npm run build            # vite build via build.mjs (handles # in path, rewrites HTML)
npm run archive          # build + ch5-cli archive → output/<name>.ch5z
npm run deploy           # archive + ch5-cli deploy to __PANEL_HOST__
```

## What the placeholder UI proves

- Header status pill driven by a `subscribeDigital` feedback
- Glass-card layout in the main slot
- A toggle button using `pulseSignal` + optimistic store update
- Footer slot for project-specific transport/power controls
- Preview dock for resolution scaling

Once you've redesigned `src/App.svelte`, delete the placeholder card and the
template README — they're not meant to ship in a real project.
