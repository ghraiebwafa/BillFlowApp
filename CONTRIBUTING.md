# Contributing to BillFlow

Thank you for your interest in contributing to BillFlow. This project is open source and welcomes improvements from the community.

## Before you start

1. Read the [README](README.md) and run the app locally.
2. Check existing [issues](https://github.com/WafaGHraieb/BillFlowProject/issues) to avoid duplicate work.
3. For large changes, open an issue first to discuss the approach.

## Development setup

```bash
./scripts/setup-env.sh
docker compose -f docker-compose.yml -f docker-compose.local.yml up -d --build
```

See [Backend/README.md](Backend/README.md) and [Frontend/README.md](Frontend/README.md) for service-specific commands.

## Pull request workflow

1. Fork the repository and create a feature branch from `main`.
2. Make focused changes with clear commit messages.
3. Run tests before opening a PR:
   - `cd Backend && dotnet test`
   - `cd Frontend && npm run build`
4. Open a pull request using the PR template.
5. Address review feedback and keep the branch up to date with `main`.

## Code guidelines

- **Backend:** follow existing patterns in services, repositories, and `ProblemDetails` error responses.
- **Frontend:** match existing React + TypeScript conventions, i18n keys in `en.ts` and `fr.ts`, and design tokens in `global.css`.
- **Scope:** keep PRs small and reviewable. Prefer one feature or fix per PR.
- **Secrets:** never commit `.env`, credentials, or production URLs.

## Testing

- Add or update integration tests for API behavior changes.
- Add frontend validation where API contracts change.
- Manual smoke test: auth flow, invoice create, payment record, report download.

## Reporting bugs

Use the bug report issue template and include:

- Steps to reproduce
- Expected vs actual behavior
- Environment (OS, .NET version, browser)
- Relevant logs (redact secrets)

## Security

Do not open public issues for vulnerabilities. See [SECURITY.md](SECURITY.md).

## License

By contributing, you agree that your contributions will be licensed under the [MIT License](LICENSE).
