<?php

/**
 * Shared app header. Include after setting (optionally):
 *   $isDashboard (bool, default true) — show the search box / live-status /
 *   pause button, which are specific to the server-monitoring page.
 */
$isDashboard = $isDashboard ?? true;

// Dashboard pages live at /{companyName}/page.php (a rewritten path, see /.htaccess) — the company name is just the URL's first segment.
$__reqPath = parse_url($_SERVER['REQUEST_URI'] ?? '', PHP_URL_PATH) ?? '';
$__segments = array_values(array_filter(explode('/', $__reqPath), fn($s) => $s !== ''));
$companySlug = count($__segments) >= 2 ? $__segments[0] : null;
function nw_url(string $page): string
{
    global $companySlug;
    return $companySlug !== null ? '/' . $companySlug . '/' . $page : '/' . $page;
}
?>
<header class="app-header">
    <a href="<?= htmlspecialchars(nw_url('index.php'), ENT_QUOTES, 'UTF-8') ?>" id="brand-home" class="app-header__brand app-header__brand--link nw-logo" title="Til oversigten" aria-label="Nimbus Watch – til oversigten">
        <img src="/assets/img/nimbus-mark-small.svg" alt="" width="40" height="40" class="nw-mark nw-mark--light">
        <img src="/assets/img/nimbus-mark-on-dark.svg" alt="" width="40" height="40" class="nw-mark nw-mark--dark">
        <span class="nw-word">
            <span class="nw-word__a">NIMBUS</span>
            <span class="nw-word__b">WATCH</span>
        </span>
    </a>
    <?php if ($isDashboard): ?>
        <div class="search-box">
            <i class="fa-solid fa-magnifying-glass"></i>
            <input type="search" id="server-search" class="search-box__input" placeholder="Søg efter server eller IP">
        </div>
        <div class="type-filter">
            <i class="fa-solid fa-filter"></i>
            <select id="server-type-filter" class="type-filter__select"></select>
        </div>
    <?php endif; ?>
    <div class="header-spacer"></div>
    <?php if ($isDashboard): ?>
        <div class="live-status">
            <span id="live-dot" class="live-status__dot"></span>
            <span class="live-status__detail">
                <span id="live-label">Live · opdaterer hvert 2. sekund</span>
                <span class="live-status__divider">·</span>
                <span>Sidst opdateret <span id="clock">--:--</span></span>
            </span>
        </div>
        <button type="button" id="pause-btn" class="btn">
            <i class="fa-solid fa-rotate"></i><span class="btn-label">Pause</span>
        </button>
    <?php endif; ?>
    <button type="button" id="theme-toggle" class="btn btn--icon" title="Skift tema" aria-label="Skift til mørkt tema">
        <i class="fa-solid fa-moon" id="theme-toggle-icon"></i>
    </button>
    <div class="avatar-menu">
        <span id="user-name-label" class="avatar-menu__name"></span>
        <button type="button" class="avatar avatar-menu__trigger" id="avatar-trigger" aria-haspopup="true" aria-expanded="false"></button>
        <div class="avatar-menu__panel" id="avatar-panel" role="menu" aria-labelledby="avatar-trigger" hidden>
            <a href="<?= htmlspecialchars(nw_url('profile.php'), ENT_QUOTES, 'UTF-8') ?>" class="avatar-menu__item" role="menuitem"><i class="fa-solid fa-user"></i>Profil</a>
            <a href="<?= htmlspecialchars(nw_url('users.php'), ENT_QUOTES, 'UTF-8') ?>" id="users-nav-link" class="avatar-menu__item" role="menuitem" hidden><i class="fa-solid fa-users"></i>Brugere</a>
            <div class="avatar-menu__divider"></div>
            <a href="/login.php" class="avatar-menu__item" role="menuitem"><i class="fa-solid fa-arrow-right-from-bracket"></i>Log ud</a>
        </div>
    </div>
</header>