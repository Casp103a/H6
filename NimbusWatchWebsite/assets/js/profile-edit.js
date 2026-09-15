/* ==========================================================================
   Self-service profile — GET /api/user/GetCurrentUserInfo loads the caller's
   own email/role (cookie-scoped, works for any role), PUT /api/user/
   UpdateProfile saves email/password. Unlike users.php's admin edit form,
   there's no id in the URL (it's always "me") and the role field is
   read-only — the API's UpdateProfile action ignores any roleId sent to it,
   so this couldn't change your own role even if the field were editable.
   Password field is always blank on load; leaving it blank on save keeps
   the existing password, same reasoning as user-detail.js.
   ========================================================================== */
(function () {
  "use strict";

  var State = { id: null, roles: [] };

  function setStatus(text) {
    var statusEl = document.getElementById("profile-status");
    var formEl = document.getElementById("edit-profile-form");
    if (text) { statusEl.textContent = text; statusEl.hidden = false; formEl.hidden = true; }
    else { statusEl.hidden = true; formEl.hidden = false; }
  }

  function setFormError(text) {
    var el = document.getElementById("profile-error");
    if (text) { el.textContent = text; el.hidden = false; } else { el.hidden = true; }
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
      State.id = user.id;
      State.roleId = user.roleId;
      document.getElementById("profile-email").value = user.email;
      document.getElementById("profile-role").value = getRoleName(user.roleId);
      setStatus(null);
    }).catch(function (err) {
      setStatus("Kunne ikke hente profil: " + err.message);
    });
  }

  function submitEditForm(e) {
    e.preventDefault();
    var data = new FormData(e.target);
    var body = {
      id: State.id,
      email: data.get("email").trim(),
      roleId: State.roleId
    };
    var password = data.get("password");
    if (password) body.newPassword = password;

    setFormError(null);
    return NWApi.put("/user/UpdateUser/" + State.id, body).then(function () {
      window.location.href = NW.url("profile.php");
    }).catch(function (err) {
      setFormError(err.message);
    });
  }

  document.addEventListener("DOMContentLoaded", function () {
    loadProfile();
    document.getElementById("edit-profile-form").addEventListener("submit", submitEditForm);
  });
})();
