import { FormEvent, useState } from "react";

type Severity = "Critical" | "High" | "Medium" | "Low" | "Info";

interface Finding {
  id: string;
  category: string;
  subcategory: string;
  owasp?: string;
  cwe?: string;
  severity: Severity;
  confidence: number;
  file: string;
  lineStart: number;
  lineEnd: number;
  description: string;
  impact: string;
  suggestedFix: string;
  correctedCode?: string;
}

interface AnalysisResponse {
  analysisId: string;
  status: string;
  summary: {
    critical: number;
    high: number;
    medium: number;
    low: number;
    info: number;
    total: number;
  };
  findings: Finding[];
  metadata: {
    languages: string[];
    durationMs: number;
    model: string;
    schemaValidated: boolean;
  };
}

const initialDiff = `diff --git a/src/Data/UserRepository.cs b/src/Data/UserRepository.cs
@@
-var query = "SELECT * FROM Users";
+var query = "SELECT * FROM Users WHERE Id = " + id;
+return db.Database.Execute(query);`;

export default function App() {
  const [diff, setDiff] = useState(initialDiff);
  const [severity, setSeverity] = useState("All");
  const [result, setResult] = useState<AnalysisResponse | null>(null);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  async function analyze(event: FormEvent) {
    event.preventDefault();
    setLoading(true);
    setError("");

    try {
      const response = await fetch("http://localhost:5000/api/v1/analyses", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          source: { type: "Diff", content: diff },
          languages: [],
          options: { includeQuality: true, includeOWASP: true, includeLogic: true, maxFindings: 10 },
        }),
      });
      if (!response.ok) {
        throw new Error((await response.json()).message || "Analysis failed");
      }
      setResult(await response.json());
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "Analysis failed");
    } finally {
      setLoading(false);
    }
  }

  const findings = result?.findings.filter(
    (finding) => severity === "All" || finding.severity === severity,
  ) ?? [];

  return (
    <main>
      <header>
        <div>
          <p className="eyebrow">Developer security</p>
          <h1>AI Code Review Scanner</h1>
        </div>
        {result && <span className="status">Schema validated</span>}
      </header>

      <section className="input-panel">
        <form onSubmit={analyze}>
          <label htmlFor="diff">Unified diff or source patch</label>
          <textarea id="diff" value={diff} onChange={(event) => setDiff(event.target.value)} />
          <button type="submit" disabled={loading}>{loading ? "Analyzing..." : "Analyze code"}</button>
          {error && <p className="error">{error}</p>}
        </form>
      </section>

      {result && (
        <>
          <section className="summary">
            {(["Critical", "High", "Medium", "Low", "Info"] as Severity[]).map((level) => (
              <button
                key={level}
                className={`summary-card ${level.toLowerCase()}`}
                onClick={() => setSeverity(level)}
              >
                <span>{level}</span>
                <strong>{result.summary[level.toLowerCase() as keyof typeof result.summary]}</strong>
              </button>
            ))}
            <button className="summary-card" onClick={() => setSeverity("All")}>
              <span>Total</span>
              <strong>{result.summary.total}</strong>
            </button>
          </section>

          <div className="toolbar">
            <label>
              Severity
              <select value={severity} onChange={(event) => setSeverity(event.target.value)}>
                <option>All</option>
                <option>Critical</option>
                <option>High</option>
                <option>Medium</option>
                <option>Low</option>
                <option>Info</option>
              </select>
            </label>
            <span>{result.metadata.durationMs} ms · {result.metadata.model} · {result.metadata.languages.join(", ")}</span>
          </div>

          <section className="findings">
            {findings.map((finding) => (
              <article className="finding" key={finding.id}>
                <div className="finding-heading">
                  <div>
                    <span className={`badge ${finding.severity.toLowerCase()}`}>{finding.severity}</span>
                    <span className="category">{finding.category} · {finding.subcategory}</span>
                  </div>
                  <code>{finding.file}:{finding.lineStart}-{finding.lineEnd}</code>
                </div>
                <p>{finding.description}</p>
                <p><strong>Impact:</strong> {finding.impact}</p>
                <p><strong>Suggested fix:</strong> {finding.suggestedFix}</p>
                {finding.correctedCode && <pre>{finding.correctedCode}</pre>}
              </article>
            ))}
          </section>
        </>
      )}
    </main>
  );
}
