/* ==========================================================================
   Shared by index.php (the overview grid, see dashboard.js) and
   server-detail.php (the single-server view, see server-detail.js) — the
   server roster/reading history, the status/formatting helpers both pages
   need, and the header controls common to both (clock, live/pause, search,
   type filter).

   Loads data in two steps: GET /api/ServerService/GetAll/{companyId} for the
   fleet roster (clientId is the GUID used as this app's server id
   everywhere), then per server GET /api/CompanyHardwareData/
   GetHardwareData/{companyId}?clientId={clientId} for its reading history.
   The latest reading is the fleet/gauge snapshot; the rest feed the graph.

   Load after common.js and before dashboard.js/server-detail.js.
   ========================================================================== */
window.NWFleet = (function () {
  "use strict";

  // Shared palette + helpers, defined in common.js so every page draws status colors from one place.
  var OK = NW.colors.OK;
  var WARN = NW.colors.WARN;
  var CRIT = NW.colors.CRIT;
  var TINT_OK = NW.colors.TINT_OK;
  var TINT_WARN = NW.colors.TINT_WARN;
  var TINT_CRIT = NW.colors.TINT_CRIT;
  var INK_OK = NW.colors.INK_OK;
  var INK_WARN = NW.colors.INK_WARN;
  var INK_CRIT = NW.colors.INK_CRIT;
  var esc = NW.esc;
  var nf = NW.nf;

  // Drives status(), the alert banner, and the ram/disk gauge rings. CPU's ring uses its own, more sensitive pair (see server-detail.js) on purpose.
  var MB = 1024 * 1024;
  var THRESHOLDS = {
    cpuWarn: 85, cpuCrit: 95,
    ramWarn: 75, ramCrit: 90,
    diskWarn: 85, diskCrit: 95,
    networkSentWarn: 250 * MB, networkSentCrit: 1024 * MB,
    networkReceivedWarn: 250 * MB, networkReceivedCrit: 1024 * MB
  };

  var POLL_MS = 60000; // real network calls, not a client-side generator — no need for a 2.2s cadence

  var State = { servers: [], history: {}, selected: null, live: true, companyId: null, query: "", serverTypes: [], typeFilter: "" };

  var UNKNOWN_TYPE_LABEL = "Ukendt type";

  // Human-readable name for a serverTypeId, from the /ServerTypes list loaded once at startup.
  function typeName(serverTypeId) {
    var match = State.serverTypes.filter(function (t) { return t.id === serverTypeId; })[0];
    return match ? match.serverTypeName : UNKNOWN_TYPE_LABEL;
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

  // Forces n to stay within [lo, hi] — keeps a percentage from ever showing as e.g. 104% or -3%.
  function clamp(n, lo, hi) { return Math.max(lo, Math.min(hi, n)); }

  // RAM used as a percentage of total, guarded against a missing/zero ramTotal so it never divides by zero into NaN.
  function ramPct(s) { return s.ramTotal ? clamp((s.ramUsage / s.ramTotal) * 100, 0, 100) : 0; }

  // Disk used as a percentage of total (total minus remaining, over total), same divide-by-zero guard as ramPct.
  function diskUsedPct(s) {
    return s.diskTotalSpace ? clamp(((s.diskTotalSpace - s.diskRemainingSpace) / s.diskTotalSpace) * 100, 0, 100) : 0;
  }

  // A byte count in the largest unit that keeps it >= 1 (B/KB/MB/GB/TB) — a raw byte count is unreadable on its own.
  function formatBytes(n) {
    var v = Math.abs(n) || 0;
    var units = ["B", "KB", "MB", "GB", "TB"];
    var i = 0;
    while (v >= 1024 && i < units.length - 1) { v /= 1024; i++; }
    return nf(v, i === 0 ? 0 : 1) + " " + units[i];
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

  // Returns the currently selected server object from the State, or null if none is selected (or it's not in the roster).
  function getSelected() {
    for (var i = 0; i < State.servers.length; i++) {
      if (State.servers[i].id === State.selected) return State.servers[i];
    }
    return null;
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

  // Writes the OK/Advarsel/Kritisk tally into whichever of the two possible markups the current page has — the grid overview's
  // #fleet-tally-grid or the detail view's sidebar #fleet-tally — skipping whichever pair isn't on the page.
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

    [["fleet-tally", "fleet-count"], ["fleet-tally-grid", "fleet-count-grid"]].forEach(function (ids) {
      var tallyEl = document.getElementById(ids[0]);
      var countEl = document.getElementById(ids[1]);
      if (tallyEl) tallyEl.innerHTML = tallyHtml;
      if (countEl) countEl.textContent = countText;
    });
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

  // Combines a server's roster info with its reading history: the last reading is the current snapshot, the full history feeds the graph.
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

  // Set by start() to the page's own render function, so loadServers/refreshServerData can trigger a re-render without knowing what the page looks like.
  var onUpdate = function () {};

  async function loadServers() {
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
      renderClock();
      renderTally();
      onUpdate();
    } catch (err) {
      onUpdate(err);
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
      renderTally();
      onUpdate();
    });
  }

  function scheduleTick() {
    setInterval(function () {
      if (State.live) loadServers();
    }, POLL_MS);
  }

  // Boots the shared header controls (clock/live/pause, search, type filter) and the fleet roster/poll loop, then calls render
  // whenever the data changes — pass the page's own render(). Call once, from the page's DOMContentLoaded handler.
  function start(render) {
    onUpdate = render;

    document.getElementById("pause-btn").addEventListener("click", function () {
      State.live = !State.live;
      renderLiveStatus();
    });
    document.getElementById("server-search").addEventListener("input", function (e) {
      State.query = e.target.value;
      render();
    });
    document.getElementById("server-type-filter").addEventListener("change", function (e) {
      State.typeFilter = e.target.value;
      render();
    });

    renderLiveStatus();
    loadServerTypes();
    loadServers();
    scheduleTick();
  }

  return {
    State: State,
    THRESHOLDS: THRESHOLDS,
    typeName: typeName,
    visibleServers: visibleServers,
    clamp: clamp,
    ramPct: ramPct,
    diskUsedPct: diskUsedPct,
    formatBytes: formatBytes,
    status: status,
    getSelected: getSelected,
    refreshServerData: refreshServerData,
    start: start
  };
})();
