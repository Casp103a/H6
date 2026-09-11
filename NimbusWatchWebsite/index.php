<?php

declare(strict_types=1);

$brandName = 'Nimbus Watch';
$pageTitle = 'Serverovervågning';
// Servers are loaded client-side from GET /api/company/servers by
// assets/js/dashboard.js. No fake/local data.
?>
<!DOCTYPE html>
<html lang="da">

<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=1440">
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

        <section id="grid-view" class="grid-view">
            <div class="grid-view__head">
                <div>
                    <h1 class="grid-view__title">Servere</h1>
                    <span id="fleet-count-grid" class="grid-view__count"></span>
                </div>
                <div id="fleet-tally-grid" class="fleet-tally fleet-tally--grid"></div>
            </div>
            <p id="fleet-status" class="empty-note">Indlæser servere…</p>
            <div id="fleet-grid" class="fleet-grid"></div>
        </section>

        <div id="detail-view" class="layout" hidden>

            <aside class="sidebar">
                <div class="sidebar__heading">
                    <span class="sidebar__heading-label">Servere</span>
                    <span id="fleet-count" class="sidebar__heading-count"></span>
                </div>
                <div id="fleet-tally" class="fleet-tally"></div>
                <div id="server-list" class="server-list"></div>
            </aside>

            <main class="main">

                <div class="host-header">
                    <div>
                        <button type="button" id="back-to-grid" class="back-link"><i class="fa-solid fa-arrow-left"></i>Alle servere</button>
                        <div class="host-header__title-row">
                            <h1 id="host-name" class="host-header__title"></h1>
                            <span id="host-status-pill" class="status-pill"></span>
                        </div>
                        <div id="host-meta" class="host-header__meta"></div>
                    </div>
                    <div class="host-header__actions">
                        <button type="button" class="btn">Eksportér rapport</button>
                        <button type="button" class="btn btn--primary-soft">Konfigurér grænser</button>
                    </div>
                </div>

                <div id="alert-banner" class="alert-banner" hidden>
                    <i class="fa-solid fa-triangle-exclamation alert-banner__icon"></i>
                    <div class="alert-banner__body">
                        <div id="alert-title" class="alert-banner__title"></div>
                        <div id="alert-body" class="alert-banner__text"></div>
                    </div>
                    <a href="#" class="alert-banner__link">Se hændelseslog</a>
                </div>

                <div id="gauge-list" class="gauges"></div>

                <div class="card disk-card">
                    <div class="disk-card__title">Diskplads</div>
                    <div class="volume">
                        <div class="volume__head">
                            <span class="volume__mount">Anvendt diskplads</span>
                            <span id="disk-free-text" class="volume__free"></span>
                        </div>
                        <div class="volume__track">
                            <div id="disk-fill" class="volume__fill"></div>
                        </div>
                        <div id="disk-note" class="volume__note"></div>
                    </div>
                </div>

            </main>
        </div>
    </div>

    <script src="/assets/js/api.js"></script>
    <script src="/assets/js/common.js"></script>
    <script src="/assets/js/dashboard.js"></script>
</body>

</html>