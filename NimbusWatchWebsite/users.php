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
                <button type="button" id="create-user-btn" class="btn btn--primary-soft">
                    <i class="fa-solid fa-plus"></i>Opret bruger
                </button>
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

        <div id="user-detail-view" class="user-detail" hidden>
            <button type="button" id="back-to-users" class="back-link"><i class="fa-solid fa-arrow-left"></i>Alle brugere</button>

            <div class="user-identity">
                <div id="user-avatar" class="avatar user-identity__avatar"></div>
                <div>
                    <div class="user-identity__name-row">
                        <h1 id="user-name" class="host-header__title"></h1>
                        <span id="user-role-pill" class="status-pill"></span>
                    </div>
                    <div id="user-meta" class="host-header__meta"></div>
                </div>
            </div>

            <div class="card user-info-card">
                <div class="disk-stats user-info-grid">
                    <div>
                        <div class="disk-stat__label">Status</div>
                        <span id="user-status-pill" class="state-pill"></span>
                    </div>
                    <div>
                        <div class="disk-stat__label">Oprettet</div>
                        <div id="user-created" class="disk-stat__value"></div>
                    </div>
                </div>
            </div>
        </div>

        <div id="user-create-view" class="user-detail" hidden>
            <button type="button" id="back-to-users-from-create" class="back-link"><i class="fa-solid fa-arrow-left"></i>Alle brugere</button>
            <h1 class="host-header__title user-create-title">Opret bruger</h1>

            <form id="create-user-form" class="card user-form">
                <label class="field">
                    <span class="field__label">E-mail</span>
                    <input type="email" name="email" autocomplete="email" required>
                </label>
                <label class="field">
                    <span class="field__label">Adgangskode</span>
                    <input type="password" name="password" autocomplete="new-password" minlength="8" required>
                </label>
                <label class="field">
                    <span class="field__label">Rolle</span>
                    <select name="roleId" id="create-user-role" required></select>
                </label>
                <p id="create-user-error" class="form-error" hidden></p>
                <div class="user-form__actions">
                    <button type="button" id="cancel-create-user" class="btn">Annullér</button>
                    <button type="submit" class="btn btn--primary-soft">Opret bruger</button>
                </div>
            </form>
        </div>
    </div>

    <script src="/assets/js/api.js"></script>
    <script src="/assets/js/common.js"></script>
    <script src="/assets/js/users.js"></script>
</body>

</html>