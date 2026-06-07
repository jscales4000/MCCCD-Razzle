# 2026-06-07 — Handoff: User/Technician View-Modes Verification

**Author:** Claude Code (claude-opus-4-8)
**Project:** MCCCD-AA140 Touchpanel (FRED `c1937681-e57d-4354-aa58-a5b0f6e9ca23`)
**Branch reviewed:** `feat/screen-relay-and-view-modes` (worktree `.worktrees/device-integration-usb-signage`, HEAD `457b752`)
**Spec:** `docs/superpowers/specs/2026-06-03-user-technician-view-modes-design.md`

## What this session did
Verified the three in-review view-modes tasks by code review against the design spec. No code was changed. FRED tasks were updated to reflect results.

## Branch / location (important)
- The view-modes work is **NOT on `main`**. It lives on `feat/screen-relay-and-view-modes`, which is **stacked on** `feat/device-integration-usb-signage`.
- That branch is checked out in the worktree `.worktrees/device-integration-usb-signage/` — the primary checkout is still on `main` and does not contain these files.
- Merge path to `main`: the tip branch contains both stacked bodies of work. Decide with Jordan whether to merge `device-integration-usb-signage` then `screen-relay-and-view-modes`, or collapse into one merge.

## Verification results

| Task | Result |
|------|--------|
| `f68d2d11` role store + PinModal + global TechGate | **PASS** → done |
| `b336ea02` gate Audio trims + output-max; gate D5 signage | **PASS** → done |
| `6ee87d9e` gate Cameras advanced controls | **PASS w/ 1 deviation** → kept in review |

### Confirmed correct
- **`src/lib/stores/role.ts`** — `writable<'user'|'tech'>` default `user`, never persisted; `enterTech(pin)` / `exitTech()` / `bumpActivity()`; `TECH_TIMEOUT_MS = 5min`.
- **`src/components/PinModal.svelte`** — numeric keypad, validates PIN const, shake + clear on wrong.
- **`src/components/TechGate.svelte`** — invisible top-right hotspot, 2s hold (`HOLD_MS=2000`) → modal; "Tech" badge + Exit (top-left) while tech; capture-phase `pointerdown → bumpActivity`. Mounted at root in `App.svelte`.
- **Audio** — `AudioMixer.svelte` passes `advanced={$role==='tech'}` to all 4 `MixerChannel`s; User view hides the line-out (output-feed) fader **and** input trim/gain, keeps VU meters + mute. `MasterStrip` (master volume / output select), Mute All, and scene/preset recall stay visible. 
- **Display Routing** — `DisplayRouting.svelte:303` gates Outside Signage (D5) behind tech; ScreenControl (screen up/down) + USB host stay visible.
- **Cameras** — gated correctly: PTZ drive pad (`:267`), live coords/zoom-ratio (`:328`), PTZ speed sliders (`:370`), Send-to-VTC (`:403`), preset zones + tracking profiles (`:427`). User keeps camera/feed select, zoom, presenter/group/auto framing.

### Open deviation (1)
- **Cameras Home + Tracking Shot** (`Cameras.svelte:422-423`) sit in the **ungated** Shot Presets block, so they show in User view. Spec line 36 lists "home/tracking-shot" as **Technician-only**.
- Tracked as new task **`1c950487` — "View modes: gate Cameras Home + Tracking-Shot (spec deviation fix)"**. Fix = wrap only those two buttons in `{#if $role==='tech'}` (keep the `{#each presets}` recall ungated), then move `6ee87d9e` → done. Alt: keep them user-accessible and update the spec instead.

### PIN correction
- Production PIN const is **`1981`** (`role.ts`, commit `b8897c5`) — the legacy task text said 1988. The user task `db4a5ef9` was corrected. Final value still to be confirmed at commissioning.

## Verification method note
This was a **browser/code** verification. The Vite dev server (`npm run dev` from the worktree) renders and gates the UI correctly, but **CrComLib has no processor** there, so live signal feedback is not exercised. Real signal/panel verification still requires `deploy:both` to TS-1070 (.80) + TSW-1070 (.78).

## Next steps
1. Decide Home/Tracking-Shot intent → apply `1c950487` or update spec; close `6ee87d9e`.
2. Confirm production Technician PIN (currently `1981`).
3. Merge the stacked branch(es) to `main` (`7ba44fad`).
4. `deploy:both` and run live panel verification (`db4a5ef9`): boot→User, long-press→PIN→tech, idle/Exit revert, both panels.
5. Field: screen-relay wiring + PULSE_MS (`41d7f2e2`).
