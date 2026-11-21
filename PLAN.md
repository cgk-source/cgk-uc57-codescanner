# AI Code Review & Security Scanner Implementation Plan

## Architecture

- React and TypeScript dashboard
- ASP.NET Core .NET 8 Web API
- Azure OpenAI GPT-4o analysis
- Azure DevOps REST API pull request extraction
- Azure SQL persistence
- Azure Key Vault, Blob Storage, and Application Insights

## API Contract

`POST /api/v1/analyses` accepts pasted diffs, uploaded patches, or an Azure DevOps pull request URL.

The response contains an analysis ID, status, summary counts, findings, and metadata. Findings are
validated against a strict schema before they are returned.

## Implementation Milestones

1. **Foundation** — API solution, request contracts, validation, logging, SQL schema, and React app.
2. **Diff Processing** — Unified diff parser, language detection, hunk extraction, and 150-line chunking.
3. **AI Integration** — Azure OpenAI GPT-4o prompts, JSON-only responses, schema validation, retries, and confidence normalization.
4. **Pull Requests** — Azure DevOps URL parsing, PR metadata, changed files, and source line mapping.
5. **Findings** — Deduplication, severity ranking, remediation, and false-positive feedback.
6. **Reporting** — JSON download, HTML report, severity badges, and persisted history.
7. **Dashboard** — Diff/file/URL input, filters, finding details, and remediation actions.
8. **Quality** — Benchmark corpus, false-positive testing, prompt-injection controls, load tests, and P95 latency measurement.
9. **Deployment** — Docker, Azure DevOps pipelines, infrastructure as code, monitoring, staging, and production smoke tests.

## Delivery Slices

- **Slice 1:** Scaffold API and dashboard; support pasted and uploaded diffs.
- **Slice 2:** Add Azure OpenAI and Azure DevOps providers.
- **Slice 3:** Add Azure SQL persistence and remediation history.
- **Slice 4:** Add HTML reports, benchmarks, telemetry, and deployment automation.

## Acceptance Criteria

- Pasted diffs, uploaded patches, and Azure DevOps PR URLs are accepted.
- Python, JavaScript, Java, C#, and TypeScript are detected.
- JSON responses pass strict schema validation.
- Findings include category, severity, file, line range, impact, and suggested remediation.
- HTML reports show severity badges.
- A 500-line diff has a P95 latency below 20 seconds.
- Benchmark false-positive rate remains below 20%.
- Secrets and source content are excluded from logs.
