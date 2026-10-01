/* ==========================================================================
   Self-service profile — GET /api/user/GetCurrentUserInfo loads the caller's
   own email/role (cookie-scoped, works for any role) and GET /api/
   UserProfile their name/phone. Saving sends PUT /api/user/UpdateUser for
   email/password and PUT /api/UserProfile/update for name/phone — each only
   when its own fields differ from what was loaded (or a new password was
   typed), so an unchanged section costs no request. Unlike
   users.php's admin edit form, there's no id in the URL (it's always "me")
   and the role field is read-only. A user without a profile yet (GET
   UserProfile → 404) is sent to profile-create.php instead, since update
   only works on an existing profile.
   Password field is always blank on load; leaving it blank on save keeps
   the existing password, same reasoning as user-detail.js.
   ========================================================================== */
(function () {
  "use strict";

  // original holds the values as loaded, so submit only calls the endpoint(s) whose fields actually changed.
  var State = { id: null, roles: [], original: null };

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

  // Resolves null when the user hasn't created a profile yet (404).
  function loadUserProfile() {
    return NWApi.get("/UserProfile").catch(function (err) {
      if (err.status === 404) return null;
      throw err;
    });
  }

  function loadProfile() {
    setStatus("Indlæser profil…");
    return Promise.all([
      NWApi.get("/user/GetCurrentUserInfo"),
      loadUserProfile(),
      loadRoles()
    ]).then(function (results) {
      var user = results[0];
      var profile = results[1];
      if (!profile) {
        window.location.replace(NW.url("profile-create.php"));
        return;
      }
      State.id = user.id;
      State.roleId = user.roleId;
      State.original = {
        email: user.email,
        fullName: profile.fullName || "",
        phoneNumber: profile.phoneNumber || ""
      };
      document.getElementById("profile-fullname").value = profile.fullName || "";
      document.getElementById("profile-phone").value = profile.phoneNumber || "";
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
    var email = data.get("email").trim();
    var password = data.get("password");
    var fullName = data.get("fullName").trim();
    var phoneNumber = data.get("phoneNumber").trim();

    var requests = [];

    if (email !== State.original.email || password) {
      var userBody = { id: State.id, email: email, roleId: State.roleId };
      if (password) userBody.newPassword = password;
      requests.push(NWApi.put("/user/UpdateUser/" + State.id, userBody));
    }

    if (fullName !== State.original.fullName || phoneNumber !== State.original.phoneNumber) {
      requests.push(NWApi.put("/UserProfile/update", { fullName: fullName, phoneNumber: phoneNumber }));
    }

    setFormError(null);
    return Promise.all(requests).then(function () {
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
