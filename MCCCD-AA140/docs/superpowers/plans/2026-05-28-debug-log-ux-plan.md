# Debug Panel Log UX Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the freeform Live Log card with a filterable, searchable, scroll-locked event browser inside the existing CWS debug panel.

**Architecture:** Pure frontend in `MCCCD-AA140-SIMPL/debug-ui-src/`. Three files: `debug.html` (replace `#log-card` markup), `debug.css` (add chip + row + sticky-header styles), `debug.js` (replace the log append-on-poll path with state + filter + render pipeline). No backend changes. All filtering client-side over an in-memory event array. After each task, rebuild `.cpz` and deploy to the RMC4 at `192.168.2.198`, then verify the listed acceptance criteria in a browser at `https://192.168.2.198/cws/aa140/debug/`.

**Tech Stack:** Vanilla HTML/CSS/JS (no framework, no build step beyond C# assembly embedding). C# `dotnet build -c Release` for the `.cpz`. `paramiko`-based `deploy.py` for SFTP+PROGLOAD.

**Spec:** [docs/superpowers/specs/2026-05-28-debug-log-ux-design.md](../specs/2026-05-28-debug-log-ux-design.md)

---

## Task 1: Replace `#log-card` HTML + add chip/row CSS

**Files:**
- Modify: `MCCCD-AA140-SIMPL/debug-ui-src/debug.html` lines 181–187 (the `<section class="card wide" id="log-card">` block)
- Modify: `MCCCD-AA140-SIMPL/debug-ui-src/debug.css` lines 86–100 (existing `/* LOG */` block) + append new rules at end

- [ ] **Step 1: Replace the `#log-card` markup**

In `MCCCD-AA140-SIMPL/debug-ui-src/debug.html`, find this block (lines 181–187):

```html
    <!-- Live log -->
    <section class="card wide" id="log-card">
      <h2>Live Log
        <button id="log-clear" class="btn small">clear</button>
        <label class="muted small"><input id="log-pause" type="checkbox"/> pause</label>
      </h2>
      <div id="log-body" class="log-body"></div>
    </section>
```

Replace with:

```html
    <!-- Live log -->
    <section class="card wide" id="log-card">
      <div class="log-hdr">
        <div class="log-titlebar">
          <h2>Live Log</h2>
          <div class="log-meta">
            <span id="log-count-total">0</span> events · <span id="log-count-visible">0</span> visible
            <button id="log-clear" class="btn small">clear</button>
            <button id="log-copy"  class="btn small">copy</button>
          </div>
        </div>

        <div class="log-chips">
          <span class="chip-group-label">type</span>
          <div id="log-chips-type" class="chips"></div>
        </div>

        <div class="log-chips">
          <span class="chip-group-label">device</span>
          <div id="log-chips-device" class="chips"></div>
        </div>

        <div class="log-controls">
          <input id="log-search" type="text" placeholder="search…" autocomplete="off" spellcheck="false"/>
          <button id="log-scroll-lock" class="btn small">▶ live</button>
          <button id="log-jump" class="btn small" hidden>↓ jump to bottom</button>
        </div>
      </div>

      <div id="log-body" class="log-body"></div>
    </section>
```

- [ ] **Step 2: Replace the `/* LOG */` CSS block**

In `MCCCD-AA140-SIMPL/debug-ui-src/debug.css`, find lines 86–100 (the `/* LOG */` block). Replace the entire block with:

```css
/* LOG */
#log-card { padding: 0; overflow: hidden; }

.log-hdr {
  position: sticky;
  top: 0;
  z-index: 5;
  background: var(--card);
  border-bottom: 1px solid var(--border);
  padding: 12px 16px;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.log-titlebar { display: flex; align-items: center; justify-content: space-between; gap: 12px; }
.log-titlebar h2 { margin: 0; font-size: 15px; letter-spacing: 0.04em; text-transform: uppercase; color: var(--muted); }
.log-meta { display: flex; align-items: center; gap: 10px; color: var(--muted); font-size: 12px; font-family: ui-monospace, SFMono-Regular, monospace; }

.log-chips { display: flex; align-items: center; gap: 6px; flex-wrap: wrap; }
.chip-group-label { color: var(--muted); font-size: 10px; text-transform: uppercase; letter-spacing: 0.08em; min-width: 50px; }
.chips { display: flex; flex-wrap: wrap; gap: 4px; }
.chip {
  display: inline-flex; align-items: center; gap: 4px;
  padding: 2px 8px;
  background: rgba(148, 163, 184, 0.10);
  color: var(--muted);
  border: 1px solid transparent;
  border-radius: 12px;
  font-size: 11px;
  cursor: pointer;
  user-select: none;
  font-family: ui-monospace, SFMono-Regular, monospace;
}
.chip:hover { background: rgba(148, 163, 184, 0.18); }
.chip.on {
  background: rgba(61, 126, 255, 0.15);
  color: var(--fg);
  border-color: rgba(61, 126, 255, 0.45);
}
.chip .ct { font-size: 10px; color: var(--muted); font-variant-numeric: tabular-nums; }
.chip.on .ct { color: var(--accent); }

.log-controls { display: flex; gap: 8px; align-items: center; }
.log-controls input {
  flex: 1;
  padding: 4px 10px;
  background: rgba(15, 23, 42, 0.7);
  border: 1px solid var(--border);
  color: var(--fg);
  border-radius: 6px;
  font-family: ui-monospace, SFMono-Regular, monospace;
  font-size: 12px;
}
.log-controls input:focus { outline: none; border-color: var(--accent); }

.log-body {
  font-family: ui-monospace, SFMono-Regular, monospace;
  font-size: 12px;
  line-height: 1.4;
  max-height: 480px;
  overflow-y: auto;
  background: rgba(0, 0, 0, 0.30);
  padding: 4px 0;
}

.log-row {
  display: grid;
  grid-template-columns: 90px 96px 110px 1fr;
  gap: 8px;
  padding: 2px 16px;
  border-bottom: 1px solid rgba(148, 163, 184, 0.06);
  cursor: pointer;
}
.log-row:hover { background: rgba(255, 255, 255, 0.03); }
.log-row.expanded { background: rgba(61, 126, 255, 0.06); }
.log-row .t  { color: var(--muted); }
.log-row .ty { font-weight: 600; }
.log-row .ty.sig_change   { color: #38bdf8; }
.log-row .ty.command      { color: #a78bfa; }
.log-row .ty.response     { color: #34d399; }
.log-row .ty.error        { color: var(--bad); }
.log-row .ty.lifecycle    { color: var(--warn); }
.log-row .ty.state_change { color: var(--warn); }
.log-row .d  { color: var(--fg); }
.log-row .v  { color: var(--muted); white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.log-row .v .k { color: #94a3b8; }

.log-detail {
  padding: 6px 16px 10px 16px;
  background: rgba(61, 126, 255, 0.04);
  border-bottom: 1px solid rgba(148, 163, 184, 0.06);
  color: var(--fg);
  font-family: ui-monospace, SFMono-Regular, monospace;
  font-size: 11px;
  line-height: 1.5;
  white-space: pre-wrap;
}
```

- [ ] **Step 3: Build and deploy**

```bash
cd "/c/Users/scale/CascadeProjects/Archon-Tests/MCCCD Razzle/.claude/worktrees/debug-ui-buildout/MCCCD-AA140-SIMPL/MCCCD-AA140" && dotnet build -c Release 2>&1 | tail -5
```

Expected: `Build succeeded. 0 Error(s)` (one pre-existing warning about `_systemOn`).

```bash
PROC_HOST=192.168.2.198 python "/c/Users/scale/CascadeProjects/Archon-Tests/MCCCD Razzle/.claude/worktrees/debug-ui-buildout/MCCCD-AA140-SIMPL/scripts/deploy.py" "/c/Users/scale/CascadeProjects/Archon-Tests/MCCCD Razzle/.claude/worktrees/debug-ui-buildout/MCCCD-AA140-SIMPL/MCCCD-AA140/bin/Release/net6.0/MCCCD-AA140.cpz" 2>&1 | tail -8
```

Expected: `[deploy] OK in ~17s`, `**Restarting Program:1**` line in output.

- [ ] **Step 4: Verify chrome renders (UI shell only — chips will be empty until Task 2)**

In browser at `https://192.168.2.198/cws/aa140/debug/`, scroll to Live Log card. Verify:
- Sticky header visible at top of log card
- "0 events · 0 visible" meta line + clear/copy buttons
- Two empty `type` and `device` chip rows
- Search input + "▶ live" button + hidden "↓ jump to bottom" button
- Log body empty (because the existing `appendLog` path still exists but appends `.log-line` divs that are no longer styled — that's fine; Task 2 replaces it)

**Do not commit yet.** This is a half-state. Task 2 finishes the migration.

---

## Task 2: Replace log polling + rendering in `debug.js`

**Files:**
- Modify: `MCCCD-AA140-SIMPL/debug-ui-src/debug.js` — replace lines 1–99 (everything from the IIFE start through the `$logPause.addEventListener` block). The device-loading, camera, mic, audio, power, and boot sections (lines 100+) stay unchanged.

- [ ] **Step 1: Replace the head of `debug.js`**

In `MCCCD-AA140-SIMPL/debug-ui-src/debug.js`, replace lines 1–99 with the following. Keep everything from line 100 onward (`// ─── Devices UI ───` and below) UNCHANGED.

```javascript
// AA140 debug panel — vanilla JS, no build step. All UI state derives from
// two backend endpoints: GET /events (event ring) and GET /devices (config).

(function () {
  "use strict";

  // ─── State ─────────────────────────────────────────────────────────
  var POLL_MS = 1000;
  var since = 0;
  var consecutiveErrors = 0;
  var MAX_EVENTS = 5000;   // client-side ring cap; server ring is 1000

  var eventsAll = [];      // all events received, oldest first
  var typeFilters   = new Set(["sig_change", "command", "response", "error", "lifecycle", "state_change"]);
  var deviceFilters = new Set();         // empty = match all
  var deviceSeen    = new Set();         // for dynamic chip population
  var typeCounts    = Object.create(null);
  var deviceCounts  = Object.create(null);
  var searchTerm    = "";
  var searchDebounce = null;
  var scrollLocked  = false;
  var pendingRender = false;
  var expandedIds   = new Set();         // event ids whose detail is expanded

  var TYPE_LIST = ["sig_change", "command", "response", "error", "lifecycle", "state_change"];

  // ─── DOM ───────────────────────────────────────────────────────────
  var $pollStatus    = document.getElementById("poll-status");
  var $pollCur       = document.getElementById("poll-cur");
  var $pollHost      = document.getElementById("poll-host");
  var $logBody       = document.getElementById("log-body");
  var $logClear      = document.getElementById("log-clear");
  var $logCopy       = document.getElementById("log-copy");
  var $logCountTotal = document.getElementById("log-count-total");
  var $logCountVis   = document.getElementById("log-count-visible");
  var $chipsType     = document.getElementById("log-chips-type");
  var $chipsDevice   = document.getElementById("log-chips-device");
  var $search        = document.getElementById("log-search");
  var $scrollLock    = document.getElementById("log-scroll-lock");
  var $jumpBottom    = document.getElementById("log-jump");
  var $devicesBody   = document.getElementById("devices-body");

  $pollHost.textContent = window.location.host;

  // Device key → human label. Order matters (display order in the UI).
  var DEVICE_KEYS = [
    ["p300",     "Shure P300 DSP"],
    ["mxa-a",    "Shure MXA-A"],
    ["mxa-b",    "Shure MXA-B"],
    ["sony-1",   "Sony VPL #1"],
    ["sony-2",   "Sony VPL #2"],
    ["newline",  "Newline display"],
    ["airmedia", "AirMedia"],
    ["cam-1",    "Camera 1"],
    ["cam-2",    "Camera 2"],
  ];

  // ─── Event polling ─────────────────────────────────────────────────
  function poll() {
    fetch("./events?since=" + since + "&max=200", { cache: "no-store" })
      .then(function (r) {
        if (!r.ok) throw new Error("HTTP " + r.status);
        return r.json();
      })
      .then(function (data) {
        consecutiveErrors = 0;
        $pollStatus.className = "pill ok";
        $pollStatus.textContent = "live";
        $pollCur.textContent = "events: " + (data.current || 0);
        since = data.next || since;

        if (data.events && data.events.length) {
          ingest(data.events);
          scheduleRender();
        }
      })
      .catch(function () {
        consecutiveErrors++;
        $pollStatus.className = "pill bad";
        $pollStatus.textContent = "err (" + consecutiveErrors + ")";
      })
      .then(function () {
        setTimeout(poll, POLL_MS);
      });
  }

  function ingest(events) {
    var newDevices = false;
    for (var i = 0; i < events.length; i++) {
      var ev = events[i];
      if (!ev) continue;
      // Stable id for expand-state across re-renders; use server timestamp+device+i fallback.
      ev._id = ev.timestamp + "|" + (ev.device || "") + "|" + i + "|" + eventsAll.length;
      var t = ev.eventType || "?";
      var d = ev.device || "system";
      typeCounts[t]   = (typeCounts[t]   || 0) + 1;
      deviceCounts[d] = (deviceCounts[d] || 0) + 1;
      if (!deviceSeen.has(d)) {
        deviceSeen.add(d);
        deviceFilters.add(d);  // default: ON for any newly-seen device
        newDevices = true;
      }
      eventsAll.push(ev);
    }
    while (eventsAll.length > MAX_EVENTS) eventsAll.shift();
    renderChips(newDevices);
    $logCountTotal.textContent = eventsAll.length;
  }

  // ─── Chip rows ─────────────────────────────────────────────────────
  function renderChips(force) {
    // Type row: fixed list, always rendered.
    if (!$chipsType.firstChild || force === "all") {
      $chipsType.innerHTML = "";
      TYPE_LIST.forEach(function (t) {
        $chipsType.appendChild(makeChip(t, typeFilters, "type"));
      });
    } else {
      // Just update counts in place.
      Array.prototype.forEach.call($chipsType.children, function (el) {
        var t = el.getAttribute("data-key");
        var ct = el.querySelector(".ct");
        if (ct) ct.textContent = typeCounts[t] || 0;
      });
    }

    // Device row: dynamic. Re-render entirely if new device appeared, else update counts.
    if (force === true || force === "all" || !$chipsDevice.firstChild) {
      $chipsDevice.innerHTML = "";
      var devs = Array.from(deviceSeen).sort(function (a, b) {
        if (a === "system") return  1;
        if (b === "system") return -1;
        return a < b ? -1 : a > b ? 1 : 0;
      });
      devs.forEach(function (d) {
        $chipsDevice.appendChild(makeChip(d, deviceFilters, "device"));
      });
    } else {
      Array.prototype.forEach.call($chipsDevice.children, function (el) {
        var d = el.getAttribute("data-key");
        var ct = el.querySelector(".ct");
        if (ct) ct.textContent = deviceCounts[d] || 0;
      });
    }
  }

  function makeChip(key, filterSet, kind) {
    var el = document.createElement("span");
    el.className = "chip" + (filterSet.has(key) ? " on" : "");
    el.setAttribute("data-key", key);
    el.setAttribute("data-kind", kind);
    var label = document.createElement("span");
    label.textContent = key;
    var ct = document.createElement("span");
    ct.className = "ct";
    ct.textContent = (kind === "type" ? typeCounts[key] : deviceCounts[key]) || 0;
    el.appendChild(label);
    el.appendChild(ct);
    el.addEventListener("click", function () { toggleChip(key, filterSet, el); });
    return el;
  }

  function toggleChip(key, filterSet, el) {
    if (filterSet.has(key)) filterSet.delete(key); else filterSet.add(key);
    el.classList.toggle("on");
    scheduleRender();
  }

  // ─── Render ────────────────────────────────────────────────────────
  function scheduleRender() {
    if (pendingRender) return;
    pendingRender = true;
    requestAnimationFrame(function () { pendingRender = false; renderLog(); });
  }

  function renderLog() {
    var wasAtBottom = !scrollLocked && atBottom();
    var rows = [];
    var visible = 0;
    var term = searchTerm;

    for (var i = 0; i < eventsAll.length; i++) {
      var ev = eventsAll[i];
      if (!passes(ev, term)) continue;
      visible++;
      rows.push(buildRow(ev));
      if (expandedIds.has(ev._id)) rows.push(buildDetail(ev));
    }
    $logBody.innerHTML = "";
    for (var j = 0; j < rows.length; j++) $logBody.appendChild(rows[j]);
    $logCountVis.textContent = visible;

    if (wasAtBottom) $logBody.scrollTop = $logBody.scrollHeight;
  }

  function passes(ev, term) {
    var t = ev.eventType || "?";
    var d = ev.device || "system";
    if (!typeFilters.has(t)) return false;
    if (deviceFilters.size > 0 && !deviceFilters.has(d)) return false;
    if (term) {
      var s = JSON.stringify(ev).toLowerCase();
      if (s.indexOf(term) === -1) return false;
    }
    return true;
  }

  function buildRow(ev) {
    var d = document.createElement("div");
    d.className = "log-row";
    d.setAttribute("data-id", ev._id);
    if (expandedIds.has(ev._id)) d.classList.add("expanded");
    d.innerHTML =
      '<span class="t">'  + escapeHtml(shortTs(ev.timestamp)) + '</span>' +
      '<span class="ty ' + escapeHtml(ev.eventType || "?") + '">' + escapeHtml(ev.eventType || "?") + '</span>' +
      '<span class="d">'  + escapeHtml(ev.device || "system") + '</span>' +
      '<span class="v">'  + bodyHtml(ev) + '</span>';
    d.addEventListener("click", function () {
      if (expandedIds.has(ev._id)) expandedIds.delete(ev._id);
      else expandedIds.add(ev._id);
      scheduleRender();
    });
    return d;
  }

  function buildDetail(ev) {
    var d = document.createElement("div");
    d.className = "log-detail";
    var clone = {};
    for (var k in ev) if (k !== "_id") clone[k] = ev[k];
    d.textContent = JSON.stringify(clone, null, 2);
    return d;
  }

  function bodyHtml(ev) {
    var t = ev.eventType, dat = ev.data || {};
    if (t === "sig_change") {
      return '<span class="k">' + escapeHtml(dat.signal) + '</span> ' +
             escapeHtml(dat.signalType || "?") + '=' + escapeHtml(String(dat.value));
    }
    if (t === "command") {
      var corr = ev.correlationId ? ' · <span class="k">corr</span>=' + escapeHtml(ev.correlationId) : '';
      return '<span class="k">method</span>=' + escapeHtml(String(dat.method || "?")) + corr;
    }
    if (t === "response") {
      var corr2 = ev.correlationId ? ' · <span class="k">corr</span>=' + escapeHtml(ev.correlationId) : '';
      return '<span class="k">raw</span>=' + escapeHtml(String(dat.raw || "")) + corr2;
    }
    if (t === "error") {
      return '<span class="k">message</span>=' + escapeHtml(String(dat.message || ""));
    }
    if (t === "lifecycle" || t === "state_change") {
      var pairs = [];
      for (var k in dat) {
        if (k === "message") continue;
        pairs.push('<span class="k">' + escapeHtml(k) + '</span>=' + escapeHtml(String(dat[k])));
      }
      var head = dat.message ? '<span class="k">message</span>=' + escapeHtml(dat.message) : '';
      var tail = pairs.length ? (head ? ' · ' : '') + pairs.join(" · ") : '';
      return head + tail;
    }
    return escapeHtml(safeStringify(dat));
  }

  function shortTs(iso) {
    if (!iso) return "";
    var m = /T(\d{2}:\d{2}:\d{2}\.\d{1,3})/.exec(iso);
    return m ? m[1] : iso;
  }

  function escapeHtml(s) {
    return (s == null ? "" : String(s))
      .replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;")
      .replace(/"/g, "&quot;").replace(/'/g, "&#039;");
  }

  function safeStringify(d) {
    if (d == null) return "";
    if (typeof d === "string") return d;
    try { return JSON.stringify(d); } catch (e) { return String(d); }
  }

  // ─── Scroll lock ───────────────────────────────────────────────────
  function atBottom() {
    return ($logBody.scrollHeight - $logBody.scrollTop - $logBody.clientHeight) < 8;
  }

  $logBody.addEventListener("scroll", function () {
    if (atBottom()) {
      if (scrollLocked) setScrollLocked(false);
    } else {
      if (!scrollLocked) setScrollLocked(true);
    }
  });

  function setScrollLocked(v) {
    scrollLocked = v;
    $scrollLock.textContent = v ? "■ paused" : "▶ live";
    $jumpBottom.hidden = !v;
  }

  $scrollLock.addEventListener("click", function () {
    if (scrollLocked) {
      $logBody.scrollTop = $logBody.scrollHeight;  // jump to bottom and resume
      setScrollLocked(false);
    } else {
      setScrollLocked(true);
    }
  });

  $jumpBottom.addEventListener("click", function () {
    $logBody.scrollTop = $logBody.scrollHeight;
    setScrollLocked(false);
  });

  // ─── Search ────────────────────────────────────────────────────────
  $search.addEventListener("input", function () {
    clearTimeout(searchDebounce);
    searchDebounce = setTimeout(function () {
      searchTerm = ($search.value || "").toLowerCase();
      scheduleRender();
    }, 200);
  });

  // ─── Clear & Copy ──────────────────────────────────────────────────
  $logClear.addEventListener("click", function () {
    eventsAll = [];
    typeCounts = Object.create(null);
    deviceCounts = Object.create(null);
    expandedIds = new Set();
    $logCountTotal.textContent = "0";
    renderChips("all");
    renderLog();
  });

  $logCopy.addEventListener("click", function () {
    var lines = [];
    var term = searchTerm;
    for (var i = 0; i < eventsAll.length; i++) {
      var ev = eventsAll[i];
      if (!passes(ev, term)) continue;
      var clone = {};
      for (var k in ev) if (k !== "_id") clone[k] = ev[k];
      try { lines.push(JSON.stringify(clone)); } catch (e) {}
    }
    var text = lines.join("\n");
    var prev = $logCopy.textContent;
    function flash(msg) {
      $logCopy.textContent = msg;
      setTimeout(function () { $logCopy.textContent = prev; }, 1200);
    }
    if (navigator.clipboard && navigator.clipboard.writeText) {
      navigator.clipboard.writeText(text).then(function () { flash("copied!"); }, function () { fallback(); });
    } else {
      fallback();
    }
    function fallback() {
      var ta = document.createElement("textarea");
      ta.value = text;
      ta.style.position = "fixed";
      ta.style.left = "-9999px";
      document.body.appendChild(ta);
      ta.select();
      try { document.execCommand("copy"); flash("copied!"); }
      catch (e) { flash("copy failed"); }
      document.body.removeChild(ta);
    }
  });

  // Initial chip render so the rows exist before any events arrive.
  renderChips("all");

```

The file now has TWO unterminated blocks (the IIFE and an unclosed function) because the old code ended at line 99 with `});` etc. CONTINUE READING — the existing lines 100–254 (Devices UI, Cameras, Mic table, Audio/power, post helper, Boot) remain unchanged, and they include the matching IIFE close `})();` at line 254.

After this edit, the file should be coherent: the new code above ends mid-IIFE, then the existing line-100+ code resumes from `// ─── Devices UI ───` through the closing `})();`.

- [ ] **Step 2: Build and deploy**

```bash
cd "/c/Users/scale/CascadeProjects/Archon-Tests/MCCCD Razzle/.claude/worktrees/debug-ui-buildout/MCCCD-AA140-SIMPL/MCCCD-AA140" && dotnet build -c Release 2>&1 | tail -5
```

Expected: `Build succeeded.`

```bash
PROC_HOST=192.168.2.198 python "/c/Users/scale/CascadeProjects/Archon-Tests/MCCCD Razzle/.claude/worktrees/debug-ui-buildout/MCCCD-AA140-SIMPL/scripts/deploy.py" "/c/Users/scale/CascadeProjects/Archon-Tests/MCCCD Razzle/.claude/worktrees/debug-ui-buildout/MCCCD-AA140-SIMPL/MCCCD-AA140/bin/Release/net6.0/MCCCD-AA140.cpz" 2>&1 | tail -8
```

Expected: `[deploy] OK in ~17s`, `**Restarting Program:1**`. The cleanup handler from `checkpoint-program-status-cleanup` should mean no stale-cache reboot needed.

- [ ] **Step 3: Probe CWS to confirm it came up**

```bash
sleep 10 && curl -sk --max-time 10 "https://192.168.2.198/cws/aa140/debug/events?since=0&max=10" | head -c 1500; echo
```

Expected: a JSON payload with at least the `debug_server_start` lifecycle event and the `panel_online_change` lifecycle event from boot. If empty or times out, see fallback in Step 5.

- [ ] **Step 4: Verify acceptance criteria in browser**

At `https://192.168.2.198/cws/aa140/debug/`:

1. **Initial load.** Type chip row shows all six types, all "on". Device row populates with whatever devices appeared during boot (at minimum `system`, `panel-cip`). Each chip shows a count.
2. **Toggle a type chip off.** Click `sig_change`. Visible count drops; sig_change rows disappear from log; total event count keeps climbing.
3. **Toggle a device chip off.** Click `panel-cip`. Slider drags on the panel no longer add visible rows; `panel-cip` chip count still increments. Visible total stays small.
4. **Search.** Type `join-18494` (or whatever join is firing). Only rows whose JSON contains that substring remain. Clear search → all return.
5. **Scroll up manually.** Button changes to `■ paused`, `↓ jump to bottom` appears. New events accumulate but viewport stays. Click `↓ jump to bottom` → scrolls to bottom and resumes "▶ live".
6. **Click an event row.** Inline JSON detail appears below. Click again → collapses.
7. **Click `copy`.** Button briefly shows `copied!`. Paste into a scratch buffer — should be JSON-lines of the currently-filtered rows.

- [ ] **Step 5: Fallback — CWS hung after deploy**

If Step 3 times out or the browser shows Connection refused, the cleanup handler from the prior deploy may not have completed cleanly. Reboot the processor:

```bash
python -c "
import paramiko, time
ssh = paramiko.SSHClient()
ssh.set_missing_host_key_policy(paramiko.AutoAddPolicy())
ssh.connect('192.168.2.198', username='admin', password='password', allow_agent=False, look_for_keys=False, timeout=15)
ch = ssh.invoke_shell(); time.sleep(1); ch.recv(8192)
ch.send('reboot\r\n'); time.sleep(3); print(ch.recv(8192).decode(errors='replace'))
ssh.close()
"
```

Wait ~100s, re-run Step 3, then Step 4.

---

## Task 3: Commit + tag

- [ ] **Step 1: Inspect diff scope**

```bash
cd "/c/Users/scale/CascadeProjects/Archon-Tests/MCCCD Razzle/.claude/worktrees/debug-ui-buildout" && git status --short && git diff --stat
```

Expected: three files changed (`debug.html`, `debug.css`, `debug.js`), with diff sizes roughly matching the spec (chip CSS + sticky header in `.css`, replaced `#log-card` block in `.html`, replaced log-render path in `.js`).

- [ ] **Step 2: Commit**

```bash
git add MCCCD-AA140-SIMPL/debug-ui-src/debug.html MCCCD-AA140-SIMPL/debug-ui-src/debug.css MCCCD-AA140-SIMPL/debug-ui-src/debug.js
git commit -m "$(cat <<'EOF'
feat(debug-ui): filterable searchable scroll-locked event browser

Phase 1 of the debug-panel UI build-out (see
docs/superpowers/specs/2026-05-28-debug-log-ux-design.md).

Live Log card gains:
- Two chip rows (type, device) with live counts; click to toggle
- Search input (200ms debounce) substring-matching event JSON
- Scroll lock: auto-engages when user scrolls up, manual toggle button,
  jump-to-bottom button appears when locked
- Click row to expand full JSON inline
- Sticky header keeps filter controls visible while scrolling
- "copy" button puts the filtered view on the clipboard as JSON-lines
- Client-side ring capped at 5000 events; server's 1000-entry ring
  remains the source of truth, all filtering is client-side

No backend changes; pure frontend in debug-ui-src/. The existing
appendLog/trimLog functions are removed in favor of the new
ingest+scheduleRender pipeline. Polling, devices/cameras/mic/audio/
power blocks are unchanged.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

Expected: `1 file changed` shown for each of the three files.

- [ ] **Step 3: Tag**

```bash
git tag checkpoint-debug-log-ux
git log --oneline -3
git tag --list 'checkpoint-*' | sort
```

Expected: new tag appears in the list; HEAD now at the log-UX commit.

---

## Self-review

**Spec coverage:** All 7 in-scope items (filter chips × 2, search, scroll lock, click-to-expand, sticky header, counts, copy) are covered in Task 2. All 4 out-of-scope items (correlation grouping, persistence, time-range, server-side filter) are explicitly excluded.

**Placeholder scan:** No TBD/TODO/incomplete. All code blocks are complete; commands are exact; expected outputs are specific.

**Type consistency:** Function names cross-referenced — `scheduleRender`, `renderLog`, `renderChips`, `passes`, `buildRow`, `buildDetail`, `bodyHtml`, `shortTs`, `escapeHtml`, `safeStringify`, `atBottom`, `setScrollLocked`. DOM ID names match between HTML and JS: `log-chips-type`, `log-chips-device`, `log-search`, `log-scroll-lock`, `log-jump`, `log-copy`, `log-count-total`, `log-count-visible`, `log-body`, `log-clear`. CSS class names match `.chip`, `.chip.on`, `.log-row`, `.log-row.expanded`, `.log-detail`.

**Ambiguity:** Task 2 Step 1 instructs to "replace lines 1–99" — the agent must keep line 100+ unchanged. The note at the end of Step 1 calls this out twice.
