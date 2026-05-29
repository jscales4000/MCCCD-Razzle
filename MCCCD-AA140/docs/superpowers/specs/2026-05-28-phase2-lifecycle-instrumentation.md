# Phase 2 — Device connection lifecycle into DebugTrace

**Date.** 2026-05-28
**Scope.** Backend only. Add `DebugTrace.Lifecycle` / `DebugTrace.Error` calls **alongside** existing `ErrorLog.*` calls at connection-state-change sites. Existing err-log behavior unchanged. The browser live log gains the same data already going to err.

## Why

Phase 3 (observability) needs structured connection events to render per-device badges + route table. Phase 1 (log UX) gives the browser a place to display them. Phase 2 emits them.

## Touchpoint summary

| File | Sites | Emits |
|---|---|---|
| `ShureTcpClient.cs` | 4 | device_connected · device_connect_failed · device_socket_change · drop |
| `NvxRoutingService.cs` | 4 | nvx_encoder_online_change · nvx_decoder_online_change · nvx_route_change · nvx_ip_resolved |
| `CameraService.cs` | 2 | cam-N HTTP errors |

## Device key conventions

| Service | Device key |
|---|---|
| ShureTcpClient `_name="P300"` | `p300` |
| ShureTcpClient `_name="MXA-A"` | `mxa-a` |
| ShureTcpClient `_name="MXA-B"` | `mxa-b` |
| NVX encoder labels | `nvx-roompc`, `nvx-extpc`, `nvx-airmedia`, `nvx-nvx384` (lowercase, no spaces) |
| NVX decoders | `nvx-d1`, `nvx-d2`, `nvx-d3` |
| Cameras | `cam-1`, `cam-2` |

All taken from the existing `_name` field (lowercased) or composed from indices to align with the device chip vocabulary the browser already shows for browser-initiated commands.

## ShureTcpClient.cs — 4 changes

**Add `using MCCCD_AA140.Debug;` at top (if not already present).**

Sites and emissions:

1. **Connected success** — line ~138, inside `OnConnectComplete`, after `ErrorLog.Notice(...connected to...)`:
```csharp
DebugTrace.Lifecycle("device_connected", new Dictionary<string, object> {
    { "device", _name.ToLowerInvariant() },
    { "host", _host },
    { "port", _port },
});
```

2. **Connect failed** — line ~148, inside `OnConnectComplete` failure branch, after `ErrorLog.Warn(...connect returned status...)`:
```csharp
DebugTrace.Lifecycle("device_connect_failed", new Dictionary<string, object> {
    { "device", _name.ToLowerInvariant() },
    { "host", _host },
    { "status", c.ClientStatus.ToString() },
    { "attempt", _failedAttempts },
});
```

3. **Socket state change** — line ~159, inside `OnSocketStatusChange` non-connected branch, after `ErrorLog.Notice(...socket {status}, reconnecting...)`:
```csharp
DebugTrace.Lifecycle("device_socket_change", new Dictionary<string, object> {
    { "device", _name.ToLowerInvariant() },
    { "status", status.ToString() },
});
```

4. **Send drop** — line ~112, inside `Send` if-not-connected branch, after `ErrorLog.Notice(...drop (not connected)...)`:
```csharp
DebugTrace.Error(_name.ToLowerInvariant(), "drop (not connected): " + command);
```

Needs `using System.Collections.Generic;` (likely already present — verify and add if missing).

## NvxRoutingService.cs — 4 changes

**Add `using MCCCD_AA140.Debug;` and `using System.Collections.Generic;` if missing.**

Sites (line numbers approximate — locate by surrounding `ErrorLog.*` call):

1. **Encoder OFFLINE / ONLINE** — at the two `ErrorLog.Notice("NVX {0}: OFFLINE", label)` / `OFFLINE → ONLINE` sites (~126 and ~132):
```csharp
DebugTrace.Lifecycle("nvx_encoder_online_change", new Dictionary<string, object> {
    { "device", "nvx-" + label.ToLowerInvariant() },
    { "online", true },        // false at the OFFLINE site
    { "mcast", mcastVideo },
});
```
*(Two emissions: one in the OFFLINE branch with online=false, one in the ONLINE branch with online=true.)*

