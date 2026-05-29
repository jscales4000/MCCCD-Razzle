# Phase 3 — Live observability surface

**Date.** 2026-05-28
**Branch.** `worktree-phase3-observability` (forked from phase-2 tip 00423fb)
**Scope.** Frontend only. Derive device status, NVX routes, and last-command/response client-side from the `/events` polling stream (using phase-2 lifecycle events). No backend changes.

## Goal

Make the debug page answer "what's the system doing right now?" without scrolling the log.

Three deliverables:

1. **Connection badges** on device-bearing UI: Devices card rows, Audio (P300) card header, Camera panel headers
2. **NVX Routing card** showing D1/D2/D3 ← current URL (with source name when resolvable)
3. **Last command + last response** lines on Audio card (P300) and per-camera lines on each Camera panel

All derived from the event stream that's already being polled. No new endpoint.

## State derivation

Add `deviceStatus` map to JS state. Updated inside `ingest()`. Shape:

```js
deviceStatus = {
  // Shure (set by device_connected / device_connect_failed / device_socket_change)
  'p300':  { online: bool, host: str, lastEventTs: str, lastEventType: str, lastError?: str },
  'mxa-a': { ...same shape },
  'mxa-b': { ...same shape },

  // NVX encoders (set by nvx_encoder_online_change + nvx_ip_resolved)
  'nvx-roompc':  { online: bool, mcast: str, ip?: str },
  'nvx-extpc':   { ... },
  'nvx-airmedia':{ ... },
  'nvx-nvx-384': { ... },   // note double-prefix — known cosmetic from phase 2

  // NVX decoders (set by nvx_decoder_online_change + nvx_route_change)
  'nvx-d1': { online: bool, routeUrl?: str, routeSrc?: int },
  'nvx-d2': { ... },
  'nvx-d3': { ... },

  // Cameras (set by error events from CameraService HTTP failures)
  'cam-1': { lastError?: str, lastErrorTs?: str },
  'cam-2': { ... },
};
```

Also track recent command/response per device:

```js
deviceCmds = {
  'p300': { lastCommand?: {method, ts, corrId}, lastResponse?: {raw, ts, corrId} },
  // populated for all devices that appear in command/response event types
};
```

Update logic:

- `device_connected` → `online=true`, clear `lastError`
- `device_connect_failed` → `online=false`, `lastError = "connect failed: " + status + " (attempt N)"`
- `device_socket_change` → `online=false`, `lastError = "socket: " + status`
- `nvx_encoder_online_change` / `nvx_decoder_online_change` → set `online` on the device entry
- `nvx_ip_resolved` → set `ip` and (for the decoder consumers of this src) update `routeSrc`
- `nvx_route_change` → set `routeUrl` and `routeSrc` (parsed from URL)
- `command` event → set `lastCommand` for `device`
- `response` event → set `lastResponse` for `device`
- `error` event → set `lastError` + `lastErrorTs` for `device`

## URL → source index mapping

Multicast addresses are stable constants (matching `MCAST_VIDEO_*` in `NvxRoutingService.cs`):

| Pattern | Source index | Label |
|---|---|---|
| `rtsp://239.8.0.0:554/...` | 1 | RoomPC |
| `rtsp://239.8.0.4:554/...` | 2 | ExtPC |
| `rtsp://239.8.0.8:554/...` | 3 | AirMedia |
| `rtsp://239.8.0.12:554/...` | 4 | NVX-384 |

Unicast: derive from `deviceStatus['nvx-<label>'].ip` — when a `nvx_ip_resolved` event fires for `nvx-roompc` with IP X, store `encoderIps[X] = 1`. When a route URL contains that IP, map to source 1.

## DOM additions

### Devices card (existing rows)
Add a badge `<span class="dev-badge" data-key="...">●</span>` after `dev-status` in each row. Colors: green=online, red=offline-with-error, gray=unknown.

### Audio card
Add header badge after "Audio (Shure P300)" h2.
Add a footer line: `<div class="dev-detail">last cmd: … · last resp: …</div>`.

### Camera panels
Add header badge next to camera name + IP.
Add a footer line: `<div class="dev-detail" data-cam-N>last error: …</div>` (visible only when there is one).

### NEW NVX Routing card
Wide card placed after System / Audio (insert before `#cameras-card`):

```html
<section class="card wide" id="nvx-card">
  <h2>NVX Routing</h2>
  <table class="nvx-route-table">
    <thead><tr><th>Decoder</th><th>State</th><th>Source</th><th>URL</th></tr></thead>
    <tbody>
      <tr data-dec="nvx-d1"><td>D1</td><td class="state">—</td><td class="src">—</td><td class="url">—</td></tr>
      <tr data-dec="nvx-d2"><td>D2</td><td class="state">—</td><td class="src">—</td><td class="url">—</td></tr>
      <tr data-dec="nvx-d3"><td>D3</td><td class="state">—</td><td class="src">—</td><td class="url">—</td></tr>
    </tbody>
  </table>
  <h3 class="muted small">Encoders</h3>
  <table class="nvx-encoder-table">
    <thead><tr><th>Encoder</th><th>State</th><th>IP</th><th>Multicast</th></tr></thead>
    <tbody>
      <tr data-enc="nvx-roompc"><td>RoomPC</td><td class="state">—</td><td class="ip">—</td><td class="mcast">—</td></tr>
      <tr data-enc="nvx-extpc"><td>ExtPC</td><td class="state">—</td><td class="ip">—</td><td class="mcast">—</td></tr>
      <tr data-enc="nvx-airmedia"><td>AirMedia</td><td class="state">—</td><td class="ip">—</td><td class="mcast">—</td></tr>
      <tr data-enc="nvx-nvx-384"><td>NVX-384</td><td class="state">—</td><td class="ip">—</td><td class="mcast">—</td></tr>
    </tbody>
  </table>
</section>
```

