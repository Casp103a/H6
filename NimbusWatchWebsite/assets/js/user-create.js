/* ==========================================================================
   Create-user form — POST /api/user/register takes email, password, roleId
   (picked from GET /api/role) and companyId (the logged-in user's own
   company, from GET /api/company) — no fullName/phone at creation time.
   On success, goes back to users.php.
   ========================================================================== */
(function () {
  "use strict";

  var esc = NW.esc;

  var State = { roles: [], companyId: null };

  function setCreateError(text) {
    var el = document.getElementById("create-user-error");
    if (text) { el.textContent = text; el.hidden = false; } else { el.hidden = true; }
  }

  function loadRoles() {
    return NWApi.get("/role").then(function (roles) {
      State.roles = roles || [];
      document.getElementById("create-user-role").innerHTML = State.roles.map(function (r) {
        return '<option value="' + r.id + '">' + esc(r.name) + '</option>';
      }).join("");
    });
  }

  // Cookie-scoped, no params — returns the logged-in user's own company.
  function loadCurrentCompany() {
    return NWApi.get("/company").then(function (company) {
      State.companyId = company && company.id;
    });
  }

  function submitCreateForm(e) {
    e.preventDefault();
    var form = e.target;
    var data = new FormData(form);
    var body = {
      email: data.get("email").trim(),
      password: data.get("password"),
      roleId: parseInt(data.get("roleId"), 10),
      companyId: State.companyId
    };
    setCreateError(null);
    return NWApi.post("/user/register", body).then(function () {
      window.location.href = NW.url("users.php");
    }).catch(function (err) {
      setCreateError(err.message);
    });
  }

  document.addEventListener("DOMContentLoaded", function () {
    Promise.all([loadCurrentCompany(), loadRoles()]).catch(function (err) {
      setCreateError("Kunne ikke hente roller: " + err.message);
    });
    document.getElementById("create-user-form").addEventListener("submit", submitCreateForm);
  });
})();
