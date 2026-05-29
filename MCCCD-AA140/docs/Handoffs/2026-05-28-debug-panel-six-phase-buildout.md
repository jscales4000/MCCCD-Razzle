# 2026-05-28 — Debug Panel six-phase build-out

**State at end of session.** Six phases of debug-panel UI work landed on `feat/drag-drop-router-mockup` and merged to `main`. Both branches pushed to `origin`. The processor at RMC4 192.168.2.198 is running the phase-6 binary; CWS at `https://192.168.2.198/cws/aa140/debug/` shows the full UI (devices, system, raw-signal, NVX routing with override, Sony VPL, Newline, AirMedia, audio, mics, cameras, live log with chips/search/scroll-lock). Section collapse works on every card. Layout reflows below ~700px.

Restore points (in landed order):

| Tag | Commit | Phase | What it delivers |
|---|---|---|---|
| `checkpoint-panel-trace-wired` | `802fca5` | bridge | PanelDispatcher SmartObject SigChange → DebugTrace.SigChange so physical taps reach the browser log |
| `checkpoint-program-status-cleanup` | `7bd964f` | bridge | `CrestronEnvironment.ProgramStatusEventHandler` disposes DebugServer on Stopping. Prevents stale `CwsRouter` cache between PROGLOADs — empirically verified by six clean deploys since. |
| `checkpoint-retire-tsw-change-diag` | `541fcf9` | bridge | Raw `_tswPrimary.SigChange` capture moved from ErrorLog to DebugTrace with `panel-cip` device key. The err log no longer floods during slider drags. |
| `checkpoint-debug-log-ux` | `8cd63e6` | 1 | Live log card replaced with chip filters (type + device), search, scroll lock, click-to-expand, sticky header, copy-as-JSON-lines, 5000-event client ring |
| `checkpoint-lifecycle-instrumentation` | `00423fb` | 2 | DebugTrace.Lifecycle / Error emissions added to ShureTcpClient, NvxRoutingService, CameraService at connection-state-change sites |
| `checkpoint-observability-surface` | `a5aed96` | 3 | Frontend derives device status from /events stream. New NVX Routing card (decoder/encoder tables). Per-device badges. Per-device last cmd / last resp / last error lines |
| `checkpoint-runtime-ip-services` | `15060a7` | 4 | SonyVplService, NewlineService, AirMediaService all gained mutable `_host` + `_enabled` + SetHost/SetEnabled/ApplyConfig. Devices-card IP+enable toggles now drive these services at runtime instead of only persisting to JSON |
| `checkpoint-operational-tooling` | `9d19582` | 5 | POST /nvx/route?dec=N&src=K manual NVX route override; POST /signal?join=N&type=&value= raw SmartObject 1 signal send. Frontend buttons on NVX rows + new Raw Signal card |
| `checkpoint-phase6-polish` | `df6e142` | 6 | Sony VPL, Newline, AirMedia control cards with their own POST endpoints (`/sony/N/...`, `/newline/...`, `/airmedia/...`). Section collapse on every card via clickable h2. Responsive @ ≤700px |

`main` and `origin/main` both at `df6e142`.

## Architecture changes worth knowing about

### DebugTrace stream is now the lingua franca

Every connection-state-change, every panel publish, every browser-initiated command goes through `DebugTrace.{Lifecycle, SigChange, Command, Response, Error, StateChange}`. The ring buffer is 1000 entries server-side; clients drain via GET /events?since=N. The browser keeps a 5000-entry client-side ring and does all filtering / searching locally.

Phase 3 added a layer above this: `updateDeviceStatus()` in `debug.js` derives a `deviceStatus` map from the lifecycle stream as events arrive. The NVX card, badges, and per-card detail lines read from that map. **There is no `/status` endpoint** — observability is derived, not fetched separately. If you want to add a new badge or status field, you extend `updateDeviceStatus()` and `renderStatus()` in `debug.js`, not the backend.

### Service host/enabled is mutable across the board

Before today, only `ShureTcpClient` had `SetHost`/`SetEnabled`. After phase 4, `SonyVplService.Projector` (inner class), `NewlineService`, and `AirMediaService` all do too. The pattern:

- `_host` is a mutable field (initialized from the old constant)
- `_enabled` defaults to `false`; `Initialize()` no longer auto-Start()s
- `SetEnabled(true)` calls `Start()` (which calls `Connect()`); `SetEnabled(false)` calls `Stop()` (which closes socket + cancels reconnect timer)
- `SetHost(string)` closes any open socket and reconnects if `_enabled`
- All three guard via a `_stateLock` to avoid race conditions on the reconnect timer
- `ApplyConfigN(host, enabled)` is the entry point called by `DebugServer.ApplyConfigToService`

`DebugServer.Configure` was extended twice (phases 4 and 5) to take additional service references: now takes `(store, audio, mxa, cameras, nvx, power, projectors, newline, airmedia, panel)`. The panel reference is for the raw-signal POST endpoint.

### The CwsRouter cleanup handler is now the safety net

