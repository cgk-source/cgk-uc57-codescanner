using System.Net;
using System.Text;
using CodeReview.Api.Models;

namespace CodeReview.Api.Reports;

public static class HtmlReportRenderer
{
    public static string Render(AnalysisResponse analysis)
    {
        ArgumentNullException.ThrowIfNull(analysis);
        var html = new StringBuilder();

        html.AppendLine("<!doctype html>");
        html.AppendLine("<html lang=\"en\"><head>");
        html.AppendLine("<meta charset=\"utf-8\">");
        html.AppendLine("<meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">");
        html.AppendLine("<title>Code Review Findings</title>");
        html.AppendLine("<style>");
        html.AppendLine("body{font-family:Segoe UI,Arial,sans-serif;margin:40px;color:#172033;background:#f7f9fc}");
        html.AppendLine(".summary,.finding{background:white;border-radius:10px;margin-bottom:16px;padding:16px}");
        html.AppendLine(".badge{display:inline-block;border-radius:999px;padding:4px 10px;font-weight:700}");
        html.AppendLine(".Critical{background:#991b1b;color:white}.High{background:#c2410c;color:white}");
        html.AppendLine(".Medium{background:#a16207;color:white}.Low{background:#0369a1;color:white}.Info{background:#64748b;color:white}");
        html.AppendLine("pre{background:#111827;color:#f8fafc;padding:12px;border-radius:8px;overflow:auto}");
        html.AppendLine("</style></head><body>");
        html.AppendLine("<h1>AI Code Review Report</h1>");
        html.AppendFormat(
            "<p>Analysis {0} · {1} ms · Model {2} · Schema validated: {3}</p>",
            Escape(analysis.AnalysisId.ToString()),
            analysis.Metadata.DurationMs,
            Escape(analysis.Metadata.Model),
            analysis.Metadata.SchemaValidated);

        html.AppendFormat(
            "<section class=\"summary\"><strong>{0}</strong> findings: Critical {1} · High {2} · Medium {3} · Low {4} · Info {5}</section>",
            analysis.Summary.Total,
            analysis.Summary.Critical,
            analysis.Summary.High,
            analysis.Summary.Medium,
            analysis.Summary.Low,
            analysis.Summary.Info);

        foreach (var finding in analysis.Findings)
        {
            html.AppendLine("<article class=\"finding\">");
            html.AppendFormat(
                "<p><span class=\"badge {0}\">{0}</span> <strong>{1}</strong></p>",
                Escape(finding.Severity),
                Escape(finding.Subcategory));
            html.AppendFormat(
                "<p><code>{0}:{1}-{2}</code> · Confidence {3:P0}",
                Escape(finding.File),
                finding.LineStart,
                finding.LineEnd,
                finding.Confidence);
            if (!string.IsNullOrWhiteSpace(finding.Cwe))
            {
                html.AppendFormat(" · {0}", Escape(finding.Cwe));
            }
            html.AppendLine("</p>");
            html.AppendFormat("<p>{0}</p>", Escape(finding.Description));
            html.AppendFormat("<p><strong>Impact:</strong> {0}</p>", Escape(finding.Impact));
            html.AppendFormat("<p><strong>Suggested fix:</strong> {0}</p>", Escape(finding.SuggestedFix));
            if (!string.IsNullOrWhiteSpace(finding.CorrectedCode))
            {
                html.AppendFormat("<pre>{0}</pre>", Escape(finding.CorrectedCode));
            }
            html.AppendLine("</article>");
        }

        html.AppendLine("</body></html>");
        return html.ToString();
    }

    private static string Escape(string? value)
    {
        return WebUtility.HtmlEncode(value ?? string.Empty);
    }
}
