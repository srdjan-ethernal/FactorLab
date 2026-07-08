using FactorLab.Web.Domain;

namespace FactorLab.Web.Services;

public static class DocumentChecklistFactory
{
    public static List<DocumentRequirement> CreateDefault()
    {
        return new List<DocumentRequirement>
        {
            Required(DocumentKind.Invoice),
            Required(DocumentKind.PurchaseOrder),
            Required(DocumentKind.ProofOfDelivery),
            Required(DocumentKind.DebtorConfirmation),
            Required(DocumentKind.Kyc),
            Required(DocumentKind.NoticeOfAssignment, DocumentStatus.Missing, "Generated after approval")
        };
    }

    public static DocumentRequirement Required(DocumentKind kind, DocumentStatus status = DocumentStatus.Missing, string note = "")
    {
        return new DocumentRequirement
        {
            Kind = kind,
            Status = status,
            ReviewerNote = note,
            FileName = status == DocumentStatus.Missing ? "" : $"{Label(kind).Replace(" ", "-").ToLowerInvariant()}.pdf",
            ReceivedAt = status == DocumentStatus.Missing ? null : DateTime.UtcNow.AddDays(-2)
        };
    }

    public static string Label(DocumentKind kind) => kind switch
    {
        DocumentKind.Invoice => "Invoice",
        DocumentKind.PurchaseOrder => "PO / contract",
        DocumentKind.ProofOfDelivery => "Proof of delivery",
        DocumentKind.DebtorConfirmation => "Debtor confirmation",
        DocumentKind.Kyc => "KYC",
        DocumentKind.NoticeOfAssignment => "Notice of assignment",
        _ => kind.ToString()
    };
}
