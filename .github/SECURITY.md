# Security Policy

## Reporting a Vulnerability

Do not open public issues for security vulnerabilities.

Report privately to: security@example.com

## Supported Versions

| Version | Supported |
|---------|-----------|
| Latest  | ✅ |

## Security Best Practices (for contributors)

- Never commit secrets, keys, or environment variable values
- Use `appsettings.*.local.json` for local secrets (gitignored)
- Review PRs for accidental secret exposure
- Run CodeQL analysis before merging security-sensitive changes
