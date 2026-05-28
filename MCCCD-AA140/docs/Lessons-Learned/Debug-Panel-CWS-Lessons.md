# CWS Debug Panel + DebugTrace Wiring — Lessons Learned

**Scope.** Two-session arc on 2026-05-27 building a CWS-hosted debug panel for MCCCD-AA140, porting the architecture from 1Beyond/ISMIv3, then breaking it in spectacular fashion trying to wire physical-panel events into the live log. Concludes with a full processor reboot to recover.

**Tagged restore points (in chronological order):**

| Tag | What it represents |
|---|---|
| `checkpoint-nvx-unicast-routing` | NVX live, contract embed; debug panel not yet started |
| `checkpoint-panel-dispatcher` | Three services wired via PanelDispatcher; clean state |
| `checkpoint-dispatcher-verified` | Dispatcher trace + Shure backoff; verified via err log |
| **`checkpoint-debug-panel`** | **CWS debug panel functional; current HEAD after rollback** |
| `checkpoint-debug-trace-wired` | DebugTrace hooks added to hot paths; **broke CWS — do not deploy** |
| `stash@{0}` (broken-debug-trace-hooks-attempts) | Multi-iteration debugging attempts that destabilized init |

**Source files added in this arc:**
- [Debug/JsonProtocol.cs](../../../MCCCD-AA140-SIMPL/MCCCD-AA140/Debug/JsonProtocol.cs)
- [Debug/ResourceServer.cs](../../../MCCCD-AA140-SIMPL/MCCCD-AA140/Debug/ResourceServer.cs)
- [Debug/DebugTrace.cs](../../../MCCCD-AA140-SIMPL/MCCCD-AA140/Debug/DebugTrace.cs)
- [Debug/DebugServer.cs](../../../MCCCD-AA140-SIMPL/MCCCD-AA140/Debug/DebugServer.cs)
- [Debug/DeviceConfigStore.cs](../../../MCCCD-AA140-SIMPL/MCCCD-AA140/Debug/DeviceConfigStore.cs)
- [debug-ui-src/debug.html](../../../MCCCD-AA140-SIMPL/debug-ui-src/debug.html), [debug.js](../../../MCCCD-AA140-SIMPL/debug-ui-src/debug.js), [debug.css](../../../MCCCD-AA140-SIMPL/debug-ui-src/debug.css)

