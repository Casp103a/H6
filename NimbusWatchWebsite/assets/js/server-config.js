/* ==========================================================================
   Service-configuration form (server-config.php?id={clientId}): lets an
   admin edit an existing server's report interval and watched services.
   Reading the current config is a two-step async trip through
   ConfigurationHub/ConfigurationController: POST /Configuration/request/
   {clientId} asks the agent for it, and the hub later pushes
   "ReceiveConfigurationResponse" with the answer once the agent replies.
   Saving is a plain POST — see its handler for a caveat about that.
   ========================================================================== */
(function () {
  "use strict";

  var HUB_URL = "https://nimbus-watch.com/hubs/configuration";

  var State = { companyId: null, clientId: null, watchedServices: [] };
  var DEFAULT_REPORT_INTERVAL_SECONDS = 60; // matches ServerServiceController's ReportFrequencyInMilliSec default

  function renderWatchedServices() {
    var list = document.getElementById("watched-service-items");
    list.innerHTML = "";

    State.watchedServices.forEach(function (name, index) {
      var item = document.createElement("li");
      item.className = "watch-list__item";
      item.textContent = name;

      var remove = document.createElement("button");
      remove.type = "button";
      remove.className = "watch-list__item-remove";
      remove.setAttribute("aria-label", "Fjern " + name);
      remove.textContent = "×";
      remove.addEventListener("click", function () {
        State.watchedServices.splice(index, 1);
        renderWatchedServices();
      });

      item.appendChild(remove);
      list.appendChild(item);
    });
  }

  function addWatchedService() {
    var input = document.getElementById("watched-service-input");
    var name = input.value.trim();
    if (!name || State.watchedServices.indexOf(name) !== -1) return;

    State.watchedServices.push(name);
    input.value = "";
    renderWatchedServices();
    input.focus();
  }

  function setError(text) {
    var el = document.getElementById("server-config-error");
    if (text) { el.textContent = text; el.hidden = false; } else { el.hidden = true; }
  }

  function goToServerDetail() {
    window.location.href = "server-detail.php?id=" + encodeURIComponent(State.clientId);
  }

  function loadCurrentCompany() {
    return NWApi.get("/company").then(function (company) {
      State.companyId = company && company.id;
    }).catch(function (err) {
      setError("Kunne ikke hente virksomhed: " + err.message);
    });
  }

  function loadServerInfo() {
    return NWApi.get("/ServerService/GetServerInfo/" + State.companyId + "?clientId=" + encodeURIComponent(State.clientId)).then(function (info) {
      document.getElementById("server-config-subtitle").textContent = info ? info.serverName + " · " + info.ipAddress : "";
    }).catch(function (err) {
      setError("Kunne ikke hente serveren: " + err.message);
    });
  }

  // Fills the form from the agent's live config. ConfigurationDto comes through with its raw C# (PascalCase)
  // property names — SignalR's default JSON hub protocol doesn't apply the camelCase policy our REST responses use.
  function applyConfig(configuration) {
    if (!configuration) return;
    if (configuration.ReportFrequency) {
      document.getElementById("report-interval").value = Math.round(configuration.ReportFrequency / 1000);
    }
    State.watchedServices = (configuration.MonitoredServices || []).map(function (s) { return s.Name; });
    renderWatchedServices();
  }

  // Connects to the ConfigurationHub and asks the agent (via the API) for its current config.
  // Never rejects — a failure just leaves the form at its blank defaults, editable from scratch.
  function loadLiveConfig() {
    var connection = new signalR.HubConnectionBuilder()
      .withUrl(HUB_URL)
      .withAutomaticReconnect()
      .build();

    // ReceiveConfigurationResponse is (requestId, configuration) — requestId only correlates the API's
    // internal PendingConfigurationRequestStore entry server-side; this page has at most one request in
    // flight at a time, so the configuration itself is all that matters here.
    connection.on("ReceiveConfigurationResponse", function (requestId, configuration) {
      console.log("ReceiveConfigurationResponse:", requestId, JSON.stringify(configuration)); // TEMP: remove once MonitoredServices' actual shape is confirmed
      applyConfig(configuration);
    });

    return connection.start().then(function () {
      return NWApi.post("/Configuration/request/" + encodeURIComponent(State.clientId));
    }).catch(function (err) {
      setError("Kunne ikke hente nuværende konfiguration: " + err.message);
    });
  }

  function initPage() {
    var form = document.getElementById("server-config-form");

    document.getElementById("report-interval").value = DEFAULT_REPORT_INTERVAL_SECONDS;

    document.getElementById("back-to-detail").addEventListener("click", goToServerDetail);
    document.getElementById("cancel-server-config").addEventListener("click", goToServerDetail);

    document.getElementById("watched-service-add").addEventListener("click", addWatchedService);
    document.getElementById("watched-service-input").addEventListener("keydown", function (e) {
      if (e.key !== "Enter") return;
      e.preventDefault();
      addWatchedService();
    });

    loadServerInfo();
    loadLiveConfig();

    form.addEventListener("submit", function (e) {
      e.preventDefault();
      setError(null);

      var data = new FormData(form);
      var body = {
        reportFrequency: parseInt(data.get("report-interval"), 10) * 1000,
        monitoredServices: State.watchedServices.map(function (name) {
          return { name: name };
        })
      };

      // Update/{clientId} only broadcasts ReceiveConfigurationUpdate to that service's SignalR group — there's
      // no persistence and no delivery confirmation, so a 200 here means "broadcast sent", not "agent applied
      // it": if that server's agent isn't connected to the hub right now, this silently reaches no one.
      NWApi.post("/Configuration/update/" + encodeURIComponent(State.clientId), body).then(function () {
        goToServerDetail();
      }).catch(function (err) {
        setError(err.message);
      });
    });
  }

  document.addEventListener("DOMContentLoaded", function () {
    State.clientId = new URLSearchParams(window.location.search).get("id");
    if (!State.clientId) {
      window.location.href = "index.php";
      return;
    }

    NWApi.get("/user/GetCurrentUserInfo").then(function (user) {
      return NW.isAdmin(user);
    }).catch(function () {
      return false;
    }).then(function (admin) {
      if (!admin) { goToServerDetail(); return; }
      loadCurrentCompany().then(initPage);
    });
  });
})();
