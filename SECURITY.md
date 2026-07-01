# Security Policy

## Supported versions

| Version | Supported |
| ------- | --------- |
| `main`  | Yes       |

## Reporting a vulnerability

If you discover a security issue, please **do not** open a public GitHub issue.

Instead, report it privately to the repository maintainer via GitHub Security Advisories or direct email (add your contact in repository settings before publishing).

Include:

- Description of the vulnerability
- Steps to reproduce
- Impact assessment
- Suggested fix (if any)

We aim to acknowledge reports within **72 hours** and provide a remediation timeline when possible.

## Security practices in this project

- JWT access tokens with refresh rotation
- Password hashing via ASP.NET Core Identity hasher
- Rate limiting on auth and sensitive endpoints
- CORS validation in production
- Secrets loaded from environment variables (never committed)
- Content Security Policy on the frontend nginx image

## Responsible disclosure

We appreciate responsible disclosure and will credit reporters in release notes when fixes are published (unless you prefer to remain anonymous).
