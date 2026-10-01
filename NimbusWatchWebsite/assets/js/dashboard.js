/* ==========================================================================
   Fleet overview grid (index.php). The roster/reading history, status
   helpers, and header controls (clock, live/pause, search, type filter) live
   in dataprovider.js, shared with the single-server view in server-detail.php
   — this file only renders the grid and reacts to clicking a server, which
   navigates to server-detail.php?id={clientId}.
   ========================================================================== */
(function () {
  "use strict";

  var F = window.NWFleet;
  var State = F.State;
  var status = F.status;
  var ramPct = F.ramPct;
  var diskUsedPct = F.diskUsedPct;
  var formatBytes = F.formatBytes;
  var visibleServers = F.visibleServers;
  var typeName = F.typeName;

  var esc = NW.esc;
  var nf = NW.nf;

  var UNKNOWN_TYPE_LABEL = "Ukendt type";

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

  function render(err) {
    if (err) { setFleetStatus("Kunne ikke hente servere: " + err.message); return; }
    renderFleetStatus();
    renderFleetGrid();
  }

  document.addEventListener("DOMContentLoaded", function () {
    document.getElementById("fleet-grid").addEventListener("click", function (e) {
      var btn = e.target.closest("[data-id]");
      if (!btn) return;
      window.location.href = "server-detail.php?id=" + encodeURIComponent(btn.getAttribute("data-id"));
    });

    F.start(render);
  });
})();
