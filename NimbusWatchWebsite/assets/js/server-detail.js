/* ==========================================================================
   Single-server detail view (server-detail.php?id={clientId}).

   Renders the server-list sidebar, host header, alert banner, gauges, disk
   card, service-error list, and the CPU/RAM/disk/network graphs. Graphs are drawn with Chart.js
   (loaded via CDN in server-detail.php), not hand-rolled SVG.

   This file only reads the selected id from the URL and renders around it.
   The server roster/reading history and the shared header controls (search,
   filter, live/pause) live in dataprovider.js instead.

   Picking a different server in the sidebar loads that server's own URL —
   it's a real navigation, not a content swap — so the URL always matches
   what's on screen.
   ========================================================================== */
(function () {
  "use strict";

  var F = window.NWFleet;
  var State = F.State;
  var THRESHOLDS = F.THRESHOLDS;
  var status = F.status;
  var ramPct = F.ramPct;
  var diskUsedPct = F.diskUsedPct;
  var formatBytes = F.formatBytes;
  var clamp = F.clamp;
  var visibleServers = F.visibleServers;

  // Shared palette + helpers, defined in common.js so every page draws status colors from one place.
  var OK = NW.colors.OK;
  var WARN = NW.colors.WARN;
  var CRIT = NW.colors.CRIT;
  var BLUE = NW.colors.BLUE;
  var esc = NW.esc;
  var nf = NW.nf;

  State.selected = new URLSearchParams(window.location.search).get("id");
  if (!State.selected) {
    window.location.href = "index.php";
    return;
  }

  // CPU's gauge ring uses its own, more sensitive pair than THRESHOLDS.cpuWarn/cpuCrit on purpose.
  var GAUGE_CPU_WARN = 60, GAUGE_CPU_CRIT = 85;

  // Builds the CSS conic-gradient string that draws a gauge ring: a colored arc for pct% of the circle, the track color for the rest.
  function ring(pct, color) {
    return "conic-gradient(" + color + " " + Math.round(pct * 3.6) + "deg, var(--nf-track-bg) 0deg)";
  }

  // Network bytes as % of its critical threshold — ring() needs 0-100, and a raw byte count would wrap its conic-gradient many times over.
  function networkPct(bytes, crit) { return crit ? clamp((bytes / crit) * 100, 0, 100) : 0; }

  // A reading's CreatedAt as "DD/MM HH:MM:SS" — the graph's date range can span
  // more than one day, so it's not time-only like the header clock.
  function formatTimestamp(date) {
    if (!date || isNaN(date.getTime())) return "—";
    var pad = function (n) { return String(n).padStart(2, "0"); };
    return pad(date.getDate()) + "/" + pad(date.getMonth() + 1) + " " +
      pad(date.getHours()) + ":" + pad(date.getMinutes()) + ":" + pad(date.getSeconds());
  }

  var DateOptions = [
    { key: "today", label: "I dag", from: function () { var d = new Date(); d.setHours(0, 0, 0, 0); return d.getTime(); } },
    { key: "week", label: "Seneste uge", from: function () { return Date.now() - 7 * 24 * 60 * 60 * 1000; } },
    { key: "month", label: "Seneste måned", from: function () { return Date.now() - 30 * 24 * 60 * 60 * 1000; } },
    { key: "year", label: "Seneste år", from: function () { return Date.now() - 365 * 24 * 60 * 60 * 1000; } }
  ];

  var graphView = "chart", dateRange = "today";

  // Filters a server's full reading history down to the selected date range.
  function filterHistoryByRange(history) {
    var range = DateOptions.filter(function (r) { return r.key === dateRange; })[0] || DateOptions[0];
    var from = range.from();
    return history.filter(function (h) { return h.createdAt && h.createdAt.getTime() >= from; });
  }

  // Above this many readings, buildGraphPoints averages them into evenly-timed buckets instead of plotting every raw reading.
  var MAX_GRAPH_POINTS = 150;

  function buildGraphPoints(history) {
    var points = history.map(function (h) {
      return { cpu: h.cpuUsage, ram: ramPct(h), disk: diskUsedPct(h), NetworkBytesSent: h.NetworkBytesSent, NetworkBytesReceived: h.NetworkBytesReceived, time: h.createdAt };
    });
    if (points.length <= MAX_GRAPH_POINTS) return points;

    var startMs = points[0].time.getTime();
    var endMs = points[points.length - 1].time.getTime();
    var span = Math.max(1, endMs - startMs);
    var buckets = [];
    points.forEach(function (p) {
      var bucketIndex = clamp(Math.floor(((p.time.getTime() - startMs) / span) * MAX_GRAPH_POINTS), 0, MAX_GRAPH_POINTS - 1);
      var bucket = buckets[bucketIndex] || (buckets[bucketIndex] = { cpu: 0, ram: 0, disk: 0, NetworkBytesSent: 0, NetworkBytesReceived: 0, timeSum: 0, n: 0 });
      bucket.cpu += p.cpu; bucket.ram += p.ram; bucket.disk += p.disk; bucket.NetworkBytesSent += p.NetworkBytesSent; bucket.NetworkBytesReceived += p.NetworkBytesReceived; bucket.timeSum += p.time.getTime(); bucket.n++;
    });
    return buckets.filter(Boolean).map(function (b) {
      return { cpu: b.cpu / b.n, ram: b.ram / b.n, disk: b.disk / b.n, NetworkBytesSent: b.NetworkBytesSent / b.n, NetworkBytesReceived: b.NetworkBytesReceived / b.n, time: new Date(b.timeSum / b.n) };
    });
  }

  // CPU/RAM/disk share one 0-100% axis. Network is bytes, not a percentage, so it gets its own chart with its own axis (see renderGraph).
  var PERCENT_SERIES = [
    { key: "cpu", label: "CPU", colorVar: "--nf-chart-cpu" },
    { key: "ram", label: "Hukommelse", colorVar: "--nf-chart-ram" },
    { key: "disk", label: "Disk", colorVar: "--nf-chart-disk" }
  ];
  var NETWORK_SERIES = [
    { key: "NetworkBytesSent", label: "Netværk (Sendt)", colorVar: "--nf-chart-network-sent" },
    { key: "NetworkBytesReceived", label: "Netværk (Modtaget)", colorVar: "--nf-chart-network-received" }
  ];

  // Resolves a design token (e.g. "--nf-text-muted") to its current computed color — Canvas, unlike SVG/CSS, can't resolve var() itself.
  function cssVar(name) { return getComputedStyle(document.documentElement).getPropertyValue(name).trim(); }

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
    document.getElementById("service-config-link").href = "server-config.php?id=" + encodeURIComponent(s.id);
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

  // Short byte label for inside a gauge ring (no decimals) so values like "35,5 KB" still fit on one line; the card text shows the exact value.
  function formatBytesRing(n) {
    var v = Math.abs(n) || 0;
    var units = ["B", "KB", "MB", "GB", "TB"];
    var i = 0;
    while (v >= 1024 && i < units.length - 1) { v /= 1024; i++; }
    return nf(v, 0) + " " + units[i];
  }

  function renderGauges(s) {
    var ram = ramPct(s), disk = diskUsedPct(s);
    var gauges = [
      { label: "CPU-belastning", value: nf(s.cpuUsage, 0) + "%", big: nf(s.cpuUsage, 1) + " %", hint: "", ring: ring(s.cpuUsage, s.cpuUsage > GAUGE_CPU_CRIT ? CRIT : s.cpuUsage > GAUGE_CPU_WARN ? WARN : BLUE) },
      { label: "Hukommelse", value: nf(ram, 0) + "%", big: nf(s.ramUsage, 1) + " GB", hint: "af " + nf(s.ramTotal, 0) + " GB i alt", ring: ring(ram, ram > THRESHOLDS.ramCrit ? CRIT : ram > THRESHOLDS.ramWarn ? WARN : BLUE) },
      { label: "Diskplads", value: nf(disk, 0) + "%", big: nf(s.diskRemainingSpace, 0) + " GB fri", hint: "af " + nf(s.diskTotalSpace, 0) + " GB i alt", ring: ring(disk, disk >= THRESHOLDS.diskCrit ? CRIT : disk >= THRESHOLDS.diskWarn ? WARN : BLUE) },
      { label: "Netværk (Sendt)", value: formatBytesRing(s.NetworkBytesSent), compact: true, big: formatBytes(s.NetworkBytesSent), hint: "", ring: ring(networkPct(s.NetworkBytesSent, THRESHOLDS.networkSentCrit), s.NetworkBytesSent >= THRESHOLDS.networkSentCrit ? CRIT : s.NetworkBytesSent >= THRESHOLDS.networkSentWarn ? WARN : BLUE) },
      { label: "Netværk (Modtaget)", value: formatBytesRing(s.NetworkBytesReceived), compact: true, big: formatBytes(s.NetworkBytesReceived), hint: "", ring: ring(networkPct(s.NetworkBytesReceived, THRESHOLDS.networkReceivedCrit), s.NetworkBytesReceived >= THRESHOLDS.networkReceivedCrit ? CRIT : s.NetworkBytesReceived >= THRESHOLDS.networkReceivedWarn ? WARN : BLUE) }
    ];
    document.getElementById("gauge-list").innerHTML = gauges.map(function (g) {
      return '<div class="gauge-card">' +
        '<div class="gauge-ring" style="background:' + g.ring + '">' +
        '<div class="gauge-ring__value' + (g.compact ? ' gauge-ring__value--compact' : '') + '">' + g.value + '</div>' +
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

  // ServerService only reports services that aren't Running, so every row here is an error. Stopped is critical, the in-between states a warning.
  var SERVICE_STATUS_LABELS = {
    Stopped: "Stoppet",
    Paused: "Sat på pause",
    StartPending: "Starter",
    StopPending: "Stopper",
    ContinuePending: "Genoptager",
    PausePending: "Sætter på pause"
  };

  var servicesLoading = false;

  // How much older a service's latest error may be than the server's latest reading and still count as current (see loadServices).
  var SERVICE_ERROR_MARGIN_MS = 30 * 1000;

  // Fetches the server's service-status history and keeps only the newest row per service name.
  function loadServices(s) {
    if (servicesLoading || !s.serviceId || !State.companyId) {
      if (!s.serviceId) renderServices([]);
      return;
    }
    servicesLoading = true;
    NWApi.get("/ServiceStatus/Get/" + State.companyId + "?serverId=" + encodeURIComponent(s.serviceId)).then(function (rows) {
      var latest = {};
      (rows || []).forEach(function (row) {
        if (!latest[row.name] || row.id > latest[row.name].id) latest[row.name] = row;
      });
      // An error row is posted in the same cycle as each hardware reading, so a service that's still failing has an error about as new as
      // the latest reading. One that has recovered stops getting rows, falls behind, and is dropped. If the server itself stops reporting,
      // the latest reading doesn't move either, so its last errors stay visible rather than looking resolved.
      var history = State.history[s.id] || [];
      var lastReading = history.length ? history[history.length - 1].createdAt : null;
      var list = Object.keys(latest).map(function (name) { return latest[name]; }).filter(function (row) {
        if (!lastReading) return true;
        return new Date(row.createdAt).getTime() >= lastReading.getTime() - SERVICE_ERROR_MARGIN_MS;
      });
      list.sort(function (a, b) { return b.id - a.id; });
      // The user may have navigated on while the request was in flight.
      if (F.getSelected() && F.getSelected().serviceId === s.serviceId) renderServices(list);
    }).catch(function () {
      renderServices([]);
    }).then(function () {
      servicesLoading = false;
    });
  }

  function renderServices(list) {
    document.getElementById("services-table-wrap").hidden = !list.length;
    document.getElementById("services-empty").hidden = !!list.length;
    document.getElementById("services-table-body").innerHTML = list.map(function (row) {
      var isStopped = row.status === "Stopped";
      return '<tr>' +
        '<td>' + esc(row.name) + '</td>' +
        '<td><span class="status-pill" style="color:' + (isStopped ? NW.colors.INK_CRIT : NW.colors.INK_WARN) + ';background:' + (isStopped ? NW.colors.TINT_CRIT : NW.colors.TINT_WARN) + '">' +
        esc(SERVICE_STATUS_LABELS[row.status] || row.status) + '</span></td>' +
        '<td class="is-right">' + esc(formatTimestamp(row.createdAt ? new Date(row.createdAt) : null)) + '</td>' +
        '</tr>';
    }).join("");
  }

  // Shows the charts or the table (never both), without touching their content, so toggling doesn't fight the poll re-render.
  function applyGraphView() {
    var isTable = graphView === "table";
    document.getElementById("graph-chart-wrap").hidden = isTable;
    document.getElementById("graph-chart-wrap-network").hidden = isTable;
    document.getElementById("graph-table-wrap").hidden = !isTable;
    document.getElementById("graph-table-toggle").textContent = isTable ? "Vis som graf" : "Vis som tabel";
  }

  // Top of the network chart's y-axis: the highest byte value either series reaches, with 15% headroom so the peak line isn't pinned to
  // the very top of the chart, and a 1KB floor so a near-empty graph doesn't get a 0-height axis.
  function networkScaleMax(points) {
    var max = 0;
    points.forEach(function (p) {
      NETWORK_SERIES.forEach(function (series) { if (p[series.key] > max) max = p[series.key]; });
    });
    return Math.max(1024, max * 1.15);
  }

  // The two Chart.js instances (percent + network), created once and updated in place on every render rather than torn down and rebuilt.
  var percentChart = null;
  var networkChart = null;

  function chartDatasets(series, points) {
    var lastIndex = points.length - 1;
    return series.map(function (s) {
      var color = cssVar(s.colorVar);
      return {
        label: s.label,
        colorVar: s.colorVar,
        data: points.map(function (p) { return { x: p.time.getTime(), y: p[s.key] }; }),
        borderColor: color,
        backgroundColor: color,
        borderWidth: 2,
        // Only the latest reading gets a dot — same "current value" marker the old SVG chart drew, not a mark on every point.
        pointRadius: function (ctx) { return ctx.dataIndex === lastIndex ? 4 : 0; },
        pointHoverRadius: function (ctx) { return ctx.dataIndex === lastIndex ? 4 : 0; },
        pointBorderColor: cssVar("--nf-base-100"),
        pointBorderWidth: 2,
        pointHitRadius: 10,
        tension: 0
      };
    });
  }

  // Draws a vertical hover line. Chart.js's tooltip only draws the value box, not a line, so this plugin adds it.
  // It has to be passed via the `plugins: []` array on the Chart itself (see renderChart) — putting it under `options.plugins` instead
  // would just be ignored, since that's only for configuring plugins Chart.js already knows about (legend, tooltip, ...).
  var crosshairPlugin = {
    id: "crosshair",
    afterDraw: function (chart) {
      var active = chart.tooltip && chart.tooltip._active;
      if (!active || !active.length) return;
      var x = active[0].element.x;
      var ctx = chart.ctx;
      ctx.save();
      ctx.beginPath();
      ctx.moveTo(x, chart.scales.y.top);
      ctx.lineTo(x, chart.scales.y.bottom);
      ctx.lineWidth = 1;
      ctx.strokeStyle = cssVar("--nf-border-color-hover");
      ctx.stroke();
      ctx.restore();
    }
  };

  // Builds a fresh Chart.js config, reading today's theme colors as it goes. Only runs when a chart is first created — after that,
  // the data-theme observer below (applyThemeColors) is what keeps an already-drawn chart's colors in sync with the theme.
  function chartOptions(scaleMax, formatTick, formatValue) {
    var tickColor = cssVar("--nf-text-muted");
    return {
      responsive: true,
      maintainAspectRatio: false,
      animation: false,
      interaction: { mode: "index", intersect: false },
      plugins: {
        legend: { position: "top", align: "start", labels: { color: tickColor, boxWidth: 12, boxHeight: 2, usePointStyle: false, font: { size: 12 } } },
        tooltip: {
          backgroundColor: cssVar("--nf-base-100"),
          borderColor: cssVar("--nf-divider"),
          borderWidth: 1,
          titleColor: cssVar("--nf-text-muted"),
          bodyColor: cssVar("--nf-text-heading"),
          padding: 8,
          callbacks: {
            title: function (items) { return items.length ? formatTimestamp(new Date(items[0].parsed.x)) : ""; },
            label: function (item) { return item.dataset.label + ": " + formatValue(item.parsed.y); }
          }
        }
      },
      scales: {
        // Points are plotted by their real timestamp (a "linear" x-axis of millisecond values), not just spaced out evenly by index.
        // Using "linear" here instead of Chart.js's "time" scale means we don't need to load a date-adapter library just for this.
        // The axis itself is hidden — same as the old chart, which had no x-axis and relied on hover (the crosshair + tooltip above)
        // to show a timestamp.
        x: {
          type: "linear",
          display: false
        },
        y: {
          min: 0, max: scaleMax,
          // Always exactly 3 grid lines — 0, half, and the max — matching the old chart, instead of Chart.js's own auto-picked ticks.
          afterBuildTicks: function (axis) { axis.ticks = [{ value: 0 }, { value: scaleMax / 2 }, { value: scaleMax }]; },
          grid: { color: cssVar("--nf-hairline") },
          border: { display: false },
          ticks: { color: tickColor, callback: formatTick }
        }
      }
    };
  }

  // Creates a chart on first call, otherwise swaps in new data/scale on the existing instance (cheaper, and keeps hover state stable).
  function renderChart(existingChart, canvasId, points, series, scaleMax, formatTick, formatValue, ariaLabel) {
    var canvas = document.getElementById(canvasId);
    canvas.setAttribute("role", "img");
    canvas.setAttribute("aria-label", ariaLabel);

    if (existingChart) {
      existingChart.data.datasets = chartDatasets(series, points);
      existingChart.options.scales.y.max = scaleMax;
      existingChart.update();
      return existingChart;
    }
    return new Chart(canvas.getContext("2d"), {
      type: "line",
      data: { datasets: chartDatasets(series, points) },
      options: chartOptions(scaleMax, formatTick, formatValue),
      plugins: [crosshairPlugin]
    });
  }

  // Runs whenever the theme toggle flips data-theme. A Canvas chart bakes in literal colors when it draws, unlike the old SVG version,
  // which just used var(...) and got new colors for free from CSS. So on a theme change we re-read every token used above and push the
  // new values into the already-drawn charts by hand.
  function applyThemeColors() {
    [percentChart, networkChart].forEach(function (chart) {
      if (!chart) return;
      var tickColor = cssVar("--nf-text-muted");
      chart.options.plugins.legend.labels.color = tickColor;
      chart.options.plugins.tooltip.backgroundColor = cssVar("--nf-base-100");
      chart.options.plugins.tooltip.borderColor = cssVar("--nf-divider");
      chart.options.plugins.tooltip.titleColor = tickColor;
      chart.options.plugins.tooltip.bodyColor = cssVar("--nf-text-heading");
      chart.options.scales.x.ticks.color = tickColor;
      chart.options.scales.y.ticks.color = tickColor;
      chart.options.scales.y.grid.color = cssVar("--nf-hairline");
      chart.data.datasets.forEach(function (ds) {
        var color = cssVar(ds.colorVar);
        ds.borderColor = color;
        ds.backgroundColor = color;
      });
      chart.update();
    });
  }
  new MutationObserver(applyThemeColors).observe(document.documentElement, { attributes: true, attributeFilter: ["data-theme"] });

  function renderGraph(s) {
    var history = filterHistoryByRange(State.history[s.id] || []);
    var toggleBtn = document.getElementById("graph-table-toggle");
    var emptyNote = document.getElementById("graph-empty");

    if (history.length < 2) {
      document.getElementById("graph-chart-wrap").hidden = true;
      document.getElementById("graph-chart-wrap-network").hidden = true;
      document.getElementById("graph-table-wrap").hidden = true;
      toggleBtn.hidden = true;
      emptyNote.hidden = false;
      return;
    }
    emptyNote.hidden = true;
    toggleBtn.hidden = false;
    applyGraphView();

    var points = buildGraphPoints(history);

    percentChart = renderChart(percentChart, "graph-chart", points, PERCENT_SERIES, 100,
      function (v) { return v + "%"; },
      function (v) { return nf(v, 0) + " %"; },
      "Udvikling for CPU, hukommelse og disk over de seneste målinger");

    networkChart = renderChart(networkChart, "graph-chart-network", points, NETWORK_SERIES, networkScaleMax(points),
      formatBytes,
      formatBytes,
      "Udvikling for netværkstrafik (sendt/modtaget) over de seneste målinger");

    document.getElementById("graph-table-body").innerHTML = points.map(function (p) {
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

  // If the id in the URL isn't in the roster (removed server, bad link), there's nothing to show — back to the grid.
  function render() {
    var sel = F.getSelected();
    if (!sel) {
      if (State.servers.length) window.location.href = "index.php";
      return;
    }
    renderSidebar();
    renderHostHeader(sel);
    renderAlert(sel);
    renderGauges(sel);
    renderDisk(sel);
    loadServices(sel);
    renderGraph(sel);
  }

  document.addEventListener("DOMContentLoaded", function () {
    document.getElementById("server-list").addEventListener("click", function (e) {
      var btn = e.target.closest("[data-id]");
      if (!btn) return;
      var id = btn.getAttribute("data-id");
      if (id === State.selected) return;
      window.location.href = "server-detail.php?id=" + encodeURIComponent(id);
    });
    document.getElementById("graph-table-toggle").addEventListener("click", function () {
      graphView = graphView === "table" ? "chart" : "table";
      applyGraphView();
    });

    var intervalSelect = document.getElementById("graph-interval");
    intervalSelect.innerHTML = DateOptions.map(function (r) {
      return '<option value="' + r.key + '">' + esc(r.label) + '</option>';
    }).join("");
    intervalSelect.value = dateRange;
    intervalSelect.addEventListener("change", function (e) {
      dateRange = e.target.value;
      var sel = F.getSelected();
      if (sel) renderGraph(sel);
    });

    F.start(render);
  });
})();
