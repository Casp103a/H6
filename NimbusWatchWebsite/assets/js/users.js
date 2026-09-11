/* ==========================================================================
   Users admin — loads real data from the API: GET /api/user/GetAllUsers
   returns every user across every company, filtered client-side down to the
   logged-in user's own company (its id comes from GET /api/company, which
   is cookie-scoped — no params, always "your own company"). POST
   /api/user/register adds a new one. A listed user record is whatever the
   API returns for GET (id, companyId, fullName, email, role, phoneNumber,
   isActive, createdAt) — there's no department field anywhere in the
   schema, so it's not shown. Creating one takes email, password, roleId
   (picked from GET /api/role) and companyId — no fullName/phone at
   creation time.
   ========================================================================== */
(function () {
  "use strict";

  var esc = NW.esc;
  var TINT_OK = NW.colors.TINT_OK, INK_OK = NW.colors.INK_OK;

  var State = { users: [], roles: [], view: "list", companyId: null };

  function setView(view) {
    State.view = view;
    document.getElementById("user-list-view").hidden = view !== "list";
    document.getElementById("user-detail-view").hidden = view !== "detail";
    document.getElementById("user-create-view").hidden = view !== "create";
  }

  function setListStatus(text) {
    var el = document.getElementById("user-status");
    if (text) { el.textContent = text; el.hidden = false; } else { el.hidden = true; }
  }

  function getUser(id) {
    for (var i = 0; i < State.users.length; i++) {
      if (State.users[i].id === id) return State.users[i];
    }
    return null;
  }

  function formatDate(iso) {
    var d = new Date(iso);
    if (isNaN(d.getTime())) return String(iso || "—");
    return String(d.getDate()).padStart(2, "0") + "." +
      String(d.getMonth() + 1).padStart(2, "0") + "." + d.getFullYear();
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

  function renderDetail(u) {
    document.getElementById("user-avatar").textContent = NW.initials(u.fullName);
    document.getElementById("user-name").textContent = u.fullName;

    var rolePill = document.getElementById("user-role-pill");
    rolePill.textContent = u.role;
    rolePill.style.color = "var(--nf-primary)";
    rolePill.style.background = "var(--nf-primary-light)";

    var meta = '<span><i class="fa-solid fa-envelope"></i>' + esc(u.email) + '</span>';
    if (u.phoneNumber) meta += '<span><i class="fa-solid fa-phone"></i>' + esc(u.phoneNumber) + '</span>';
    document.getElementById("user-meta").innerHTML = meta;

    var statusPill = document.getElementById("user-status-pill");
    statusPill.textContent = u.isActive ? "Aktiv" : "Inaktiv";
    statusPill.style.color = u.isActive ? INK_OK : "var(--nf-badge-neutral-fg)";
    statusPill.style.background = u.isActive ? TINT_OK : "var(--nf-badge-neutral-bg)";

    document.getElementById("user-created").textContent = formatDate(u.createdAt);
  }

  function openUser(id) {
    var u = getUser(id);
    if (!u) return;
    renderDetail(u);
    setView("detail");
  }

  function loadUsers() {
    setListStatus("Indlæser brugere…");
    return Promise.all([
      NWApi.get("/user/GetAllUsers"),
      loadCurrentCompany()
    ]).then(function (results) {
      var users = results[0] || [];
      State.users = users.filter(function (u) { return u.companyId === State.companyId; });
      setListStatus(State.users.length ? null : "Ingen brugere endnu.");
      renderList();
    }).catch(function (err) {
      setListStatus("Kunne ikke hente brugere: " + err.message);
    });
  }

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

  function openCreateForm() {
    document.getElementById("create-user-form").reset();
    setCreateError(null);
    setView("create");
    if (!State.roles.length) {
      loadRoles().catch(function (err) {
        setCreateError("Kunne ikke hente roller: " + err.message);
      });
    }
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
      setView("list");
      loadUsers();
    }).catch(function (err) {
      setCreateError(err.message);
    });
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

    document.getElementById("back-to-users").addEventListener("click", function () {
      setView("list");
    });
    document.getElementById("create-user-btn").addEventListener("click", openCreateForm);
    document.getElementById("back-to-users-from-create").addEventListener("click", function () {
      setView("list");
    });
    document.getElementById("cancel-create-user").addEventListener("click", function () {
      setView("list");
    });
    document.getElementById("create-user-form").addEventListener("submit", submitCreateForm);

    setView("list");
    loadUsers();
  });
})();
