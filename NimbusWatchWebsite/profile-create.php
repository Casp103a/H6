<?php

declare(strict_types=1);

$brandName = 'Nimbus Watch';
$pageTitle = 'Opret profil';
$isDashboard = false;
// First-time creation of the caller's own profile info (name/phone), saved client-side by assets/js/profile-create.js via POST UserProfile/create.
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

        <div id="profile-view" class="user-detail">
            <a href="<?= htmlspecialchars(nw_url('profile.php'), ENT_QUOTES, 'UTF-8') ?>" class="back-link"><i class="fa-solid fa-arrow-left"></i>Min profil</a>
            <h1 class="host-header__title user-create-title">Opret profil</h1>

            <form id="create-profile-form" class="card user-form">
                <label class="field">
                    <span class="field__label">Fulde navn</span>
                    <input type="text" name="fullName" autocomplete="name" required>
                </label>
                <label class="field">
                    <span class="field__label">Telefonnummer</span>
                    <input type="tel" name="phoneNumber" autocomplete="tel" required>
                </label>
                <p id="profile-error" class="form-error" hidden></p>
                <div class="user-form__actions">
                    <a href="<?= htmlspecialchars(nw_url('profile.php'), ENT_QUOTES, 'UTF-8') ?>" class="btn">Annullér</a>
                    <button type="submit" class="btn btn--primary-soft">Opret</button>
                </div>
            </form>
        </div>
    </div>

    <script src="/assets/js/api.js"></script>
    <script src="/assets/js/common.js"></script>
    <script src="/assets/js/profile-create.js"></script>
</body>

</html>
