CREATE TABLE Clients (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Clients PRIMARY KEY,
    Name nvarchar(200) NOT NULL,
    Industry nvarchar(160) NOT NULL,
    Country nvarchar(80) NOT NULL,
    FacilityLimit decimal(18,2) NOT NULL,
    ConcentrationLimitPercent decimal(9,2) NOT NULL,
    KycStatus nvarchar(40) NOT NULL,
    BuyDecision nvarchar(40) NOT NULL,
    AccountManager nvarchar(160) NOT NULL
);

CREATE TABLE Debtors (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Debtors PRIMARY KEY,
    Name nvarchar(200) NOT NULL,
    Sector nvarchar(160) NOT NULL,
    Country nvarchar(80) NOT NULL,
    CreditLimit decimal(18,2) NOT NULL,
    Rating nvarchar(10) NOT NULL,
    BuyDecision nvarchar(40) NOT NULL,
    AverageDaysToPay int NOT NULL,
    DilutionPercent decimal(9,2) NOT NULL
);

CREATE TABLE FactoringTerms (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_FactoringTerms PRIMARY KEY,
    Currency nvarchar(10) NOT NULL,
    AdvanceRate decimal(9,2) NOT NULL,
    DiscountRatePer30Days decimal(9,2) NOT NULL,
    ServiceFeeRate decimal(9,2) NOT NULL,
    MinimumFee decimal(18,2) NOT NULL,
    BankApr decimal(9,2) NOT NULL,
    IsNonRecourse bit NOT NULL,
    MinimumScore int NOT NULL,
    MaximumTenorDays int NOT NULL,
    LongTenorReviewDays int NOT NULL,
    ConcentrationWarningPercent decimal(9,2) NOT NULL,
    FacilityWarningUtilizationPercent decimal(9,2) NOT NULL,
    CreditWarningUtilizationPercent decimal(9,2) NOT NULL,
    HighDilutionPercent decimal(9,2) NOT NULL,
    SlowPaymentDays int NOT NULL,
    IsActive bit NOT NULL CONSTRAINT DF_FactoringTerms_IsActive DEFAULT 1,
    UpdatedAt datetime2 NOT NULL CONSTRAINT DF_FactoringTerms_UpdatedAt DEFAULT SYSUTCDATETIME()
);

CREATE TABLE Invoices (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Invoices PRIMARY KEY,
    ClientName nvarchar(200) NOT NULL,
    Debtor nvarchar(200) NOT NULL,
    InvoiceNumber nvarchar(80) NOT NULL CONSTRAINT UQ_Invoices_InvoiceNumber UNIQUE,
    Amount decimal(18,2) NOT NULL,
    DaysToPay int NOT NULL,
    Rating nvarchar(10) NOT NULL,
    ConcentrationPercent decimal(9,2) NOT NULL,
    Status nvarchar(40) NOT NULL,
    FundingStage nvarchar(40) NOT NULL
);

CREATE TABLE DocumentRequirements (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_DocumentRequirements PRIMARY KEY,
    InvoiceNumber nvarchar(80) NOT NULL,
    Kind nvarchar(80) NOT NULL,
    Status nvarchar(40) NOT NULL,
    IsRequired bit NOT NULL,
    FileName nvarchar(260) NOT NULL,
    ReviewerNote nvarchar(1000) NOT NULL,
    ReceivedAt datetime2 NULL,
    CONSTRAINT FK_DocumentRequirements_Invoices FOREIGN KEY (InvoiceNumber) REFERENCES Invoices(InvoiceNumber)
);

CREATE TABLE UnderwritingDecisions (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_UnderwritingDecisions PRIMARY KEY,
    InvoiceNumber nvarchar(80) NOT NULL,
    Status nvarchar(60) NOT NULL,
    AssignedTo nvarchar(160) NOT NULL,
    DecisionNote nvarchar(2000) NOT NULL,
    Conditions nvarchar(2000) NOT NULL,
    IsManualOverride bit NOT NULL,
    DecidedAt datetime2 NULL,
    CONSTRAINT FK_UnderwritingDecisions_Invoices FOREIGN KEY (InvoiceNumber) REFERENCES Invoices(InvoiceNumber)
);