2. **Decoder OFFLINE / ONLINE** — at the `ErrorLog.Notice("NVX D{0}: OFFLINE/ONLINE...")` sites (~235 and ~251):
```csharp
DebugTrace.Lifecycle("nvx_decoder_online_change", new Dictionary<string, object> {
    { "device", "nvx-d" + displayNum },
    { "online", true },        // false at the OFFLINE site
});
```

3. **Route apply** — line ~321, after `ErrorLog.Notice("NVX route: D{0} <- {1}", displayNum, url)`:
```csharp
DebugTrace.Lifecycle("nvx_route_change", new Dictionary<string, object> {
    { "device", "nvx-d" + displayNum },
    { "url", url },
});
```

4. **IP resolved (initial + retry)** — at both `ErrorLog.Notice("NVX {0}: src{1} ip={2} via {3} -> ...")` sites (~150 and ~201):
```csharp
DebugTrace.Lifecycle("nvx_ip_resolved", new Dictionary<string, object> {
    { "device", "nvx-" + label.ToLowerInvariant() },
    { "src", srcIndex },
    { "ip", resolvedIp },
    { "url", newUrl },
});
```

## CameraService.cs — 2 changes

**Add `using MCCCD_AA140.Debug;` and `using System.Collections.Generic;` if missing.**

1. **HTTP non-200** — line ~219, after `ErrorLog.Warn("CameraService HTTP {0}: {1}", resp.Code, url)`:
```csharp
DebugTrace.Error("cam-" + _active, "HTTP " + resp.Code + ": " + url);
```

2. **HTTP exception** — line ~222, after `ErrorLog.Error("CameraService HTTP exception: {0}", ex.Message)`:
```csharp
DebugTrace.Error("cam-" + _active, "HTTP exception: " + ex.Message);
```

## Out of scope (defer to phase 3 or later)

- `nvx_route_apply_failed` and other warn-level paths — phase 3 if needed
- Per-protocol commands/responses inside ShureP300Service / ShureMxaService (these have their own ErrorLog calls but aren't connection lifecycle)
- Sony / Newline / AirMedia inner clients (phase 4 — they don't have stable lifecycle infrastructure yet)
- IP-conflict / NVX advisory events

## Testing

Manual via browser at `/cws/aa140/debug/`:

1. **Initial deploy.** Boot lifecycle events for encoders + decoders appear in live log within ~5s.
2. **Disable a Shure device.** Toggle MXA-B off via Devices card. Within ~5s, a `device_socket_change` event appears in the log with device=`mxa-b`. Connect-fail spam stops.
3. **Re-enable the Shure device.** `device_connect_failed` events appear at the retry cadence (5s for first 3 attempts then 60s) until either the device is reachable or stays disabled. If the IP is reachable, `device_connected` event shows.
4. **Route a display.** Push a route on the panel → `nvx_route_change` event appears with the resolved URL.
5. **Click a `nvx_encoder_online_change` row.** Detail JSON shows `device`, `online`, `mcast` fields.

## Commit + tag

Single commit. Tag: `checkpoint-lifecycle-instrumentation`.

## Plan

Three tasks:

### Task 1 — ShureTcpClient.cs (4 edits)
Edit each of the 4 sites listed above. Verify `using System.Collections.Generic;` is present (it is — line 1 area). Add `using MCCCD_AA140.Debug;`.

### Task 2 — NvxRoutingService.cs (6 edits, since some sites have OFFLINE + ONLINE pairs) + CameraService.cs (2 edits)
Edit per the spec. Verify usings.

### Task 3 — Build + deploy + verify + commit + tag
Standard build/deploy. Browser verification per the 5-step list above. Commit message scopes `feat(debug-trace):`. Tag.
