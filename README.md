# FactorLab

FactorLab is an invoice factoring workspace for analysing receivables, pricing an indicative offer, and managing the first funding workflow.

The repository now has two layers:

- `index.html`: the original static prototype.
- `FactorLab.sln`: the Microsoft-stack production foundation built with ASP.NET Core / Blazor Server.

## Run the .NET app

```powershell
dotnet restore FactorLab.sln --configfile NuGet.Config
dotnet build FactorLab.sln --no-restore
dotnet run --project src\FactorLab.Web\FactorLab.Web.csproj
```

EF Core package references are configured behind `EnableEfCore=true` so the default build stays stable if NuGet is unavailable. When NuGet access is working:

```powershell
dotnet restore FactorLab.sln --configfile NuGet.Config /p:EnableEfCore=true
dotnet build FactorLab.sln --no-restore /p:EnableEfCore=true
```

The default development URLs are:

- `https://localhost:7192`
- `http://localhost:5054`

## Current features

- Editable invoice book with debtor, amount, tenor, rating, concentration, and status.
- Facility application intake with requested limit, turnover, debtor diversification, trading history, risk score and approval workflow.
- Bulk CSV invoice import with add/update behavior, audit trail and integration event.
- Eligibility scoring with per-invoice funding reasons and automatic exclusion for disputed or high-risk receivables.
- Funding workflow stages from draft through settlement.
- Funding batch creation for approved invoices, including gross receivables, advance, fees, reserve and net cash.
- Funding ledger for advances, fees, reserve held, debtor payments, reserve release and chargebacks.
- Client offer portal workflow with offer creation, send, acceptance, decline, expiry, portal token, dedicated `/portal/{token}` page and transparent fee metrics.
- Document checklist for each invoice, including invoice, PO/contract, proof of delivery, debtor confirmation, KYC, and notice of assignment.
- Document readiness metrics and reviewer notes.
- Local document upload storage under `App_Data/Uploads`, behind a storage service that can be swapped for Azure Blob Storage.
- Client profiles with facility limits, KYC status, concentration limits and buy/watch/no-buy status.
- Automatic client profile creation or refresh when a facility application is approved.
- Debtor profiles with credit limits, payment history, dilution and buy/watch/no-buy status.
- Exposure tracking against client facilities and debtor credit limits.
- Borrowing base and availability calculation by client after eligibility, concentration, debtor limits and existing advances.
- Action center with prioritized work items for documents, KYC, underwriting and collections.
- Portfolio covenant monitoring for document readiness, facility utilization, debtor limits, concentration, KYC and collections exceptions.
- Fraud and anomaly detection for duplicates, lookalike invoices, unknown debtors, missing confirmations, adverse statuses and manual overrides.
- Underwriting approval desk with approve, conditional approval, decline and manual override actions.
- Audit trail for scoring, decisions and invoice creation.
- Collections pipeline with aging buckets, debtor contact actions, promise-to-pay, payment matching, chargeback and reserve release.
- Payment reconciliation CSV import for matched, partial and unmatched remittances.
- Dispute and dilution management for open disputes, credit notes, resolution and chargeback paths.
- Persistence foundation with repository contract, SQL Server schema, connection string and conditional EF Core setup.
- SQL schema coverage for terms/risk policy, action items, integration events, generated templates and covenant snapshots.
- Auth-ready user and role model with Client, Underwriter, Operations and Admin personas.
- Role-based UI actions for funding submission, underwriting and collections operations.
- AI-ready risk memo panel with local deterministic recommendation, strengths, risks and conditions.
- Document intelligence abstraction with local metadata extraction, ready to swap for Azure AI Document Intelligence.
- Reporting/export center with portfolio, exposure, applications, collections, underwriting, ledger, borrowing-base, payments, disputes, confirmations and fraud CSV reports.
- Debtor confirmation workflow with sent/confirmed/disputed responses and automatic document readiness updates.
- Integration outbox for Microsoft Teams, Outlook, Power Automate, Power BI and Azure AI Document Intelligence events.
- Template center for funding offers, notices of assignment, debtor confirmations and collection reminders.
- Deal controls for advance rate, discount fee, service fee, minimum fee, bank APR, and recourse model.
- Configurable risk policy for score threshold, tenor, concentration, limit utilization, dilution and payment-speed warnings.
- Cash-today, reserve, total-cost, effective-APR, and portfolio-quality metrics.
- Cash-flow chart and underwriting signal list.
- Funding pipeline summary.
- CSV import and export.
- Local browser storage.
- Blazor Server production foundation with C# domain model and calculator service.

## Internal API endpoints

- `GET /api/portfolio/summary`
- `GET /api/reports/{portfolio|exposure|collections|underwriting}`
- `GET /api/action-items`
- `GET /api/integration-events`
- `GET /api/facility-applications`
- `POST /api/facility-applications`
- `POST /api/facility-applications/review/{applicationNumber}`
- `POST /api/facility-applications/approve/{applicationNumber}`
- `POST /api/facility-applications/decline/{applicationNumber}`
- `GET /api/templates/{fundingOffer|noticeOfAssignment|debtorConfirmation|collectionReminder}/{invoiceNumber}`
- `GET /api/covenants`
- `POST /api/invoices/import`
- `GET /api/ledger`
- `GET /api/borrowing-base`
- `POST /api/funding-batches/create`
- `POST /api/payments/reconcile`
- `POST /api/disputes/open/{invoiceNumber}`
- `POST /api/disputes/resolve/{caseNumber}`
- `POST /api/debtor-confirmations/send/{invoiceNumber}`
- `POST /api/debtor-confirmations/confirm/{requestNumber}`
- `POST /api/debtor-confirmations/dispute/{requestNumber}`
- `GET /api/fraud-signals`
- `GET /api/client-offers`
- `POST /api/client-offers/create/{clientName}`
- `POST /api/client-offers/accept/{offerNumber}`
- `POST /api/client-offers/decline/{offerNumber}`

## CSV columns

Accepted columns are:

```csv
debtor,invoice,amount,days,rating,concentration,status,fundingStage
```

`status` can be `clean`, `unverified`, `disputed`, or `overdue`.

`fundingStage` can be `draft`, `submitted`, `review`, `approved`, `funded`, `collected`, or `settled`.

Sample import file: `samples/invoices-import-sample.csv`.
Sample payment reconciliation file: `samples/payments-reconciliation-sample.csv`.