CREATE TABLE CollectionCases (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_CollectionCases PRIMARY KEY,
    InvoiceNumber nvarchar(80) NOT NULL,
    DueDate datetime2 NOT NULL,
    Status nvarchar(60) NOT NULL,
    PromiseToPayDate datetime2 NULL,
    AmountPaid decimal(18,2) NOT NULL,
    ChargebackAmount decimal(18,2) NOT NULL,
    ReserveReleased decimal(18,2) NOT NULL,
    Owner nvarchar(160) NOT NULL,
    NextAction nvarchar(1000) NOT NULL,
    CONSTRAINT FK_CollectionCases_Invoices FOREIGN KEY (InvoiceNumber) REFERENCES Invoices(InvoiceNumber)
);

CREATE TABLE CollectionActivities (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_CollectionActivities PRIMARY KEY,
    InvoiceNumber nvarchar(80) NOT NULL,
    OccurredAt datetime2 NOT NULL,
    ActivityType nvarchar(80) NOT NULL,
    ContactName nvarchar(200) NOT NULL,
    Note nvarchar(2000) NOT NULL,
    CONSTRAINT FK_CollectionActivities_Invoices FOREIGN KEY (InvoiceNumber) REFERENCES Invoices(InvoiceNumber)
);

CREATE TABLE PaymentMatches (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_PaymentMatches PRIMARY KEY,
    ReceivedAt datetime2 NOT NULL,
    Reference nvarchar(160) NOT NULL,
    InvoiceNumber nvarchar(80) NOT NULL,
    Debtor nvarchar(200) NOT NULL,
    Amount decimal(18,2) NOT NULL,
    Currency nvarchar(10) NOT NULL,
    Status nvarchar(40) NOT NULL,
    Note nvarchar(1000) NOT NULL
);

CREATE TABLE DisputeCases (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_DisputeCases PRIMARY KEY,
    CaseNumber nvarchar(80) NOT NULL CONSTRAINT UQ_DisputeCases_CaseNumber UNIQUE,
    InvoiceNumber nvarchar(80) NOT NULL,
    OpenedAt datetime2 NOT NULL,
    Status nvarchar(60) NOT NULL,
    DisputedAmount decimal(18,2) NOT NULL,
    CreditNoteAmount decimal(18,2) NOT NULL,
    Reason nvarchar(1000) NOT NULL,
    Owner nvarchar(160) NOT NULL,
    NextAction nvarchar(1000) NOT NULL,
    ResolvedAt datetime2 NULL,
    CONSTRAINT FK_DisputeCases_Invoices FOREIGN KEY (InvoiceNumber) REFERENCES Invoices(InvoiceNumber)
);

CREATE TABLE DebtorConfirmationRequests (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_DebtorConfirmationRequests PRIMARY KEY,
    RequestNumber nvarchar(80) NOT NULL CONSTRAINT UQ_DebtorConfirmationRequests_RequestNumber UNIQUE,
    InvoiceNumber nvarchar(80) NOT NULL,
    Debtor nvarchar(200) NOT NULL,
    CreatedAt datetime2 NOT NULL,
    SentAt datetime2 NULL,
    DueAt datetime2 NOT NULL,
    Status nvarchar(60) NOT NULL,
    SentBy nvarchar(160) NOT NULL,
    ResponseNote nvarchar(2000) NOT NULL,
    RespondedAt datetime2 NULL,
    CONSTRAINT FK_DebtorConfirmationRequests_Invoices FOREIGN KEY (InvoiceNumber) REFERENCES Invoices(InvoiceNumber)
);

