using ILP.Server.Features.InboxMonitoring.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ILP.Server.Features.InboxMonitoring.Persistence.Migrations;

[DbContext(typeof(InboxExtractionDbContext))]
[Migration("20261104180000_AddAccountsPayableReferenceData")]
public sealed class AddAccountsPayableReferenceData : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AccountsPayableAccounts",
            columns: table => new
            {
                Code = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                Name = table.Column<string>(type: "TEXT", nullable: false),
                Type = table.Column<string>(type: "TEXT", nullable: false),
                NormalBalance = table.Column<string>(type: "TEXT", nullable: false),
                ApRole = table.Column<string>(type: "TEXT", nullable: false),
                UseWhen = table.Column<string>(type: "TEXT", nullable: false),
                Enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                Sector = table.Column<string>(type: "TEXT", nullable: false),
                AiSuggestion = table.Column<string>(type: "TEXT", nullable: false),
                Notes = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_AccountsPayableAccounts", account => account.Code));

        migrationBuilder.CreateTable(
            name: "AccountsPayableGstTaxCodes",
            columns: table => new
            {
                Code = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                Description = table.Column<string>(type: "TEXT", nullable: false),
                SuggestedTreatment = table.Column<string>(type: "TEXT", nullable: false),
                InputGstAccount = table.Column<string>(type: "TEXT", nullable: false),
                AutoProcessing = table.Column<string>(type: "TEXT", nullable: false),
                ReviewConditions = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_AccountsPayableGstTaxCodes", code => code.Code));

        migrationBuilder.InsertData(
            table: "AccountsPayableAccounts",
            columns: ["Code", "Name", "Type", "NormalBalance", "ApRole", "UseWhen", "Enabled", "Sector", "AiSuggestion", "Notes"],
            columnTypes: ["TEXT", "TEXT", "TEXT", "TEXT", "TEXT", "TEXT", "INTEGER", "TEXT", "TEXT", "TEXT"],
            values: new object[,]
            {
                { "1000", "Operating Bank Account", "Asset", "Debit", "Payment", "Supplier payment from company bank", true, "All", "Review", "Payment stage only; never use to classify invoice cost" },
                { "1200", "GST Input Tax Recoverable", "Asset", "Debit", "Tax", "Eligible GST-registered business purchase input tax", true, "All", "Conditional", "Only when GST recovery conditions are met" },
                { "1300", "Prepaid Expenses", "Asset", "Debit", "Classification", "Costs relating to future periods e.g. annual subscription", true, "All", "Review", "Expense recognition is separate from invoice approval" },
                { "1400", "Supplier Deposits and Advances", "Asset", "Debit", "Classification", "Refundable supplier deposit or advance payment", true, "All", "Review", "Distinguish advance invoice from taxable supply and final bill" },
                { "1500", "Inventory", "Asset", "Debit", "Classification", "Purchases for stock held on balance sheet", false, "Inventory", "Review", "Enable only if company uses inventory accounting" },
                { "1600", "Property Plant and Equipment", "Asset", "Debit", "Classification", "Capital expenditure that meets company capitalization policy", true, "All", "Review", "Require approval of capitalization and asset register" },
                { "1610", "Capital Work in Progress", "Asset", "Debit", "Classification", "Incomplete qualifying capital project", false, "Project", "Review", "Not routine operating expense" },
                { "2000", "Accounts Payable - Trade", "Liability", "Credit", "AP Control", "Supplier invoice outstanding until payment", true, "All", "Rule", "Single AP GL control account; vendor detail belongs in AP subledger" },
                { "2010", "Other Payables", "Liability", "Credit", "Liability", "Approved non-trade liability", true, "All", "Review", "Not an alternative to AP Trade for ordinary vendor bills" },
                { "2020", "Accrued Expenses", "Liability", "Credit", "Accrual", "Service incurred but invoice not yet received", true, "All", "Review", "Never generate solely from a supplier SOA" },
                { "2030", "Goods Received Not Invoiced (GRNI)", "Liability", "Credit", "Accrual", "Goods received before invoice; managed with PO/GRN", false, "Inventory/Project", "Review", "Reconcile or reverse when invoice is posted" },
                { "2040", "Withholding Tax Payable", "Liability", "Credit", "Tax", "Applicable withholding tax determined by tax review", true, "All", "Review", "Do not apply automatically from overseas vendor address" },
                { "2050", "Retention Payable", "Liability", "Credit", "Liability", "Contractual retention owed to subcontractor", false, "Project", "Review", "Require contract term and due date" },
                { "2060", "GST Settlement / Clearing", "Liability", "Credit", "Tax", "GST net balance settlement if accounting structure requires", false, "All", "Review", "Use according to company ledger design; not expense account" },
                { "5000", "General Direct Purchases", "Cost of Sales", "Debit", "Direct Cost", "Direct cost not more accurately classified below", false, "Trading/Project", "Review", "Avoid when specific direct-material or subcontract account fits" },
                { "5010", "Direct Materials / Project Materials", "Cost of Sales", "Debit", "Direct Cost", "Materials consumed for a customer project", false, "Project", "Rule", "Use inventory first where stock capitalization is required" },
                { "5020", "Subcontractor Costs", "Cost of Sales", "Debit", "Direct Cost", "Project subcontracted works / outsourced installation", false, "Project", "Rule", "May involve WHT or retention requiring review" },
                { "5030", "Direct Equipment Rental", "Cost of Sales", "Debit", "Direct Cost", "Equipment rented for a customer project", false, "Project", "Rule", "Separate from office equipment rental" },
                { "5040", "Direct Freight and Delivery", "Cost of Sales", "Debit", "Direct Cost", "Freight attributable to goods or customer project", false, "Project", "Rule", "Capitalizable delivery costs may belong with inventory or asset" },
                { "5050", "Project Site Expenses", "Cost of Sales", "Debit", "Direct Cost", "Project-specific site expense", false, "Project", "Review", "Require project / cost centre" },
                { "6000", "Office Supplies", "Expense", "Debit", "Expense", "Stationery printer paper office consumables", true, "All", "Rule", "Exclude items requiring inventory or asset capitalization" },
                { "6010", "Office Rental", "Expense", "Debit", "Expense", "Office space rental", true, "All", "Rule", "Prepaid periods may be coded to 1300" },
                { "6020", "Utilities", "Expense", "Debit", "Expense", "Electricity and water services", true, "All", "Rule", "Split project-site utility if needed" },
                { "6030", "Telephone and Internet", "Expense", "Debit", "Expense", "Telecom mobile and internet bills", true, "All", "Rule", "" },
                { "6040", "Software Subscriptions", "Expense", "Debit", "Expense", "SaaS Xero Microsoft 365 AI subscriptions", true, "All", "Rule", "Annual subscription may be prepaid" },
                { "6050", "Repairs and Maintenance", "Expense", "Debit", "Expense", "Routine repair services and upkeep", true, "All", "Rule", "Enhancement/capital expenditure requires review" },
                { "6060", "Professional Fees", "Expense", "Debit", "Expense", "Legal audit accounting and consultancy fees", true, "All", "Rule", "Consider cross-border withholding tax" },
                { "6070", "Transport and Delivery", "Expense", "Debit", "Expense", "Operating courier and local delivery", true, "All", "Rule", "Not direct freight to project or inventory" },
                { "6080", "Staff Welfare", "Expense", "Debit", "Expense", "Staff welfare meals and amenities", true, "All", "Rule", "GST recovery may be restricted; review tax code" },
                { "6090", "Insurance Expenses", "Expense", "Debit", "Expense", "General business insurance", true, "All", "Rule", "Review prepaid coverage periods" },
                { "6100", "Advertising and Marketing", "Expense", "Debit", "Expense", "Ads printing and digital marketing", true, "All", "Rule", "" },
                { "6110", "Bank Charges", "Expense", "Debit", "Expense", "Service charges and transfer fees", true, "All", "Rule", "Often supported by bank statement rather than supplier tax invoice" },
                { "6120", "Training Expenses", "Expense", "Debit", "Expense", "Training courses and staff development", true, "All", "Rule", "" },
                { "6130", "General Expenses - Review Only", "Expense", "Debit", "Expense", "Unclassified small operating expense", true, "All", "Review", "Never auto-approve; require human coding" },
                { "6140", "Business Travel", "Expense", "Debit", "Expense", "Business flights and accommodations", true, "All", "Rule", "Tax treatment and supporting documents need checking" },
                { "6150", "Entertainment", "Expense", "Debit", "Expense", "Client-related meals and entertainment", true, "All", "Review", "Check policy and input GST restrictions" },
                { "6160", "Licences and Permits", "Expense", "Debit", "Expense", "Regulatory licences permit and registration charges", true, "All", "Rule", "" },
                { "6170", "Cleaning and Security", "Expense", "Debit", "Expense", "Office cleaning and security services", true, "All", "Rule", "" }
            });

        migrationBuilder.InsertData(
            table: "AccountsPayableGstTaxCodes",
            columns: ["Code", "Description", "SuggestedTreatment", "InputGstAccount", "AutoProcessing", "ReviewConditions"],
            columnTypes: ["TEXT", "TEXT", "TEXT", "TEXT", "TEXT", "TEXT"],
            values: new object[,]
            {
                { "SG_STD_9", "Local standard-rated purchase, 9% GST", "Dr eligible cost net + Dr 1200 GST; Cr 2000 gross", "1200", "Conditional", "Check company GST status, valid invoice, recoverability, correct time of supply, allowed input tax" },
                { "SG_ZERO", "Supplier zero-rated taxable supply", "Dr eligible cost gross; Cr 2000 gross", "None", "Conditional", "Confirm zero-rate shown and that invoice supports treatment" },
                { "SG_EXEMPT", "Exempt supply on supplier invoice", "Dr eligible cost gross; Cr 2000 gross", "None", "Conditional", "Exempt supplier output does not create recoverable input GST" },
                { "SG_NO_GST", "No GST charged / supplier non-GST registered", "Dr eligible cost gross; Cr 2000 gross", "None", "Conditional", "Never manufacture a GST 9% amount" },
                { "SG_BLOCKED", "GST charged but input tax not claimable", "Dr eligible cost gross incl nonrecoverable GST; Cr 2000 gross", "None", "Review", "Check relevant IRAS blocked input tax criteria" },
                { "SG_OUT", "Out-of-scope purchase/charge", "Dr eligible cost gross; Cr 2000 gross", "None", "Review", "Confirm true classification; out-of-scope not interchangeable with zero-rated" },
                { "SG_IMPORT_REVIEW", "Import / overseas service / reverse-charge potential", "Stop tax auto-post and route for tax review", "TBD", "No", "Import GST, reverse charge, overseas vendor, foreign currency, WHT as applicable" },
                { "SG_UNKNOWN", "GST classification not supported by evidence", "Stop and request tax review", "None", "No", "Missing tax invoice, mismatch, ambiguous tax treatment" }
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AccountsPayableGstTaxCodes");
        migrationBuilder.DropTable(name: "AccountsPayableAccounts");
    }
}