## CSS additions

```css
.dev-badge { display: inline-block; width: 8px; height: 8px; border-radius: 50%; background: #64748b; margin-left: 6px; vertical-align: middle; }
.dev-badge.online { background: var(--good); box-shadow: 0 0 6px rgba(52,211,153,0.6); }
.dev-badge.offline { background: var(--bad); }
.dev-badge.warn { background: var(--warn); }

.dev-detail { font-size: 11px; color: var(--muted); font-family: ui-monospace, monospace; margin-top: 6px; word-break: break-all; }
.dev-detail .k { color: var(--fg); }

#nvx-card table { width: 100%; border-collapse: collapse; font-size: 12px; }
#nvx-card th { text-align: left; font-weight: 600; color: var(--muted); padding: 6px 8px; border-bottom: 1px solid var(--border); font-size: 10px; text-transform: uppercase; letter-spacing: 0.08em; }
#nvx-card td { padding: 6px 8px; border-bottom: 1px solid rgba(148,163,184,0.08); font-family: ui-monospace, monospace; }
#nvx-card td.state { font-weight: 600; }
#nvx-card td.state.online { color: var(--good); }
#nvx-card td.state.offline { color: var(--bad); }
#nvx-card td.state.unknown { color: var(--muted); }
#nvx-card td.url { color: var(--accent); word-break: break-all; }
#nvx-card td.src { color: var(--fg); }
#nvx-card h3 { margin: 16px 0 8px; }
```

## JS additions

Three new functions wired into the existing render lifecycle:

```js
function updateDeviceStatus(ev) { /* match by ev.eventType + ev.data, update deviceStatus / deviceCmds / encoderIps */ }
function renderStatus() {
  // Update each .dev-badge[data-key], .dev-detail, and the NVX table cells.
  // Cheap: read state, write text/class. No row creation after initial DOM exists.
}
function urlToSourceIndex(url) {
  if (!url) return 0;
  if (url.indexOf("239.8.0.0:")  >= 0) return 1;
  if (url.indexOf("239.8.0.4:")  >= 0) return 2;
  if (url.indexOf("239.8.0.8:")  >= 0) return 3;
  if (url.indexOf("239.8.0.12:") >= 0) return 4;
  // unicast — match to known encoder IP
  for (var ip in encoderIps) if (url.indexOf(ip + ":") >= 0) return encoderIps[ip];
  return 0;
}
```

Hook `updateDeviceStatus(ev)` inside the existing `ingest()` loop (one call per event). Call `renderStatus()` at the end of `ingest()`. Both are cheap; render writes text and toggles classes on a small fixed DOM set.

Initial render after `loadDevices()` populates the rows with badges; status renders on first poll response.

## Source labels

```js
var SRC_LABELS = { 1: "RoomPC", 2: "ExtPC", 3: "AirMedia", 4: "NVX-384", 0: "—" };
```

## Out of scope

- **Polling** an additional `/status` endpoint — the lifecycle stream is sufficient
- **Last-cmd persistence across reload** — phase 6 polish
- **Color-coding by recency** — flat state-color only (current online vs offline)
- **Encoder IP table editing** — phase 4 covers that

## Plan

Single task — all DOM, CSS, JS additions land together, deploy once.

### Task 1 — Implementation
- Modify `debug.html`: add NVX Routing card; add badge slots on devices/audio/camera; add detail lines on audio/cameras
- Modify `debug.css`: badge, detail, NVX-table styles
- Modify `debug.js`: `deviceStatus`, `deviceCmds`, `encoderIps` state; `updateDeviceStatus`, `renderStatus`, `urlToSourceIndex` functions; hook into `ingest()`; ensure badges render after `loadDevices()`

### Task 2 — Build + deploy + verify
- `dotnet build -c Release` → `.cpz`
- `PROC_HOST=192.168.2.198 python scripts/deploy.py …`
- Probe `/events` to confirm CWS up
- Browser verify:
  1. NVX Routing card renders with three decoder rows + four encoder rows
  2. On boot, encoder/decoder rows turn green within ~5s
  3. Mic chip on Devices card stays gray for disabled Shure, red after retry attempts
  4. Push a panel route → corresponding D-row updates URL + Source within 1s
  5. Audio card header has a badge; footer shows last cmd/resp (after pressing a button)
  6. Camera headers have badges; footer error line appears if HTTP fails

### Task 3 — Commit + tag `checkpoint-observability-surface`
