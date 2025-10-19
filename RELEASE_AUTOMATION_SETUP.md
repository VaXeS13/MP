# Release Automation Setup Guide

This guide explains how to set up and use the automated release workflow for the Marketplace Pavilion project.

## Overview

The Release Automation workflow (`release-automation.yml`) provides:

✅ **Semantic Versioning** - Automatic version bumping based on commit messages
✅ **Changelog Generation** - Automatic changelog from commit history
✅ **GitHub Releases** - Create releases with changelogs
✅ **Git Tags** - Automatic tagging for releases
✅ **Artifact Publishing** - Publish to NuGet and npm
✅ **Notifications** - Slack notifications on release

## Commit Message Convention (Conventional Commits)

The workflow uses the Conventional Commits specification to determine version bumps:

### Commit Format
```
<type>(<scope>): <subject>

<body>

<footer>
```

### Types

- **feat**: A new feature → **MINOR** version bump
- **fix**: A bug fix → **PATCH** version bump
- **feat!**: or **fix!**: Breaking change → **MAJOR** version bump
- **docs**: Documentation changes (no release)
- **style**: Code style changes (no release)
- **refactor**: Code refactoring (no release)
- **perf**: Performance improvements (no release)
- **test**: Test additions/changes (no release)
- **chore**: Build/dependency changes (no release)
- **ci**: CI/CD changes (no release)

### Examples

**Feature (MINOR bump: 1.2.3 → 1.3.0)**
```
feat(payments): add Stripe payment provider support

- Implement Stripe API integration
- Add payment webhook handling
- Support multiple payment methods
```

**Bug Fix (PATCH bump: 1.2.3 → 1.2.4)**
```
fix(rentals): resolve date validation issue

Fixes #123
```

**Breaking Change (MAJOR bump: 1.2.3 → 2.0.0)**
```
feat(api)!: redesign rental API endpoints

BREAKING CHANGE: The /api/rental/{id} endpoint now returns a different response format.
Old format: { rental: {...} }
New format: {...}

Fixes #456
```

## How It Works

### Automatic Triggers

The workflow runs automatically when:
1. Code is pushed to `main` branch
2. Changes affect `src/**`, `angular/src/**`, or the workflow itself

### Manual Triggers

Trigger a release manually via GitHub Actions UI:

```
Settings > Actions > All workflows > Release Automation > Run workflow
```

Options:
- **Version**: Override automatic version (e.g., "1.2.3")
- **Pre-release**: Mark as pre-release (false/true)

### Workflow Steps

1. **Determine Version**
   - Analyzes commits since last tag
   - Counts feat, fix, breaking changes
   - Determines semantic version bump
   - Generates changelog from commit messages

2. **Create Release**
   - Creates git tag
   - Pushes tag to repository
   - Creates GitHub Release with changelog

3. **Publish Artifacts**
   - Builds .NET solution (Release config)
   - Publishes to NuGet (if credentials available)
   - Builds Angular frontend
   - Publishes to npm (if credentials available)
   - Uploads build artifacts to release

4. **Send Notifications**
   - Sends Slack notification
   - Creates GitHub Step Summary

5. **Cleanup**
   - Deletes old pre-releases (keeps last 3)

## GitHub Secrets Setup

Configure these secrets in GitHub repository settings:

### Required Secrets

```
GITHUB_TOKEN
  └─ Automatically provided by GitHub Actions
     (No setup needed - built-in)
```

### Optional Secrets for Publishing

```
NUGET_API_KEY
  └─ API key from nuget.org for publishing .NET packages
     Obtain from: https://www.nuget.org/account/apikeys

NPM_TOKEN
  └─ Authentication token for npm registry
     Obtain from: https://www.npmjs.com/settings/[username]/tokens
     Create token with "Publish" permission

SLACK_WEBHOOK_RELEASES
  └─ Slack webhook URL for release notifications
     Obtain from: Slack App > Incoming Webhooks
     (Set to "suggestion" severity to allow skipping)
```

### Setup Instructions

1. Go to GitHub repository **Settings**
2. Navigate to **Secrets and variables > Actions**
3. Click **New repository secret**
4. Add each secret:

**Example for NuGet API Key:**
- **Name**: `NUGET_API_KEY`
- **Value**: `oy2axxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx`

**Example for npm Token:**
- **Name**: `NPM_TOKEN`
- **Value**: `npm_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx`

**Example for Slack Webhook:**
- **Name**: `SLACK_WEBHOOK_RELEASES`
- **Value**: `https://hooks.slack.com/services/TXXXXXXXX/BXXXXXXXX/XXXXXXXXXXXXXXXXXXXXXXXX`

## Version Bumping Examples

### Example Scenario 1: Feature Release

**Current version**: v1.2.3

**Commits since last release:**
```
feat(dashboard): add analytics charts
fix(rentals): resolve date calculation bug
docs(readme): update installation guide
```

**Determined version**: v1.3.0 (MINOR bump for feature)

**Result**:
- Tag: `v1.3.0`
- Changelog includes 1 feature and 1 fix
- Release created with changelog

