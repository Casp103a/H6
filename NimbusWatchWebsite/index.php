<?php

declare(strict_types=1);

$brandName = 'Nimbus Watch';
$pageTitle = 'Serverovervågning';
// Servers are loaded client-side from GET /api/CompanyHardwareData/GetAll/{companyId}
// by assets/js/fleet.js (shared roster/history + header) and rendered
// here by assets/js/dashboard.js. Clicking a server navigates to
// server-detail.php?id={clientId}, which renders its single-server view.
// No fake/local data.
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
</head>

<body>

    <div class="scroll-shell">
        <?php include __DIR__ . '/partials/header.php'; ?>

        <section class="grid-view">
            <div class="grid-view__head">
                <div>
                    <h1 class="grid-view__title">Servere</h1>
                    <span id="fleet-count-grid" class="grid-view__count"></span>
                    <a id="create-server-link" href="<?= htmlspecialchars(nw_url('create-server.php'), ENT_QUOTES, 'UTF-8') ?>" class="btn btn--primary-soft" hidden>Tilføj server</a>
                </div>
                <div id="fleet-tally-grid" class="fleet-tally fleet-tally--grid"></div>
            </div>
            <p id="fleet-status" class="empty-note">Indlæser servere…</p>
            <div id="fleet-grid" class="fleet-groups"></div>
        </section>
    </div>

    <script src="/assets/js/api.js"></script>
    <script src="/assets/js/common.js"></script>
    <script src="/assets/js/fleet.js"></script>
    <script src="/assets/js/dashboard.js"></script>
</body>

</html>
