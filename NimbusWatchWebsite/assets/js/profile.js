/* ==========================================================================
   Read-only profile view — GET /api/user/GetCurrentUserInfo (cookie-scoped,
   works for any role) loads the caller's own email/role. Editing lives on
   profile-edit.php/profile-edit.js (PUT /api/user/UpdateProfile); this page
   only displays.
   ========================================================================== */
(function () {
  "use strict";

  var State = { roles: [] };

  function setStatus(text) {
    var statusEl = document.getElementById("profile-status");
    var contentEl = document.getElementById("profile-content");
    if (text) { statusEl.textContent = text; statusEl.hidden = false; contentEl.hidden = true; }
    else { statusEl.hidden = true; contentEl.hidden = false; }
  }

  function getRoleName(roleId) {
    for (var i = 0; i < State.roles.length; i++) {
      if (State.roles[i].id === roleId) return State.roles[i].name;
    }
    return "—";
  }

  function loadRoles() {
    return NWApi.get("/role").then(function (roles) {
      State.roles = roles || [];
    });
  }

  function loadProfile() {
    setStatus("Indlæser profil…");
    return Promise.all([
      NWApi.get("/user/GetCurrentUserInfo"),
      loadRoles()
    ]).then(function (results) {
      var user = results[0];

      document.getElementById("profile-avatar").textContent = user.email.slice(0, 2).toUpperCase();
      document.getElementById("profile-email").textContent = user.email;

      var rolePill = document.getElementById("profile-role-pill");
      rolePill.textContent = getRoleName(user.roleId);
      rolePill.style.color = "var(--nf-primary)";
      rolePill.style.background = "var(--nf-primary-light)";

      setStatus(null);
    }).catch(function (err) {
      setStatus("Kunne ikke hente profil: " + err.message);
    });
  }

  document.addEventListener("DOMContentLoaded", loadProfile);
})();
