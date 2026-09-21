/* ==========================================================================
   Users list — loads real data from the API: GET /api/user/GetAllUsers
   requires a companyId query param and filters server-side, so the
   logged-in user's own company id is fetched first (GET /api/company,
   cookie-scoped — no params, always "your own company") and appended to
   the request. Clicking a row goes to user-detail.php?id=…; "Opret bruger"
   is a plain link to user-create.php (see users.php).
   ========================================================================== */
(function () {
  "use strict";

  var esc = NW.esc;

  var State = { users: [], roles: [], companyId: null };

  function setListStatus(text) {
    var el = document.getElementById("user-status");
    if (text) { el.textContent = text; el.hidden = false; } else { el.hidden = true; }
  }

  function getRoleName(roleId) {
    for (var i = 0; i < State.roles.length; i++) {
      if (State.roles[i].id === roleId) return State.roles[i].name;
    }
    return "—";
  }

  function renderList() {
    document.getElementById("user-count").textContent = State.users.length + " i alt";
    document.getElementById("user-tbody").innerHTML = State.users.map(function (u) {
      return '<tr class="user-row" tabindex="0" role="button" data-id="' + u.id + '" aria-label="' + esc(u.fullName) + '">' +
        '<td class="data-table__mono">' + esc(u.email) + '</td>' +
        '<td>' + esc(u.role) + '</td>' +
        '</tr>';
    }).join("");
  }

  function loadRoles() {
    return NWApi.get("/role").then(function (roles) {
      State.roles = roles || [];
    });
  }

  // Cookie-scoped, no params — returns the logged-in user's own company.
  function loadCurrentCompany() {
    return NWApi.get("/company").then(function (company) {
      State.companyId = company && company.id;
    });
  }

  function loadUsers() {
    setListStatus("Indlæser brugere…");
    return loadCurrentCompany().then(function () {
      return Promise.all([
        NWApi.get("/user/GetAllUsers?companyId=" + encodeURIComponent(State.companyId)),
        State.roles.length ? Promise.resolve() : loadRoles()
      ]);
    }).then(function (results) {
      var users = results[0] || [];
      State.users = users.map(function (u) {
        u.role = getRoleName(u.roleId);
        return u;
      });
      setListStatus(State.users.length ? null : "Ingen brugere endnu.");
      renderList();
    }).catch(function (err) {
      setListStatus("Kunne ikke hente brugere: " + err.message);
    });
  }

  function openUser(id) {
    window.location.href = NW.url("user-detail.php?id=" + id);
  }

  document.addEventListener("DOMContentLoaded", function () {
    var tbody = document.getElementById("user-tbody");
    tbody.addEventListener("click", function (e) {
      var row = e.target.closest("[data-id]");
      if (!row) return;
      openUser(parseInt(row.getAttribute("data-id"), 10));
    });
    tbody.addEventListener("keydown", function (e) {
      if (e.key !== "Enter" && e.key !== " ") return;
      var row = e.target.closest("[data-id]");
      if (!row) return;
      e.preventDefault();
      openUser(parseInt(row.getAttribute("data-id"), 10));
    });

    loadUsers();
  });

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
