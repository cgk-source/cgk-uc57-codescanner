# AI Code Review and Security Scanner - Step-by-Step Implementation Plan

## 1. Objective

Create a developer-facing code-review solution that accepts pasted diffs, uploaded patch/source files,
GitHub pull requests, and Azure DevOps pull requests. The solution analyzes changed code for OWASP
Top 10 issues, security defects, anti-patterns, reliability problems, and logic bugs. It returns at
most 10 findings per scan, ranked by severity, with JSON and developer-friendly HTML reports.

## 2. Success Criteria

- ASP.NET Core .NET 8 Web API analyzes a 500-line diff in under 20 seconds.
- GPT-4o is used for AI analysis without allowing invalid model output into the dashboard.
- Python, JavaScript, TypeScript, Java, and C# are recognized.
- Every finding contains category, severity, file, line range, impact, confidence, and suggested fix.
- Severity values are only `Critical`, `High`, `Medium`, `Low`, or `Info`.
- JSON output is validated against a strict schema before display or persistence.
- HTML output uses escaped content and visible severity badges.
- Secure-pattern benchmark false-positive rate remains below 20%.

## 3. Target Architecture

```text
React Dashboard
      |
      v
ASP.NET Core Web API
  - Source ingestion
  - Diff parsing
  - Language detection
  - Static candidate checks
  - GPT-4o orchestration
  - Strict JSON validation
  - Finding ranking/deduplication
  - JSON and HTML reports
      |
      +--> Azure OpenAI GPT-4o
      +--> Azure DevOps REST API
      +--> GitHub REST API
      +--> Azure SQL Database
      +--> Azure Blob Storage
      +--> Azure Key Vault
      +--> Application Insights
```

## 4. Repository Layout

```text
src/CodeReview.Api/
  Models/
  Services/
  Validators/
  Reports/
  Persistence/

dashboard/
  src/
  public/

tests/CodeReview.Api.Tests/
tests/CodeReview.Regression.Tests/

infrastructure/
  main.bicep
  modules/

.github/workflows/
```

As implementation proceeds, extract domain, application, infrastructure, and contract projects only
where the added responsibility improves testability and deployment separation.

## 5. Delivery Sequence

### Milestone 1 - Harden the API Foundation

1. Add a solution file and test project.
2. Replace ad-hoc demo-only contracts with versioned request and response records.
3. Add request-size, source-type, language, finding-limit, and line-count validation.
4. Add cancellation, timeout, idempotency, and rate-limit configuration.
5. Add health, readiness, and OpenAPI endpoints.
6. Add unit tests for all validation and response invariants.
7. Build and test the API, then commit and push.

**Definition of done:** Invalid requests are rejected consistently and completed responses always
match the public contract.

### Milestone 2 - Diff Parsing and Language Detection

1. Implement unified-diff parsing for `diff --git`, file headers, hunk headers, added lines, and
   source line numbers.
2. Return changed file, changed line range, language, and changed source blocks.
3. Detect Python, JavaScript, TypeScript, Java, and C# from extensions and source content.
4. Normalize line endings and remove binary or unsupported files.
5. Split a diff into bounded analysis chunks of at most 150 added/context lines.
6. Redact likely secrets, access tokens, private keys, passwords, connection strings, and credentials
   before model prompts or persistent storage.
7. Add parser, language, secret-redaction, and chunking tests.
8. Commit and push after all targeted tests pass.

**Definition of done:** A 500-line diff produces deterministic file and line metadata that can be
analyzed independently.

### Milestone 3 - Deterministic Security Checks

1. Implement pre-model rules for SQL concatenation, dynamic shell use, unsafe deserialization,
   weak cryptography, path traversal, disabled certificate validation, dangerous DOM use,
   hard-coded secrets, and insecure logging.
