<?php

declare(strict_types=1);

$brandName = 'Nimbus Watch';
$pageTitle = 'Server';
// Reached as server-detail.php?id={clientId} (see the fleet-card/server-row
// links in dashboard.js and server-detail.js). The id is read and validated
// client-side against the roster loaded by assets/js/dataprovider.js — there's
// no server-side lookup here, same as the rest of this app's data.
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
    <link rel="stylesheet" href="/assets/css/theme.css">
    <link rel="stylesheet" href="/assets/css/dashboard.css">
    <link rel="stylesheet" href="/assets/css/server-detail.css">
</head>

<body>

    <div class="scroll-shell">
        <?php include __DIR__ . '/partials/header.php'; ?>

        <div class="layout">

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
                        <a href="<?= htmlspecialchars(nw_url('index.php'), ENT_QUOTES, 'UTF-8') ?>" class="back-link"><i class="fa-solid fa-arrow-left"></i>Alle servere</a>
                        <div class="host-header__title-row">
                            <h1 id="host-name" class="host-header__title"></h1>
                            <span id="host-status-pill" class="status-pill"></span>
                        </div>
                        <div id="host-meta" class="host-header__meta"></div>
                    </div>
                    <div class="host-header__actions">
                        <a id="service-config-link" href="#" class="btn" hidden><i class="fa-solid fa-sliders"></i>Service-konfiguration</a>
                    </div>
                </div>

                <div id="alert-banner" class="alert-banner" hidden>
                    <i class="fa-solid fa-triangle-exclamation alert-banner__icon"></i>
                    <div class="alert-banner__body">
                        <div id="alert-title" class="alert-banner__title"></div>
                        <div id="alert-body" class="alert-banner__text"></div>
                    </div>
                </div>

                <div id="gauge-list" class="gauges"></div>

                <div class="card graph-card">
                    <div class="graph-card__head">
                        <div class="graph-card__title">Udvikling · seneste målinger</div>
                        <div class="graph-card__controls">
                            <label for="graph-interval" class="graph-card__controls-label">Vælg tidsinterval</label>
                            <select name="graph-interval" id="graph-interval" class="graph-interval-select" required></select>
                            <button type="button" id="graph-table-toggle" class="back-link" style="margin-bottom:0">Vis som tabel</button>
                        </div>
                    </div>
                    <div class="graph-charts-row">
                        <div id="graph-chart-wrap" class="graph-chart-wrap">
                            <canvas id="graph-chart" class="graph-chart"></canvas>
                        </div>
                        <div id="graph-chart-wrap-network" class="graph-chart-wrap">
                            <canvas id="graph-chart-network" class="graph-chart"></canvas>
                        </div>
                    </div>
                    <div id="graph-table-wrap" class="scroll-shell" hidden>
                        <table class="data-table">
                            <thead>
                                <tr>
                                    <th class="is-left">Tidspunkt</th>
                                    <th class="is-right">CPU</th>
                                    <th class="is-right">Hukommelse</th>
                                    <th class="is-right">Disk</th>
                                    <th class="is-right">Netværk Sent</th>
                                    <th class="is-right">Netværk Modtaget</th>
                                </tr>
                            </thead>
                            <tbody id="graph-table-body"></tbody>
                        </table>
                    </div>
                    <p id="graph-empty" class="empty-note" hidden>Ikke nok målinger endnu til at vise udvikling.</p>
                </div>

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

                <div class="card services-card">
                    <div class="disk-card__title">Services med fejl</div>
                    <div id="services-table-wrap" class="scroll-shell" hidden>
                        <table class="data-table">
                            <thead>
                                <tr>
                                    <th class="is-left">Service</th>
                                    <th class="is-left">Status</th>
                                    <th class="is-right">Sidst rapporteret</th>
                                </tr>
                            </thead>
                            <tbody id="services-table-body"></tbody>
                        </table>
                    </div>
                    <p id="services-empty" class="empty-note">Ingen services fejler lige nu.</p>
                </div>

            </main>
        </div>
    </div>

    <script src="/assets/js/api.js"></script>
    <script src="/assets/js/common.js"></script>
    <script src="https://cdnjs.cloudflare.com/ajax/libs/Chart.js/4.5.1/chart.umd.min.js"></script>
    <script src="/assets/js/dataprovider.js"></script>
    <script src="/assets/js/server-detail.js"></script>
</body>

</html>
