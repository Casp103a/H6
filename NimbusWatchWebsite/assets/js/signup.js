/* ==========================================================================
   Company + admin signup. Two real API calls, in sequence, because the
   company has to exist before we know the id its admin user needs:
     1. POST /api/company/create  { name }              -> { id, ... }
     2. POST /api/user/create     { email, password, roleId, companyId }
   Doesn't log the caller in; on success they're sent to the login page.
   Password-match is still checked client-side via the native constraint
   validation API.
   ========================================================================== */
(function () {
  "use strict";

  function setError(text) {
    var el = document.getElementById("signup-error");
    if (text) { el.textContent = text; el.hidden = false; } else { el.hidden = true; }
  }

  document.addEventListener("DOMContentLoaded", function () {
    var form = document.getElementById("signup-form");
    var password = form.querySelector('[name="password"]');
    var confirm = form.querySelector('[name="confirmPassword"]');

    function checkMatch() {
      confirm.setCustomValidity(confirm.value !== password.value ? "Adgangskoderne er ikke ens" : "");
    }
    password.addEventListener("input", checkMatch);
    confirm.addEventListener("input", checkMatch);

    form.addEventListener("submit", function (e) {
      e.preventDefault();
      checkMatch();
      if (!confirm.checkValidity()) {
        confirm.reportValidity();
        return;
      }
      setError(null);

      var data = new FormData(form);
      var companyName = data.get("companyName").trim();
      var email = data.get("email").trim();
      var password = data.get("password");

      // Not atomic: if user-create fails after this, the company exists with no admin. Needs a real transaction on the API side.
      return NWApi.get("/role").then(function (roles) {
        var adminRole = roles.find(function (role) { return role.name === "Admin"; });
        if (!adminRole) {
          throw new Error("Fandt ingen Admin-rolle — kan ikke oprette administratoren.");
        }

        return NWApi.post("/company/create", { companyName: companyName }).then(function (company) {
          if (!company || !company["id"]) {
            throw new Error("Virksomheden blev oprettet, men uden et id — kan ikke oprette administratoren.");
          }
          return NWApi.post("/user/register", {
            email: email,
            password: password,
            roleId: adminRole.id,
            companyId: company["id"]
          });
        });
      }).then(function () {
        window.location.href = "login.php";
      }).catch(function (err) {
        setError(err.message);
      });
    });
  });
})();
