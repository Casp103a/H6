/* ==========================================================================
   First-time profile creation — POST /api/UserProfile/create saves the
   caller's own name/phone (cookie-scoped, no id needed). The API refuses a
   second create for a user who already has a profile, so profile.php only
   links here when GET /api/UserProfile answered 404; editing an existing
   profile lives on profile-edit.php.
   ========================================================================== */
(function () {
  "use strict";

  function setFormError(text) {
    var el = document.getElementById("profile-error");
    if (text) { el.textContent = text; el.hidden = false; } else { el.hidden = true; }
  }

  function submitCreateForm(e) {
    e.preventDefault();
    var data = new FormData(e.target);
    var body = {
      fullName: data.get("fullName").trim(),
      phoneNumber: data.get("phoneNumber").trim()
    };

    setFormError(null);
    return NWApi.post("/UserProfile/create", body).then(function () {
      window.location.href = NW.url("profile.php");
    }).catch(function (err) {
      setFormError(err.message);
    });
  }

  document.addEventListener("DOMContentLoaded", function () {
    document.getElementById("create-profile-form").addEventListener("submit", submitCreateForm);
  });
})();
