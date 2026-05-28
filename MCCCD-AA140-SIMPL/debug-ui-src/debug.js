// AA140 debug panel — vanilla JS, no build step. All UI state derives from
// two backend endpoints: GET /events (event ring) and GET /devices (config).

(function () {
  "use strict";

  // ─── State ─────────────────────────────────────────────────────────
  var POLL_MS = 1000;
  var since = 0;
  var paused = false;
  var consecutiveErrors = 0;

  var $pollStatus = document.getElementById("poll-status");
  var $pollCur    = document.getElementById("poll-cur");
  var $pollHost   = document.getElementById("poll-host");
  var $logBody    = document.getElementById("log-body");
  var $logClear   = document.getElementById("log-clear");
  var $logPause   = document.getElementById("log-pause");
  var $devicesBody = document.getElementById("devices-body");

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
        if (!paused && data.events && data.events.length) {
          for (var i = 0; i < data.events.length; i++) appendLog(data.events[i]);
          trimLog();
          // Auto-scroll if we were at the bottom before.
          $logBody.scrollTop = $logBody.scrollHeight;
        }
      })
      .catch(function (err) {
        consecutiveErrors++;
        $pollStatus.className = "pill bad";
        $pollStatus.textContent = "err (" + consecutiveErrors + ")";
      })
      .then(function () {
        setTimeout(poll, POLL_MS);
      });
  }

  function appendLog(ev) {
    var d = document.createElement("div");
    d.className = "log-line " + (ev.eventType || "");
    var ts = (ev.timestamp || "").replace(/^.*T/, "").replace("Z", "");
    d.innerHTML =
      '<span class="t">' + escapeHtml(ts) + '</span>' +
      '<span class="ty">' + escapeHtml(ev.eventType || "?") + '</span>' +
      '<span class="d">' + escapeHtml(ev.device || "—") + '</span>' +
      '<span class="v">' + escapeHtml(stringifyData(ev.data)) + '</span>';
    $logBody.appendChild(d);
  }

  function trimLog() {
    while ($logBody.childElementCount > 500) $logBody.removeChild($logBody.firstChild);
  }

  function escapeHtml(s) {
    return (s || "").toString()
      .replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;")
      .replace(/"/g, "&quot;").replace(/'/g, "&#039;");
  }

  function stringifyData(d) {
    if (d == null) return "";
    if (typeof d === "string") return d;
    try { return JSON.stringify(d); } catch (e) { return String(d); }
  }

  $logClear.addEventListener("click", function () {
    $logBody.innerHTML = "";
  });
  $logPause.addEventListener("change", function (e) {
    paused = !!e.target.checked;
  });

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

      $devicesBody.appendChild(nameEl);
      $devicesBody.appendChild(ipEl);
      $devicesBody.appendChild(togEl);
      $devicesBody.appendChild(statusEl);

      // For cameras, also update the camera-panel header IP label.
      if (key === "cam-1" || key === "cam-2") {
        var camId = key === "cam-1" ? "1" : "2";
        var ipLabel = document.querySelector('[data-cam="' + camId + '"] [data-cam-ip]');
        if (ipLabel) ipLabel.textContent = d.host || "—";
      }
    });
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

  function post(url) {
    fetch(url, { method: "POST", cache: "no-store" })
      .catch(function (err) { console.warn("POST failed:", url, err); });
  }

  // ─── Boot ──────────────────────────────────────────────────────────
  loadDevices();
  poll();
})();
