/* ==========================================================================
   Server monitoring dashboard — loads real data from GET /api/company/servers.
   No fake/local data: a server record here is exactly { id, serverName,
   ipAddress, cpuUsage, ramUsage, ramTotal, diskTotalSpace,
   diskRemainingSpace } — the schema has no temperature, history,
   per-process, network/disk-IO/fan, or "offline" fields, so none of that is
   rendered anymore.
   ========================================================================== */
(function () {
  "use strict";

  // Shared palette + helpers live in common.js so every page (dashboard,
  // users, …) draws status colors from one place — see common.js for what
  // each of these actually is.
  var OK = NW.colors.OK;
  var WARN = NW.colors.WARN;
  var CRIT = NW.colors.CRIT;
  var BLUE = NW.colors.BLUE;
  var TINT_OK = NW.colors.TINT_OK;
  var TINT_WARN = NW.colors.TINT_WARN;
  var TINT_CRIT = NW.colors.TINT_CRIT;
  var INK_OK = NW.colors.INK_OK;
  var INK_WARN = NW.colors.INK_WARN;
  var INK_CRIT = NW.colors.INK_CRIT;
  var esc = NW.esc;
  var nf = NW.nf;

  // Operational thresholds: drive status(), the alert banner, and gauge-ring
  // coloring for ram/disk. CPU's ring uses its own, more sensitive pair
  // below (deliberately, not a duplicate) so it flags rising load visually
  // before the overall host status escalates.
  var THRESHOLDS = {
    cpuWarn: 85, cpuCrit: 95,
    ramWarn: 75, ramCrit: 90,
    diskWarn: 85, diskCrit: 95
  };
  var GAUGE_CPU_WARN = 60, GAUGE_CPU_CRIT = 85;

  var POLL_MS = 15000; // real network calls, not a client-side generator — no need for a 2.2s cadence

  var State = { servers: [], selected: null, live: true, view: "grid" };

  // Forces n to stay within [lo, hi] — keeps a percentage from ever showing as e.g. 104% or -3%.
  function clamp(n, lo, hi) { return Math.max(lo, Math.min(hi, n)); }

  // Builds the CSS conic-gradient string that draws a gauge ring: a colored arc for pct% of the circle, the track color for the rest.
  function ring(pct, color) {
    return "conic-gradient(" + color + " " + Math.round(pct * 3.6) + "deg, var(--nf-track-bg) 0deg)";
  }

  // RAM used as a percentage of total, guarded against a missing/zero ramTotal so it never divides by zero into NaN.
  function ramPct(s) { return s.ramTotal ? clamp((s.ramUsage / s.ramTotal) * 100, 0, 100) : 0; }

  // Disk used as a percentage of total (total minus remaining, over total), same divide-by-zero guard as ramPct.
  function diskUsedPct(s) {
    return s.diskTotalSpace ? clamp(((s.diskTotalSpace - s.diskRemainingSpace) / s.diskTotalSpace) * 100, 0, 100) : 0;
  }

  // Classifies a server as OK/Advarsel/Kritisk from its CPU/RAM/disk against THRESHOLDS, worst metric wins.
  function status(s) {
    var ram = ramPct(s), disk = diskUsedPct(s);
    if (s.cpuUsage >= THRESHOLDS.cpuCrit || ram >= THRESHOLDS.ramCrit || disk >= THRESHOLDS.diskCrit) {
      return { label: "Kritisk", color: CRIT, bg: TINT_CRIT, fg: INK_CRIT };
    }
    if (s.cpuUsage >= THRESHOLDS.cpuWarn || ram >= THRESHOLDS.ramWarn || disk >= THRESHOLDS.diskWarn) {
      return { label: "Advarsel", color: WARN, bg: TINT_WARN, fg: INK_WARN };
    }
    return { label: "OK", color: OK, bg: TINT_OK, fg: INK_OK };
  }

  // Returns the currently selected server object from the State, or null if none is selected.
  function getSelected() {
    for (var i = 0; i < State.servers.length; i++) {
      if (State.servers[i].id === State.selected) return State.servers[i];
    }
    return null;
  }

  function render() {
    renderTally();
    renderFleetGrid();
    renderSidebar();
    var sel = getSelected();
    if (sel) {
      renderHostHeader(sel);
      renderAlert(sel);
      renderGauges(sel);
      renderDisk(sel);
    }
  }

  function renderClock() {
    var now = new Date();
    document.getElementById("clock").textContent =
      String(now.getHours()).padStart(2, "0") + ":" + String(now.getMinutes()).padStart(2, "0");
  }

  function renderLiveStatus() {
    var dot = document.getElementById("live-dot");
    var label = document.getElementById("live-label");
    var pauseBtn = document.getElementById("pause-btn");
    dot.className = "live-status__dot" + (State.live ? "" : " live-status__dot--paused");
    label.textContent = State.live ? "Live · opdaterer hvert 15. sekund" : "Opdatering sat på pause";
    dot.title = label.textContent;
    pauseBtn.querySelector(".btn-label").textContent = State.live ? "Pause" : "Genoptag";
  }

  function renderTally() {
    var counts = { OK: 0, Advarsel: 0, Kritisk: 0 };
    State.servers.forEach(function (s) { counts[status(s).label]++; });
    var tallies = [
      { label: "Normal", n: counts.OK, color: OK },
      { label: "Advarsel", n: counts.Advarsel, color: WARN },
      { label: "Kritisk", n: counts.Kritisk, color: CRIT }
    ];
    var tallyHtml = tallies.map(function (t) {
      return '<div class="tally-cell">' +
        '<div class="tally-cell__count" style="color:' + t.color + '">' + t.n + '</div>' +
        '<div class="tally-cell__label">' + esc(t.label) + '</div>' +
        '</div>';
    }).join("");
    var countText = State.servers.length + " i alt";
    document.getElementById("fleet-tally").innerHTML = tallyHtml;
    document.getElementById("fleet-count").textContent = countText;
    document.getElementById("fleet-tally-grid").innerHTML = tallyHtml;
    document.getElementById("fleet-count-grid").textContent = countText;
  }

  function renderFleetGrid() {
    document.getElementById("fleet-grid").innerHTML = State.servers.map(function (s) {
      var st = status(s);
      return '<button type="button" class="card fleet-card" data-id="' + s.id + '" style="border-left:3px solid ' + st.color + '">' +
        '<div class="fleet-card__head">' +
        '<span class="fleet-card__name">' + esc(s.serverName) + '</span>' +
        '<span class="status-pill" style="color:' + st.fg + ';background:' + st.bg + '">' + esc(st.label) + '</span>' +
        '</div>' +
        '<div class="fleet-card__meta">' + esc(s.ipAddress) + '</div>' +
        '<div class="fleet-card__metrics">' +
        '<div><div class="fleet-card__metric-label">CPU</div><div class="fleet-card__metric-value" style="color:' + st.color + '">' + nf(s.cpuUsage, 0) + ' %</div></div>' +
        '<div><div class="fleet-card__metric-label">Hukommelse</div><div class="fleet-card__metric-value">' + nf(ramPct(s), 0) + ' %</div></div>' +
        '<div><div class="fleet-card__metric-label">Disk</div><div class="fleet-card__metric-value">' + nf(diskUsedPct(s), 0) + ' %</div></div>' +
        '</div>' +
        '</button>';
    }).join("");
  }

  function renderSidebar() {
    document.getElementById("server-list").innerHTML = State.servers.map(function (s) {
      var st = status(s);
      var active = s.id === State.selected;
      return '<button type="button" class="server-row' + (active ? " server-row--selected" : "") + '" data-id="' + s.id + '">' +
        '<span class="server-row__dot" style="background:' + st.color + '"></span>' +
        '<span class="server-row__id">' +
        '<span class="server-row__name">' + esc(s.serverName) + '</span>' +
        '<span class="server-row__sub">' + esc(s.ipAddress) + '</span>' +
        '</span>' +
        '<span class="server-row__metrics">' +
        '<span class="server-row__cpu" style="color:' + st.color + '">' + nf(s.cpuUsage, 0) + ' %</span>' +
        '<span class="server-row__secondary">' + nf(diskUsedPct(s), 0) + ' % disk</span>' +
        '</span>' +
        '</button>';
    }).join("");
  }

  function renderHostHeader(s) {
    var st = status(s);
    document.getElementById("host-name").textContent = s.serverName;
    var pill = document.getElementById("host-status-pill");
    pill.textContent = st.label;
    pill.style.color = st.fg;
    pill.style.background = st.bg;
    document.getElementById("host-meta").innerHTML =
      '<span><i class="fa-solid fa-network-wired"></i>' + esc(s.ipAddress) + '</span>';
  }

  function renderAlert(s) {
    var st = status(s);
    var box = document.getElementById("alert-banner");
    if (st.label === "OK") { box.hidden = true; return; }
    box.hidden = false;
    var disk = diskUsedPct(s), ram = ramPct(s);
    var title = st.label === "Kritisk"
      ? "Kritisk grænse overskredet på " + s.serverName
      : "Værdi over advarselsgrænsen på " + s.serverName;
    var body;
    if (disk >= THRESHOLDS.diskWarn) {
      body = "Diskforbrug " + nf(disk, 0) + " % mod grænsen " + THRESHOLDS.diskWarn + " % · CPU " + nf(s.cpuUsage, 0) + " % · hukommelse " + nf(ram, 0) + " %.";
    } else if (s.cpuUsage >= THRESHOLDS.cpuWarn) {
      body = "CPU " + nf(s.cpuUsage, 0) + " % vedvarende · hukommelse " + nf(ram, 0) + " % · diskforbrug " + nf(disk, 0) + " %.";
    } else {
      body = "Hukommelse " + nf(ram, 0) + " % mod grænsen " + THRESHOLDS.ramWarn + " % · CPU " + nf(s.cpuUsage, 0) + " % · diskforbrug " + nf(disk, 0) + " %.";
    }
    document.getElementById("alert-title").textContent = title;
    document.getElementById("alert-body").textContent = body;
  }

  function renderGauges(s) {
    var ram = ramPct(s), disk = diskUsedPct(s);
    var gauges = [
      { label: "CPU-belastning", value: nf(s.cpuUsage, 0) + "%", big: nf(s.cpuUsage, 1) + " %", hint: "", ring: ring(s.cpuUsage, s.cpuUsage > GAUGE_CPU_CRIT ? CRIT : s.cpuUsage > GAUGE_CPU_WARN ? WARN : BLUE) },
      { label: "Hukommelse", value: nf(ram, 0) + "%", big: nf(s.ramUsage, 1) + " GB", hint: "af " + nf(s.ramTotal, 0) + " GB i alt", ring: ring(ram, ram > THRESHOLDS.ramCrit ? CRIT : ram > THRESHOLDS.ramWarn ? WARN : BLUE) },
      { label: "Diskplads", value: nf(disk, 0) + "%", big: nf(s.diskRemainingSpace, 0) + " GB fri", hint: "af " + nf(s.diskTotalSpace, 0) + " GB i alt", ring: ring(disk, disk >= THRESHOLDS.diskCrit ? CRIT : disk >= THRESHOLDS.diskWarn ? WARN : BLUE) }
    ];
    document.getElementById("gauge-list").innerHTML = gauges.map(function (g) {
      return '<div class="gauge-card">' +
        '<div class="gauge-ring" style="background:' + g.ring + '">' +
        '<div class="gauge-ring__value">' + g.value + '</div>' +
        '</div>' +
        '<div class="gauge-card__text">' +
        '<div class="gauge-card__label">' + esc(g.label) + '</div>' +
        '<div class="gauge-card__value">' + g.big + '</div>' +
        '<div class="gauge-card__hint">' + g.hint + '</div>' +
        '</div>' +
        '</div>';
    }).join("");
  }

  function renderDisk(s) {
    var disk = diskUsedPct(s);
    var color = disk >= THRESHOLDS.diskCrit ? CRIT : disk >= THRESHOLDS.diskWarn ? WARN : BLUE;
    document.getElementById("disk-free-text").textContent =
      nf(s.diskRemainingSpace, 0) + " GB fri af " + nf(s.diskTotalSpace, 0) + " GB";
    var fill = document.getElementById("disk-fill");
    fill.style.width = nf(disk, 0) + "%";
    fill.style.background = color;
    document.getElementById("disk-note").textContent = nf(disk, 0) + " % brugt";
  }

  function setFleetStatus(text) {
    var el = document.getElementById("fleet-status");
    if (text) { el.textContent = text; el.hidden = false; } else { el.hidden = true; }
  }

  function loadServers() {
    setFleetStatus("Indlæser servere…");
    return NWApi.get("/company/servers").then(function (servers) {
      State.servers = servers || [];
      if (State.selected == null || !getSelected()) {
        State.selected = State.servers.length ? State.servers[0].id : null;
      }
      setFleetStatus(State.servers.length ? null : "Ingen servere endnu.");
      renderClock();
      render();
    }).catch(function (err) {
      setFleetStatus("Kunne ikke hente servere: " + err.message);
    });
  }

  function scheduleTick() {
    setInterval(function () {
      if (State.live) loadServers();
    }, POLL_MS);
  }

  function setView(view) {
    State.view = view;
    document.getElementById("grid-view").hidden = view !== "grid";
    document.getElementById("detail-view").hidden = view !== "detail";
  }

  document.addEventListener("DOMContentLoaded", function () {
    document.getElementById("server-list").addEventListener("click", function (e) {
      var btn = e.target.closest("[data-id]");
      if (!btn) return;
      State.selected = parseInt(btn.getAttribute("data-id"), 10);
      render();
    });
    document.getElementById("fleet-grid").addEventListener("click", function (e) {
      var btn = e.target.closest("[data-id]");
      if (!btn) return;
      State.selected = parseInt(btn.getAttribute("data-id"), 10);
      setView("detail");
      render();
    });
    document.getElementById("back-to-grid").addEventListener("click", function () {
      setView("grid");
    });
    document.getElementById("brand-home").addEventListener("click", function (e) {
      e.preventDefault();
      setView("grid");
    });
    document.getElementById("pause-btn").addEventListener("click", function () {
      State.live = !State.live;
      renderLiveStatus();
    });

    renderLiveStatus();
    setView(State.view);
    loadServers();
    scheduleTick();
  });
})();
