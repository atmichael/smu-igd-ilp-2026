import {
  Document,
  Paragraph,
  TextRun,
  HeadingLevel,
  Table,
  TableRow,
  TableCell,
  AlignmentType,
  WidthType,
  BorderStyle,
  Header,
  Footer,
  PageNumber,
  Packer,
} from 'docx';

export function createTddDocument(): Document {
  const borderNone = {
    top: { style: BorderStyle.NONE, size: 0, color: 'auto' },
    bottom: { style: BorderStyle.NONE, size: 0, color: 'auto' },
    left: { style: BorderStyle.NONE, size: 0, color: 'auto' },
    right: { style: BorderStyle.NONE, size: 0, color: 'auto' },
  };

  const tableBorderLight = {
    top: { style: BorderStyle.SINGLE, size: 1, color: 'CCCCCC' },
    bottom: { style: BorderStyle.SINGLE, size: 1, color: 'CCCCCC' },
    left: { style: BorderStyle.SINGLE, size: 1, color: 'CCCCCC' },
    right: { style: BorderStyle.SINGLE, size: 1, color: 'CCCCCC' },
    insideHorizontal: { style: BorderStyle.SINGLE, size: 1, color: 'E2E8F0' },
    insideVertical: { style: BorderStyle.SINGLE, size: 1, color: 'E2E8F0' },
  };

  const createCell = (text: string, isHeader = false, widthPercent = 25) => {
    return new TableCell({
      width: { size: widthPercent, type: WidthType.PERCENTAGE },
      shading: isHeader ? { fill: '0F172A' } : undefined,
      children: [
        new Paragraph({
          children: [
            new TextRun({
              text,
              bold: isHeader,
              color: isHeader ? 'FFFFFF' : '334155',
              size: isHeader ? 20 : 18,
            }),
          ],
        }),
      ],
    });
  };

  return new Document({
    sections: [
      {
        properties: {
          page: {
            margin: {
              top: 1440, // 1 inch
              bottom: 1440,
              left: 1440,
              right: 1440,
            },
          },
        },
        headers: {
          default: new Header({
            children: [
              new Paragraph({
                alignment: AlignmentType.RIGHT,
                children: [
                  new TextRun({
                    text: 'PaySG AI — Technical Design Document (Confidential)',
                    size: 16,
                    color: '94A3B8',
                  }),
                ],
              }),
            ],
          }),
        },
        footers: {
          default: new Footer({
            children: [
              new Paragraph({
                alignment: AlignmentType.RIGHT,
                children: [
                  new TextRun({ text: 'Page ', size: 16, color: '94A3B8' }),
                  new TextRun({
                    children: [PageNumber.CURRENT],
                    size: 16,
                    color: '94A3B8',
                  }),
                  new TextRun({ text: ' of ', size: 16, color: '94A3B8' }),
                  new TextRun({
                    children: [PageNumber.TOTAL_PAGES],
                    size: 16,
                    color: '94A3B8',
                  }),
                ],
              }),
            ],
          }),
        },
        children: [
          // Document Title
          new Paragraph({
            text: 'PaySG AI — Accounts Payable (AP) Automation Platform',
            heading: HeadingLevel.TITLE,
            alignment: AlignmentType.CENTER,
            spacing: { after: 120 },
          }),

          new Paragraph({
            alignment: AlignmentType.CENTER,
            children: [
              new TextRun({
                text: 'Technical Design Document (TDD) — Architecture, AI Engineering & Implementation',
                bold: true,
                size: 24,
                color: '059669', // Emerald
              }),
            ],
            spacing: { after: 360 },
          }),

          // Metadata Table
          new Table({
            width: { size: 100, type: WidthType.PERCENTAGE },
            borders: tableBorderLight,
            rows: [
              new TableRow({
                children: [
                  createCell('Document Version', true, 25),
                  createCell('1.0.0 (Prototype Release)', false, 25),
                  createCell('Date', true, 25),
                  createCell('September 2026', false, 25),
                ],
              }),
              new TableRow({
                children: [
                  createCell('Target Audience', true, 25),
                  createCell('Singapore SME Business Owners & AP Teams', false, 25),
                  createCell('System Status', true, 25),
                  createCell('Ready for Testing & Demonstration', false, 25),
                ],
              }),
              new TableRow({
                children: [
                  createCell('Primary Cloud Services', true, 25),
                  createCell('Google Gemini 3.8 Flash, Firebase Auth, Gmail API', false, 25),
                  createCell('Accounting ERP Sync', true, 25),
                  createCell('Xero Singapore API (ACCPAY Bills)', false, 25),
                ],
              }),
            ],
          }),

          new Paragraph({ spacing: { after: 400 } }),

          // SECTION 1: EXECUTIVE SUMMARY
          new Paragraph({
            text: '1. Executive Summary & Problem Statement',
            heading: HeadingLevel.HEADING_1,
            spacing: { before: 300, after: 180 },
          }),
          new Paragraph({
            children: [
              new TextRun({
                text: 'Singapore Small and Medium Enterprises (SMEs) face acute resource constraints in their back-office accounting operations. Typically staffed by just 1 or 2 Accounts Payable (AP) team members (or frequently handled directly by business owners or office managers), teams are overwhelmed by high volumes of diverse incoming vendor documents. These documents arrive unpredictably via Gmail inbox attachments and physical paper delivery slips, including: Tax Invoices, Statements of Account (SOA), signed Delivery Orders (DO), Goods Receipt Notes (GRN), Service Work Orders, and Credit/Debit Notes.',
              }),
            ],
            spacing: { after: 160 },
          }),
          new Paragraph({
            children: [
              new TextRun({
                text: 'The manual processing overhead creates severe operational bottlenecks:',
              }),
            ],
            spacing: { after: 120 },
          }),
          new Paragraph({
            bullet: { level: 0 },
            children: [
              new TextRun({
                text: 'Manual Data Entry Fatigue: ',
                bold: true,
              }),
              new TextRun({
                text: 'Re-typing vendor names, Singapore UENs, bank account numbers, PayNow proxies, line items, and invoice dates into ERPs like Xero.',
              }),
            ],
          }),
          new Paragraph({
            bullet: { level: 0 },
            children: [
              new TextRun({
                text: 'IRAS 9% GST Compliance Exposure: ',
                bold: true,
              }),
              new TextRun({
                text: 'Failure to verify whether a supplier is actively GST-registered with IRAS, miscalculating input tax, or erroneously attempting to claim GST from Statements of Account rather than valid Tax Invoices.',
              }),
            ],
          }),
          new Paragraph({
            bullet: { level: 0 },
            children: [
              new TextRun({
                text: 'Procurement Overcharges & Missing Deliveries: ',
                bold: true,
              }),
              new TextRun({
                text: 'Paying supplier invoices without cross-checking agreed Purchase Order (PO) contract rates (price inflation) or warehouse Delivery Orders (short deliveries).',
              }),
            ],
          }),
          new Paragraph({
            bullet: { level: 0 },
            children: [
              new TextRun({
                text: 'PayNow Disbursal Delay: ',
                bold: true,
              }),
              new TextRun({
                text: 'Manual copy-pasting of banking details into corporate FAST/PayNow banking portals risking human typographical errors.',
              }),
            ],
            spacing: { after: 200 },
          }),
          new Paragraph({
            children: [
              new TextRun({
                text: 'Solution Objective: ',
                bold: true,
                color: '059669',
              }),
              new TextRun({
                text: 'PaySG AI delivers an autonomous AP processing engine tailored specifically for Singapore SMEs. By connecting seamlessly to the dedicated AP Gmail inbox and providing a rich manual document drag-and-drop portal, the application uses multimodal Gemini 3.8 Flash to extract, classify, IRAS-audit, 3-way match, and sync vendor bills directly into Xero with 1 click.',
              }),
            ],
            spacing: { after: 300 },
          }),

          // SECTION 2: SYSTEM ARCHITECTURE
          new Paragraph({
            text: '2. High-Level Architecture & Technical Stack',
            heading: HeadingLevel.HEADING_1,
            spacing: { before: 300, after: 180 },
          }),
          new Paragraph({
            children: [
              new TextRun({
                text: 'PaySG AI is built on a clean full-stack TypeScript architecture designed for rapid cloud deployment, high OCR fidelity, and strict security compliance.',
              }),
            ],
            spacing: { after: 200 },
          }),

          new Table({
            width: { size: 100, type: WidthType.PERCENTAGE },
            borders: tableBorderLight,
            rows: [
              new TableRow({
                children: [
                  createCell('Layer', true, 20),
                  createCell('Technology', true, 30),
                  createCell('Architectural Role & Description', true, 50),
                ],
              }),
              new TableRow({
                children: [
                  createCell('Frontend Client', false, 20),
                  createCell('React 19, TypeScript, Tailwind CSS v4, Motion, Lucide Icons', false, 30),
                  createCell('Single Page Application (SPA) offering real-time AP workspace, interactive 3-Way Match Studio, Singapore Cashflow Outflow Timeline, and live document detail inspectors.', false, 50),
                ],
              }),
              new TableRow({
                children: [
                  createCell('Document Generation', false, 20),
                  createCell('jsPDF', false, 30),
                  createCell('Client-side binary PDF generator producing authentic IRAS-compliant Singapore tax invoices, delivery orders, and purchase orders for live demo testing.', false, 50),
                ],
              }),
              new TableRow({
                children: [
                  createCell('Backend API Server', false, 20),
                  createCell('Node.js, Express, tsx runtime, Vite Middleware', false, 30),
                  createCell('Secure proxy handling multipart file uploads, Google OAuth token exchange, Gemini multimodal API orchestration, and Xero ERP payload formatting.', false, 50),
                ],
              }),
              new TableRow({
                children: [
                  createCell('AI Foundation Model', false, 20),
                  createCell('Google Gemini 3.8 Flash via @google/genai SDK', false, 30),
                  createCell('Multimodal vision and structured JSON document extractor evaluating layout, line items, mathematical sums, GST tax compliance, and posting verdicts.', false, 50),
                ],
              }),
              new TableRow({
                children: [
                  createCell('Email Ingestion', false, 20),
                  createCell('Google Workspace Gmail API (OAuth 2.0)', false, 30),
                  createCell('Client-side authenticated inbox scanner searching messages with query (invoice OR "tax invoice" OR "credit note" OR payment OR SOA OR DO) has:attachment.', false, 50),
                ],
              }),
              new TableRow({
                children: [
                  createCell('ERP Integration', false, 20),
                  createCell('Xero Accounting ACCPAY Bill API', false, 30),
                  createCell('Direct automated payload mapping (Contacts, Line Items, Account GL Codes 400/429/445/473/493, 9% GST tax types, and draft/approved status).', false, 50),
                ],
              }),
            ],
          }),

          new Paragraph({ spacing: { after: 300 } }),

          // SECTION 3: CORE CAPABILITIES & DATA FLOW
          new Paragraph({
            text: '3. Core Functional Capabilities & Data Flow',
            heading: HeadingLevel.HEADING_1,
            spacing: { before: 300, after: 180 },
          }),

          new Paragraph({
            text: '3.1 Dual Ingestion Pipeline: Gmail OAuth & Manual Drag-and-Drop',
            heading: HeadingLevel.HEADING_2,
            spacing: { before: 180, after: 120 },
          }),
          new Paragraph({
            children: [
              new TextRun({
                text: '1. Gmail Ingestion: ',
                bold: true,
              }),
              new TextRun({
                text: 'The AP staff member signs in with their Google Workspace account via Firebase Authentication (using the authorized scope: https://www.googleapis.com/auth/gmail.readonly). The system automatically queries the AP mailbox for vendor correspondence, downloads invoice attachments, and triggers instant OCR parsing.',
              }),
            ],
            spacing: { after: 100 },
          }),
          new Paragraph({
            children: [
              new TextRun({
                text: '2. Manual Upload Portal: ',
                bold: true,
              }),
              new TextRun({
                text: 'Staff can drag and drop PDF, PNG, JPG, or text documents directly into the portal. The backend encodes binary files into Base64 format and delivers them directly into Gemini 3.8 Flash multimodal vision inputs.',
              }),
            ],
            spacing: { after: 200 },
          }),

          new Paragraph({
            text: '3.2 AI Document Classification & Extraction Specification',
            heading: HeadingLevel.HEADING_2,
            spacing: { before: 180, after: 120 },
          }),
          new Paragraph({
            children: [
              new TextRun({
                text: 'Gemini 3.8 Flash is constrained by a strict JSON schema that outputs deterministic, strongly typed AP documents conforming to the following model:',
              }),
            ],
            spacing: { after: 120 },
          }),
          new Paragraph({
            bullet: { level: 0 },
            children: [
              new TextRun({ text: 'Document Classification: ', bold: true }),
              new TextRun({ text: 'TAX_INVOICE, PROFORMA_INVOICE, CREDIT_NOTE, DEBIT_NOTE, STATEMENT_OF_ACCOUNT, DELIVERY_ORDER, GOODS_RECEIPT, SERVICE_ORDER, or PURCHASE_ORDER.' }),
            ],
          }),
          new Paragraph({
            bullet: { level: 0 },
            children: [
              new TextRun({ text: 'Singapore Vendor Registry: ', bold: true }),
              new TextRun({ text: 'Vendor Name, Unique Entity Number (UEN), IRAS GST Registration Number, billing address, and detected PayNow Corporate UEN / FAST Bank Account.' }),
            ],
          }),
          new Paragraph({
            bullet: { level: 0 },
            children: [
              new TextRun({ text: 'Tax & Amount Integrity: ', bold: true }),
              new TextRun({ text: 'Currency (SGD), Subtotal, 9% GST Amount, Total Payable, and line-item breakdowns (Item Code, Description, Quantity, Unit Price, Line Total, and Suggested Xero GL Code).' }),
            ],
          }),
          new Paragraph({
            bullet: { level: 0 },
            children: [
              new TextRun({ text: 'Posting Verdict Engine: ', bold: true }),
              new TextRun({ text: 'APPROVED (ready for automated ledger post), REQUIRES_ATTENTION (price variance, short delivery, or SOA flag), or CRITICAL_BLOCKER (math mismatch > $1.00 or missing vendor).' }),
            ],
            spacing: { after: 240 },
          }),

          // SECTION 4: 3-WAY MATCHING
          new Paragraph({
            text: '4. The 3-Way Matching Engine: How It Works & PO/GR Requirements',
            heading: HeadingLevel.HEADING_1,
            spacing: { before: 300, after: 180 },
          }),
          new Paragraph({
            children: [
              new TextRun({
                text: 'A common question from SME owners is: ',
                bold: true,
              }),
              new TextRun({
                text: '"How does the 3-way match work, and do I have to upload a PO or GRN for every invoice?"',
                italics: true,
              }),
            ],
            spacing: { after: 120 },
          }),
          new Paragraph({
            children: [
              new TextRun({
                text: 'The 3-way match reconciles three independent records before money leaves the bank account:',
              }),
            ],
            spacing: { after: 120 },
          }),
          new Paragraph({
            bullet: { level: 0 },
            children: [
              new TextRun({ text: 'Record 1: Purchase Order (PO) — ', bold: true }),
              new TextRun({ text: 'What the company authorized to purchase and the agreed contracted unit prices. Protects against rogue or unauthorized procurement.' }),
            ],
          }),
          new Paragraph({
            bullet: { level: 0 },
            children: [
              new TextRun({ text: 'Record 2: Delivery Order (DO) / Goods Receipt (GRN) — ', bold: true }),
              new TextRun({ text: 'What the warehouse or office physically accepted and signed. Protects against paying for damaged or undelivered goods.' }),
            ],
          }),
          new Paragraph({
            bullet: { level: 0 },
            children: [
              new TextRun({ text: 'Record 3: Vendor Tax Invoice — ', bold: true }),
              new TextRun({ text: 'What the supplier actually billed the company for, including Singapore 9% GST.' }),
            ],
            spacing: { after: 160 },
          }),

          new Paragraph({
            text: 'Do You Have to Upload PO or GRN?',
            heading: HeadingLevel.HEADING_2,
            spacing: { before: 180, after: 120 },
          }),
          new Paragraph({
            children: [
              new TextRun({
                text: 'NO, uploading PO and DO is completely flexible and NOT mandatory for every transaction:',
                bold: true,
                color: '059669',
              }),
            ],
            spacing: { after: 120 },
          }),
          new Paragraph({
            bullet: { level: 0 },
            children: [
              new TextRun({ text: 'Exempt / Non-PO Expenses: ', bold: true }),
              new TextRun({ text: 'Recurring utilities and services such as Singtel fibre broadband, SP Group electricity, AWS/Google Cloud hosting, and office rental do NOT require a PO or GRN. PaySG AI automatically recognizes these as "Non-PO / Exempt" and allows 1-click posting straight to Xero.' }),
            ],
          }),
          new Paragraph({
            bullet: { level: 0 },
            children: [
              new TextRun({ text: 'Automatic Invoice Reference Matching: ', bold: true }),
              new TextRun({ text: 'If a vendor mentions your PO# (e.g. PO-2026-089) or DO# on their tax invoice, the AI automatically extracts the reference and pairs the invoice with documents already residing in your repository or incoming Gmail attachments.' }),
            ],
          }),
          new Paragraph({
            bullet: { level: 0 },
            children: [
              new TextRun({ text: 'Active Variance Protection: ', bold: true }),
              new TextRun({ text: 'When a PO and DO are uploaded, the AI performs line-by-line validation, catching price hikes (e.g. OfficeWorks charging $700 instead of contracted $660) and quantity shortages (e.g. Sheng Siong short-delivering 2 cartons of coffee) before payment is disbursed.' }),
            ],
            spacing: { after: 240 },
          }),

          // SECTION 5: DEMO TEST GUIDE
          new Paragraph({
            text: '5. Comprehensive Demo Guide & Test Transactions',
            heading: HeadingLevel.HEADING_1,
            spacing: { before: 300, after: 180 },
          }),
          new Paragraph({
            children: [
              new TextRun({
                text: 'The prototype includes 4 ready-to-test demonstration scenarios that can be executed in front of stakeholders or clients in under 5 minutes:',
              }),
            ],
            spacing: { after: 160 },
          }),

          new Table({
            width: { size: 100, type: WidthType.PERCENTAGE },
            borders: tableBorderLight,
            rows: [
              new TableRow({
                children: [
                  createCell('Scenario', true, 20),
                  createCell('Vendor & Documents', true, 30),
                  createCell('Expected AI Behavior & Output', true, 50),
                ],
              }),
              new TableRow({
                children: [
                  createCell('Scenario 1: 100% 3-Way Match', false, 20),
                  createCell('OfficeWorks SG Pte Ltd (INV-SG-44120 + PO-2026-089 + DO-88219)', false, 30),
                  createCell('All 10 Dell monitors and 5 ergonomic chairs match perfectly across PO, signed DO, and Invoice. AI issues APPROVED verdict and displays green "Post Verified Bill to Xero" button.', false, 50),
                ],
              }),
              new TableRow({
                children: [
                  createCell('Scenario 2: Price Discrepancy', false, 20),
                  createCell('OfficeWorks SG Pte Ltd (INV-SG-45102 + PO-2026-104 + DO-89104)', false, 30),
                  createCell('Invoiced at $700.00/unit vs contracted PO rate of $660.00/unit. AI flags WARNING, calculates exact +$218.00 variance (inclusive of 9% GST), and halts automatic payment until procurement approval.', false, 50),
                ],
              }),
              new TableRow({
                children: [
                  createCell('Scenario 3: Short Delivery & Credit Note', false, 20),
                  createCell('Sheng Siong Supermarket (SS-INV-9912 + DO-SS-5542 + SS-CN-088)', false, 30),
                  createCell('Invoice billed 5 coffee cartons ($300), but signed DO noted only 3 cartons received ($180). AI flags -$130.80 shortage and automatically pairs with Credit Note SS-CN-088 to balance the AP ledger.', false, 50),
                ],
              }),
              new TableRow({
                children: [
                  createCell('Scenario 4: Direct Non-PO Utility', false, 20),
                  createCell('Singapore Telecommunications (SIN-2026-90412)', false, 30),
                  createCell('Recurring dedicated Meg@POP fibre bill. AI validates IRAS GST Reg M2-0000000-8, verifies math ($850 + $76.50 = $926.50), marks PO matching as Exempt, and enables instant Xero sync.', false, 50),
                ],
              }),
            ],
          }),

          new Paragraph({ spacing: { after: 240 } }),

          new Paragraph({
            text: 'Testing With Fake Dummy PDFs',
            heading: HeadingLevel.HEADING_2,
            spacing: { before: 180, after: 120 },
          }),
          new Paragraph({
            children: [
              new TextRun({
                text: 'Testers can click ',
              }),
              new TextRun({ text: '"Download PDF"', bold: true }),
              new TextRun({
                text: ' on any document in the application to generate an authentic Singapore Tax Invoice PDF onto their computer. Testers can then click ',
              }),
              new TextRun({ text: '"Upload AP Document"', bold: true }),
              new TextRun({
                text: ' and drag that downloaded PDF back into the upload zone to experience real-time Gemini OCR extraction and classification firsthand.',
              }),
            ],
            spacing: { after: 240 },
          }),

          // SECTION 6: SECURITY & ROADMAP
          new Paragraph({
            text: '6. Security, IRAS Compliance & Production Roadmap',
            heading: HeadingLevel.HEADING_1,
            spacing: { before: 300, after: 180 },
          }),
          new Paragraph({
            children: [
              new TextRun({ text: 'Singapore Data Protection & Security: ', bold: true }),
              new TextRun({
                text: 'PaySG AI adheres to the Singapore Personal Data Protection Act (PDPA). Google Workspace OAuth access is strictly read-only (gmail.readonly) and executed purely client-side without storing user email credentials or persistent refresh tokens on unencrypted disks.',
              }),
            ],
            spacing: { after: 120 },
          }),
          new Paragraph({
            children: [
              new TextRun({ text: 'IRAS Form 5 Box 7 Compliance: ', bold: true }),
              new TextRun({
                text: 'All input tax is categorized into claimable (tax invoices with valid UEN and GST registration) vs. non-claimable (statements of account or non-registered entities), allowing instant aggregation for quarterly GST Form 5 filing.',
              }),
            ],
            spacing: { after: 120 },
          }),
          new Paragraph({
            children: [
              new TextRun({ text: 'Future Production Enhancements: ', bold: true }),
            ],
            spacing: { after: 60 },
          }),
          new Paragraph({
            bullet: { level: 0 },
            children: [
              new TextRun({ text: 'InvoiceNow (Peppol Network): ', bold: true }),
              new TextRun({ text: 'Direct integration with the Infocomm Media Development Authority (IMDA) Peppol network for automated nationwide e-invoicing.' }),
            ],
          }),
          new Paragraph({
            bullet: { level: 0 },
            children: [
              new TextRun({ text: 'Live Xero OAuth PKCE: ', bold: true }),
              new TextRun({ text: 'Upgrading the simulated ACCPAY API endpoint to live multi-tenant Xero OAuth 2.0 with automatic tracking category assignments.' }),
            ],
          }),
          new Paragraph({
            bullet: { level: 0 },
            children: [
              new TextRun({ text: 'Gmail Push Notifications: ', bold: true }),
              new TextRun({ text: 'Implementing Google Cloud Pub/Sub webhooks to trigger instantaneous document ingestion the exact second a vendor invoice email arrives in the inbox.' }),
            ],
            spacing: { after: 300 },
          }),

          // Signoff
          new Paragraph({
            alignment: AlignmentType.CENTER,
            children: [
              new TextRun({
                text: '— End of Technical Design Document —',
                bold: true,
                color: '94A3B8',
              }),
            ],
          }),
        ],
      },
    ],
  });
}
