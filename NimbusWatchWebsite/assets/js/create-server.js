/* ==========================================================================
   Create server — POST /api/ServerService/Add { name, ipAddress, companyId },
   matching CreateServerServiceDto. Registers a service-client identity for
   the monitoring agent that will run on that server; response body is empty
   (just 200/500), nothing to read back. companyId comes from GET /api/company
   (cookie-scoped, no params — same "your own company" call used by
   login.js/users.js), fetched once when the page loads.

   Known backend gap: companyId is currently accepted by the DTO but never
   persisted (the serviceclients table has no CompanyId column), so a created
   server can't yet be scoped back to a company server-side. Sent anyway so
   this call doesn't need to change once that's fixed. On success, back to
   the dashboard so it can reload the fleet.
   ========================================================================== */
(function () {
  "use strict";

  var State = { companyId: null };

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

  document.addEventListener("DOMContentLoaded", function () {
    var form = document.getElementById("create-server-form");

    function goToDashboard() {
      window.location.href = "index.php";
    }

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

      NWApi.post("/ServerService/Add", body).then(function () {
        goToDashboard();
      }).catch(function (err) {
        setError(err.message);
      });
    });
  });
})();
