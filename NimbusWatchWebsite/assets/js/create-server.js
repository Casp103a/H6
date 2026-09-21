/* ==========================================================================
   Create server — POST /api/ServerService/Add { name, ipAddress, companyId },
   matching CreateServerServiceDto. Registers a service-client identity for
   the monitoring agent that will run on that server. companyId comes from
   GET /api/company (cookie-scoped, no params — same "your own company" call
   used by login.js/users.js), fetched once when the page loads.

   Known backend gap: companyId is currently accepted by the DTO but never
   persisted (the serviceclients table has no CompanyId column), so a created
   server can't yet be scoped back to a company server-side. Sent anyway so
   this call doesn't need to change once that's fixed.

   On success the response body is the ready-to-use service config itself —
   raw bytes, Content-Type: application/octet-stream — rather than JSON, so
   this call goes through NWApi.postBlob instead of NWApi.post. It's
   downloaded as-is under the name ServerService's ConfigReader expects at
   %ProgramData%\NimbusWatch\Service_Config.config on the target machine,
   so it can be dropped in directly with no manual assembly or renaming.

   Admin-only page: index.php only shows the "Tilføj server" link to admins
   (see common.js's initCreateServerVisibility), but that just hides the
   entry point — a non-admin could still navigate here directly, so this
   redirects them straight back to the dashboard before the form (or the
   /company fetch) ever initializes. The API itself doesn't enforce this
   (ServerServiceController.Add has no [Authorize] at all yet), so this is
   the only gate there currently is.
   ========================================================================== */
(function () {
  "use strict";

  var CONFIG_FILE_NAME = "Service_Config.config";

  var State = { companyId: null };

  function downloadConfigFile(blob) {
    var url = URL.createObjectURL(blob);

    var link = document.createElement("a");
    link.href = url;
    link.download = CONFIG_FILE_NAME;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
  }

  function setError(text) {
    var el = document.getElementById("create-server-error");
    if (text) { el.textContent = text; el.hidden = false; } else { el.hidden = true; }
  }

  function loadCurrentCompany() {
    return NWApi.get("/company").then(function (company) {
      State.companyId = company && company.id;
    }).catch(function (err) {
      setError("Kunne ikke hente virksomhed: " + err.message);
    });
  }

  function goToDashboard() {
    window.location.href = "index.php";
  }

  function initPage() {
    var form = document.getElementById("create-server-form");

    document.getElementById("back-to-dashboard").addEventListener("click", goToDashboard);
    document.getElementById("cancel-create-server").addEventListener("click", goToDashboard);

    loadCurrentCompany();

    form.addEventListener("submit", function (e) {
      e.preventDefault();
      setError(null);

      var data = new FormData(form);
      var body = {
        name: data.get("server-name").trim(),
        ipAddress: data.get("server-ip").trim(),
        companyId: State.companyId
      };

      NWApi.postBlob("/ServerService/Add", body).then(function (configFile) {
        if (configFile) {
          downloadConfigFile(configFile);
        }
        goToDashboard();
      }).catch(function (err) {
        setError(err.message);
      });
    });
  }

  document.addEventListener("DOMContentLoaded", function () {
    NWApi.get("/user/GetCurrentUserInfo").then(function (user) {
      return NW.isAdmin(user);
    }).catch(function () {
      return false;
    }).then(function (admin) {
      if (admin) { initPage(); } else { goToDashboard(); }
    });
  });
})();
