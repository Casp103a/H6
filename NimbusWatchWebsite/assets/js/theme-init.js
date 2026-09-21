/* Applies the stored/system theme before first paint (avoids a flash of the
   wrong theme). Must load synchronously in <head>, before any stylesheet
   that reads [data-theme] — shared by index.php and login.php. */
(function () {
    try {
        var t = localStorage.getItem('nimbuswatch-theme');
        if (!t) t = (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) ? 'dark' : 'light';
        if (t === 'dark') document.documentElement.setAttribute('data-theme', 'dark');
    } catch (e) { }
})();
