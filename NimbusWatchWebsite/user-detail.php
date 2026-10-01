<?php

declare(strict_types=1);

$brandName = 'Nimbus Watch';
$pageTitle = 'Bruger';
$isDashboard = false;
// The user is loaded and saved client-side by assets/js/user-detail.js,
// keyed off the ?id= query param (GET/PUT /api/user/{Get,Update}User/{id}).
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
    <link rel="stylesheet" href="/assets/css/theme.css">
    <link rel="stylesheet" href="/assets/css/dashboard.css">
    <link rel="stylesheet" href="/assets/css/users.css">
</head>

<body>

    <div class="scroll-shell">
        <?php include __DIR__ . '/partials/header.php'; ?>

        <div id="user-detail-view" class="user-detail">
            <a href="<?= htmlspecialchars(nw_url('users.php'), ENT_QUOTES, 'UTF-8') ?>" class="back-link"><i class="fa-solid fa-arrow-left"></i>Alle brugere</a>
            <h1 class="host-header__title user-create-title">Rediger bruger</h1>

            <p id="user-detail-status" class="empty-note"></p>

            <form id="edit-user-form" class="card user-form" hidden>
                <label class="field">
                    <span class="field__label">E-mail</span>
                    <input type="email" name="email" id="edit-user-email" autocomplete="email" required>
                </label>
                <label class="field">
                    <span class="field__label">Ny adgangskode</span>
                    <input type="password" name="password" id="edit-user-password" autocomplete="new-password" minlength="8" placeholder="Lad stå tomt for at beholde nuværende adgangskode">
                </label>
                <label class="field">
                    <span class="field__label">Rolle</span>
                    <select name="roleId" id="edit-user-role" required></select>
                    <span id="edit-user-role-note" class="field__label" hidden>Du kan ikke ændre din egen rolle.</span>
                </label>
                <p id="edit-user-error" class="form-error" hidden></p>
                <div class="user-form__actions">
                    <a href="<?= htmlspecialchars(nw_url('users.php'), ENT_QUOTES, 'UTF-8') ?>" class="btn">Annullér</a>
                    <button type="submit" class="btn btn--primary-soft">Gem</button>
                </div>
            </form>
        </div>
    </div>

    <script src="/assets/js/api.js"></script>
    <script src="/assets/js/common.js"></script>
    <script src="/assets/js/user-detail.js"></script>
</body>

</html>
