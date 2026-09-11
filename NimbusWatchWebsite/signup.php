<?php

declare(strict_types=1);

$brandName = 'Nimbus Watch';
$pageTitle = 'Opret virksomhed';
// Submitted client-side by assets/js/signup.js: creates the company first
// (POST /api/company/create) to get its id, then creates the admin user
// with that id (POST /api/user/create).
?>
<!DOCTYPE html>
<html lang="da">

<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <script src="assets/js/theme-init.js"></script>
    <title><?= htmlspecialchars($brandName . ' – ' . $pageTitle, ENT_QUOTES, 'UTF-8') ?></title>
    <link rel="icon" href="assets/img/nimbus-mark.svg" type="image/svg+xml" media="(prefers-color-scheme: light)">
    <link rel="icon" href="assets/img/nimbus-mark-on-dark.svg" type="image/svg+xml" media="(prefers-color-scheme: dark)">
    <link rel="preconnect" href="https://fonts.bunny.net">
    <link href="https://fonts.bunny.net/css?family=inter:400,500,600,700,800|jost:300,500" rel="stylesheet">
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.5.2/css/all.min.css">
    <link rel="stylesheet" href="assets/css/tokens.css">
    <link rel="stylesheet" href="assets/css/dashboard.css">
    <link rel="stylesheet" href="assets/css/login.css">
</head>

<body class="auth-body">

    <div class="card auth-card auth-card--wide">
        <div class="nw-logo nw-logo--stacked">
            <img src="assets/img/nimbus-mark.svg" alt="" width="64" height="64" class="nw-mark nw-mark--light">
            <img src="assets/img/nimbus-mark-on-dark.svg" alt="" width="64" height="64" class="nw-mark nw-mark--dark">
            <span class="nw-word">
                <span class="nw-word__a">NIMBUS</span>
                <span class="nw-word__b">WATCH</span>
            </span>
        </div>
        <h1 class="auth-card__title">Opret virksomhed</h1>
        <p class="auth-card__subtitle">Opret jeres virksomhed og den første administrator-konto</p>

        <form id="signup-form" class="auth-form">
            <div class="form-section">
                <p class="form-section__title">Virksomhed</p>
                <label class="field">
                    <span class="field__label">Virksomhedsnavn</span>
                    <input type="text" name="companyName" autocomplete="organization" required>
                </label>
            </div>

            <div class="form-section">
                <p class="form-section__title">Administrator-konto</p>
                <label class="field">
                    <span class="field__label">E-mail</span>
                    <input type="email" name="email" autocomplete="email" required>
                </label>
                <label class="field">
                    <span class="field__label">Adgangskode</span>
                    <input type="password" name="password" autocomplete="new-password" minlength="8" required>
                </label>
                <label class="field">
                    <span class="field__label">Bekræft adgangskode</span>
                    <input type="password" name="confirmPassword" autocomplete="new-password" minlength="8" required>
                </label>
            </div>

            <p id="signup-error" class="form-error" hidden></p>
            <button type="submit" class="btn btn--primary-soft btn--block">Opret virksomhed</button>
        </form>

        <p class="auth-card__footnote">Har du allerede en konto? <a href="login.php">Log ind</a></p>
    </div>

    <script src="assets/js/api.js"></script>
    <script src="assets/js/signup.js"></script>
</body>

</html>