/* ==========================================================================
   Shared across every page: the status color palette, small formatting
   helpers, and the header chrome (avatar menu, theme toggle) that's
   identical everywhere. Load before any page-specific script.
   ========================================================================== */
window.NW = (function () {
  "use strict";

  // No color VALUES live here, only CSS custom properties from tokens.css — change the look by editing tokens.css, not here.
  var colors = {
    OK: "var(--nf-success)",
    WARN: "var(--nf-warning)",
    CRIT: "var(--nf-error)",
    BLUE: "var(--nf-primary)",
    GRAY: "var(--nf-neutral-status)",
    TINT_OK: "var(--nf-success-tint)",
    TINT_WARN: "var(--nf-warning-tint)",
    TINT_CRIT: "var(--nf-error-tint)",
    INK_OK: "var(--nf-success-ink)",
    INK_WARN: "var(--nf-warning-ink)",
    INK_CRIT: "var(--nf-error-ink)"
  };

  // HTML-escapes a string so it's safe to drop into innerHTML — prevents both broken markup and script injection from API/user-supplied text.
  function esc(s) {
    return String(s).replace(/[&<>"']/g, function (c) {
      return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c];
    });
  }
  // Formats a number in Danish style (comma decimals, period thousands) with exactly d decimal places.
  function nf(n, d) {
    return n.toLocaleString("da-DK", { minimumFractionDigits: d, maximumFractionDigits: d });
  }
  // Turns a full name into up to two uppercase initials, for avatar circles (e.g. "Freja Sørensen" -> "FS").
  function initials(name) {
    return String(name).trim().split(/\s+/).map(function (p) { return p.charAt(0); }).join("").slice(0, 2).toUpperCase();
  }

  // Company-scoped pages live at /{companyName}/page.php — mirrors nw_url() in partials/header.php for JS-driven navigation.
  function url(page) {
    var segments = window.location.pathname.split("/").filter(function (s) { return s !== ""; });
    var companySlug = segments.length >= 2 ? segments[0] : null;
    return companySlug !== null ? "/" + companySlug + "/" + page : "/" + page;
  }

  function initLogout() {
    var link = document.querySelector('.avatar-menu__item[href="/login.php"]');
    if (!link) return;
    link.addEventListener("click", function (e) {
      e.preventDefault();
      NWApi.post("/Token/Logout").catch(function () { /* best-effort — navigate regardless */ })
        .then(function () { window.location.href = "/login.php"; });
    });
  }

  var THEME_KEY = "nimbuswatch-theme";

  function initThemeToggle() {
    var btn = document.getElementById("theme-toggle");
    if (!btn) return;
    var icon = document.getElementById("theme-toggle-icon");

    function apply(theme) {
      document.documentElement.setAttribute("data-theme", theme);
      icon.className = theme === "dark" ? "fa-solid fa-sun" : "fa-solid fa-moon";
      btn.setAttribute("aria-label", theme === "dark" ? "Skift til lyst tema" : "Skift til mørkt tema");
      try { localStorage.setItem(THEME_KEY, theme); } catch (e) { /* private mode etc. — theme just won't persist */ }
    }

    btn.addEventListener("click", function () {
      var current = document.documentElement.getAttribute("data-theme") === "dark" ? "dark" : "light";
      apply(current === "dark" ? "light" : "dark");
    });

    apply(document.documentElement.getAttribute("data-theme") === "dark" ? "dark" : "light");
  }

  // Resolves whether `user` holds the Admin role, so Admin-only UI can hide itself instead of erroring after a 403.
  function isAdmin(user) {
    if (!user) return Promise.resolve(false);
    return NWApi.get("/role").then(function (roles) {
      var role = (roles || []).find(function (r) { return r.id === user.roleId; });
      return !!role && role.name === "Admin";
    }).catch(function () { return false; });
  }

  // GetAllUsers is Admin-only on the API, so this nav link stays hidden unless the current user is an Admin.
  function initUsersNavVisibility(user) {
    var link = document.getElementById("users-nav-link");
    if (!link) return;
    isAdmin(user).then(function (admin) { link.hidden = !admin; });
  }

  // "Tilføj server" is Admin-only — same hidden-until-confirmed pattern as initUsersNavVisibility.
  function initCreateServerVisibility(user) {
    var link = document.getElementById("create-server-link");
    if (!link) return;
    isAdmin(user).then(function (admin) { link.hidden = !admin; });
  }

  // Populates the header's name label and avatar initials from the logged-in user's email.
  function initUserIdentity() {
    var nameLabel = document.getElementById("user-name-label");
    var avatarBtn = document.getElementById("avatar-trigger");

    return NWApi.get("/user/GetCurrentUserInfo").then(function (user) {
      var email = (user && user.email) || "";
      if (nameLabel) nameLabel.textContent = email.split("@")[0];
      if (avatarBtn) avatarBtn.textContent = email.slice(0, 2).toUpperCase();
      initUsersNavVisibility(user);
      initCreateServerVisibility(user);
    }).catch(function () { /* leave header blank if this fails */ });
  }

  function initAvatarMenu() {
    var trigger = document.getElementById("avatar-trigger");
    if (!trigger) return;
    var panel = document.getElementById("avatar-panel");

    function close() {
      panel.hidden = true;
      trigger.setAttribute("aria-expanded", "false");
    }
    function open() {
      panel.hidden = false;
      trigger.setAttribute("aria-expanded", "true");
    }

    trigger.addEventListener("click", function (e) {
      e.stopPropagation();
      if (panel.hidden) open(); else close();
    });
    document.addEventListener("click", function (e) {
      if (!panel.hidden && !panel.contains(e.target) && e.target !== trigger) close();
    });
    document.addEventListener("keydown", function (e) {
      if (e.key === "Escape" && !panel.hidden) { close(); trigger.focus(); }
    });
  }

  // Every page loading common.js is auth-required — bounce to login if there's no valid session.
  document.addEventListener("DOMContentLoaded", function () {
    NWApi.ensureSession().then(function (ok) {
      if (!ok) {
        window.location.href = "/login.php";
        return;
      }
      initAvatarMenu();
      initThemeToggle();
      initLogout();
      initUserIdentity();
    });
  });

  return { colors: colors, esc: esc, nf: nf, initials: initials, url: url, isAdmin: isAdmin };
})();