CREATE TABLE LedgerEntries (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_LedgerEntries PRIMARY KEY,
    PostedAt datetime2 NOT NULL,
    InvoiceNumber nvarchar(80) NOT NULL,
    ClientName nvarchar(200) NOT NULL,
    Debtor nvarchar(200) NOT NULL,
    EntryType nvarchar(80) NOT NULL,
    Debit decimal(18,2) NOT NULL,
    Credit decimal(18,2) NOT NULL,
    Currency nvarchar(10) NOT NULL,
    Note nvarchar(1000) NOT NULL,
    CONSTRAINT FK_LedgerEntries_Invoices FOREIGN KEY (InvoiceNumber) REFERENCES Invoices(InvoiceNumber)
);

CREATE TABLE FundingBatches (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_FundingBatches PRIMARY KEY,
    BatchNumber nvarchar(80) NOT NULL CONSTRAINT UQ_FundingBatches_BatchNumber UNIQUE,
    CreatedAt datetime2 NOT NULL,
    CreatedBy nvarchar(160) NOT NULL,
    InvoiceCount int NOT NULL,
    GrossReceivables decimal(18,2) NOT NULL,
    AdvanceAmount decimal(18,2) NOT NULL,
    EstimatedFees decimal(18,2) NOT NULL,
    ReserveHeld decimal(18,2) NOT NULL,
    NetCash decimal(18,2) NOT NULL,
    Currency nvarchar(10) NOT NULL,
    InvoiceNumbers nvarchar(2000) NOT NULL
);

CREATE TABLE BorrowingBaseSnapshots (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_BorrowingBaseSnapshots PRIMARY KEY,
    CapturedAt datetime2 NOT NULL CONSTRAINT DF_BorrowingBaseSnapshots_CapturedAt DEFAULT SYSUTCDATETIME(),
    ClientName nvarchar(200) NOT NULL,
    FacilityLimit decimal(18,2) NOT NULL,
    GrossReceivables decimal(18,2) NOT NULL,
    EligibleReceivables decimal(18,2) NOT NULL,
    IneligibleReceivables decimal(18,2) NOT NULL,
    ConcentrationExcess decimal(18,2) NOT NULL,
    DebtorLimitExcess decimal(18,2) NOT NULL,
    BorrowingBase decimal(18,2) NOT NULL,
    MaxAdvance decimal(18,2) NOT NULL,
    ExistingAdvance decimal(18,2) NOT NULL,
    Availability decimal(18,2) NOT NULL,
    Status nvarchar(80) NOT NULL
);

CREATE TABLE AuditEvents (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_AuditEvents PRIMARY KEY,
    OccurredAt datetime2 NOT NULL,
    Actor nvarchar(160) NOT NULL,
    Entity nvarchar(160) NOT NULL,
    Action nvarchar(120) NOT NULL,
    Detail nvarchar(2000) NOT NULL
);

CREATE TABLE ActionItems (
    Id nvarchar(160) NOT NULL CONSTRAINT PK_ActionItems PRIMARY KEY,
    Title nvarchar(240) NOT NULL,
    Detail nvarchar(2000) NOT NULL,
    RelatedEntity nvarchar(160) NOT NULL,
    OwnerRole nvarchar(40) NOT NULL,
    Priority nvarchar(40) NOT NULL,
    Category nvarchar(60) NOT NULL,
    DueAt datetime2 NOT NULL,
    CompletedAt datetime2 NULL,
    CompletedBy nvarchar(160) NULL
);

CREATE TABLE IntegrationEvents (
    Id nvarchar(64) NOT NULL CONSTRAINT PK_IntegrationEvents PRIMARY KEY,
    OccurredAt datetime2 NOT NULL,
    EventType nvarchar(160) NOT NULL,
    Entity nvarchar(160) NOT NULL,
    Summary nvarchar(2000) NOT NULL,
    Target nvarchar(160) NOT NULL,
    Status nvarchar(40) NOT NULL,
    Attempts int NOT NULL
);

