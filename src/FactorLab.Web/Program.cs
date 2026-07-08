using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.DataProtection;
using FactorLab.Web.Domain;
using FactorLab.Web.Persistence;
using FactorLab.Web.Services;
#if ENABLE_EFCORE
using Microsoft.EntityFrameworkCore;
#endif

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "App_Data", "Keys")));
builder.Services.AddSingleton<FactoringCalculator>();
builder.Services.AddSingleton<SamplePortfolioService>();
builder.Services.AddSingleton<IPortfolioRepository>(provider => provider.GetRequiredService<SamplePortfolioService>());
builder.Services.AddSingleton<CurrentUserService>();
builder.Services.AddSingleton<IDocumentStorageService, LocalDocumentStorageService>();
builder.Services.AddSingleton<IDocumentExtractionService, LocalDocumentExtractionService>();
builder.Services.AddSingleton<IRiskMemoService, LocalRiskMemoService>();
builder.Services.AddSingleton<IReportExportService, CsvReportExportService>();
builder.Services.AddSingleton<IActionCenterService, LocalActionCenterService>();
builder.Services.AddSingleton<IIntegrationOutboxService, LocalIntegrationOutboxService>();
builder.Services.AddSingleton<ITemplateGenerationService, LocalTemplateGenerationService>();
builder.Services.AddSingleton<IPortfolioMonitoringService, PortfolioMonitoringService>();
builder.Services.AddSingleton<IInvoiceImportService, CsvInvoiceImportService>();
builder.Services.AddSingleton<IFundingLedgerService, FundingLedgerService>();
builder.Services.AddSingleton<IBorrowingBaseService, BorrowingBaseService>();
builder.Services.AddSingleton<IFundingBatchService, FundingBatchService>();
builder.Services.AddSingleton<IPaymentReconciliationService, CsvPaymentReconciliationService>();
builder.Services.AddSingleton<IDisputeService, DisputeService>();
builder.Services.AddSingleton<IDebtorConfirmationService, DebtorConfirmationService>();
builder.Services.AddSingleton<IFraudSignalService, FraudSignalService>();
builder.Services.AddSingleton<IClientOfferService, ClientOfferService>();
builder.Services.AddSingleton<IFacilityApplicationService, FacilityApplicationService>();
builder.Services.AddSingleton<IEvmLedgerService, LocalEvmLedgerService>();
#if ENABLE_EFCORE
builder.Services.AddDbContext<FactorLabDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("FactorLabSql")));
#endif

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapGet("/api/portfolio/summary", (IPortfolioRepository portfolio, FactoringCalculator calculator) =>
{
    var summary = calculator.Summarize(portfolio.Invoices, portfolio.Terms, portfolio.Clients, portfolio.Debtors);
    return Results.Ok(new
    {
        summary.EligibleAmount,
        summary.EligibleCount,
        summary.CashToday,
        summary.TotalCost,
        summary.EffectiveApr,
        summary.PortfolioQuality,
        summary.DocumentReadyCount,
        summary.MissingRequiredDocuments,
        InvoiceCount = portfolio.Invoices.Count,
        ActiveInvoices = portfolio.Invoices.Count(invoice => invoice.FundingStage != FundingStage.Draft && invoice.FundingStage != FundingStage.Settled)
    });
});

app.MapGet("/api/reports/{kind}", (string kind, IPortfolioRepository portfolio, IReportExportService reports) =>
{
    if (!Enum.TryParse<ReportKind>(kind, ignoreCase: true, out var reportKind))
    {
        return Results.BadRequest(new { Error = "Unknown report kind." });
    }

    var export = reports.Export(reportKind, portfolio, portfolio.Terms);
    return Results.Text(export.Content, export.ContentType);
});

app.MapGet("/api/action-items", (IPortfolioRepository portfolio, IActionCenterService actionCenter) =>
    Results.Ok(actionCenter.BuildQueue(portfolio, portfolio.Terms, Array.Empty<string>())));

app.MapGet("/api/integration-events", (IIntegrationOutboxService outbox) =>
    Results.Ok(outbox.Events));

app.MapGet("/api/templates/{kind}/{invoiceNumber}", (string kind, string invoiceNumber, IPortfolioRepository portfolio, ITemplateGenerationService templates) =>
{
    if (!Enum.TryParse<TemplateKind>(kind, ignoreCase: true, out var templateKind))
    {
        return Results.BadRequest(new { Error = "Unknown template kind." });
    }

    var invoice = portfolio.Invoices.FirstOrDefault(item => item.InvoiceNumber.Equals(invoiceNumber, StringComparison.OrdinalIgnoreCase));
    if (invoice is null)
    {
        return Results.NotFound(new { Error = "Invoice not found." });
    }

    var template = templates.Generate(templateKind, invoice, portfolio, portfolio.Terms);
    return Results.Text(template.Content, template.ContentType);
});

