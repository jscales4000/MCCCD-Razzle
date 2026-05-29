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

  // Phase 3 — observability state derived from /events stream.
  // online: null=unknown, true=connected, false=disconnected/error.
  var deviceStatus = Object.create(null);
  var deviceCmds   = Object.create(null);   // {device: {lastCommand, lastResponse}}
  var encoderIps   = Object.create(null);   // {ip: sourceIndex}
  var SRC_LABELS   = { 0: "—", 1: "RoomPC", 2: "ExtPC", 3: "AirMedia", 4: "NVX-384" };
  var MCAST_TO_SRC = {
    "239.8.0.0":  1,
    "239.8.0.4":  2,
    "239.8.0.8":  3,
    "239.8.0.12": 4,
  };
  // device key prefix → which encoder label maps to which source index
  var ENC_LABEL_TO_SRC = {
    "nvx-roompc":   1,
    "nvx-extpc":    2,
    "nvx-airmedia": 3,
    "nvx-nvx-384":  4,
  };

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
      updateDeviceStatus(ev);
      eventsAll.push(ev);
    }
    while (eventsAll.length > MAX_EVENTS) eventsAll.shift();
    renderChips(newDevices);
    renderStatus();
    $logCountTotal.textContent = eventsAll.length;
  }

  // ─── Phase 3 — observability state + render ────────────────────────
  function ensureStatus(key) {
    var s = deviceStatus[key];
    if (!s) { s = { online: null }; deviceStatus[key] = s; }
    return s;
  }

  function updateDeviceStatus(ev) {
    var t = ev.eventType;
    var dat = ev.data || {};
    var devKey = dat.device || ev.device;   // most lifecycle events tuck device into data
    var ts = ev.timestamp;

    if (t === "system") {
      var msg = dat.message;
      if (!msg) return;
      if (msg === "device_connected") {
        var s = ensureStatus(devKey);
        s.online = true; s.host = dat.host; s.lastEventTs = ts; s.lastEventType = msg;
        s.lastError = null;
      } else if (msg === "device_connect_failed") {
        var s = ensureStatus(devKey);
        s.online = false; s.host = dat.host; s.lastEventTs = ts; s.lastEventType = msg;
        s.lastError = "connect failed: " + dat.status + " (attempt " + dat.attempt + ")";
      } else if (msg === "device_socket_change") {
        var s = ensureStatus(devKey);
        s.online = false; s.lastEventTs = ts; s.lastEventType = msg;
        s.lastError = "socket: " + dat.status;
      } else if (msg === "nvx_encoder_online_change") {
        var s = ensureStatus(devKey);
        s.online = !!dat.online; s.mcast = dat.mcast; s.lastEventTs = ts;
      } else if (msg === "nvx_decoder_online_change") {
        var s = ensureStatus(devKey);
        s.online = !!dat.online; s.lastEventTs = ts;
      } else if (msg === "nvx_route_change") {
        var s = ensureStatus(devKey);
        s.routeUrl = dat.url;
        s.routeSrc = urlToSourceIndex(dat.url);
        s.lastEventTs = ts;
      } else if (msg === "nvx_ip_resolved") {
        var s = ensureStatus(devKey);
        s.ip = dat.ip; s.lastEventTs = ts;
        var srcIdx = ENC_LABEL_TO_SRC[devKey];
        if (srcIdx && dat.ip) encoderIps[dat.ip] = srcIdx;
        // Re-resolve any existing decoder route URLs that may have been pre-resolution
        for (var k in deviceStatus) {
          if (k.indexOf("nvx-d") === 0 && deviceStatus[k].routeUrl) {
            deviceStatus[k].routeSrc = urlToSourceIndex(deviceStatus[k].routeUrl);
          }
        }
      }
    } else if (t === "command") {
      if (!devKey) return;
      if (!deviceCmds[devKey]) deviceCmds[devKey] = {};
      deviceCmds[devKey].lastCommand = { method: dat.method, ts: ts, corrId: ev.correlationId };
    } else if (t === "response") {
      if (!devKey) return;
      if (!deviceCmds[devKey]) deviceCmds[devKey] = {};
      deviceCmds[devKey].lastResponse = { raw: dat.raw, ts: ts, corrId: ev.correlationId };
    } else if (t === "error") {
      if (!devKey) return;
      var s = ensureStatus(devKey);
      s.lastError = dat.message;
      s.lastErrorTs = ts;
    }
  }

  function urlToSourceIndex(url) {
    if (!url) return 0;
    for (var mc in MCAST_TO_SRC) {
      if (url.indexOf(mc + ":") >= 0) return MCAST_TO_SRC[mc];
    }
    for (var ip in encoderIps) {
      if (url.indexOf(ip + ":") >= 0) return encoderIps[ip];
    }
    return 0;
  }

  function renderStatus() {
    // Badges
    var badges = document.querySelectorAll(".dev-badge[data-key]");
    for (var i = 0; i < badges.length; i++) {
      var key = badges[i].getAttribute("data-key");
      badges[i].className = "dev-badge " + badgeStateClass(key);
      badges[i].title = badgeTitle(key);
    }

    // NVX decoder rows
    ["nvx-d1","nvx-d2","nvx-d3"].forEach(function (k) {
      var row = document.querySelector('[data-dec="' + k + '"]');
      if (!row) return;
      var s = deviceStatus[k] || {};
      var stateCell = row.querySelector(".state");
      var srcCell   = row.querySelector(".src");
      var urlCell   = row.querySelector(".url");
      stateCell.className = "state " + (s.online === true ? "online" : s.online === false ? "offline" : "unknown");
      stateCell.textContent = s.online === true ? "online" : s.online === false ? "offline" : "—";
      srcCell.textContent = s.routeSrc ? (s.routeSrc + " · " + SRC_LABELS[s.routeSrc]) : "—";
      urlCell.textContent = s.routeUrl || "—";
    });

    // NVX encoder rows
    Object.keys(ENC_LABEL_TO_SRC).forEach(function (k) {
      var row = document.querySelector('[data-enc="' + k + '"]');
      if (!row) return;
      var s = deviceStatus[k] || {};
      var stateCell = row.querySelector(".state");
      var ipCell    = row.querySelector(".ip");
      var mcCell    = row.querySelector(".mcast");
      stateCell.className = "state " + (s.online === true ? "online" : s.online === false ? "offline" : "unknown");
      stateCell.textContent = s.online === true ? "online" : s.online === false ? "offline" : "—";
      ipCell.textContent = s.ip || "—";
      mcCell.textContent = s.mcast || "—";
    });

    // Audio (P300) detail line: last cmd / last resp / last error
    var $audio = document.getElementById("audio-detail");
    if ($audio) {
      $audio.innerHTML = renderDevDetail("p300");
    }

    // Camera detail lines
    [1, 2].forEach(function (n) {
      var el = document.querySelector('[data-cam-detail="' + n + '"]');
      if (!el) return;
      el.innerHTML = renderDevDetail("cam-" + n);
    });
  }

  function renderDevDetail(devKey) {
    var s = deviceStatus[devKey] || {};
    var cmds = deviceCmds[devKey] || {};
    var parts = [];
    if (cmds.lastCommand) {
      parts.push('<span class="k">last cmd:</span> ' + escapeHtml(cmds.lastCommand.method || "?") +
        (cmds.lastCommand.corrId ? " (" + escapeHtml(cmds.lastCommand.corrId) + ")" : ""));
    }
    if (cmds.lastResponse) {
      parts.push('<span class="k">last resp:</span> ' + escapeHtml(String(cmds.lastResponse.raw || "")));
    }
    if (s.lastError) {
      parts.push('<span class="err">err: ' + escapeHtml(s.lastError) + '</span>');
    }
    return parts.length ? parts.join('<span class="sep">·</span>') : "";
  }

  function badgeStateClass(key) {
    var s = deviceStatus[key];
    if (!s || s.online == null) return "";
    return s.online ? "online" : "offline";
  }

  function badgeTitle(key) {
    var s = deviceStatus[key];
    if (!s) return key + ": unknown";
    var parts = [key];
    if (s.online === true)  parts.push("online");
    if (s.online === false) parts.push("offline");
    if (s.host)             parts.push("host=" + s.host);
    if (s.lastError)        parts.push("err=" + s.lastError);
    return parts.join(" · ");
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

  // ─── Devices UI ────────────────────────────────────────────────────
  function loadDevices() {
    fetch("./devices", { cache: "no-store" })
      .then(function (r) { return r.json(); })
      .then(function (data) { renderDevices(data.devices || {}); })
      .catch(function (err) {
        $devicesBody.innerHTML = '<div class="muted">load failed: ' + escapeHtml(err.message) + '</div>';
      });
  }

  function renderDevices(devices) {
    $devicesBody.innerHTML = "";
    DEVICE_KEYS.forEach(function (kv) {
      var key = kv[0], label = kv[1];
      var d = devices[key] || { host: "", enabled: false };

      var nameEl = document.createElement("div");
      nameEl.className = "dev-name";
      nameEl.textContent = label;

      var ipEl = document.createElement("input");
      ipEl.type = "text";
      ipEl.className = "dev-ip";
      ipEl.value = d.host || "";
      ipEl.spellcheck = false;
      ipEl.placeholder = "0.0.0.0";
      ipEl.addEventListener("change", function () {
        postDeviceUpdate(key, { host: ipEl.value });
      });

      var togEl = document.createElement("label");
      togEl.className = "toggle";
      var togIn = document.createElement("input");
      togIn.type = "checkbox";
      togIn.checked = !!d.enabled;
      var togSl = document.createElement("span");
      togSl.className = "slider";
      togEl.appendChild(togIn);
      togEl.appendChild(togSl);
      togIn.addEventListener("change", function () {
        postDeviceUpdate(key, { enabled: togIn.checked });
      });

      var statusEl = document.createElement("div");
      statusEl.className = "dev-status " + (d.enabled ? "ok" : "off");
      statusEl.textContent = d.enabled ? "enabled" : "disabled";

      var badgeEl = document.createElement("span");
      badgeEl.className = "dev-badge";
      badgeEl.setAttribute("data-key", key);
      badgeEl.style.marginLeft = "0";   // grid cell handles spacing

      $devicesBody.appendChild(nameEl);
      $devicesBody.appendChild(ipEl);
      $devicesBody.appendChild(togEl);
      $devicesBody.appendChild(statusEl);
      $devicesBody.appendChild(badgeEl);

      // For cameras, also update the camera-panel header IP label.
      if (key === "cam-1" || key === "cam-2") {
        var camId = key === "cam-1" ? "1" : "2";
        var ipLabel = document.querySelector('[data-cam="' + camId + '"] [data-cam-ip]');
        if (ipLabel) ipLabel.textContent = d.host || "—";
      }
    });
    renderStatus();
  }

  function postDeviceUpdate(key, fields) {
    var q = [];
    if (typeof fields.host    !== "undefined") q.push("host=" + encodeURIComponent(fields.host));
    if (typeof fields.enabled !== "undefined") q.push("enabled=" + (fields.enabled ? "true" : "false"));
    fetch("./devices/" + encodeURIComponent(key) + "?" + q.join("&"), { method: "POST" })
      .then(function (r) {
        if (!r.ok) throw new Error("HTTP " + r.status);
        return r.json();
      })
      .then(function () { loadDevices(); })
      .catch(function (err) { console.warn("device update failed", err); });
  }

  // ─── Cameras (PTZ + presets + tracking + VTC) ──────────────────────
  document.querySelectorAll(".cam-panel").forEach(function (panel) {
    var camId = panel.getAttribute("data-cam");

    panel.querySelectorAll('[data-act="ptz"]').forEach(function (btn) {
      var dir = btn.getAttribute("data-dir");
      var go     = function () { post("./cam/" + camId + "/ptz?dir=" + dir + "&state=start"); };
      var stop   = function () { post("./cam/" + camId + "/ptz?dir=" + dir + "&state=stop"); };
      btn.addEventListener("pointerdown", function (e) { e.preventDefault(); go(); });
      btn.addEventListener("pointerup",   function (e) { e.preventDefault(); stop(); });
      btn.addEventListener("pointerleave", function () { stop(); });
    });
    panel.querySelectorAll('[data-act="ptz-stop"]').forEach(function (btn) {
      btn.addEventListener("click", function () { post("./cam/" + camId + "/ptz?state=stop"); });
    });
    panel.querySelectorAll('[data-act="zoom"]').forEach(function (btn) {
      var dir = btn.getAttribute("data-dir");
      btn.addEventListener("pointerdown", function () { post("./cam/" + camId + "/zoom?dir=" + dir + "&state=start"); });
      btn.addEventListener("pointerup",   function () { post("./cam/" + camId + "/zoom?dir=" + dir + "&state=stop"); });
      btn.addEventListener("pointerleave", function () { post("./cam/" + camId + "/zoom?state=stop"); });
    });
    panel.querySelectorAll('[data-act="recall"]').forEach(function (btn) {
      btn.addEventListener("click", function () { post("./cam/" + camId + "/preset-recall?id=" + btn.getAttribute("data-id")); });
    });
    panel.querySelectorAll('[data-act="save"]').forEach(function (btn) {
      btn.addEventListener("click", function () { post("./cam/" + camId + "/preset-save?id=" + btn.getAttribute("data-id")); });
    });
    panel.querySelectorAll('[data-act="delete"]').forEach(function (btn) {
      btn.addEventListener("click", function () { post("./cam/" + camId + "/preset-delete?id=" + btn.getAttribute("data-id")); });
    });
    panel.querySelectorAll('[data-act="tracking"]').forEach(function (btn) {
      btn.addEventListener("click", function () { post("./cam/" + camId + "/tracking?mode=" + btn.getAttribute("data-mode")); });
    });
    panel.querySelectorAll('[data-act="vtc"]').forEach(function (btn) {
      btn.addEventListener("click", function () { post("./cam/" + camId + "/vtc"); });
    });
  });

  // ─── Mic table ─────────────────────────────────────────────────────
  document.querySelectorAll(".mic-table tbody tr").forEach(function (row) {
    var key = row.getAttribute("data-mic");

    row.querySelectorAll('[data-act="mute-on"]').forEach(function (btn) {
      btn.addEventListener("click", function () { post("./mic/" + key + "/mute?on=true"); });
    });
    row.querySelectorAll('[data-act="mute-off"]').forEach(function (btn) {
      btn.addEventListener("click", function () { post("./mic/" + key + "/mute?on=false"); });
    });
    var debounce = null;
    row.querySelectorAll('[data-act="trim"]').forEach(function (inp) {
      inp.addEventListener("input", function () {
        clearTimeout(debounce);
        debounce = setTimeout(function () { post("./mic/" + key + "/trim?v=" + inp.value); }, 50);
      });
    });
    row.querySelectorAll('[data-act="lineout"]').forEach(function (inp) {
      inp.addEventListener("input", function () {
        clearTimeout(debounce);
        debounce = setTimeout(function () { post("./mic/" + key + "/lineout?v=" + inp.value); }, 50);
      });
    });
  });

  // ─── Audio master + power ──────────────────────────────────────────
  document.querySelectorAll("[data-audio]").forEach(function (btn) {
    btn.addEventListener("click", function () { post("./audio/" + btn.getAttribute("data-audio")); });
  });
  document.querySelectorAll("[data-power]").forEach(function (btn) {
    btn.addEventListener("click", function () { post("./power/" + btn.getAttribute("data-power")); });
  });

  // ─── NVX manual route override ─────────────────────────────────────
  document.querySelectorAll("#nvx-card td.nvx-override button").forEach(function (btn) {
    btn.addEventListener("click", function () {
      var row = btn.closest("tr");
      var dec = row && row.getAttribute("data-dec-num");
      var src = btn.getAttribute("data-route-src");
      if (!dec || src == null) return;
      post("./nvx/route?dec=" + dec + "&src=" + src);
    });
  });

  // ─── Raw signal send ───────────────────────────────────────────────
  var $sigJoin   = document.getElementById("sig-join");
  var $sigType   = document.getElementById("sig-type");
  var $sigValue  = document.getElementById("sig-value");
  var $sigSend   = document.getElementById("sig-send");
  var $sigStatus = document.getElementById("sig-status");
  if ($sigSend) {
    $sigSend.addEventListener("click", function () {
      var join = ($sigJoin.value || "").trim();
      var type = $sigType.value;
      var val  = ($sigValue.value || "").trim();
      if (!join) { setSigStatus("bad", "join required"); return; }
      if (type === "ushort" && !/^\d+$/.test(val)) { setSigStatus("bad", "ushort needs 0..65535"); return; }
      var url = "./signal?join=" + encodeURIComponent(join) + "&type=" + encodeURIComponent(type) + "&value=" + encodeURIComponent(val);
      fetch(url, { method: "POST", cache: "no-store" })
        .then(function (r) { return r.json().then(function (j) { return { ok: r.ok, j: j }; }); })
        .then(function (res) {
          if (res.ok && res.j && res.j.ok) setSigStatus("ok", "sent");
          else setSigStatus("bad", (res.j && res.j.error) || "failed");
        })
        .catch(function (err) { setSigStatus("bad", err.message); });
    });
  }
  function setSigStatus(cls, msg) {
    if (!$sigStatus) return;
    $sigStatus.className = "muted small " + cls;
    $sigStatus.textContent = msg;
    setTimeout(function () { $sigStatus.textContent = ""; $sigStatus.className = "muted small"; }, 2000);
  }

  function post(url) {
    fetch(url, { method: "POST", cache: "no-store" })
      .catch(function (err) { console.warn("POST failed:", url, err); });
  }

  // ─── Boot ──────────────────────────────────────────────────────────
  loadDevices();
  poll();
})();