2. Model rules as candidates with CWE and OWASP metadata.
3. Treat static results as evidence for the AI verifier, never as automatically final findings.
4. Add language-specific allow patterns for parameterized queries, framework escape functions,
   standard crypto APIs, authentication middleware, and secret configuration.
5. Unit-test vulnerable and secure examples.
6. Commit and push after the deterministic suite passes.

**Definition of done:** Common vulnerable patterns are found quickly and common secure patterns do
not become final findings without AI confirmation.

### Milestone 4 - Azure OpenAI GPT-4o Integration

1. Add Azure OpenAI configuration using Key Vault or environment variables.
2. Implement a typed OpenAI client behind `IAiCodeAnalyzer`.
3. Add a low-temperature JSON-only prompt with security, quality, logic, and remediation instructions.
4. Request no more than 10 findings and provide exact file/line context.
5. Add request timeout and bounded retries.
6. Parse model responses without permissive fallback for required fields.
7. Validate output against a strict JSON contract.
8. Repair or retry invalid output once; reject output that still fails validation.
9. Record model deployment and prompt version with every analysis.
10. Add integration tests using mocks and, separately, optional Azure smoke tests.
11. Commit and push after local and mocked tests pass.

**Definition of done:** Only schema-valid findings reach the API response, database, or dashboard.

### Milestone 5 - Finding Validation and Ranking

1. Add strict data validation for enums, ranges, file names, categories, confidence, and text fields.
2. Add stable finding fingerprints.
3. Merge duplicate findings using file, category/CWE, and overlapping or nearby lines.
4. Drop findings that reference no changed line or have confidence below the configured threshold.
5. Sort by severity, confidence, impact, file, and line.
6. Return no more than the requested limit, with a default maximum of 10.
7. Preserve unknown OWASP/CWE values only through explicit nullable fields.
8. Add unit tests for deduplication and ranking.
9. Commit and push after the suite passes.

**Definition of done:** The top findings are deterministic, relevant, and free of duplicate reports.

### Milestone 6 - Azure DevOps and GitHub Source Ingestion

1. Implement source URL parsers for Azure DevOps and GitHub.
2. Add Azure DevOps PAT or service-principal authentication.
3. Retrieve pull-request metadata, changed files, and file patches with the Azure DevOps REST API.
4. Add GitHub App/PAT authentication and changed-file retrieval.
5. Convert PR files into normalized unified diff inputs.
6. Preserve repository, commit, PR, file, and source URL metadata.
7. Add integration tests for URL parsing and API response mapping.
8. Commit and push after PR fixtures pass.

**Definition of done:** A PR URL produces findings against the correct changed files and source lines.

### Milestone 7 - Azure SQL Persistence

1. Add EF Core and Azure SQL configuration.
2. Create `Scans`, `Findings`, `Sources`, `FixSuggestions`, and `RemediationFeedback` entities.
3. Add migrations and SQL indexes for scan ID, severity, source, and fingerprint.
4. Persist source metadata only after secret redaction.
5. Persist validation status and model/prompt metadata.
6. Implement scan history and finding-feedback endpoints.
7. Add repository and integration tests using SQLite or a compatible test container.
8. Commit and push after migrations and tests pass.

**Definition of done:** Completed scans and developer feedback survive process restarts.

### Milestone 8 - JSON and HTML Reporting

1. Add JSON export endpoint and download in the dashboard.
2. Implement server-side HTML rendering with severity badges.
3. Include summary counts, scan metadata, OWASP/CWE values, code snippets, fixes, and confidence.
4. HTML-encode every model-provided value.
5. Add report links to Azure Blob Storage or signed local download endpoints.
6. Add XSS, escaping, and report snapshot tests.
7. Commit and push after report tests pass.

**Definition of done:** Reports are developer-readable, portable, and safe to render.

### Milestone 9 - React Dashboard

