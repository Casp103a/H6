/* ==========================================================================
   Login — POSTs to POST /api/login/login. Cookie-based session: on success
   the API has set the session cookie via Set-Cookie. Dashboard URLs are
   company-scoped (/{companyName}/index.php), so a successful login also
   fetches GET /api/company (cookie-scoped — returns the caller's own
   company) purely to get the name for that redirect.
   ========================================================================== */
(function () {
  "use strict";

  function setError(text) {
    var el = document.getElementById("login-error");
    if (text) { el.textContent = text; el.hidden = false; } else { el.hidden = true; }
  }

  document.addEventListener("DOMContentLoaded", function () {
    var form = document.getElementById("login-form");
    form.addEventListener("submit", function (e) {
      e.preventDefault();
      setError(null);
      var data = new FormData(form);
      return NWApi.post("/Login/login", {
        email: data.get("email").trim(),
        password: data.get("password")
      }).then(function () {
        return NWApi.get("/company");
      }).then(function (company) {
        window.location.href = "/" + encodeURIComponent(company.companyName) + "/index.php";
      }).catch(function (err) {
        setError(err.status === 401 ? "Forkert e-mail eller adgangskode." : err.message);
      });
    });
  });
})();
