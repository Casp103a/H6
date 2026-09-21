/* ==========================================================================
   Server monitoring dashboard — loads real data from
   GET /api/CompanyHardwareData/GetAll/{companyId} (companyId comes from
   GET /api/company first, same pattern as users.js/create-server.js).
   That endpoint has no join to the serviceclients table, so it returns no
   serverName/ipAddress — only a ServiceId. Until a backend endpoint exists
   for that, servers are labelled "Service {ServiceId}" with ipAddress "—".
   It's also a raw insert log (one row per reading, no update/upsert): the
   latest row per ServiceId is the fleet/gauge snapshot, and the recent rows
   before it feed the per-server trend chart — see mapHardwareRows/renderTrend.
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

  var State = { servers: [], history: {}, selected: null, live: true, view: "grid", companyId: null, trendView: "chart" };

  // Trend chart series (renderTrendChart/renderTrendLegend) — CPU/RAM/disk
  // share one 0-100% axis, so one chart with three lines instead of three
  // separate ones. Colors come from tokens.css (validated together via
  // dataviz's validate_palette.js — see the tokens.css comment).
  var TREND_SERIES = [
    { key: "cpu", label: "CPU", color: "var(--nf-chart-cpu)" },
    { key: "ram", label: "Hukommelse", color: "var(--nf-chart-ram)" },
    { key: "disk", label: "Disk", color: "var(--nf-chart-disk)" }
  ];

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
      renderTrend(sel);
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

  function renderTrendLegend() {
    document.getElementById("trend-legend").innerHTML = TREND_SERIES.map(function (series) {
      return '<span class="trend-legend__item"><span class="trend-legend__key" style="background:' + series.color + '"></span>' + esc(series.label) + '</span>';
    }).join("");
  }

  // Shows the chart or the table (never both) per State.trendView, without
  // touching their content — so toggling doesn't fight the 15s poll re-render.
  function applyTrendView() {
    var isTable = State.trendView === "table";
    document.getElementById("trend-chart-wrap").hidden = isTable;
    document.getElementById("trend-table-wrap").hidden = !isTable;
    document.getElementById("trend-table-toggle").textContent = isTable ? "Vis som graf" : "Vis som tabel";
  }

  // Draws the per-server CPU/hukommelse/disk trend: one 0-100% axis, three
  // lines (see TREND_SERIES) sharing it — never a dual-axis chart. A hover
  // crosshair + tooltip reads all three at once; the table toggle is the
  // no-hover/keyboard fallback so every value stays reachable.
  function renderTrend(s) {
    var history = State.history[s.id] || [];
    var toggleBtn = document.getElementById("trend-table-toggle");
    var emptyNote = document.getElementById("trend-empty");
    var tooltip = document.getElementById("trend-tooltip");

    renderTrendLegend();

    if (history.length < 2) {
      document.getElementById("trend-chart-wrap").hidden = true;
      document.getElementById("trend-table-wrap").hidden = true;
      toggleBtn.hidden = true;
      tooltip.hidden = true;
      emptyNote.hidden = false;
      return;
    }
    emptyNote.hidden = true;
    toggleBtn.hidden = false;
    applyTrendView();

    var points = history.map(function (h) {
      return { cpu: h.cpuUsage, ram: ramPct(h), disk: diskUsedPct(h) };
    });

    var chartWidth = 640, chartHeight = 200;
    var paddingLeft = 32, paddingRight = 12, paddingTop = 10, paddingBottom = 10;
    var plotWidth = chartWidth - paddingLeft - paddingRight;
    var plotHeight = chartHeight - paddingTop - paddingBottom;
    var lastReadingIndex = points.length - 1;

    function xForReading(readingIndex) {
      return paddingLeft + (lastReadingIndex === 0 ? 0 : (readingIndex / lastReadingIndex) * plotWidth);
    }
    function yForPercent(percent) {
      return paddingTop + plotHeight - (clamp(percent, 0, 100) / 100) * plotHeight;
    }
    function pathFor(seriesKey) {
      return points.map(function (p, readingIndex) {
        return (readingIndex === 0 ? "M" : "L") + xForReading(readingIndex).toFixed(1) + "," + yForPercent(p[seriesKey]).toFixed(1);
      }).join(" ");
    }

    var gridSvg = [0, 50, 100].map(function (tickPercent) {
      var tickY = yForPercent(tickPercent).toFixed(1);
      return '<line class="trend-chart__grid" x1="' + paddingLeft + '" x2="' + (chartWidth - paddingRight) + '" y1="' + tickY + '" y2="' + tickY + '"></line>' +
        '<text class="trend-chart__tick" x="' + (paddingLeft - 6) + '" y="' + (yForPercent(tickPercent) + 3).toFixed(1) + '" text-anchor="end">' + tickPercent + '%</text>';
    }).join("");

    var linesSvg = TREND_SERIES.map(function (series) {
      return '<path class="trend-chart__line" d="' + pathFor(series.key) + '" style="stroke:' + series.color + '"></path>';
    }).join("");

    var endDotsSvg = TREND_SERIES.map(function (series) {
      return '<circle class="trend-chart__dot" r="4" cx="' + xForReading(lastReadingIndex).toFixed(1) + '" cy="' + yForPercent(points[lastReadingIndex][series.key]).toFixed(1) + '" style="fill:' + series.color + '"></circle>';
    }).join("");

    var chartEl = document.getElementById("trend-chart");
    chartEl.innerHTML =
      '<svg viewBox="0 0 ' + chartWidth + ' ' + chartHeight + '" role="img" aria-label="Udvikling for CPU, hukommelse og disk over de seneste målinger">' +
      gridSvg +
      linesSvg + endDotsSvg +
      '<g class="trend-chart__crosshair-group" hidden><line class="trend-chart__crosshair" y1="' + paddingTop + '" y2="' + (paddingTop + plotHeight) + '"></line></g>' +
      '<rect class="trend-chart__hit" x="' + paddingLeft + '" y="' + paddingTop + '" width="' + plotWidth + '" height="' + plotHeight + '"></rect>' +
      '</svg>';

    var svg = chartEl.querySelector("svg");
    var hitRect = chartEl.querySelector(".trend-chart__hit");
    var crosshairGroup = chartEl.querySelector(".trend-chart__crosshair-group");
    var crosshairLine = crosshairGroup.querySelector("line");
    var wrapEl = document.getElementById("trend-chart-wrap");

    function nearestReadingIndex(pointerClientX) {
      var box = svg.getBoundingClientRect();
      var svgX = (pointerClientX - box.left) / box.width * chartWidth;
      return clamp(Math.round(((svgX - paddingLeft) / plotWidth) * lastReadingIndex), 0, lastReadingIndex);
    }

    function showTooltip(readingIndex, pointerClientX) {
      var p = points[readingIndex];
      crosshairGroup.hidden = false;
      crosshairLine.setAttribute("x1", xForReading(readingIndex).toFixed(1));
      crosshairLine.setAttribute("x2", xForReading(readingIndex).toFixed(1));

      tooltip.innerHTML =
        '<div class="trend-tooltip__title">Måling ' + (readingIndex + 1) + ' af ' + points.length + '</div>' +
        TREND_SERIES.map(function (series) {
          return '<div class="trend-tooltip__row">' +
            '<span class="trend-tooltip__row-label"><span class="trend-tooltip__key" style="background:' + series.color + '"></span>' + esc(series.label) + '</span>' +
            '<span class="trend-tooltip__value">' + nf(p[series.key], 0) + ' %</span>' +
            '</div>';
        }).join("");
      tooltip.hidden = false;

      var wrapBox = wrapEl.getBoundingClientRect();
      var left = pointerClientX - wrapBox.left + 14;
      if (left + 150 > wrapBox.width) left = pointerClientX - wrapBox.left - 150 - 14;
      tooltip.style.left = Math.max(0, left) + "px";
    }

    function hideTooltip() {
      crosshairGroup.hidden = true;
      tooltip.hidden = true;
    }

    hitRect.addEventListener("mousemove", function (e) { showTooltip(nearestReadingIndex(e.clientX), e.clientX); });
    hitRect.addEventListener("mouseleave", hideTooltip);

    document.getElementById("trend-table-body").innerHTML = points.map(function (p, i) {
      return '<tr>' +
        '<td>Måling ' + (i + 1) + '</td>' +
        '<td class="is-right">' + nf(p.cpu, 0) + ' %</td>' +
        '<td class="is-right">' + nf(p.ram, 0) + ' %</td>' +
        '<td class="is-right">' + nf(p.disk, 0) + ' %</td>' +
        '</tr>';
    }).join("");
  }

  function setFleetStatus(text) {
    var el = document.getElementById("fleet-status");
    if (text) { el.textContent = text; el.hidden = false; } else { el.hidden = true; }
  }

  function ensureCompanyId() {
    if (State.companyId) return Promise.resolve(State.companyId);
    return NWApi.get("/company").then(function (company) {
      State.companyId = company && company.id;
      return State.companyId;
    });
  }

  // How many recent readings the trend chart keeps per server — a company
  // can have arbitrarily many historical rows in companyhardwaredata; the
  // chart only needs a recent-trend window, not the full log.
  var HISTORY_LIMIT = 30;

  // companyhardwaredata has no serverName/ipAddress and is an insert-only
  // log (one row per reading, no update/upsert), so this groups rows by
  // ServiceId, keeps the most recent HISTORY_LIMIT per service (oldest
  // first) for the trend chart, and treats the last one as that server's
  // current snapshot for the fleet list/gauges.
  function mapHardwareRows(rows) {
    var byService = {};
    rows.forEach(function (row) {
      (byService[row.serviceId] = byService[row.serviceId] || []).push(row);
    });

    var servers = [];
    var history = {};
    Object.keys(byService).forEach(function (serviceId) {
      var readings = byService[serviceId].sort(function (a, b) { return a.id - b.id; });
      if (readings.length > HISTORY_LIMIT) readings = readings.slice(readings.length - HISTORY_LIMIT);

      var mapped = readings.map(function (row) {
        return {
          cpuUsage: row.cpuUsages,
          ramUsage: row.ramUsage / 1024,
          ramTotal: row.ramTotal / 1024,
          diskTotalSpace: row.diskTotalSpace,
          diskRemainingSpace: row.diskRemainingSpace
        };
      });
      history[serviceId] = mapped;

      var latest = mapped[mapped.length - 1];
      servers.push({
        id: Number(serviceId),
        serverName: "Service " + serviceId,
        ipAddress: "—",
        cpuUsage: latest.cpuUsage,
        ramUsage: latest.ramUsage,
        ramTotal: latest.ramTotal,
        diskTotalSpace: latest.diskTotalSpace,
        diskRemainingSpace: latest.diskRemainingSpace
      });
    });

    return { servers: servers, history: history };
  }

  function loadServers() {
    setFleetStatus("Indlæser servere…");
    return ensureCompanyId().then(function (companyId) {
      return NWApi.get("/CompanyHardwareData/GetAll/" + companyId);
    }).then(function (rows) {
      var mapped = mapHardwareRows(rows || []);
      State.servers = mapped.servers;
      State.history = mapped.history;
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
    document.getElementById("trend-table-toggle").addEventListener("click", function () {
      State.trendView = State.trendView === "table" ? "chart" : "table";
      applyTrendView();
    });

    renderLiveStatus();
    setView(State.view);
    loadServers();
    scheduleTick();
  });
})();