app.MapGet("/api/covenants", (IPortfolioRepository portfolio, IPortfolioMonitoringService monitoring) =>
    Results.Ok(monitoring.BuildChecks(portfolio, portfolio.Terms)));

app.MapPost("/api/invoices/import", async (HttpRequest request, IPortfolioRepository portfolio, IInvoiceImportService importService, IIntegrationOutboxService outbox) =>
{
    using var reader = new StreamReader(request.Body);
    var csv = await reader.ReadToEndAsync();
    var result = importService.Import(csv, portfolio);
    outbox.Publish("InvoiceBook.Imported", "Portfolio", $"{result.Added} added / {result.Updated} updated from API import.", "Power Automate");
    return Results.Ok(result);
});

app.MapGet("/api/ledger", (IPortfolioRepository portfolio, IFundingLedgerService ledger) =>
{
    var entries = ledger.BuildLedger(portfolio, portfolio.Terms);
    return Results.Ok(new
    {
        Summary = ledger.Summarize(entries),
        Entries = entries
    });
});

app.MapGet("/api/borrowing-base", (IPortfolioRepository portfolio, IBorrowingBaseService borrowingBase) =>
{
    var lines = borrowingBase.Build(portfolio, portfolio.Terms);
    return Results.Ok(new
    {
        Summary = borrowingBase.Summarize(lines),
        Lines = lines
    });
});

app.MapPost("/api/funding-batches/create", (IPortfolioRepository portfolio, IFundingBatchService batches, IEvmLedgerService evmLedger, IIntegrationOutboxService outbox) =>
{
    var batch = batches.CreateApprovedBatch(portfolio, portfolio.Terms, "API");
    if (batch is null)
    {
        return Results.BadRequest(new { Error = "No approved invoices available for funding." });
    }

    var evmEvents = evmLedger.RecordFundingBatch(portfolio, batch, "API");
    outbox.Publish("FundingBatch.Created", batch.BatchNumber, $"{batch.InvoiceCount} invoice(s) funded for {batch.Currency} {batch.NetCash:N2} net cash.", "Power Automate");
    outbox.Publish("Evm.BuyReceivable", batch.BatchNumber, $"{evmEvents.Count} buy transaction(s) submitted to EVM.", "EVM RPC");
    return Results.Ok(batch);
});

app.MapPost("/api/payments/reconcile", async (HttpRequest request, IPortfolioRepository portfolio, IPaymentReconciliationService reconciliation, IIntegrationOutboxService outbox) =>
{
    using var reader = new StreamReader(request.Body);
    var csv = await reader.ReadToEndAsync();
    var result = reconciliation.Reconcile(csv, portfolio);
    outbox.Publish("Payments.Reconciled", "Portfolio", $"{result.Matched} matched / {result.Partial} partial / {result.Unmatched} unmatched.", "Power Automate");
    return Results.Ok(result);
});

app.MapPost("/api/disputes/open/{invoiceNumber}", (string invoiceNumber, IPortfolioRepository portfolio, IDisputeService disputes, IIntegrationOutboxService outbox) =>
{
    var invoice = portfolio.Invoices.FirstOrDefault(item => item.InvoiceNumber.Equals(invoiceNumber, StringComparison.OrdinalIgnoreCase));
    if (invoice is null)
    {
        return Results.NotFound(new { Error = "Invoice not found." });
    }

    var dispute = disputes.Open(portfolio, invoice, invoice.Amount, "Opened from API.", "API");
    outbox.Publish("Dispute.Opened", dispute.CaseNumber, $"{invoice.InvoiceNumber} dispute opened for {invoice.Amount:N2}.", "Microsoft Teams");
    return Results.Ok(dispute);
});

app.MapPost("/api/disputes/resolve/{caseNumber}", (string caseNumber, IPortfolioRepository portfolio, IDisputeService disputes, IIntegrationOutboxService outbox) =>
{
    var dispute = portfolio.DisputeCases.FirstOrDefault(item => item.CaseNumber.Equals(caseNumber, StringComparison.OrdinalIgnoreCase));
    if (dispute is null)
    {
        return Results.NotFound(new { Error = "Dispute not found." });
    }

    disputes.Resolve(portfolio, dispute, 0m, chargeback: false);
    outbox.Publish("Dispute.Resolved", dispute.CaseNumber, $"{dispute.InvoiceNumber} dispute resolved.", "Microsoft Teams");
    return Results.Ok(dispute);
});

