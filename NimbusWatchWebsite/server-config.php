<?php

declare(strict_types=1);

$brandName = 'Nimbus Watch';
$pageTitle = 'Service-konfiguration';
$isDashboard = false;
// Reached as server-config.php?id={clientId} (see the button in
// server-detail.php's host header). Same client-side id handling as
// server-detail.php — the id is read and resolved against the API in
// assets/js/server-config.js, there's no server-side lookup here.
?>
<!DOCTYPE html>
<html lang="da">

<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <script src="/assets/js/theme-init.js"></script>
    <title><?= htmlspecialchars($brandName . ' – ' . $pageTitle, ENT_QUOTES, 'UTF-8') ?></title>
    <link rel="icon" href="/assets/img/nimbus-mark.svg" type="image/svg+xml" media="(prefers-color-scheme: light)">
    <link rel="icon" href="/assets/img/nimbus-mark-on-dark.svg" type="image/svg+xml" media="(prefers-color-scheme: dark)">
    <link rel="preconnect" href="https://fonts.bunny.net">
    <link href="https://fonts.bunny.net/css?family=inter:400,500,600,700,800|jost:300,500" rel="stylesheet">
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.5.2/css/all.min.css">
    <link rel="stylesheet" href="/assets/css/tokens.css">
    <link rel="stylesheet" href="/assets/css/dashboard.css">
    <link rel="stylesheet" href="/assets/css/server-config.css">
</head>

<body>

    <div class="scroll-shell">
        <?php include __DIR__ . '/partials/header.php'; ?>

        <div id="server-config-view" class="server-config">
            <button type="button" id="back-to-detail" class="back-link"><i class="fa-solid fa-arrow-left"></i>Tilbage til server</button>
            <h1 class="host-header__title server-config__title">Service-konfiguration</h1>
            <p id="server-config-subtitle" class="server-config__subtitle"></p>

            <form id="server-config-form" class="card server-config__form">
                <label class="field">
                    <span class="field__label">Sendeinterval (sekunder)</span>
                    <input type="number" name="report-interval" id="report-interval" min="1" step="1" required>
                </label>
                <div class="field">
                    <span class="field__label">Services der skal overvåges</span>
                    <div class="watch-list" id="watched-services">
                        <div class="watch-list__input-row">
                            <input type="text" id="watched-service-input" placeholder="fx Website, Database">
                            <button type="button" id="watched-service-add" class="btn">Tilføj</button>
                        </div>
                        <ul class="watch-list__items" id="watched-service-items"></ul>
                    </div>
                </div>
                <p id="server-config-error" class="form-error" hidden></p>
                <div class="server-config__actions">
                    <button type="button" id="cancel-server-config" class="btn">Annullér</button>
                    <button type="submit" class="btn btn--primary-soft">Gem konfiguration</button>
                </div>
            </form>
        </div>
    </div>

    <script src="/assets/js/api.js"></script>
    <script src="/assets/js/common.js"></script>
    <script src="/assets/js/server-config.js"></script>
</body>

</html>
