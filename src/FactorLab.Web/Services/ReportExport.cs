namespace FactorLab.Web.Services;

public sealed class ReportExport
{
    public string FileName { get; init; } = "";
    public string ContentType { get; init; } = "text/csv";
    public string Content { get; init; } = "";
}
