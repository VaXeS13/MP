# Code Quality & SonarCloud Setup Guide

This document explains how to configure and use SonarQube/SonarCloud for code quality analysis in the Marketplace Pavilion project.

## Overview

The project uses multiple quality analysis tools:

- **SonarQube/SonarCloud**: Static code analysis, code coverage, duplications
- **ESLint**: TypeScript/Angular linting
- **OWASP Dependency-Check**: Security vulnerability scanning
- **npm audit**: Frontend dependency vulnerability check
- **dotnet list package --vulnerable**: Backend dependency vulnerability check

## SonarCloud Setup

### Prerequisites

1. **GitHub Account**: Required for SonarCloud authentication
2. **SonarCloud Organization**: Create at https://sonarcloud.io
3. **Project Setup**: Both backend and frontend projects need to be created

### Step 1: Create SonarCloud Organization

1. Go to https://sonarcloud.io
2. Click "Sign up with GitHub"
3. Authorize SonarCloud to access your GitHub account
4. Create a new organization (e.g., "vaxes")
5. Accept the terms and create the organization

### Step 2: Create Backend Project

1. In SonarCloud, go to your organization
2. Click "Analyze new project"
3. Select the "marketplace-pavilion" repository
4. Choose "GitHub Actions" as setup method
5. Click "Create project"
6. You'll receive:
   - `SONAR_TOKEN` (secret)
   - `SONAR_HOST_URL` (usually https://sonarcloud.io)

### Step 3: Create GitHub Secrets

Add these secrets to your GitHub repository settings (`Settings > Secrets and variables > Actions`):

```
SONAR_TOKEN: <your-sonarcloud-token>
SONAR_HOST_URL: https://sonarcloud.io
SLACK_WEBHOOK_DEPLOYMENT: <optional-slack-webhook>
```

### Step 4: Configure Backend Project Settings

In SonarCloud project settings for backend:

1. **Key**: `marketplace-pavilion`
2. **Language**: C#
3. **Project Visibility**: Public or Private (as needed)
4. **Quality Gate**: "Sonar way" (or create custom)

#### Quality Gate Rules (Backend)

Configure minimum quality standards:

- **Coverage**: Minimum 70% code coverage
- **Duplicated Lines Density**: < 3%
- **Blocker Issues**: 0
- **Critical Issues**: 0
- **Reliability Rating**: A (max 1 bug per 1K lines)
- **Security Rating**: A (max 1 vulnerability per 1K lines)
- **Maintainability Rating**: A (technical debt < 5%)

### Step 5: Configure Frontend Project Settings

If analyzing Angular frontend as separate project:

1. Create new project: `marketplace-pavilion-frontend`
2. **Key**: `marketplace-pavilion-frontend`
3. **Language**: TypeScript
4. **Project Visibility**: Public or Private
5. **Quality Gate**: Configure same standards

## GitHub Actions Integration

### Automatic Workflows

The project includes automated workflows that run code quality analysis:

#### 1. Backend Tests & Analysis (`backend-tests.yml`)
- Runs on: Push to main/develop, PRs, nightly schedule
- Includes: SonarScanner analysis, code coverage

#### 2. Frontend Tests & Analysis (`frontend-tests.yml`)
- Runs on: Push to main/develop, PRs, nightly schedule
- Includes: ESLint, coverage, dependency check

#### 3. Code Quality (`code-quality.yml`)
- Runs on: All PRs, push to main/develop, weekly schedule
- Includes:
  - SonarScanner for .NET (backend)
  - SonarCloud for TypeScript (frontend)
  - Security & dependency analysis
  - Architecture analysis
  - Test coverage quality gates

### Manual Workflow Trigger

To manually run code quality analysis:

```bash
gh workflow run code-quality.yml
```

## Configuration Files

### sonar-project.properties

Located at project root, contains:

```properties
sonar.projectKey=marketplace-pavilion
sonar.projectName=Marketplace Pavilion
sonar.sources=src
sonar.tests=test
sonar.cs.coverage.reportPaths=**/coverage.opencover.xml
```

Customize this file for your project structure.

### .sonarcloud.properties (Alternative)

For SonarCloud-specific configuration (optional):

```properties
sonar.projectKey=marketplace-pavilion
sonar.organization=vaxes
sonar.sources=src
sonar.exclusions=**/bin/**,**/obj/**
```

## Local Development

### Running SonarQube Analysis Locally

#### For Backend (C#/.NET):

1. **Install SonarScanner CLI**:
   ```bash
   dotnet tool install --global dotnet-sonarscanner
   ```

2. **Start analysis**:
   ```bash
   dotnet sonarscanner begin /k:"marketplace-pavilion" /d:sonar.host.url="http://localhost:9000" /d:sonar.login="<token>"
   ```

3. **Build and test**:
   ```bash
   dotnet build MP.sln
   dotnet test MP.sln /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
   ```

4. **End analysis**:
   ```bash
   dotnet sonarscanner end /d:sonar.login="<token>"
   ```

#### For Frontend (TypeScript/Angular):

1. **Install SonarQube Scanner**:
   ```bash
   npm install -g sonarqube-scanner
   ```

2. **Run analysis**:
   ```bash
   cd angular
   sonar-scanner \
     -Dsonar.projectKey=marketplace-pavilion-frontend \
     -Dsonar.sources=src \
     -Dsonar.host.url=http://localhost:9000 \
     -Dsonar.login=<token> \
     -Dsonar.typescript.lcov.reportPaths=coverage/lcov.info
   ```

## Docker-based SonarQube (Development)

For local development, you can run SonarQube in Docker:

```bash
docker run -d \
  --name sonarqube \
  -e SONARQUBE_JDBC_URL=jdbc:postgresql://db:5432/sonar \
  -e SONARQUBE_JDBC_USERNAME=sonar \
  -e SONARQUBE_JDBC_PASSWORD=sonar \
  -p 9000:9000 \
  sonarqube:latest
```

Access at: http://localhost:9000 (default: admin/admin)

## Quality Metrics Explained

### Backend Metrics (C#)

1. **Reliability Rating**
   - A: 0 Bugs
   - B: At least 1 minor bug
   - C: At least 1 major bug
   - D: At least 1 critical bug
   - E: At least 1 blocker bug

2. **Security Rating**
   - Similar scale to Reliability
   - Measures code vulnerabilities

3. **Maintainability Rating**
   - A: Technical debt ratio < 5%
   - B: 5% ≤ ratio < 10%
   - C: 10% ≤ ratio < 20%
   - D: 20% ≤ ratio < 50%
   - E: ratio ≥ 50%

4. **Code Coverage**
   - Line coverage: Percentage of executable lines covered by tests
   - Branch coverage: Percentage of decision points covered
   - Target: >= 70%

### Frontend Metrics (TypeScript)

Same ratings as backend, plus:

1. **Complexity**
   - Cyclomatic complexity
   - Cognitive complexity

2. **Code Smells**
   - Maintainability issues
   - Best practice violations

3. **Duplicated Code**
   - Percentage of duplicated lines
   - Target: < 3%

## Pull Request Analysis

### Automatic PR Analysis

When you create a PR:

1. GitHub Actions runs `code-quality.yml`
2. SonarCloud analyzes the changes
3. Results appear as:
   - Status check in PR
   - SonarCloud comment with findings
   - Metrics comparison with main branch

### PR Quality Gate

The PR will fail if:

- New code coverage < 70%
- New critical issues found
- Failing security/reliability rating
- Failing quality gate

## Continuous Integration Checks

### Branch Protection Rules

Recommended settings in GitHub (`Settings > Branches > main`):

```
✓ Require status checks to pass before merging:
  - Build & Test (.NET 9.0)
  - Frontend Tests & Analysis
  - Code Quality Analysis
  - Security Scanning

✓ Require quality gate pass (if configured)
✓ Dismiss stale PR approvals when new commits are pushed
✓ Require code review from at least 1 approver
```

## Troubleshooting

### Common Issues

#### 1. "Authentication failed" error

**Solution**: Check that `SONAR_TOKEN` is correctly set in GitHub Secrets:
```bash
gh secret view SONAR_TOKEN
```

#### 2. "Project not found" error

**Solution**: Ensure project key in workflow matches SonarCloud project key:
```yaml
/k:"marketplace-pavilion"  # Must match SonarCloud
```

#### 3. Coverage report not found

**Solution**: Ensure tests run with coverage:
```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

Coverage files should be named `coverage.opencover.xml`

#### 4. SonarCloud status check pending

**Solution**:
- Check GitHub Actions logs for errors
- Verify SONAR_TOKEN is still valid
- Check SonarCloud organization for quota limits

### Getting Help

1. **SonarCloud Documentation**: https://docs.sonarcloud.io
2. **SonarQube Documentation**: https://docs.sonarqube.org
3. **GitHub Issues**: Report issues in repository

## Best Practices

### For Developers

1. **Run local analysis** before pushing:
   ```bash
   dotnet sonarscanner begin /k:"marketplace-pavilion" /d:sonar.host.url="http://localhost:9000" /d:sonar.login="<token>"
   dotnet build MP.sln
   dotnet test MP.sln /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
   dotnet sonarscanner end /d:sonar.login="<token>"
   ```

2. **Write tests first** to ensure coverage
3. **Fix code smells** before PR review
4. **Follow sonar rules** in your IDE (with SonarLint extension)

### For Teams

1. **Set realistic quality gates** (not too strict, not too loose)
2. **Review quality trends** weekly
3. **Refactor high-debt areas** regularly
4. **Celebrate quality improvements** in team meetings
5. **Keep dependencies updated** to reduce security issues

## IDE Integration

### Visual Studio

1. Install **SonarLint** extension
2. In Tools > Options > SonarLint Configuration:
   - Add SonarCloud connection
   - Enter organization key
   - Bind project to SonarCloud

### Visual Studio Code

1. Install **SonarLint** extension by SonarSource
2. In settings:
   ```json
   "sonarlint.connectedMode.connections.sonarcloud": {
     "organizationKey": "vaxes"
   },
   "sonarlint.connectedMode.project": {
     "projectKey": "marketplace-pavilion"
   }
   ```

### JetBrains IDEs (Rider, WebStorm, etc.)

1. Install **SonarLint** plugin
2. Configure SonarCloud connection
3. Bind project to SonarCloud

## Maintenance

### Regular Tasks

- **Weekly**: Review quality metrics dashboard
- **Monthly**: Update quality gates based on progress
- **Quarterly**: Refactor high-debt areas
- **Annually**: Review security vulnerabilities from OWASP

### Updating Dependencies

```bash
# Check outdated packages
npm outdated --prefix angular
dotnet outdated

# Update with caution and test thoroughly
npm update --prefix angular
dotnet add package <PackageName> --version <NewVersion>
```

## References

- **SonarCloud**: https://sonarcloud.io
- **SonarQube**: https://www.sonarqube.org
- **SonarLint**: https://www.sonarlint.org
- **OWASP Dependency-Check**: https://owasp.org/www-project-dependency-check/
- **GitHub Actions**: https://docs.github.com/en/actions
