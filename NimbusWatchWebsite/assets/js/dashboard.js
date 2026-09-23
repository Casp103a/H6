/* ==========================================================================
   Server monitoring dashboard. Loads data in two steps: GET /api/
   ServerService/GetAll/{companyId} for the fleet roster (clientId is the
   GUID used as this app's server id everywhere), then per server GET /api/
   CompanyHardwareData/GetHardwareData/{companyId}?clientId={clientId} for
   its reading history. The latest reading is the fleet/gauge snapshot; the
   rest feed the trend chart (see buildServer/renderTrend).
   ========================================================================== */
(function () {
  "use strict";

  // Shared palette + helpers, defined in common.js so every page draws status colors from one place.
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

  // Drives status(), the alert banner, and the ram/disk gauge rings. CPU's ring uses its own, more sensitive pair below on purpose.
  var MB = 1024 * 1024;
  var THRESHOLDS = {
    cpuWarn: 85, cpuCrit: 95,
    ramWarn: 75, ramCrit: 90,
    diskWarn: 85, diskCrit: 95,
    networkSentWarn: 250 * MB, networkSentCrit: 1024 * MB,
    networkReceivedWarn: 250 * MB, networkReceivedCrit: 1024 * MB
  };
  var GAUGE_CPU_WARN = 60, GAUGE_CPU_CRIT = 85;

  var POLL_MS = 60000; // real network calls, not a client-side generator — no need for a 2.2s cadence

  var State = { servers: [], history: {}, selected: null, live: true, view: "grid", companyId: null, trendView: "chart", trendRange: "today", query: "", serverTypes: [], typeFilter: "" };

  var UNKNOWN_TYPE_LABEL = "Ukendt type";

  // Human-readable name for a serverTypeId, from the /ServerTypes list loaded once at startup.
  function typeName(serverTypeId) {
    var match = State.serverTypes.filter(function (t) { return t.id === serverTypeId; })[0];
    return match ? match.serverTypeName : UNKNOWN_TYPE_LABEL;
  }

  // Trend interval options (#graph-interval). "today" is calendar-day; the rest are rolling windows back from now.
  var TREND_RANGES = [
    { key: "today", label: "I dag", from: function () { var d = new Date(); d.setHours(0, 0, 0, 0); return d.getTime(); } },
    { key: "week", label: "Seneste uge", from: function () { return Date.now() - 7 * 24 * 60 * 60 * 1000; } },
    { key: "month", label: "Seneste måned", from: function () { return Date.now() - 30 * 24 * 60 * 60 * 1000; } },
    { key: "year", label: "Seneste år", from: function () { return Date.now() - 365 * 24 * 60 * 60 * 1000; } }
  ];

  // Filters a server's full reading history down to the selected trend range.
  function filterHistoryByRange(history) {
    var range = TREND_RANGES.filter(function (r) { return r.key === State.trendRange; })[0] || TREND_RANGES[0];
    var from = range.from();
    return history.filter(function (h) { return h.createdAt && h.createdAt.getTime() >= from; });
  }

  // Above this many readings, buildTrendPoints averages them into evenly-timed buckets instead of plotting every raw reading.
  var MAX_TREND_POINTS = 150;

  function buildTrendPoints(history) {
    var points = history.map(function (h) {
      return { cpu: h.cpuUsage, ram: ramPct(h), disk: diskUsedPct(h), NetworkBytesSent: h.NetworkBytesSent, NetworkBytesReceived: h.NetworkBytesReceived, time: h.createdAt };
    });
    if (points.length <= MAX_TREND_POINTS) return points;

    var startMs = points[0].time.getTime();
    var endMs = points[points.length - 1].time.getTime();
    var span = Math.max(1, endMs - startMs);
    var buckets = [];
    points.forEach(function (p) {
      var bucketIndex = clamp(Math.floor(((p.time.getTime() - startMs) / span) * MAX_TREND_POINTS), 0, MAX_TREND_POINTS - 1);
      var bucket = buckets[bucketIndex] || (buckets[bucketIndex] = { cpu: 0, ram: 0, disk: 0, NetworkBytesSent: 0, NetworkBytesReceived: 0, timeSum: 0, n: 0 });
      bucket.cpu += p.cpu; bucket.ram += p.ram; bucket.disk += p.disk; bucket.NetworkBytesSent += p.NetworkBytesSent; bucket.NetworkBytesReceived += p.NetworkBytesReceived; bucket.timeSum += p.time.getTime(); bucket.n++;
    });
    return buckets.filter(Boolean).map(function (b) {
      return { cpu: b.cpu / b.n, ram: b.ram / b.n, disk: b.disk / b.n, NetworkBytesSent: b.NetworkBytesSent / b.n, NetworkBytesReceived: b.NetworkBytesReceived / b.n, time: new Date(b.timeSum / b.n) };
    });
  }

  // Servers matching the header search box, by serverName or ipAddress
  // (case-insensitive substring), further narrowed by the server-type
  // filter — empty values for either match everything.
  function visibleServers() {
    var q = State.query.trim().toLowerCase();
    return State.servers.filter(function (s) {
      if (State.typeFilter !== "" && String(s.serverTypeId) !== State.typeFilter) return false;
      if (!q) return true;
      return (s.serverName || "").toLowerCase().indexOf(q) !== -1 || (s.ipAddress || "").toLowerCase().indexOf(q) !== -1;
    });
  }

  // CPU/RAM/disk share one 0-100% axis. Network is bytes, not a percentage, so it gets its own chart with its own axis (see drawChart).
  var PERCENT_SERIES = [
    { key: "cpu", label: "CPU", color: "var(--nf-chart-cpu)" },
    { key: "ram", label: "Hukommelse", color: "var(--nf-chart-ram)" },
    { key: "disk", label: "Disk", color: "var(--nf-chart-disk)" }
  ];
  var NETWORK_SERIES = [
    { key: "NetworkBytesSent", label: "Netværk (Sendt)", color: "var(--nf-chart-network-sent)" },
    { key: "NetworkBytesReceived", label: "Netværk (Modtaget)", color: "var(--nf-chart-network-received)" }
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

  // Network bytes as % of its critical threshold — ring() needs 0-100, and a raw byte count would wrap its conic-gradient many times over.
  function networkPct(bytes, crit) { return crit ? clamp((bytes / crit) * 100, 0, 100) : 0; }

  // A byte count in the largest unit that keeps it >= 1 (B/KB/MB/GB/TB) — a raw byte count is unreadable on its own.
  function formatBytes(n) {
    var v = Math.abs(n) || 0;
    var units = ["B", "KB", "MB", "GB", "TB"];
    var i = 0;
    while (v >= 1024 && i < units.length - 1) { v /= 1024; i++; }
    return nf(v, i === 0 ? 0 : 1) + " " + units[i];
  }

  // A reading's CreatedAt as "DD/MM HH:MM:SS" — the trend window can span
  // more than one day, so it's not time-only like renderClock's header clock.
  function formatTimestamp(date) {
    if (!date || isNaN(date.getTime())) return "—";
    var pad = function (n) { return String(n).padStart(2, "0"); };
    return pad(date.getDate()) + "/" + pad(date.getMonth() + 1) + " " +
      pad(date.getHours()) + ":" + pad(date.getMinutes()) + ":" + pad(date.getSeconds());
  }

  // Classifies a server as OK/Advarsel/Kritisk from its CPU/RAM/disk against THRESHOLDS, worst metric wins.
  function status(s) {
    var ram = ramPct(s), disk = diskUsedPct(s);
    if (s.cpuUsage >= THRESHOLDS.cpuCrit || ram >= THRESHOLDS.ramCrit || disk >= THRESHOLDS.diskCrit || s.NetworkBytesSent >= THRESHOLDS.networkSentCrit || s.NetworkBytesReceived >= THRESHOLDS.networkReceivedCrit) {
      return { label: "Kritisk", color: CRIT, bg: TINT_CRIT, fg: INK_CRIT };
    }
    if (s.cpuUsage >= THRESHOLDS.cpuWarn || ram >= THRESHOLDS.ramWarn || disk >= THRESHOLDS.diskWarn || s.NetworkBytesSent >= THRESHOLDS.networkSentWarn || s.NetworkBytesReceived >= THRESHOLDS.networkReceivedWarn) {
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
    renderFleetStatus();
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
    label.textContent = State.live ? "Live · opdaterer hvert 60. sekund" : "Opdatering sat på pause";
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

  function fleetCardHtml(s) {
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
      '<div><div class="fleet-card__metric-label">Netværk (Sendt)</div><div class="fleet-card__metric-value">' + formatBytes(s.NetworkBytesSent) + '</div></div>' +
      '<div><div class="fleet-card__metric-label">Netværk (Modtaget)</div><div class="fleet-card__metric-value">' + formatBytes(s.NetworkBytesReceived) + '</div></div>' +
      '</div>' +
      '</button>';
  }

  // Groups the visible servers by server type, sorted by type name (unknown/unmatched types last), for the grid's section headers.
  function groupByType(servers) {
    var groups = {};
    servers.forEach(function (s) {
      var key = String(s.serverTypeId);
      if (!groups[key]) groups[key] = { label: typeName(s.serverTypeId), servers: [] };
      groups[key].servers.push(s);
    });
    return Object.keys(groups).map(function (key) { return groups[key]; }).sort(function (a, b) {
      if (a.label === UNKNOWN_TYPE_LABEL) return 1;
      if (b.label === UNKNOWN_TYPE_LABEL) return -1;
      return a.label.localeCompare(b.label, "da");
    });
  }

  function renderFleetGrid() {
    var groups = groupByType(visibleServers());
    document.getElementById("fleet-grid").innerHTML = groups.map(function (g) {
      return '<div class="fleet-group">' +
        '<div class="fleet-group__title">' + esc(g.label) + ' <span class="fleet-group__count">(' + g.servers.length + ')</span></div>' +
        '<div class="fleet-grid">' + g.servers.map(fleetCardHtml).join("") + '</div>' +
        '</div>';
    }).join("");
  }

  function renderSidebar() {
    document.getElementById("server-list").innerHTML = visibleServers().map(function (s) {
      var st = status(s);
      var active = s.id === State.selected;
      return '<button type="button" class="server-row' + (active ? " server-row--selected" : "") + '" data-id="' + s.id + '">' +
        '<span class="server-row__dot" style="background:' + st.color + '"></span>' +
        '<span class="server-row__id">' +
        '<span class="server-row__name">' + esc(s.serverName) + '</span>' +
        '<span class="server-row__sub">' + esc(s.ipAddress) + '</span>' +
        '</span>' +
        '<span class="server-row__metrics">' +
        '<span class="server-row__cpu" style="color:' + st.color + '">' + nf(s.cpuUsage, 0) + ' % CPU</span>' +
        '<span class="server-row__secondary">' + nf(diskUsedPct(s), 0) + ' % Disk</span>' +
        '<span class="server-row__secondary">' + formatBytes(s.NetworkBytesSent) + ' Sendt</span>' +
        '<span class="server-row__secondary">' + formatBytes(s.NetworkBytesReceived) + ' Modtaget</span>' +
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
      body = "Diskforbrug " + nf(disk, 0) + " % mod grænsen " + THRESHOLDS.diskWarn + " % · CPU " + nf(s.cpuUsage, 0) + " % · hukommelse " + nf(ram, 0) + " % · Netværk (Sendt) " + formatBytes(s.NetworkBytesSent) + " · Netværk (Modtaget) " + formatBytes(s.NetworkBytesReceived) + "." ;
    } else if (s.cpuUsage >= THRESHOLDS.cpuWarn) {
      body = "CPU " + nf(s.cpuUsage, 0) + " % vedvarende · hukommelse " + nf(ram, 0) + " % · diskforbrug " + nf(disk, 0) + " % · Netværk (Sendt) " + formatBytes(s.NetworkBytesSent) + " · Netværk (Modtaget) " + formatBytes(s.NetworkBytesReceived) + ".";
    } else {
      body = "Hukommelse " + nf(ram, 0) + " % mod grænsen " + THRESHOLDS.ramWarn + " % · CPU " + nf(s.cpuUsage, 0) + " % · diskforbrug " + nf(disk, 0) + " % · Netværk (Sendt) " + formatBytes(s.NetworkBytesSent) + " · Netværk (Modtaget) " + formatBytes(s.NetworkBytesReceived) + ".";
    }
    document.getElementById("alert-title").textContent = title;
    document.getElementById("alert-body").textContent = body;
  }

  function renderGauges(s) {
    var ram = ramPct(s), disk = diskUsedPct(s);
    var gauges = [
      { label: "CPU-belastning", value: nf(s.cpuUsage, 0) + "%", big: nf(s.cpuUsage, 1) + " %", hint: "", ring: ring(s.cpuUsage, s.cpuUsage > GAUGE_CPU_CRIT ? CRIT : s.cpuUsage > GAUGE_CPU_WARN ? WARN : BLUE) },
      { label: "Hukommelse", value: nf(ram, 0) + "%", big: nf(s.ramUsage, 1) + " GB", hint: "af " + nf(s.ramTotal, 0) + " GB i alt", ring: ring(ram, ram > THRESHOLDS.ramCrit ? CRIT : ram > THRESHOLDS.ramWarn ? WARN : BLUE) },
      { label: "Diskplads", value: nf(disk, 0) + "%", big: nf(s.diskRemainingSpace, 0) + " GB fri", hint: "af " + nf(s.diskTotalSpace, 0) + " GB i alt", ring: ring(disk, disk >= THRESHOLDS.diskCrit ? CRIT : disk >= THRESHOLDS.diskWarn ? WARN : BLUE) },
      { label: "Netværk (Sendt)", value: formatBytes(s.NetworkBytesSent), big: formatBytes(s.NetworkBytesSent), hint: "", ring: ring(networkPct(s.NetworkBytesSent, THRESHOLDS.networkSentCrit), s.NetworkBytesSent >= THRESHOLDS.networkSentCrit ? CRIT : s.NetworkBytesSent >= THRESHOLDS.networkSentWarn ? WARN : BLUE) },
      { label: "Netværk (Modtaget)", value: formatBytes(s.NetworkBytesReceived), big: formatBytes(s.NetworkBytesReceived), hint: "", ring: ring(networkPct(s.NetworkBytesReceived, THRESHOLDS.networkReceivedCrit), s.NetworkBytesReceived >= THRESHOLDS.networkReceivedCrit ? CRIT : s.NetworkBytesReceived >= THRESHOLDS.networkReceivedWarn ? WARN : BLUE) }
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

  function legendHtml(series) {
    return series.map(function (s) {
      return '<span class="trend-legend__item"><span class="trend-legend__key" style="background:' + s.color + '"></span>' + esc(s.label) + '</span>';
    }).join("");
  }

  function renderTrendLegend() {
    document.getElementById("trend-legend").innerHTML = legendHtml(PERCENT_SERIES);
    document.getElementById("trend-legend-network").innerHTML = legendHtml(NETWORK_SERIES);
  }

  // Shows the charts or the table (never both), without touching their content, so toggling doesn't fight the poll re-render.
  function applyTrendView() {
    var isTable = State.trendView === "table";
    document.getElementById("trend-chart-wrap").hidden = isTable;
    document.getElementById("trend-chart-wrap-network").hidden = isTable;
    document.getElementById("trend-table-wrap").hidden = !isTable;
    document.getElementById("trend-table-toggle").textContent = isTable ? "Vis som graf" : "Vis som tabel";
  }

  // The highest value either network series reaches, with headroom so the peak isn't pinned to the top, floored to avoid a 0-height axis.
  function networkScaleMax(points) {
    var max = 0;
    points.forEach(function (p) {
      NETWORK_SERIES.forEach(function (series) { if (p[series.key] > max) max = p[series.key]; });
    });
    return Math.max(1024, max * 1.15);
  }

  // Draws one trend chart for the given series/scale (see PERCENT_SERIES/NETWORK_SERIES) with a hover crosshair + tooltip.
  function drawChart(chartElId, tooltipElId, wrapElId, points, series, scaleMax, formatTick, formatValue, ariaLabel) {
    var chartWidth = 640, chartHeight = 190;
    var paddingLeft = 44, paddingRight = 12, paddingTop = 10, paddingBottom = 10;
    var plotWidth = chartWidth - paddingLeft - paddingRight;
    var plotHeight = chartHeight - paddingTop - paddingBottom;
    var lastReadingIndex = points.length - 1;
    var firstTime = points[0].time.getTime();
    var lastTime = points[lastReadingIndex].time.getTime();
    var timeSpan = Math.max(1, lastTime - firstTime);

    // Positions a point by its actual timestamp, not its array index, so irregular gaps between readings don't skew the line.
    function xForPoint(point) {
      return paddingLeft + (timeSpan === 0 ? 0 : ((point.time.getTime() - firstTime) / timeSpan) * plotWidth);
    }
    function yForValue(value) {
      return paddingTop + plotHeight - (clamp(value, 0, scaleMax) / scaleMax) * plotHeight;
    }
    function pathFor(seriesKey) {
      return points.map(function (p, readingIndex) {
        return (readingIndex === 0 ? "M" : "L") + xForPoint(p).toFixed(1) + "," + yForValue(p[seriesKey]).toFixed(1);
      }).join(" ");
    }

    var gridSvg = [0, scaleMax / 2, scaleMax].map(function (tickValue) {
      var tickY = yForValue(tickValue).toFixed(1);
      return '<line class="trend-chart__grid" x1="' + paddingLeft + '" x2="' + (chartWidth - paddingRight) + '" y1="' + tickY + '" y2="' + tickY + '"></line>' +
        '<text class="trend-chart__tick" x="' + (paddingLeft - 6) + '" y="' + (yForValue(tickValue) + 3).toFixed(1) + '" text-anchor="end">' + esc(formatTick(tickValue)) + '</text>';
    }).join("");

    var linesSvg = series.map(function (s) {
      return '<path class="trend-chart__line" d="' + pathFor(s.key) + '" style="stroke:' + s.color + '"></path>';
    }).join("");

    var endDotsSvg = series.map(function (s) {
      return '<circle class="trend-chart__dot" r="4" cx="' + xForPoint(points[lastReadingIndex]).toFixed(1) + '" cy="' + yForValue(points[lastReadingIndex][s.key]).toFixed(1) + '" style="fill:' + s.color + '"></circle>';
    }).join("");

    var chartEl = document.getElementById(chartElId);
    chartEl.innerHTML =
      '<svg viewBox="0 0 ' + chartWidth + ' ' + chartHeight + '" role="img" aria-label="' + esc(ariaLabel) + '">' +
      gridSvg +
      linesSvg + endDotsSvg +
      '<g class="trend-chart__crosshair-group" hidden><line class="trend-chart__crosshair" y1="' + paddingTop + '" y2="' + (paddingTop + plotHeight) + '"></line></g>' +
      '<rect class="trend-chart__hit" x="' + paddingLeft + '" y="' + paddingTop + '" width="' + plotWidth + '" height="' + plotHeight + '"></rect>' +
      '</svg>';

    var hitRect = chartEl.querySelector(".trend-chart__hit");
    var crosshairGroup = chartEl.querySelector(".trend-chart__crosshair-group");
    var crosshairLine = crosshairGroup.querySelector("line");
    var wrapEl = document.getElementById(wrapElId);
    var tooltip = document.getElementById(tooltipElId);

    // Finds the point whose timestamp is closest to the pointer's x position (binary search — points are time-sorted). Reads the hit
    // rect's own rendered box rather than the <svg>'s, so it stays accurate even if the SVG ever letterboxes.
    function nearestReadingIndex(pointerClientX) {
      var box = hitRect.getBoundingClientRect();
      var fraction = box.width === 0 ? 0 : clamp((pointerClientX - box.left) / box.width, 0, 1);
      var targetTime = firstTime + fraction * timeSpan;
      var lo = 0, hi = lastReadingIndex;
      while (lo < hi) {
        var mid = (lo + hi) >> 1;
        if (points[mid].time.getTime() < targetTime) lo = mid + 1; else hi = mid;
      }
      if (lo > 0 && Math.abs(points[lo - 1].time.getTime() - targetTime) < Math.abs(points[lo].time.getTime() - targetTime)) lo--;
      return lo;
    }

    function showTooltip(readingIndex, pointerClientX) {
      var p = points[readingIndex];
      crosshairGroup.hidden = false;
      crosshairLine.setAttribute("x1", xForPoint(p).toFixed(1));
      crosshairLine.setAttribute("x2", xForPoint(p).toFixed(1));

      tooltip.innerHTML =
        '<div class="trend-tooltip__title">' + esc(formatTimestamp(p.time)) + '</div>' +
        series.map(function (s) {
          return '<div class="trend-tooltip__row">' +
            '<span class="trend-tooltip__row-label"><span class="trend-tooltip__key" style="background:' + s.color + '"></span>' + esc(s.label) + '</span>' +
            '<span class="trend-tooltip__value">' + esc(formatValue(p[s.key])) + '</span>' +
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
  }

  function renderTrend(s) {
    var history = filterHistoryByRange(State.history[s.id] || []);
    var toggleBtn = document.getElementById("trend-table-toggle");
    var emptyNote = document.getElementById("trend-empty");

    renderTrendLegend();

    if (history.length < 2) {
      document.getElementById("trend-chart-wrap").hidden = true;
      document.getElementById("trend-chart-wrap-network").hidden = true;
      document.getElementById("trend-table-wrap").hidden = true;
      toggleBtn.hidden = true;
      document.getElementById("trend-tooltip").hidden = true;
      document.getElementById("trend-tooltip-network").hidden = true;
      emptyNote.hidden = false;
      return;
    }
    emptyNote.hidden = true;
    toggleBtn.hidden = false;
    applyTrendView();

    var points = buildTrendPoints(history);

    drawChart("trend-chart", "trend-tooltip", "trend-chart-wrap", points, PERCENT_SERIES, 100,
      function (v) { return v + "%"; },
      function (v) { return nf(v, 0) + " %"; },
      "Udvikling for CPU, hukommelse og disk over de seneste målinger");

    drawChart("trend-chart-network", "trend-tooltip-network", "trend-chart-wrap-network", points, NETWORK_SERIES, networkScaleMax(points),
      formatBytes,
      formatBytes,
      "Udvikling for netværkstrafik (sendt/modtaget) over de seneste målinger");

    document.getElementById("trend-table-body").innerHTML = points.map(function (p) {
      return '<tr>' +
        '<td>' + esc(formatTimestamp(p.time)) + '</td>' +
        '<td class="is-right">' + nf(p.cpu, 0) + ' %</td>' +
        '<td class="is-right">' + nf(p.ram, 0) + ' %</td>' +
        '<td class="is-right">' + nf(p.disk, 0) + ' %</td>' +
        '<td class="is-right">' + formatBytes(p.NetworkBytesSent) + '</td>' +
        '<td class="is-right">' + formatBytes(p.NetworkBytesReceived) + '</td>' +
        '</tr>';
    }).join("");
  }

  function setFleetStatus(text) {
    var el = document.getElementById("fleet-status");
    if (text) { el.textContent = text; el.hidden = false; } else { el.hidden = true; }
  }

  // Fleet-grid empty state: no servers at all vs. none matching the search box.
  function renderFleetStatus() {
    if (!State.servers.length) { setFleetStatus("Ingen servere endnu."); return; }
    if (State.query.trim() && !visibleServers().length) {
      setFleetStatus('Ingen servere matcher "' + State.query.trim() + '".');
      return;
    }
    setFleetStatus(null);
  }

  function ensureCompanyId() {
    if (State.companyId) return Promise.resolve(State.companyId);
    return NWApi.get("/company").then(function (company) {
      State.companyId = company && company.id;
      return State.companyId;
    });
  }

  // The server-type catalog for the group headers/filter dropdown. Loaded once at startup, not on every poll tick — it rarely changes. Never rejects — a failure just means no types to group/filter by.
  function loadServerTypes() {
    return NWApi.get("/ServerTypes").then(function (types) {
      State.serverTypes = types || [];
      renderTypeFilterOptions();
    }).catch(function () {
      State.serverTypes = [];
    });
  }

  function renderTypeFilterOptions() {
    var select = document.getElementById("server-type-filter");
    select.innerHTML = '<option value="">Alle typer</option>' + State.serverTypes.map(function (t) {
      return '<option value="' + t.id + '">' + esc(t.serverTypeName) + '</option>';
    }).join("");
    select.value = State.typeFilter;
  }

  // The fleet roster. Never rejects — a failure just means an empty fleet.
  function loadServerInfo(companyId) {
    return NWApi.get("/ServerService/GetAll/" + companyId).then(function (rows) {
      return rows || [];
    }).catch(function () {
      return [];
    });
  }

  // One server's reading history. Never rejects — no data just means no history.
  function loadHardwareData(companyId, clientId) {
    return NWApi.get("/CompanyHardwareData/GetHardwareData/" + companyId + "?clientId=" + encodeURIComponent(clientId)).then(function (rows) {
      return rows || [];
    }).catch(function () {
      return [];
    });
  }

  // Combines a server's roster info with its reading history: the last reading is the current snapshot, the full history feeds the trend chart.
  function buildServer(info, readings) {
    var sorted = readings.slice().sort(function (a, b) { return a.id - b.id; });

    var history = sorted.map(function (row) {
      return {
        cpuUsage: row.cpuUsages,
        ramUsage: row.ramUsage / 1024,
        ramTotal: row.ramTotal / 1024,
        diskTotalSpace: row.diskTotalSpace,
        diskRemainingSpace: row.diskRemainingSpace,
        NetworkBytesSent: row.networkBytesSent,
        NetworkBytesReceived: row.networkBytesReceived,
        createdAt: row.createdAt ? new Date(row.createdAt) : null
      };
    });

    var latest = history[history.length - 1] || { cpuUsage: 0, ramUsage: 0, ramTotal: 0, diskTotalSpace: 0, diskRemainingSpace: 0, NetworkBytesSent: 0, NetworkBytesReceived: 0 };

    return {
      server: {
        id: info.clientId,
        serverName: info.serverName,
        ipAddress: info.ipAddress,
        serverTypeId: info.serverTypeId,
        cpuUsage: latest.cpuUsage,
        ramUsage: latest.ramUsage,
        ramTotal: latest.ramTotal,
        diskTotalSpace: latest.diskTotalSpace,
        diskRemainingSpace: latest.diskRemainingSpace,
        NetworkBytesSent: latest.NetworkBytesSent,
        NetworkBytesReceived: latest.NetworkBytesReceived
      },
      history: history
    };
  }

  async function loadServers() {
    if (!State.servers.length) setFleetStatus("Indlæser servere…");
    try {
      var companyId = await ensureCompanyId();
      var serverInfoList = await loadServerInfo(companyId);
      var built = await Promise.all(serverInfoList.map(async function (info) {
        var readings = await loadHardwareData(companyId, info.clientId);
        return buildServer(info, readings);
      }));

      State.servers = built.map(function (b) { return b.server; });
      State.history = {};
      built.forEach(function (b) { State.history[b.server.id] = b.history; });
      if (State.selected == null || !getSelected()) {
        State.selected = State.servers.length ? State.servers[0].id : null;
      }
      renderClock();
      render();
    } catch (err) {
      setFleetStatus("Kunne ikke hente servere: " + err.message);
    }
  }

  // Refetches just one server's history and re-renders — used when opening its detail view, so it shows fresh data right away.
  function refreshServerData(clientId) {
    var existing = getSelected();
    if (!existing || existing.id !== clientId || !State.companyId) return;

    return loadHardwareData(State.companyId, clientId).then(function (readings) {
      var built = buildServer({ clientId: clientId, serverName: existing.serverName, ipAddress: existing.ipAddress, serverTypeId: existing.serverTypeId }, readings);
      var index = State.servers.findIndex(function (s) { return s.id === clientId; });
      if (index === -1) return;
      State.servers[index] = built.server;
      State.history[clientId] = built.history;
      render();
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
      State.selected = btn.getAttribute("data-id");
      render();
      refreshServerData(State.selected);
    });
    document.getElementById("fleet-grid").addEventListener("click", function (e) {
      var btn = e.target.closest("[data-id]");
      if (!btn) return;
      State.selected = btn.getAttribute("data-id");
      setView("detail");
      render();
      refreshServerData(State.selected);
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
    document.getElementById("server-search").addEventListener("input", function (e) {
      State.query = e.target.value;
      render();
    });
    document.getElementById("server-type-filter").addEventListener("change", function (e) {
      State.typeFilter = e.target.value;
      render();
    });

    var intervalSelect = document.getElementById("graph-interval");
    intervalSelect.innerHTML = TREND_RANGES.map(function (r) {
      return '<option value="' + r.key + '">' + esc(r.label) + '</option>';
    }).join("");
    intervalSelect.value = State.trendRange;
    intervalSelect.addEventListener("change", function (e) {
      State.trendRange = e.target.value;
      var sel = getSelected();
      if (sel) renderTrend(sel);
    });

    renderLiveStatus();
    setView(State.view);
    loadServerTypes();
    loadServers();
    scheduleTick();
  });
})();