app.MapPost("/api/debtor-confirmations/send/{invoiceNumber}", (string invoiceNumber, IPortfolioRepository portfolio, IDebtorConfirmationService confirmations, IIntegrationOutboxService outbox) =>
{
    var invoice = portfolio.Invoices.FirstOrDefault(item => item.InvoiceNumber.Equals(invoiceNumber, StringComparison.OrdinalIgnoreCase));
    if (invoice is null)
    {
        return Results.NotFound(new { Error = "Invoice not found." });
    }

    var request = confirmations.Send(portfolio, invoice, "API");
    outbox.Publish("DebtorConfirmation.Sent", request.RequestNumber, $"{request.InvoiceNumber} confirmation sent to {request.Debtor}.", "Outlook");
    return Results.Ok(request);
});

app.MapPost("/api/debtor-confirmations/confirm/{requestNumber}", (string requestNumber, IPortfolioRepository portfolio, IDebtorConfirmationService confirmations, IIntegrationOutboxService outbox) =>
{
    var request = portfolio.DebtorConfirmations.FirstOrDefault(item => item.RequestNumber.Equals(requestNumber, StringComparison.OrdinalIgnoreCase));
    if (request is null)
    {
        return Results.NotFound(new { Error = "Confirmation request not found." });
    }

    confirmations.Confirm(portfolio, request, "Confirmed from API.");
    outbox.Publish("DebtorConfirmation.Confirmed", request.RequestNumber, $"{request.InvoiceNumber} confirmed by debtor.", "Microsoft Teams");
    return Results.Ok(request);
});

app.MapPost("/api/debtor-confirmations/dispute/{requestNumber}", (string requestNumber, IPortfolioRepository portfolio, IDebtorConfirmationService confirmations, IDisputeService disputes, IIntegrationOutboxService outbox) =>
{
    var request = portfolio.DebtorConfirmations.FirstOrDefault(item => item.RequestNumber.Equals(requestNumber, StringComparison.OrdinalIgnoreCase));
    if (request is null)
    {
        return Results.NotFound(new { Error = "Confirmation request not found." });
    }

    confirmations.Dispute(portfolio, request, "Disputed from API.", disputes, "API");
    outbox.Publish("DebtorConfirmation.Disputed", request.RequestNumber, $"{request.InvoiceNumber} disputed by debtor.", "Microsoft Teams");
    return Results.Ok(request);
});

app.MapGet("/api/fraud-signals", (IPortfolioRepository portfolio, IFraudSignalService fraudSignals) =>
    Results.Ok(fraudSignals.Detect(portfolio)));

app.MapGet("/api/client-offers", (IPortfolioRepository portfolio) =>
    Results.Ok(portfolio.ClientOffers));

app.MapGet("/api/evm/trades", (IPortfolioRepository portfolio) =>
    Results.Ok(portfolio.EvmTradeEvents.OrderByDescending(item => item.CreatedAt)));

app.MapPost("/api/evm/trades/{eventId}/confirm", (string eventId, IPortfolioRepository portfolio, IEvmLedgerService evmLedger) =>
{
    var tradeEvent = portfolio.EvmTradeEvents.FirstOrDefault(item => item.EventId.Equals(eventId, StringComparison.OrdinalIgnoreCase));
    if (tradeEvent is null)
    {
        return Results.NotFound(new { Error = "EVM trade event not found." });
    }

    evmLedger.MarkConfirmed(tradeEvent);
    return Results.Ok(tradeEvent);
});

app.MapGet("/api/facility-applications", (IPortfolioRepository portfolio) =>
    Results.Ok(portfolio.FacilityApplications));

app.MapPost("/api/facility-applications", (FacilityApplication application, IPortfolioRepository portfolio, IFacilityApplicationService applications, IIntegrationOutboxService outbox) =>
{
    var submitted = applications.Submit(portfolio, application);
    outbox.Publish("FacilityApplication.Submitted", submitted.ApplicationNumber, $"{submitted.LegalName} requested {submitted.RequestedLimit:N2}.", "Dynamics 365");
    return Results.Ok(submitted);
});

