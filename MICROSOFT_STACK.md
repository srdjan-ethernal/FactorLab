# FactorLab Microsoft Stack

The full system design is maintained in [ARCHITECTURE.md](ARCHITECTURE.md).

## Current Stack

- Web app: ASP.NET Core / Blazor Server
- Domain and business logic: C#
- Data access boundary: repository interface plus conditional EF Core setup
- Current data source: in-memory sample portfolio
- Production database target: Azure SQL Database
- Auth target: Microsoft Entra ID / Entra External ID
- File storage target: Azure Blob Storage
- Document extraction target: Azure AI Document Intelligence
- AI target: Azure OpenAI for risk memos and underwriting summaries
- Reporting target: CSV exports now, Power BI datasets later
- Integrations target: Power Automate, Teams, Outlook, Dynamics 365
- Blockchain target: EVM RPC and smart contract adapter for receivable buy/sell events
- Hosting target: Azure App Service first, Azure Container Apps later if needed
- Background work target: Azure Functions or hosted workers
- Secrets/config target: Azure Key Vault
- Observability target: Application Insights

## Important Architecture Decisions

- Keep the product local-first until workflow quality is high.
- Put replaceable infrastructure behind interfaces.
- Keep the factoring workflow in one deployable app before splitting services.
- Record every receivable buy/sell action through the EVM ledger service.
- Use the integration outbox as the handoff point to Microsoft and blockchain systems.

## Near-Term Engineering Priorities

1. Complete `EvmRpcTradeSubmitter` with a real signer and deployed `FactorLabReceivables` contract.
2. Add background confirmation worker for submitted EVM transactions.
3. Complete the SQL Server persistence path by running EF migrations, deploying the schema, and enabling `Persistence:Mode = SqlServer`.
4. Add Entra authentication and role policies.
5. Replace local document storage with Azure Blob Storage.
6. Replace local document extraction with Azure AI Document Intelligence.
7. Replace local risk memo generation with Azure OpenAI.