### Example Scenario 2: Major Breaking Change

**Current version**: v2.5.0

**Commits since last release:**
```
feat(api)!: redesign rental endpoints
feat(payments): add new payment method
fix(cart): resolve item duplication
```

**Determined version**: v3.0.0 (MAJOR bump for breaking change)

**Result**:
- Tag: `v3.0.0`
- Release notes highlight breaking change
- Slack notification sent
- Old pre-releases cleaned up

## Workflow Configuration

### Default Behavior

- **Trigger**: Push to main with code changes
- **Version Strategy**: Semantic versioning
- **Changelog**: Generated from conventional commits
- **Artifacts**: Published to NuGet and npm (if secrets configured)
- **Cleanup**: Pre-releases older than 3 most recent are deleted

### Customization

Edit `.github/workflows/release-automation.yml` to:

1. **Change trigger conditions**: Modify the `on.push.paths` section
2. **Adjust version bumping logic**: Edit the "Determine version" step
3. **Modify artifacts**: Change NuGet/npm publishing sections
4. **Add new publishers**: Insert additional publish steps

## Manual Release Creation

To create a release without code changes:

1. Go to GitHub Actions > Release Automation
2. Click **Run workflow**
3. Enter version (e.g., "1.5.0")
4. (Optional) Check "Pre-release" box
5. Click **Run workflow**

This triggers the workflow to create a release with the specified version.

## Troubleshooting

### Release doesn't trigger on push

**Check**:
1. Are changes in `src/**` or `angular/src/**`?
2. Are commits using conventional commit format?
3. Is the branch `main`?

**Solution**: Check commit history with:
```bash
git log --oneline -10
```

Ensure commits match pattern: `type(scope): message`

### Version determined incorrectly

**Check**: Previous git tags
```bash
git tag -l
```

**Fix**: If needed, manually tag a commit:
```bash
git tag -a v1.0.0 -m "Release v1.0.0" <commit-hash>
git push origin v1.0.0
```

### Artifacts not published

**Check**:
1. Are NuGet/npm secrets configured?
2. Do builds complete successfully?
3. Are credentials valid?

**Solution**: Check workflow logs for specific errors:
1. Go to Actions tab
2. Click failed run
3. Expand "Publish Artifacts" step
4. Check error messages

### Slack notification not sending

**Check**:
1. Is `SLACK_WEBHOOK_RELEASES` secret configured?
2. Is webhook URL still valid?

**Solution**: Create new webhook in Slack and update secret.

## Best Practices

### 1. Use Conventional Commits Consistently

```bash
# Good
git commit -m "feat(rentals): add rental extension feature"

# Bad
git commit -m "updated rental extension"
```

### 2. Keep Commits Focused

```bash
# Good - one change per commit
git commit -m "fix(cart): resolve checkout validation bug"

# Bad - multiple unrelated changes
git commit -m "fixed bugs and added features"
```

### 3. Use Scopes Clearly

Common scopes:
- `api`: Backend API changes
- `dashboard`: Frontend dashboard changes
- `payments`: Payment-related changes
- `rentals`: Rental management changes
- `items`: Item management changes
- `auth`: Authentication/authorization changes

### 4. Document Breaking Changes

```bash
git commit -m "feat(api)!: redesign payment endpoint response

BREAKING CHANGE: Payment endpoint now returns transactionId instead of paymentId"
```

### 5. Review Before Release

Before running release workflow:
1. Ensure all tests pass
2. Verify all commits are correct
3. Check version bump is appropriate
4. Review generated changelog

## Integration with CI/CD

### Running Tests Before Release

Add a check in `.github/workflows/release-automation.yml`:

```yaml
  check-tests:
    name: Verify Tests Pass
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v4
      - name: Run tests
        run: dotnet test MP.sln

  determine-version:
    needs: check-tests
    # ... rest of job
```

### Conditional Release

Release only if tests pass:

```yaml
  release:
    needs: [check-tests, determine-version]
    if: needs.check-tests.result == 'success'
```

## Version Numbering

Using Semantic Versioning (MAJOR.MINOR.PATCH):

- **MAJOR**: Breaking changes or major features (feat!)
- **MINOR**: New backward-compatible features (feat)
- **PATCH**: Bug fixes (fix)

Examples:
- `1.0.0` - Initial release
- `1.1.0` - New feature added
- `1.1.1` - Bug fix
- `2.0.0` - Breaking changes

## References

- [Conventional Commits](https://www.conventionalcommits.org/)
- [Semantic Versioning](https://semver.org/)
- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [NuGet Publishing](https://docs.microsoft.com/en-us/nuget/publish-a-package)
- [npm Publishing](https://docs.npmjs.com/packages-and-modules/contributing-packages-to-the-registry)

## Questions?

For issues or questions about release automation:

1. Check workflow logs: GitHub Actions > Release Automation > Recent runs
2. Review commit history: `git log --oneline`
3. Check git tags: `git tag -l`
4. Review GitHub releases: Repository > Releases

---

**Last Updated**: 2025-10-19
**Version**: 1.0.0
**Maintained by**: Development Team
