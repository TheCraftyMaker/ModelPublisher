'use strict';
// Launches Brave with playwright-extra stealth plugin and exposes a CDP endpoint.
// .NET connects to it via ConnectOverCDPAsync to run publisher code without bot detection.
//
// Usage: node stealth-launcher.js <cdpPort> [profilePath]
//   profilePath: e.g. "C:\Users\...\Brave-Browser\User Data\Profile 1"
//     If it ends in "Profile N" or "Default", splits into userDataDir + --profile-directory.
//     Otherwise used directly as userDataDir (e.g. our own profiles/makerworld dir).

const { chromium } = require('playwright-extra');
const StealthPlugin = require('puppeteer-extra-plugin-stealth');
chromium.use(StealthPlugin());

const path = require('path');

const cdpPort = parseInt(process.argv[2] || '9222', 10);
const profilePath = process.argv[3] || null;

(async () => {
    const commonArgs = [
        `--remote-debugging-port=${cdpPort}`,
        '--disable-blink-features=AutomationControlled',
        '--no-first-run',
        '--no-default-browser-check',
    ];

    const launchOptions = {
        headless: false,
        executablePath: 'C:\\Program Files\\BraveSoftware\\Brave-Browser\\Application\\brave.exe',
        args: commonArgs,
    };

    let context;

    if (profilePath) {
        const profileName = path.basename(profilePath);
        const isNamedProfile = /^Profile \d+$/i.test(profileName) || profileName === 'Default';
        const userDataDir = isNamedProfile ? path.dirname(profilePath) : profilePath;

        if (isNamedProfile) {
            launchOptions.args = [...commonArgs, `--profile-directory=${profileName}`];
        }

        // Must use launchPersistentContext — playwright-extra blocks --user-data-dir as a launch arg
        context = await chromium.launchPersistentContext(userDataDir, launchOptions);
    } else {
        const browser = await chromium.launch(launchOptions);
        context = browser.contexts()[0] || await browser.newContext();
    }

    // Signal ready — .NET already knows the port
    process.stdout.write(`READY:${cdpPort}\n`);

    // Exit when the context/browser closes
    context.on('close', () => process.exit(0));
})().catch(err => {
    process.stderr.write(`stealth-launcher error: ${err.message}\n`);
    process.exit(1);
});