`ControlSystem.cs` registers `CrestronEnvironment.ProgramStatusEventHandler += OnProgramStatus;` in the ctor. On `Stopping`, it disposes `_debug`, which calls `HttpCwsServer.Unregister()`. Six PROGLOAD cycles today since this landed and zero stale-cache reboots — previously we were rebooting after almost every deploy. **Do not remove this handler.** See [Debug-Panel-CWS-Lessons.md](../Lessons-Learned/Debug-Panel-CWS-Lessons.md) for the full incident write-up.

### Browser POST quirk

Crestron's web server requires `Content-Length` on POST requests, even empty ones. The browser's `fetch({method: 'POST'})` sets `Content-Length: 0` automatically. Raw `curl -X POST` does not, and returns `411 Length Required`. When testing endpoints from the shell, add `-H "Content-Length: 0"`.

## What's deferred / next-session pickup list

1. **Command replay** (originally pickup 5c). Re-fire a previous event by correlationId. Requires reconstructing the endpoint URL from event data (which is currently per-device-specific). Easiest path: add a generic `/replay?corr=cN` endpoint server-side that looks up the event in the ring buffer and dispatches based on `device` + `data.method`. Frontend addition: a "replay" button in each `command` row's expanded JSON view.

2. **`nvx-nvx-384` double-prefix cosmetic.** The NVX-384 encoder's label is "NVX-384"; lowercased it becomes "nvx-384" and the device key prefix logic in NvxRoutingService produces `nvx-nvx-384`. Trivial fix: special-case the label in the `nvx-` prefix construction. Affects the NVX encoder row label in the debug UI (currently reads "NVX-384" via static HTML but the device key shown elsewhere is doubled).

3. **Filter state persistence**. The chip filters / search / scroll-lock all reset on page reload. One `localStorage` key holding a serialized state object would fix it. Decision: phase-6-polish skipped this; bring it back if reload-resets become annoying.

4. **Light mode toggle.** Phase-6 considered it and skipped — the dark theme works and a light variant would be significant CSS work for limited operator-room value. Revisit if requested.

5. **Dynamic status badges on Sony / Newline / AirMedia cards.** The badges render gray ("unknown") because none of the four services currently emit `device_connected` / `device_connect_failed` / `device_socket_change` lifecycle events. To fix: add the same DebugTrace emissions to `SonyVplService.Projector`, `NewlineService`, and `AirMediaService` connect-success/connect-fail paths as phase 2 did for ShureTcpClient. Each is a ~4-line addition per service.

6. **Per-MXA card.** The Devices card has mxa-a/mxa-b rows (with badges via phase 3), but there's no dedicated control card. The mic table inside the P300 card uses Lav/Handheld/CeilingA/CeilingB labels routed through the P300 service. If you need direct MXA controls (Identify, MuteArray, PresetRecall — already exposed in `ShureMxaService`), add a card with two columns mirroring the Sony VPL layout.

7. **Equipment list xlsx files in the working tree.** `MCCCD_AA140_Equipment_List.xlsx` is modified and there's an auto-recover copy untracked. Not part of this arc. Leave alone or commit if appropriate.

8. **Worktrees on disk.** The six phase worktrees still live at `.claude/worktrees/{debug-ui-buildout, phase3-observability, phase4-coverage, phase5-optools, phase6-polish}` along with branches `worktree-*`. All merged into `feat/drag-drop-router-mockup`. Safe to delete: `git worktree remove .claude/worktrees/<name>` per dir, then `git branch -d worktree-<name>` per branch. Or leave them as session artifacts.

9. **Tags not pushed.** Eight new `checkpoint-*` tags from this session (plus the earlier ones from prior sessions) are local-only. `git push origin --tags` publishes them so they show up in GitHub's tags / releases UI.

## Process notes from this session

- The worktree-per-phase pattern worked well: clean isolation, each phase landed on its own branch, FF-merged into feat as a tight integration step before moving on. Six deploys, zero stale-cache reboots, no merge conflicts.

- `EnterWorktree` consistently created the new worktree off an *older* ancestor commit (`9027db9`) rather than the current tip of `feat/drag-drop-router-mockup`. Workaround: every new phase started with `git reset --hard <prev-phase-tip-sha>` to align. Worth understanding why this happens — possibly a quirk of how the native tool resolves "current HEAD" when launched mid-session. If the next session uses the same pattern, expect the same workaround.

- The brainstorming → spec → plan → execute → commit → tag rhythm scaled well across six phases. Phases 2 and 4 used the compressed "spec-plan combined doc" format since they were mechanical; phases 1, 3, 5, 6 had standalone spec + plan docs. Both worked.

- Visual companion (browser-based mockup tool) was used for phase 1 design only — useful for the UI layout decision. Subsequent phases skipped it since they were primarily backend or additive UI. Don't force it when the decision is conceptual.

## File index of work added this session

### New
- `MCCCD-AA140/docs/Lessons-Learned/Debug-Panel-CWS-Lessons.md` (from earlier today, before the six-phase arc)
- `MCCCD-AA140/docs/superpowers/specs/2026-05-28-debug-log-ux-design.md` (phase 1 spec)
- `MCCCD-AA140/docs/superpowers/plans/2026-05-28-debug-log-ux-plan.md` (phase 1 plan)
- `MCCCD-AA140/docs/superpowers/specs/2026-05-28-phase2-lifecycle-instrumentation.md` (phase 2 combined spec+plan)
- `MCCCD-AA140/docs/superpowers/specs/2026-05-28-phase3-observability.md` (phase 3 spec)