app.MapPost("/api/facility-applications/review/{applicationNumber}", (string applicationNumber, IPortfolioRepository portfolio, IFacilityApplicationService applications, IIntegrationOutboxService outbox) =>
{
    var application = portfolio.FacilityApplications.FirstOrDefault(item => item.ApplicationNumber.Equals(applicationNumber, StringComparison.OrdinalIgnoreCase));
    if (application is null)
    {
        return Results.NotFound(new { Error = "Application not found." });
    }

    applications.MoveToReview(application, "API");
    outbox.Publish("FacilityApplication.InReview", application.ApplicationNumber, $"{application.LegalName} moved to review.", "Microsoft Teams");
    return Results.Ok(application);
});

app.MapPost("/api/facility-applications/approve/{applicationNumber}", (string applicationNumber, IPortfolioRepository portfolio, IFacilityApplicationService applications, IIntegrationOutboxService outbox) =>
{
    var application = portfolio.FacilityApplications.FirstOrDefault(item => item.ApplicationNumber.Equals(applicationNumber, StringComparison.OrdinalIgnoreCase));
    if (application is null)
    {
        return Results.NotFound(new { Error = "Application not found." });
    }

    applications.Approve(portfolio, application, "API");
    outbox.Publish("FacilityApplication.Approved", application.ApplicationNumber, $"{application.LegalName} approved for {application.ApprovedLimit:N2}.", "Dynamics 365");
    return Results.Ok(application);
});

app.MapPost("/api/facility-applications/decline/{applicationNumber}", (string applicationNumber, IPortfolioRepository portfolio, IFacilityApplicationService applications, IIntegrationOutboxService outbox) =>
{
    var application = portfolio.FacilityApplications.FirstOrDefault(item => item.ApplicationNumber.Equals(applicationNumber, StringComparison.OrdinalIgnoreCase));
    if (application is null)
    {
        return Results.NotFound(new { Error = "Application not found." });
    }

    applications.Decline(application, "API");
    outbox.Publish("FacilityApplication.Declined", application.ApplicationNumber, $"{application.LegalName} declined.", "Dynamics 365");
    return Results.Ok(application);
});

app.MapPost("/api/client-offers/create/{clientName}", (string clientName, IPortfolioRepository portfolio, IClientOfferService offers, IIntegrationOutboxService outbox) =>
{
    var offer = offers.CreateOffer(portfolio, Uri.UnescapeDataString(clientName), "API");
    if (offer is null)
    {
        return Results.BadRequest(new { Error = "No eligible submitted, review or approved invoices for this client." });
    }

    outbox.Publish("ClientOffer.Created", offer.OfferNumber, $"{offer.ClientName} offer created for {offer.InvoiceCount} invoice(s).", "Dynamics 365");
    return Results.Ok(offer);
});

app.MapPost("/api/client-offers/accept/{offerNumber}", (string offerNumber, IPortfolioRepository portfolio, IClientOfferService offers, IEvmLedgerService evmLedger, IIntegrationOutboxService outbox) =>
{
    var offer = portfolio.ClientOffers.FirstOrDefault(item => item.OfferNumber.Equals(offerNumber, StringComparison.OrdinalIgnoreCase));
    if (offer is null)
    {
        return Results.NotFound(new { Error = "Offer not found." });
    }

    offers.Accept(portfolio, offer, "Client portal");
    var evmEvents = evmLedger.RecordOfferAcceptance(portfolio, offer, "Client portal");
    outbox.Publish("ClientOffer.Accepted", offer.OfferNumber, $"{offer.ClientName} accepted {offer.NetCash:N2} net cash offer.", "Dynamics 365");
    outbox.Publish("Evm.SellReceivable", offer.OfferNumber, $"{evmEvents.Count} sell transaction(s) submitted to EVM.", "EVM RPC");
    return Results.Ok(offer);
});

app.MapPost("/api/client-offers/decline/{offerNumber}", (string offerNumber, IPortfolioRepository portfolio, IClientOfferService offers, IIntegrationOutboxService outbox) =>
{
    var offer = portfolio.ClientOffers.FirstOrDefault(item => item.OfferNumber.Equals(offerNumber, StringComparison.OrdinalIgnoreCase));
    if (offer is null)
    {
        return Results.NotFound(new { Error = "Offer not found." });
    }

    offers.Decline(offer, "Client portal");
    outbox.Publish("ClientOffer.Declined", offer.OfferNumber, $"{offer.ClientName} declined offer.", "Dynamics 365");
    return Results.Ok(offer);
});

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
