/* ==========================================================================
   Edit a single user, keyed off the ?id= query param: GET /api/user/GetUser/
   {id} loads it, PUT /api/user/UpdateUser/{id} saves it. The password field
   is always shown blank — the API stores it encrypted, not as a hash, but
   there's no reason to round-trip a decrypted secret to the browser just to
   pre-fill a form. Leaving it blank on save keeps the existing password;
   typing one replaces it.
   ========================================================================== */
(function () {
  "use strict";

  var esc = NW.esc;
  var State = { id: null, roles: [] };

  function setStatus(text) {
    var statusEl = document.getElementById("user-detail-status");
    var formEl = document.getElementById("edit-user-form");
    if (text) { statusEl.textContent = text; statusEl.hidden = false; formEl.hidden = true; }
    else { statusEl.hidden = true; formEl.hidden = false; }
  }

  function setFormError(text) {
    var el = document.getElementById("edit-user-error");
    if (text) { el.textContent = text; el.hidden = false; } else { el.hidden = true; }
  }

  function loadRoles() {
    return NWApi.get("/role").then(function (roles) {
      State.roles = roles || [];
      document.getElementById("edit-user-role").innerHTML = State.roles.map(function (r) {
        return '<option value="' + r.id + '">' + esc(r.name) + '</option>';
      }).join("");
    });
  }

  function loadUser(id) {
    setStatus("Indlæser bruger…");
    return Promise.all([
      NWApi.get("/user/GetUser/" + id),
      loadRoles()
    ]).then(function (results) {
      var user = results[0];
      document.getElementById("edit-user-email").value = user.email;
      document.getElementById("edit-user-role").value = user.roleId;
      setStatus(null);
    }).catch(function (err) {
      setStatus(err.status === 404 ? "Brugeren blev ikke fundet." : "Kunne ikke hente bruger: " + err.message);
    });
  }

  function submitEditForm(e) {
    e.preventDefault();
    var data = new FormData(e.target);
    var body = {
      id: State.id,
      email: data.get("email").trim(),
      roleId: parseInt(data.get("roleId"), 10)
    };
    var password = data.get("password");
    if (password) body.newPassword = password;

    setFormError(null);
    return NWApi.put("/user/UpdateUser/" + State.id, body).then(function () {
      window.location.href = NW.url("users.php");
    }).catch(function (err) {
      setFormError(err.message);
    });
  }

  document.addEventListener("DOMContentLoaded", function () {
    State.id = parseInt(new URLSearchParams(window.location.search).get("id"), 10);
    if (isNaN(State.id)) {
      setStatus("Ingen bruger valgt.");
      return;
    }
    loadUser(State.id);
    document.getElementById("edit-user-form").addEventListener("submit", submitEditForm);
  });
})();
