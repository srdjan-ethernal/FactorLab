namespace FactorLab.Web.Domain;

public sealed class DocumentRequirement
{
    public string InvoiceNumber { get; set; } = "";
    public DocumentKind Kind { get; set; }
    public DocumentStatus Status { get; set; }
    public bool IsRequired { get; set; } = true;
    public string FileName { get; set; } = "";
    public string StoredPath { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long SizeBytes { get; set; }
    public string UploadedBy { get; set; } = "";
    public string ReviewerNote { get; set; } = "";
    public DateTime? ReceivedAt { get; set; }

    public bool IsSatisfied => !IsRequired || Status is DocumentStatus.Verified or DocumentStatus.Waived;
}