1. Replace the API URL with environment-based configuration.
2. Add pasted diff, file upload, GitHub URL, and Azure DevOps URL input modes.
3. Add loading, progress, validation, cancellation, timeout, and error states.
4. Group findings by severity with filter chips and summary badges.
5. Add finding details, fixed-code examples, source line references, and copy actions.
6. Add JSON/HTML export and remediation feedback controls.
7. Add accessible labels, keyboard navigation, mobile layouts, and light/dark styles.
8. Add component tests and dashboard build checks.
9. Commit and push after the dashboard build and tests pass.

**Definition of done:** Users can submit every supported source and review/export findings.

### Milestone 10 - Performance and False-Positive Quality

1. Parallelize independent file and chunk analyses.
2. Cache identical normalized diff chunks.
3. Configure Azure OpenAI timeout and retry budgets.
4. Limit prompt context to changed blocks and relevant imports/classes.
5. Build a benchmark corpus of vulnerable and secure Python, JavaScript, TypeScript, Java, and C# samples.
6. Measure P50/P95/P99 latency and false-positive rate.
7. Tune prompts and deterministic allow patterns against benchmark results.
8. Verify 500 changed lines achieve P95 latency below 20 seconds.
9. Commit and push benchmark harness and passing baselines.

**Definition of done:** Performance and false-positive gates are reproducible in CI.

### Milestone 11 - Security and Operations

1. Add Microsoft Entra authentication and role authorization.
2. Use managed identities and Azure Key Vault for secrets.
3. Redact source content and credentials from logs and telemetry.
4. Add CORS allow-list, request limits, throttling, malware scanning hooks, and retention policies.
5. Add Application Insights metrics for latency, cost, retries, invalid JSON, findings, and feedback.
6. Configure alerts for latency, Azure OpenAI throttling, invalid output, API errors, and database health.
7. Add dependency, secret, container, and infrastructure security checks.
8. Commit and push after the security controls pass.

**Definition of done:** The solution has an auditable production security baseline.

### Milestone 12 - Deployment and CI/CD

1. Add Dockerfiles for API and dashboard.
2. Add Azure Bicep infrastructure for App Service/Container Apps, Azure OpenAI, SQL, Key Vault,
   Blob Storage, Application Insights, and managed identity.
3. Add CI build, unit, integration, regression, and frontend build gates.
4. Add staged release deployment with smoke tests.
5. Add database migration and rollback procedures.
6. Add production health, readiness, and canary checks.
7. Commit and push deployment templates and pipeline definitions.

**Definition of done:** Staging and production deployments are repeatable and observable.

## 6. API Contract Milestones

### Analysis Request

```json
{
  "source": {
    "type": "Diff",
    "content": "diff --git...",
    "identifier": null
  },
  "languages": ["Python", "JavaScript"],
  "options": {
    "includeQuality": true,
    "includeOWASP": true,
    "includeLogic": true,
    "maxFindings": 10
  }
}
```

### Finding Contract

```json
{
  "id": "FND-001",
  "category": "Security",
  "subcategory": "SQL Injection",
  "owasp": "A03:2021 - Injection",
  "cwe": "CWE-89",
  "severity": "Critical",
  "confidence": 0.96,
  "file": "src/users.py",
  "lineStart": 42,
  "lineEnd": 45,
  "description": "User input reaches a SQL statement without parameterization.",
  "impact": "An attacker may read or modify unauthorized records.",
  "suggestedFix": "Bind user input with a parameterized query.",
  "correctedCode": "cursor.execute(\"SELECT * FROM users WHERE id = %s\", (user_id,))"
}
```

## 7. Per-Change Commit Policy

Each milestone or focused subtask will follow this process:

1. Inspect relevant files and existing conventions.
2. Implement the smallest complete, testable module.
3. Run the smallest applicable build and tests.
4. Inspect the diff for secrets and unrelated changes.
5. Commit with a clear conventional message and the required co-author trailer.
6. Push the commit to `origin/main` once GitHub authentication permits it.
7. Continue only after the pushed slice is in a workable state.
