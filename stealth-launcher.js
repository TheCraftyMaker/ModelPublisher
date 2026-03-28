'use strict';
// Launches Brave with playwright-extra stealth plugin and exposes a CDP endpoint.
// .NET connects to it via ConnectOverCDPAsync to run publisher code without bot detection.
//
// Usage: node stealth-launcher.js <cdpPort> [profilePath]
//   profilePath: e.g. "C:\Users\...\Brave-Browser\User Data\Profile 1"
//     If it ends in "Profile N" or "Default", splits into --user-data-dir + --profile-directory.

const { chromium } = require('playwright-extra');
const StealthPlugin = require('puppeteer-extra-plugin-stealth');
chromium.use(StealthPlugin());

const path = require('path');

const cdpPort = parseInt(process.argv[2] || '9222', 10);
const profilePath = process.argv[3] || null;

(async () => {
    const args = [
        `--remote-debugging-port=${cdpPort}`,
        '--disable-blink-features=AutomationControlled',
        '--no-first-run',
        '--no-default-browser-check',
    ];

    if (profilePath) {
        const profileName = path.basename(profilePath);
        const isNamedProfile = /^Profile \d+$/i.test(profileName) || profileName === 'Default';
        if (isNamedProfile) {
            args.push(`--user-data-dir=${path.dirname(profilePath)}`);
            args.push(`--profile-directory=${profileName}`);
        } else {
            args.push(`--user-data-dir=${profilePath}`);
        }
    }

    const browser = await chromium.launch({
        headless: false,
        executablePath: 'C:\\Program Files\\BraveSoftware\\Brave-Browser\\Application\\brave.exe',
        args,
    });

    // Signal ready — .NET already knows the port
    process.stdout.write(`READY:${cdpPort}\n`);

    // Exit when .NET disconnects
    browser.on('disconnected', () => process.exit(0));
})().catch(err => {
    process.stderr.write(`stealth-launcher error: ${err.message}\n`);
    process.exit(1);
});