CREATE TABLE GeneratedTemplates (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_GeneratedTemplates PRIMARY KEY,
    GeneratedAt datetime2 NOT NULL CONSTRAINT DF_GeneratedTemplates_GeneratedAt DEFAULT SYSUTCDATETIME(),
    TemplateKind nvarchar(80) NOT NULL,
    InvoiceNumber nvarchar(80) NOT NULL,
    FileName nvarchar(260) NOT NULL,
    GeneratedBy nvarchar(160) NOT NULL,
    CONSTRAINT FK_GeneratedTemplates_Invoices FOREIGN KEY (InvoiceNumber) REFERENCES Invoices(InvoiceNumber)
);

CREATE TABLE CovenantSnapshots (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_CovenantSnapshots PRIMARY KEY,
    CapturedAt datetime2 NOT NULL CONSTRAINT DF_CovenantSnapshots_CapturedAt DEFAULT SYSUTCDATETIME(),
    Name nvarchar(160) NOT NULL,
    Scope nvarchar(160) NOT NULL,
    Status nvarchar(40) NOT NULL,
    Metric nvarchar(160) NOT NULL,
    Threshold nvarchar(160) NOT NULL,
    Action nvarchar(1000) NOT NULL
);

CREATE TABLE FraudSignalSnapshots (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_FraudSignalSnapshots PRIMARY KEY,
    CapturedAt datetime2 NOT NULL CONSTRAINT DF_FraudSignalSnapshots_CapturedAt DEFAULT SYSUTCDATETIME(),
    SignalType nvarchar(160) NOT NULL,
    Entity nvarchar(160) NOT NULL,
    Severity nvarchar(40) NOT NULL,
    Detail nvarchar(2000) NOT NULL,
    RecommendedAction nvarchar(1000) NOT NULL
);

CREATE INDEX IX_Invoices_ClientName ON Invoices(ClientName);
CREATE INDEX IX_Invoices_Debtor ON Invoices(Debtor);
CREATE INDEX IX_DocumentRequirements_InvoiceNumber ON DocumentRequirements(InvoiceNumber);
CREATE INDEX IX_CollectionCases_Status ON CollectionCases(Status);
CREATE INDEX IX_PaymentMatches_Status ON PaymentMatches(Status);
CREATE INDEX IX_PaymentMatches_InvoiceNumber ON PaymentMatches(InvoiceNumber);
CREATE INDEX IX_DisputeCases_Status ON DisputeCases(Status);
CREATE INDEX IX_DisputeCases_InvoiceNumber ON DisputeCases(InvoiceNumber);
CREATE INDEX IX_DebtorConfirmationRequests_Status ON DebtorConfirmationRequests(Status);
CREATE INDEX IX_DebtorConfirmationRequests_InvoiceNumber ON DebtorConfirmationRequests(InvoiceNumber);
CREATE INDEX IX_LedgerEntries_InvoiceNumber ON LedgerEntries(InvoiceNumber);
CREATE INDEX IX_LedgerEntries_PostedAt ON LedgerEntries(PostedAt DESC);
CREATE INDEX IX_FundingBatches_CreatedAt ON FundingBatches(CreatedAt DESC);
CREATE INDEX IX_BorrowingBaseSnapshots_ClientName ON BorrowingBaseSnapshots(ClientName);
CREATE INDEX IX_BorrowingBaseSnapshots_CapturedAt ON BorrowingBaseSnapshots(CapturedAt DESC);
CREATE INDEX IX_AuditEvents_OccurredAt ON AuditEvents(OccurredAt DESC);
CREATE INDEX IX_ActionItems_OwnerRole ON ActionItems(OwnerRole);
CREATE INDEX IX_ActionItems_DueAt ON ActionItems(DueAt);
CREATE INDEX IX_IntegrationEvents_Status ON IntegrationEvents(Status);
CREATE INDEX IX_IntegrationEvents_OccurredAt ON IntegrationEvents(OccurredAt DESC);
CREATE INDEX IX_GeneratedTemplates_InvoiceNumber ON GeneratedTemplates(InvoiceNumber);
CREATE INDEX IX_CovenantSnapshots_Status ON CovenantSnapshots(Status);
CREATE INDEX IX_FraudSignalSnapshots_Severity ON FraudSignalSnapshots(Severity);
