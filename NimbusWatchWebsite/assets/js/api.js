/* ==========================================================================
   Shared API client. Cookie-based session (credentials: "include" on every
   request, access token lives in the cookie itself — never touched from JS)
   — see each page's own script (login.js, signup.js, dashboard.js, users.js)
   for the specific endpoints it calls.

   A 401 from any call (other than the token endpoints and login itself)
   triggers one silent POST /token/RefreshToken before failing — that's the
   access-token cookie being renewed from the refresh token, same idea as
   this app's Angular AuthService. Concurrent 401s share one refresh call
   instead of firing one each. If refresh also fails, the original 401 is
   what callers see.
   ========================================================================== */
window.NWApi = (function () {
  "use strict";

  var BASE_URL = "https://nimbus-watch.com/api";
  var NO_REFRESH_PREFIXES = ["/token/", "/login/"];

  function rawRequest(method, path, body) {
    var opts = {
      method: method,
      credentials: "include",
      headers: {}
    };
    if (body !== undefined) {
      opts.headers["Content-Type"] = "application/json";
      opts.body = JSON.stringify(body);
    }
    return fetch(BASE_URL + path, opts).then(function (res) {
      if (res.status === 204) return { res: res, data: null };
      return res.json().catch(function () { return null; }).then(function (data) {
        return { res: res, data: data };
      });
    });
  }

  function toError(res, data) {
    var err = new Error((data && data.message) || res.statusText || ("HTTP " + res.status));
    err.status = res.status;
    err.data = data;
    return err;
  }

  // Cookie-based, so these carry no body — success/failure is just the
  // status code. Never throw: callers treat them as plain booleans.
  function validateToken() {
    return rawRequest("POST", "/token/ValidateToken").then(function (r) { return r.res.ok; }, function () { return false; });
  }
  function refreshToken() {
    return rawRequest("POST", "/token/RefreshToken").then(function (r) { return r.res.ok; }, function () { return false; });
  }

  // For a proactive check (e.g. before a page starts loading its own data)
  // rather than the reactive retry-on-401 below.
  function ensureSession() {
    return validateToken().then(function (valid) {
      return valid ? true : refreshToken();
    });
  }

  var refreshInFlight = null;
  function sharedRefresh() {
    if (!refreshInFlight) {
      refreshInFlight = refreshToken().then(function (ok) {
        refreshInFlight = null;
        return ok;
      });
    }
    return refreshInFlight;
  }

  function skipsRefresh(path) {
    return NO_REFRESH_PREFIXES.some(function (prefix) { return path.indexOf(prefix) === 0; });
  }

  function request(method, path, body, isRetry) {
    return rawRequest(method, path, body).then(function (r) {
      if (r.res.status === 204) return null;
      if (r.res.ok) return r.data;
      if (r.res.status === 401 && !isRetry && !skipsRefresh(path)) {
        return sharedRefresh().then(function (ok) {
          if (!ok) throw toError(r.res, r.data);
          return request(method, path, body, true);
        });
      }
      throw toError(r.res, r.data);
    });
  }

  return {
    get: function (path) { return request("GET", path); },
    post: function (path, body) { return request("POST", path, body); },
    validateToken: validateToken,
    refreshToken: refreshToken,
    ensureSession: ensureSession
  };
})();