### Modified (debug UI)
- `MCCCD-AA140-SIMPL/debug-ui-src/debug.html` — log card replacement, NVX card, Raw Signal card, Sony/Newline/AirMedia cards, badges on device-bearing UI
- `MCCCD-AA140-SIMPL/debug-ui-src/debug.css` — chip + log-row + sticky-header + badge + NVX table + display-block + section-collapse + responsive
- `MCCCD-AA140-SIMPL/debug-ui-src/debug.js` — entire log polling/render pipeline replaced; observability state derivation; chip/search/scroll-lock/copy/expand handlers; new card POSTs; section-collapse handler

### Modified (backend SIMPL#)
- `MCCCD-AA140-SIMPL/MCCCD-AA140/ControlSystem.cs` — cleanup handler hookup, raw CIP capture moved to DebugTrace, OnProgramStatus method, _debug.Configure call extended with sony/newline/airmedia/panel
- `MCCCD-AA140-SIMPL/MCCCD-AA140/PanelDispatcher.cs` — DebugTrace.SigChange in OnSmartObjectSigChange
- `MCCCD-AA140-SIMPL/MCCCD-AA140/ShureTcpClient.cs` — DebugTrace.Lifecycle at connect-success/fail and socket-change; DebugTrace.Error on send-drop
- `MCCCD-AA140-SIMPL/MCCCD-AA140/NvxRoutingService.cs` — DebugTrace.Lifecycle at encoder/decoder online change, route apply, IP resolved (initial + retry)
- `MCCCD-AA140-SIMPL/MCCCD-AA140/CameraService.cs` — DebugTrace.Error at HTTP non-200 and HTTP exception
- `MCCCD-AA140-SIMPL/MCCCD-AA140/SonyVplService.cs` — inner Projector mutable host/enabled + SetHost/SetEnabled/Start/Stop; ApplyConfig1/ApplyConfig2; Initialize no longer auto-starts
- `MCCCD-AA140-SIMPL/MCCCD-AA140/NewlineService.cs` — mutable host/enabled + SetHost/SetEnabled/ApplyConfig; Connect/ScheduleReconnect gated by _enabled
- `MCCCD-AA140-SIMPL/MCCCD-AA140/AirMediaService.cs` — mutable host/enabled + SetHost/SetEnabled/ApplyConfig; PollStatus gated by _enabled; presentation calls short-circuit when disabled
- `MCCCD-AA140-SIMPL/MCCCD-AA140/Debug/DebugServer.cs` — Configure extended; HandleNvxPost, HandleSignalPost, HandleSonyPost, HandleNewlinePost, HandleAirMediaPost; ApplyConfigToService cases wired for sony-1/sony-2/newline/airmedia

## Quick pickup recipe for the next session

```bash
# Confirm you're on the right tip
cd "C:\Users\scale\CascadeProjects\Archon-Tests\MCCCD Razzle"
git checkout feat/drag-drop-router-mockup
git log --oneline -1   # expect df6e142

# Run the panel test rig (if the processor is in some other state)
PROC_HOST=192.168.2.198 python MCCCD-AA140-SIMPL/scripts/deploy.py \
  MCCCD-AA140-SIMPL/MCCCD-AA140/bin/Release/net6.0/MCCCD-AA140.cpz

# Quick smoke test of the new endpoints (requires Content-Length on empty POSTs)
H='Content-Length: 0'
curl -skX POST -H "$H" "https://192.168.2.198/cws/aa140/debug/nvx/route?dec=1&src=2"
curl -skX POST -H "$H" "https://192.168.2.198/cws/aa140/debug/signal?join=1&type=bool&value=true"
curl -skX POST -H "$H" "https://192.168.2.198/cws/aa140/debug/sony/1/hdmi1"
curl -skX POST -H "$H" "https://192.168.2.198/cws/aa140/debug/newline/hdmi1"
curl -skX POST -H "$H" "https://192.168.2.198/cws/aa140/debug/airmedia/start"

# Read the lessons doc + this handoff before touching code
less MCCCD-AA140/docs/Lessons-Learned/Debug-Panel-CWS-Lessons.md
less MCCCD-AA140/docs/Handoffs/2026-05-28-debug-panel-six-phase-buildout.md
```

## Where to start next session

If continuing the debug-panel arc, the highest-value follow-ups in rough order:

1. Item 5 above — wire DebugTrace lifecycle into Sony/Newline/AirMedia services. Quick win, makes phase-6 badges actually function.
2. Item 1 — command replay. Useful for testing flows without re-clicking. Medium effort.
3. Item 2 — fix the `nvx-nvx-384` cosmetic. Trivial.
4. Item 8 — clean up worktrees if disk pressure or branch list noise becomes annoying.

If switching to a different concern (panel aesthetics, contract editor, Q-SYS integration, etc.), this branch / tip is the right baseline to fork from.
