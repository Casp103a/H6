/* ==========================================================================
   Create-user form — POST /api/user/register (email, password, roleId,
   companyId). roleId comes from GET /api/role, companyId from
   GET /api/company. On success, goes back to users.php.

   Admin-only page: this is just a client-side redirect gate, same pattern
   as create-server.js — the API also enforces Admin-only on submit.
   ========================================================================== */
(function () {
  "use strict";

  var esc = NW.esc;

  var State = { roles: [], companyId: null };

  function setCreateError(text) {
    var el = document.getElementById("create-user-error");
    if (text) { el.textContent = text; el.hidden = false; } else { el.hidden = true; }
  }

  // Service is the monitoring-agent role (see ServerService/ServiceAuth) —
  // not something a human account created here should ever be given.
  function loadRoles() {
    return NWApi.get("/role").then(function (roles) {
      State.roles = (roles || []).filter(function (r) { return r.name !== "Service"; });
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

  function initPage() {
    Promise.all([loadCurrentCompany(), loadRoles()]).catch(function (err) {
      setCreateError("Kunne ikke hente roller: " + err.message);
    });
    document.getElementById("create-user-form").addEventListener("submit", submitCreateForm);
  }

  document.addEventListener("DOMContentLoaded", function () {
    NWApi.get("/user/GetCurrentUserInfo").then(function (user) {
      return NW.isAdmin(user);
    }).catch(function () {
      return false;
    }).then(function (admin) {
      if (admin) { initPage(); } else { window.location.href = NW.url("index.php"); }
    });
  });
})();
