# Security Policy

## Supported Versions

Only the latest version on `master` is actively maintained.

## Reporting a Vulnerability

Please **do not** open a public GitHub issue for security vulnerabilities.

Report them privately via GitHub's [Security Advisories](https://github.com/TheCraftyMaker/ModelPublisher/security/advisories/new).

Include:
- A description of the vulnerability
- Steps to reproduce
- Potential impact

You can expect an acknowledgement within a few days. If confirmed, a fix will be prioritised and you'll be credited in the release notes.

## Scope

This is a local CLI tool — it runs entirely on your own machine and communicates only with the platforms you explicitly publish to (Printables, MakerWorld, etc.). It stores no credentials itself; authentication is handled via persistent browser profiles on your local filesystem.

The main areas of concern would be:
- Path traversal via a malicious `manifest.json`
- Arbitrary code execution via a crafted manifest
