<?php

declare(strict_types=1);

$brandName = 'Nimbus Watch';
$pageTitle = 'Opret server';
$isDashboard = false;

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
    <link rel="stylesheet" href="/assets/css/create-server.css">
</head>

<body>

    <div class="scroll-shell">
        <?php include __DIR__ . '/partials/header.php'; ?>

        <div id="create-server-view" class="create-server">
            <button type="button" id="back-to-dashboard" class="back-link"><i class="fa-solid fa-arrow-left"></i>Alle servere</button>
            <h1 class="host-header__title create-server__title">Opret server</h1>

            <form id="create-server-form" class="card create-server__form">
                <label class="field">
                    <span class="field__label">Servernavn</span>
                    <input type="text" name="server-name" id="server-name" required>
                </label>
                <label class="field">
                    <span class="field__label">IP-adresse</span>
                    <input type="text" name="server-ip" id="server-ip" placeholder="fx 10.0.0.12" required>
                </label>
                <label class="field">
                    <span class="field__label">Servertype</span>
                    <select name="server-type" id="server-type" required></select>
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
                <p id="create-server-error" class="form-error" hidden></p>
                <div class="create-server__actions">
                    <button type="button" id="cancel-create-server" class="btn">Annullér</button>
                    <button type="submit" class="btn btn--primary-soft">Opret server</button>
                </div>
            </form>
        </div>
    </div>

    <script src="/assets/js/api.js"></script>
    <script src="/assets/js/common.js"></script>
    <script src="/assets/js/create-server.js"></script>
</body>

</html>
