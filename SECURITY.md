# Security Policy

## Overview

This document outlines the security practices and vulnerability reporting procedures for the MP (Marketplace Platform) project.

## Security Scanning

### Automated Security Scanning (MP-19)

The project has automated security scanning enabled across multiple layers:

#### 1. **NPM Audit (Frontend)**
- **Frequency**: Every push to main/develop + daily at 2 AM UTC
- **Tools**: `npm audit`
- **Threshold**: Moderate and higher vulnerabilities reported
- **Reports**: Uploaded as GitHub Actions artifacts
- **Location**: `.github/workflows/security-scan.yml`

**Manual execution:**
```bash
cd angular
npm audit
npm audit fix  # For automatic fixes
```

#### 2. **.NET Security Analysis (Backend)**
- **Frequency**: Every push to main/develop + daily at 2 AM UTC
- **Tools**: .NET compiler warnings, static analysis
- **Threshold**: All warnings treated as errors in Release builds
- **Location**: `.github/workflows/security-scan.yml`

**Manual execution:**
```bash
dotnet build MP.sln --configuration Release
```

#### 3. **Dependency Vulnerability Check**
- **Frequency**: Every push to main/develop + daily at 2 AM UTC
- **Tools**: OWASP Dependency-Check
- **Features**: Enables experimental checks and retired dependency detection
- **Reports**: JSON format uploaded as artifacts
- **Location**: `.github/workflows/security-scan.yml`

#### 4. **Automated Dependency Updates (Dependabot)**
- **NPM Dependencies**: Weekly updates (Monday 3 AM UTC)
- **.NET NuGet**: Weekly updates (Monday 3 AM UTC)
- **GitHub Actions**: Weekly updates (Monday 3 AM UTC)
- **Behavior**: Creates pull requests for dependency updates
- **Configuration**: `.github/dependabot.yml`

**Features:**
- Limits to 5 open PRs for npm, 5 for NuGet, 3 for actions
- All updates reviewed and labeled as `dependencies` and `security`
- Automatic assignment to project maintainers

## Vulnerability Disclosure

### Reporting Security Issues

**Do not create public GitHub issues for security vulnerabilities.**

Instead, please email security issues to: **[security@marketplace.example.com]**

Include:
- Description of the vulnerability
- Affected component/version
- Steps to reproduce (if applicable)
- Potential impact
- Suggested fix (if any)

### Response Process

1. **Acknowledgment**: We acknowledge receipt within 24 hours
2. **Assessment**: We assess severity and impact within 48 hours
3. **Timeline**: Critical vulnerabilities fixed within 7 days
4. **Notification**: Users notified when fix is released
5. **Credit**: Researchers credited in release notes (unless requested otherwise)

## Security Best Practices

### Code Review Guidelines

All code changes must:
- Pass automated security scanning
- Be reviewed by at least 1 maintainer
- Have no unresolved security warnings
- Include tests for security-related changes

### Dependency Management

- Update dependencies weekly via Dependabot
- Review and merge dependency PRs promptly
- Never ignore major security updates
- Pin critical dependencies to specific versions when needed

### Secret Management

- Never commit secrets (API keys, passwords, tokens)
- Use environment variables for secrets
- Rotate secrets regularly
- Use GitHub Secrets for CI/CD pipelines

### Security Headers

The application implements:
- **CSP (Content Security Policy)**: Prevents XSS attacks
- **X-Content-Type-Options**: Prevents MIME type sniffing
- **X-XSS-Protection**: Additional XSS protection
- **Referrer-Policy**: Controls referrer information

See `src/MP.HttpApi.Host/MPHttpApiHostModule.cs` for implementation.

### Authentication & Authorization

- OAuth 2.0 with OpenID Connect (Volo.Abp)
- Role-based access control (RBAC)
- Permission-based authorization
- Multi-tenant isolation

### Data Protection

- HTTPS/TLS for all communications
- Encrypted sensitive data at rest
- PII handling per GDPR/privacy policy
- Regular security audits

## Supported Versions

| Version | Status | Support Ends |
|---------|--------|--------------|
| 9.x | Current | TBD |
| 8.x | Maintenance | TBD |
| < 8.0 | Unsupported | N/A |

## Security Incidents

### Incident Response

1. **Identification**: Security issue detected via scanning or report
2. **Isolation**: Affected component isolated if necessary
3. **Analysis**: Root cause analysis performed
4. **Fix**: Security patch developed and tested
5. **Release**: Fix released as hotfix or security patch
6. **Communication**: Users notified of issue and fix
7. **Post-Incident**: Review and process improvement

### Communication

Security incident communications include:
- CVE identifiers (if applicable)
- Affected versions
- Severity assessment
- Recommended actions
- Patch availability and installation instructions

## Tools & Services

### Integrated Security Tools

| Tool | Purpose | Config |
|------|---------|--------|
| npm audit | NPM vulnerability scanning | `.github/workflows/security-scan.yml` |
| Dependabot | Dependency updates | `.github/dependabot.yml` |
| OWASP Dependency-Check | Comprehensive vulnerability detection | `.github/workflows/security-scan.yml` |
| .NET Static Analysis | Backend code security | Built into .NET build |

### Optional/Future Tools

- **Snyk**: Advanced vulnerability scanning (requires upgrade)
- **SonarQube**: Code quality and security analysis
- **DAST Tools**: Dynamic application security testing
- **Penetration Testing**: Regular professional testing

## Compliance

### Standards & Certifications

- **OWASP Top 10**: Following security best practices
- **CWE**: Addressing common weaknesses
- **GDPR**: User data protection (if applicable)
- **HIPAA**: If handling health information

## Security Updates

Ensure you:
1. **Subscribe** to security notifications
2. **Review** security advisories when released
3. **Test** patches in staging before production
4. **Deploy** security updates promptly

## Additional Resources

- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [CWE/SANS Top 25](https://cwe.mitre.org/top25/)
- [ABP Framework Security](https://abp.io/docs/latest/framework/features/security)
- [Angular Security Guide](https://angular.io/guide/security)
- [ASP.NET Core Security](https://docs.microsoft.com/en-us/aspnet/core/security/)

## Contact

For security-related questions or concerns:
- **Security**: security@marketplace.example.com
- **General Support**: support@marketplace.example.com

---

**Last Updated**: 2025-10-18
**Version**: 1.0
