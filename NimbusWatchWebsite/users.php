<?php

declare(strict_types=1);

$brandName = 'Nimbus Watch';
$pageTitle = 'Brugere';
$isDashboard = false;
// Users are loaded client-side from GET /api/company/users by
// assets/js/users.js. No fake/local data.
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
    <link rel="stylesheet" href="/assets/css/users.css">
</head>

<body>

    <div class="scroll-shell">
        <?php include __DIR__ . '/partials/header.php'; ?>

        <section id="user-list-view" class="grid-view">
            <div class="grid-view__head">
                <div>
                    <h1 class="grid-view__title">Brugere</h1>
                    <span id="user-count" class="grid-view__count"></span>
                </div>
                <a href="<?= htmlspecialchars(nw_url('user-create.php'), ENT_QUOTES, 'UTF-8') ?>" class="btn btn--primary-soft">
                    <i class="fa-solid fa-plus"></i>Opret bruger
                </a>
            </div>
            <p id="user-status" class="empty-note">Indlæser brugere…</p>
            <div class="card user-table-card">
                <table class="data-table">
                    <thead>
                        <tr>
                            <th class="is-left">Bruger</th>
                            <th class="is-left">Rolle</th>
                        </tr>
                    </thead>
                    <tbody id="user-tbody"></tbody>
                </table>
            </div>
        </section>
    </div>

    <script src="/assets/js/api.js"></script>
    <script src="/assets/js/common.js"></script>
    <script src="/assets/js/users.js"></script>
</body>

</html>