**Browser URL:** `https://<processor-ip>/cws/aa140/debug/` (after first-load reboot — see Lesson #1)

---

## TL;DR

1. **Crestron's `CwsRouter` caches the registered app's listener socket and survives `PROGLOAD` cycles.** When a program crashes or stops without a clean `HttpCwsServer.Unregister()`, the cached socket points to a dead listener and all subsequent CWS requests return `Connection refused`. **Only a full processor reboot clears it.**
2. **Always register `CrestronEnvironment.ProgramStatusEventHandler` to call `Unregister()` on `Stopping`** — before the first deploy. Don't wait until you hit this in production.
3. **`InitializeSystem` must complete in milliseconds.** Any synchronous TCP connect, `HttpCwsServer.Register()`, or other Crestron-SDK blocking call inside it can hang the processor in subtle, intermittent ways. Defer to worker threads / `CTimer` for everything network-touching.
4. **The err log is small (~25-50 entries).** A noisy diagnostic listener (e.g. `_tswPrimary.SigChange += ErrorLog.Notice(...)` firing on every slider tick) will push critical init messages out of the buffer within seconds. Silence diagnostic listeners before debugging anything else.
5. **When CWS misbehaves, reboot the processor before changing more code.** Half the "fixes" attempted in this session were chasing symptoms of the stale cache, not the underlying code.

---

## What worked

### The 1Beyond port
1Beyond's `ISMIv3.Debug.*` pattern transferred cleanly. The four-class architecture is solid:
- **`JsonProtocol`** — manual `StringBuilder` JSON. Avoids Newtonsoft.Json's intermittent reliability on Crestron Mono.
- **`ResourceServer`** — reads embedded `debug.html/js/css` from the assembly manifest via `Assembly.GetManifestResourceStream(prefix + filename)`. Simple, no dependencies.
- **`DebugTrace`** — 1000-event ring buffer + monotonic IDs + `DrainSince(since, max, out nextSince)` returns events to the polling endpoint. `CCriticalSection` for thread safety. Always-allocate strategy (no `_hasClients` short-circuit since polling clients aren't tracked) is fine at <10 events/sec.
- **`DebugServer`** — `HttpCwsServer` registered at `aa140/debug`. Routes split into static (embedded HTML/JS/CSS), GET `/events` polling, GET `/devices`, and POST handlers for each device class.

The csproj `<EmbeddedResource>` with `<LogicalName>` rewriting cleanly mapped `debug-ui-src/debug.html` → `MCCCD_AA140.Resources.debug.html` so the C# code references via a stable prefix.

### Browser UI worked first deploy
The vanilla HTML/JS/CSS loaded and rendered correctly on the first PROGLOAD. The polling client (`fetch('./events?since=N')` every 1s with `since` bookkeeping) ran without flicker. Device toggles, mic sliders, PTZ press-and-hold via `pointerdown`/`pointerup` events — all worked.

### Multi-tag checkpoint discipline saved the session
Five `checkpoint-*` tags were laid down through the arc, including before AND after each chunk. When the trace-wired commit broke things, `git reset --hard checkpoint-debug-panel` was a single command back to a known-working `.cpz`. Without those tags, we'd have spent an hour cherry-picking files.

### `DeviceConfigStore` is genuinely useful
Single JSON file at `/user/aa140/devices.json` with `{host, enabled}` per device. Sane baked defaults override missing keys, so adding a new device in `Defaults()` doesn't require deleting the file. The minimal hand-rolled JSON parser (no Newtonsoft) was ~80 lines and handled all expected shapes.

### Per-device `enabled` flag silences stub IPs
The `ShureTcpClient.Stop()` + `Connect()` gating on `_enabled` means disabled devices skip the TCP connect entirely. No reconnect timer, no log spam. The `Start()`/`Stop()`/`SetHost()`/`SetEnabled()` API is the right shape — replicate it for Sony/Newline inner TCP classes when commissioning.

---

## What broke (and why)

### Failure 1: `DebugTrace.SigChange` in PanelDispatcher hot path → CWS 500s

**Goal:** Make physical panel taps appear in the debug-panel live log alongside web-panel events.

**What I did:** Added `DebugTrace.SigChange("panel", FriendlyBoolName(args.Sig.Number), "bool", v)` inside `PanelDispatcher.OnSmartObjectSigChange` so every dispatched SmartObject publish became a structured event.

**What broke:** User reported "500 internal error on debug page in browser." The CWS layer started returning 500s for the static `debug.html` GET (not just the events endpoint).

**Root cause (in retrospect):** The DebugTrace addition alone wasn't the proximate cause — but it added load to the program right around the time a separate `CwsRouter` issue had begun manifesting. Looking at the err log, the actual error was:
```
Error: CwsRouter # Exception happened in RouteDataToCws: Connection refused
System.Net.Sockets.SocketException (0x80004005): Connection refused
```
The router was trying to forward incoming requests to a **listener socket that no longer existed** (because the previous program lifetime ended without a clean `Unregister`). Adding more code didn't fix it; only a reboot did.

**Lesson:** When the err log shows `CwsRouter: Connection refused`, the listener socket is dead. No amount of code change to the SIMPL# program will fix it. **Reboot first, diagnose second.**

### Failure 2: Defensive `Unregister()` before `Register()` → InitializeSystem hang

**Goal:** Clear any stale CWS registration before re-registering on a fresh `PROGLOAD`.

**What I did:** In `DebugServer.Start()`:
```csharp
try { _server.Unregister(); } catch { /* expected on clean boot */ }
_server.Register();
```

**What broke:** `InitializeSystem` hung forever. The processor never reached `PowerUpSequence`. The err log stopped at `"Cameras: cam1 ip -> 192.168.2.172"` and never logged anything further.

**Suspected cause:** `HttpCwsServer.Unregister()` on a never-registered server doesn't throw — it hangs. The try/catch can't help with a hang.

**Lesson:** Never call `Unregister()` defensively without prior `Register()`. If you need to clean up stale state, the only reliable mechanism is `ProgramStatusEventHandler.Stopping` calling `Unregister()` on the **outgoing** program, never on a freshly-constructed `HttpCwsServer`.

### Failure 3: Moving `Start()` to a worker thread → init still hung

**Goal:** Isolate the `Register()` hang so the main `InitializeSystem` thread completes regardless.

**What I did:** Wrapped both `_debug.Start()` and the `foreach` config-apply loop in `CrestronInvoke.BeginInvoke(_ => { ... })`.

**What broke:** Init still hung at `"Cameras: cam1 ip ->"`. The added complexity of worker threads + Shure connect retries + main thread continuing made the situation worse, not better.

**Suspected cause:** Multiple `BeginInvoke` calls plus several `ShureTcpClient.ConnectToServerAsync` calls saturated Crestron's thread pool. Combined with the (unrelated) stale `CwsRouter` cache making the CWS unreachable, the system entered a thrashing state.

**Lesson:** Adding code to fix a problem you don't understand makes diagnosis exponentially harder. The right move when a deploy breaks something was to **revert immediately** and study the err log carefully, not to layer on more changes.

### Failure 4: `TSW CHANGE:` diagnostic flooded the err log

**Goal:** Trace every panel publish (kept from the early `.cse2j` debugging phase).

**What broke:** The diagnostic logged every TSW SigChange. When the user dragged a slider, this fired dozens of times per second. The err log buffer is ~25-50 entries — within seconds, the buffer was full of `TSW CHANGE: join=17219 val=...` lines, and my init `Notice` messages were pushed out.

**Lesson:** Diagnostic listeners that fire on high-frequency events should be **opt-in** (e.g., a runtime flag exposed via the debug panel) or removed once their job is done. The TSW CHANGE listener served its purpose during the `.cse2j` work and should have been deleted at that checkpoint.

### Failure 5: Used invalid Crestron console commands

**What I did:** `progstop -p:01`, `progstart -p:01`, `lscwsapps`, etc.

**What broke:** "Bad or Incomplete Command" for each. They're not real Crestron 4-Series commands. The actual command is `PROGLOAD -P:01` (which is what `deploy.py` uses).

**Lesson:** Crestron command syntax doesn't follow common conventions. Check `progregister`, `progcomments`, or the persona's "Deployment Workflow" notes before improvising. `paramiko` makes it trivial to send `reboot` (the canonical fix for stale state).

---

## What we did right

### Five checkpoint tags
Tagging at every stable point — even mid-session ones like `dispatcher-verified` — meant the rollback was a single `git reset --hard <tag>` command. Stashing the broken work with `git stash push -u -m "broken-debug-trace-hooks-attempts"` preserved it for later inspection.

### Pattern matching against a working reference
Before writing the debug panel from scratch, surveyed 1Beyond's `ISMIv3.Debug/` via a focused subagent prompt. The agent returned a structural summary under 1500 words; from that, the port took an hour with no architectural rework. Same playbook as the NVX work earlier in the project (the PepperDash receiver-side discovery, the 1Beyond `ch5-cli archive -c` flag).

### The persona-first reflex
When the user asked "what personas do you have working with?", looking up the **Crestron CWS & WebSocket Protocol Engineer** persona (priority 50 in the project) and the **Crestron SIMPL# Engineer** persona returned authoritative rules verbatim (`HttpCwsServer.Register()` semantics, thread-safety, never-use-System.Threading.Thread). These rules would have prevented several of the bugs above had I consulted them earlier.

### Recognizing thrash and stopping
At one point the processor was repeatedly stopping and restarting the program (visible in err as alternating `Program 1 Stopped` and `Program Boot Directory:01`). Recognizing the thrash and deciding to **stop trying to fix it from code and just reboot** was the correct call. Three earlier code changes had made things worse.

---

## What we did wrong

### Layered fixes instead of revert-first
When the trace-wired build returned 500s, the right move was `git reset --hard checkpoint-debug-panel` immediately to confirm the trace change was the trigger. Instead I added defensive `Unregister()` → didn't fix it → added worker thread → made it worse → added diagnostic step `Notice` lines → flooded err log → couldn't see what was happening → continued adding code. The fix would have come faster from one revert.

### Diagnostic noise blocked diagnosis
The same `TSW CHANGE:` diagnostic that helped find the missing `.cse2j` earlier became the thing that hid the symptom of the new bug. **Diagnostic instrumentation has a half-life.** Remove or gate it the moment its job is done.

### Didn't add `ProgramStatusEventHandler` clean-shutdown until too late
1Beyond's `ControlSystem.cs` has a `ProgramStatusEventHandler` that explicitly disposes the `DebugServer` (calling `Unregister`) when the program stops. I noticed this during the port and **deferred adding it** to MCCCD-AA140. That deferral is what allowed the stale `CwsRouter` cache to accumulate — every test cycle that ended badly left a stale registration behind.

### Assumed `CrestronInvoke.BeginInvoke` was reliably async
`CrestronInvoke.BeginInvoke(Action<object>)` is documented as "queues to the Crestron thread pool, returns immediately." Under heavy contention (multiple queued work items + thread pool exhaustion from Shure connect retries), its behavior on this Mono build was not as advertised. Should have tested in isolation before relying on it inside `InitializeSystem`.

### Added Notice-line "step markers" instead of structured tracing
When init hung, I added `ErrorLog.Notice("Init step 7: cameras.Initialize");` etc. throughout `InitializeSystem`. These pushed older lines out of the err buffer. Should have used `DebugTrace.Lifecycle` (which goes to the ring buffer, never rotates within 1000 events) instead — but `DebugTrace` requires CWS to be readable, which was the broken part.

---

## Key insights

### 1. `CwsRouter` is a per-firmware singleton with cached socket
The exception trace from the err log was the smoking gun:
```
Error: CwsRouter # Exception happened in RouteDataToCws: Connection refused
System.Net.Sockets.SocketException (0x80004005): Connection refused
  at System.Net.Sockets.Socket.Connect (System.Net.EndPoint remoteEP)
  at Crestron.WebScripting.CwsRouter.RouteDataToCws (System.Byte[] bytes, System.Int32 len, System.Net.Sockets.Socket clientSocket)
```
`CwsRouter` lives in Crestron's web layer (above our SIMPL# program). It maintains a map of `cws-path → socket-of-registered-app`. When the app process exits without `Unregister()`, the socket entry persists. Subsequent requests are forwarded to the dead socket, which the OS rejects → `Connection refused`. PROGLOAD doesn't clear this; reboot does.

**Defense:**
```csharp
CrestronEnvironment.ProgramStatusEventHandler += (eventType) => {
    if (eventType == eProgramStatusEventType.Stopping) {
        try { _debug?.Dispose(); } catch { } // Dispose calls HttpCwsServer.Unregister()
    }
};
```
Add this to the **first** SIMPL# Pro program that uses HttpCwsServer, before the first deploy. Once a stale entry exists, only reboot clears it.

### 2. `InitializeSystem` should not block
The Crestron documentation's vague "Initialize devices and start services" language obscures that the function must complete in milliseconds. Anything that touches the network from `InitializeSystem` is a future hang:

- `TCPClient.ConnectToServerAsync()` is **named** async but may block briefly during socket setup on Crestron Mono.
- `HttpCwsServer.Register()` can block if there's stale state.
- `Crestron.SimplSharp.Net.Https.HttpsClient.Dispatch()` is synchronous and blocks for full TLS handshake.

Pattern: do all device construction in `InitializeSystem` (cheap), but defer all I/O to `CTimer` callbacks or worker threads explicitly:
```csharp
new CTimer(_ => StartServicesAndDevices(), 0); // returns immediately, fires on timer thread
```

### 3. The err log is small and easily flooded
Approximate buffer: 25-50 entries. Single-line entries push out older ones FIFO. **Anything firing more than ~1/sec is a flood.** Specifically:
- `_tswPrimary.SigChange += ErrorLog.Notice(...)` during slider drag → 20-30 events/sec
- Shure reconnect attempts without backoff → 1 per 5 sec per failed client → 0.6/sec for 3 clients
- Encoder `BaseEvent` if logged → very high

**Defense:** Use `DebugTrace.Lifecycle(...)` for events that should never be lost (init, errors, state transitions). It writes to a 1000-event ring buffer with `DrainSince` — much more forgiving.

### 4. Reboot is a valid step in the recovery playbook
On a Crestron processor, `reboot` from the SSH/console is non-destructive (CIPNet IP tables persist, IP config persists, program slot bindings persist) and clears:
- `CwsRouter` cached sockets
- Hung child processes
- Memory pressure from leaked TCP connections
- Anything in `/dev/shm` or similar transient state

Treat `reboot` the same way you'd treat a fresh deploy: free, fast (~90s for an RMC4), and frequently the right answer when "weird CWS behavior" or "init hangs" appear.

### 5. Multi-stash recovery preserves experimental work
Even when ripping changes out via `git reset --hard`, `git stash push -u -m "<descriptor>"` first preserves them. The stash can be inspected, cherry-picked, or revived later. Lost work isn't justifiable when stashes cost nothing.

---

## What to do next time

### Adding hot-path instrumentation
**Don't** add `DebugTrace.SigChange` (or anything like it) to a path that fires more than ~10 times per second without:
1. A runtime on/off flag exposed via the debug panel
2. A separate revert-only commit so reverting it doesn't lose unrelated work
3. Smoke-testing in isolation (deploy, verify CWS still responds, then add the next change)

### Adding ProgramStatusEventHandler.Stopping cleanup BEFORE first deploy
For any new SIMPL# Pro project using `HttpCwsServer`:
```csharp
public ControlSystem() : base() {
    CrestronEnvironment.ProgramStatusEventHandler += OnProgramStatus;
    // ... rest of ctor
}

private void OnProgramStatus(eProgramStatusEventType eventType) {
    if (eventType == eProgramStatusEventType.Stopping) {
        try { _debugServer?.Dispose(); } catch { } // → calls HttpCwsServer.Unregister
        // dispose other long-lived registrations here
    }
}
```
Otherwise the first crash builds a stale `CwsRouter` entry that requires reboot.

### Diagnostic listener hygiene
Pattern for `_tswPrimary.SigChange += ...` and similar high-frequency taps:
- Add them during a specific debugging session
- Tag a checkpoint INCLUDING the diagnostic
- **Delete or gate them in the next commit** once their job is done
- Don't carry them forward into production

### When stuck, revert in one commit
If a change breaks something unexpectedly, the very next action should be `git reset --hard <last-known-good-checkpoint>` + rebuild + redeploy. Confirm the bad change was the trigger. Only THEN start adding back the change incrementally to find what about it broke.

### Document init order in code
`InitializeSystem` should have a clear, documented order. Each step should be either:
- Zero I/O (object construction, signal subscription)
- Scheduled via `CTimer` or `CrestronInvoke.BeginInvoke` (deferred I/O)

Mixing the two in a single `InitializeSystem` body invites the kind of intermittent hangs this session hit.

---

## Restore points (final state)

- **`checkpoint-debug-panel`** (current HEAD, commit `63574de`) — debug panel functional after reboot. Page loads, devices API works, events stream live for web-panel-initiated commands. Physical panel taps NOT YET in the live log.
- `checkpoint-dispatcher-verified` (commit `d2b1940`) — pre-debug-panel state, clean PanelDispatcher with trace.
- `checkpoint-debug-trace-wired` (commit `5914d46`) — the broken trace-wired build. **Do not deploy.** Kept tagged so future investigation can compare.
- `stash@{0}` — multi-iteration debugging attempts (worker-thread Start, defensive Unregister, step-marker Notices, ProgramStatusEvent handler add). Inspect, cherry-pick the good bits, discard the rest.

## Where to pick up next session

1. **Confirm the program still has a ProgramStatusEventHandler that cleans up DebugServer** — the stashed change had this; the current HEAD does NOT. Cherry-pick that one piece from the stash, deploy, verify a clean shutdown happens on the next program stop. This prevents future stale `CwsRouter` cache buildup.
2. **Re-add `DebugTrace.SigChange` to `PanelDispatcher` — but smaller.** Single-line addition, single commit, immediate redeploy + verify. If CWS holds up, keep going. If not, revert that one commit and try a different approach.
3. **Retire the `TSW CHANGE` diagnostic in `ControlSystem.cs`** — its job is done; the panel routes via SmartObject 1 reliably now. Remove the listener so the err log isn't flooded during slider drags.
4. **Refactor Sony/Newline/AirMedia inner TCP classes** for runtime IP changes (same pattern as `ShureTcpClient.SetHost/SetEnabled`) so the debug panel's IP editor actually drives those services too.
