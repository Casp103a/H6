<?php

declare(strict_types=1);

$brandName = 'Nimbus Watch';
$pageTitle = 'Profil';
$isDashboard = false;
// Read-only view of the logged-in user's own info, loaded client-side by
// assets/js/profile.js via GET GetCurrentUserInfo + GET UserProfile. Editing
// happens on profile-edit.php, first-time creation on profile-create.php.
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
            <a href="<?= htmlspecialchars(nw_url('index.php'), ENT_QUOTES, 'UTF-8') ?>" class="back-link"><i class="fa-solid fa-arrow-left"></i>Til oversigten</a>

            <p id="profile-status" class="empty-note"></p>

            <div id="profile-content" hidden>
                <div class="user-identity">
                    <div id="profile-avatar" class="avatar user-identity__avatar"></div>
                    <div>
                        <div class="user-identity__name-row">
                            <h1 id="profile-email" class="host-header__title"></h1>
                            <span id="profile-role-pill" class="status-pill"></span>
                        </div>
                    </div>
                </div>

                <div id="profile-info" class="card user-info-card" hidden>
                    <dl class="profile-info-list">
                        <div>
                            <dt>Fulde navn</dt>
                            <dd id="profile-fullname"></dd>
                        </div>
                        <div>
                            <dt>Telefonnummer</dt>
                            <dd id="profile-phone"></dd>
                        </div>
                    </dl>
                </div>
                <p id="profile-missing" class="empty-note" hidden>Du har ikke udfyldt dine profiloplysninger endnu.</p>

                <!-- href/label are switched to profile-create.php/"Opret profil" by profile.js when the user has no profile yet. -->
                <a id="profile-action-btn" href="<?= htmlspecialchars(nw_url('profile-edit.php'), ENT_QUOTES, 'UTF-8') ?>" data-create-href="<?= htmlspecialchars(nw_url('profile-create.php'), ENT_QUOTES, 'UTF-8') ?>" class="btn btn--primary-soft" hidden>
                    <i class="fa-solid fa-pen"></i><span class="btn-label">Rediger profil</span>
                </a>
            </div>
        </div>
    </div>

    <script src="/assets/js/api.js"></script>
    <script src="/assets/js/common.js"></script>
    <script src="/assets/js/profile.js"></script>
</body>

</html>
