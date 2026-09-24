/* ==========================================================================
   Read-only profile view — GET /api/user/GetCurrentUserInfo (cookie-scoped,
   works for any role) loads the caller's own email/role, GET /api/
   UserProfile their name/phone. UserProfile answers 404 until the user has
   created one, so that's treated as "no profile" rather than an error, and
   the action button becomes "Opret profil" (profile-create.php) instead of
   "Rediger profil" (profile-edit.php). This page only displays.
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

  // Resolves null when the user hasn't created a profile yet (404).
  function loadUserProfile() {
    return NWApi.get("/UserProfile").catch(function (err) {
      if (err.status === 404) return null;
      throw err;
    });
  }

  function renderProfileInfo(profile) {
    var infoEl = document.getElementById("profile-info");
    var missingEl = document.getElementById("profile-missing");
    var btn = document.getElementById("profile-action-btn");

    if (profile) {
      document.getElementById("profile-fullname").textContent = profile.fullName || "—";
      document.getElementById("profile-phone").textContent = profile.phoneNumber || "—";
      infoEl.hidden = false;
      missingEl.hidden = true;
    } else {
      infoEl.hidden = true;
      missingEl.hidden = false;
      btn.href = btn.getAttribute("data-create-href");
      btn.querySelector("i").className = "fa-solid fa-plus";
      btn.querySelector(".btn-label").textContent = "Opret profil";
    }
    btn.hidden = false;
  }

  function loadProfile() {
    setStatus("Indlæser profil…");
    return Promise.all([
      NWApi.get("/user/GetCurrentUserInfo"),
      loadUserProfile(),
      loadRoles()
    ]).then(function (results) {
      var user = results[0];

      document.getElementById("profile-avatar").textContent = user.email.slice(0, 2).toUpperCase();
      document.getElementById("profile-email").textContent = user.email;

      var rolePill = document.getElementById("profile-role-pill");
      rolePill.textContent = getRoleName(user.roleId);
      rolePill.style.color = "var(--nf-primary)";
      rolePill.style.background = "var(--nf-primary-light)";

      renderProfileInfo(results[1]);
      setStatus(null);
    }).catch(function (err) {
      setStatus("Kunne ikke hente profil: " + err.message);
    });
  }

  document.addEventListener("DOMContentLoaded", loadProfile);
})();
