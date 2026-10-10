# AP Invoice Processing: Five Layers

The MVP turns an uploaded or captured document into a reviewed, auditable invoice record. Mailbox collection and three-way matching build on that foundation. Only a supplier invoice creates AP invoice lines. Orders and receipt/service-acceptance documents provide matching evidence.

```mermaid
flowchart TD
    subgraph L1["LAYER 1 - INGESTION AND NORMALIZATION"]
        Upload[Upload PDF or image]
        Camera[Capture image]
        Mailbox[Import selected mailbox attachment]
        Validate[Validate file, retain original, normalize pages]
        Text[Read PDF text layer or OCR scanned pages and images]
        Upload --> Validate
        Camera --> Validate
        Mailbox -. Phase 2 .-> Validate
        Validate --> Text
    end

    subgraph L2["LAYER 2 - SCHEMA-CONSTRAINED EXTRACTION"]
        Quality{Text usable?}
        TextLLM[LLM classifies and extracts typed JSON from text]
        Vision[Optional multimodal LLM classifies and extracts from original]
        Type{Candidate document type}
        Text --> Quality
        Quality -->|Yes| TextLLM --> Type
        Quality -.->|No, optional fallback| Vision -.-> Type
    end

    subgraph L3["LAYER 3 - DETERMINISTIC VERIFICATION"]
        Verify[Check schema, required fields, totals, tax, and vendor]
        Master[(Vendor master and tax reference data)]
        Verify --> Route
        Master --> Verify
    end

    subgraph L4["LAYER 4 - HUMAN OVERSIGHT"]
        Route{Checks pass and record is eligible?}
        Queue[Exception queue with reasons]
        Review[User reviews source and candidate data]
        Verified[Auto-complete to verified state; not payment-approved]
        Route -->|Yes| Verified
        Route -->|No or uncertain| Queue --> Review
        Review -->|Corrected data| Verify
    end

    Type -->|Supplier invoice| Verify
    Type -->|Buyer purchase order or service order| Verify
    Type -->|Goods receipt, delivery evidence, or service acceptance| Verify
    Type -->|Supplier statement| Statement[Store for separate reconciliation]
    Type -->|Sales order or unsupported type| Other[Route outside AP matching]

    subgraph Packet["DOCUMENT PACKET TRACKING - PHASE 2"]
        Track[Track case, document arrival, and processing state]
        Complete{All expected match evidence verified?}
        Pending[Show received documents and expected documents still pending]
        Track --> Complete
        Complete -->|No| Pending
    end

    subgraph Matching["THREE-WAY MATCHING - PHASE 2"]
        InvoiceRecord[(Verified invoice and AP lines)]
        OrderRecord[(Verified buyer order)]
        ReceiptRecord[(Verified receipt or service acceptance)]
        Match[Deterministic 3-way comparison]
        MatchResult{Match result}
        APReady[Matched and ready for AP workflow]
        MatchQueue[Match exception with reason]
        Complete -->|Yes| Match
        InvoiceRecord --> Track
        OrderRecord --> Track
        ReceiptRecord --> Track
        Match --> MatchResult
        MatchResult -->|Within approved tolerances| APReady
        MatchResult -->|Missing evidence or discrepancy| MatchQueue
    end

    Verified --> RecordType{Verified document type}
    RecordType -->|Invoice| InvoiceRecord
    RecordType -->|Purchase or service order| OrderRecord
    RecordType -->|Receipt or service acceptance| ReceiptRecord
    Validate -. arrival event .-> Track

    subgraph L5["LAYER 5 - AUDIT AND GOVERNANCE"]
        Events[Record source, model/schema versions, checks, corrections, and outcomes]
        Audit[(Protected audit event log)]
        Analytics[Accuracy, override, and error-rate analytics]
        Events --> Audit -. Stretch goal .-> Analytics
    end

    Validate -. source event .-> Events
    TextLLM -. extraction event .-> Events
    Verify -. verification event .-> Events
    Review -. review event .-> Events
    Match -. matching event .-> Events
    MatchQueue --> Queue
```

For PDFs, read an existing text layer when usable and OCR only scanned pages; preprocess camera images before OCR. The normal extraction path classifies the document and asks a schema-constrained LLM for candidate JSON. An optional multimodal model can be evaluated when OCR text is unusable. Both paths use the same application contract and must pass deterministic validation. Confidence alone never authorizes a financial decision.

The MVP requires manual review for uncertain or failed checks and an audit trail for source references, model/schema versions, verification outcomes, and corrections. Straight-through processing is a later phase: a clean record may become ready for the AP workflow, but is never thereby approved for payment. Audit analytics are a stretch goal; capture their underlying events from the start.

Three-way matching compares a buyer's purchase order (or service order), supplier invoice, and evidence that goods were received or services accepted. Supplier delivery orders may support receipt verification, but buyer goods-receipt records are stronger evidence. Supplier statements belong to separate reconciliation; seller sales orders are not buyer purchase orders.

Packet tracking begins when the first document arrives and links later documents to the same case. Mark a document pending only when an order or configured business rule establishes that it is expected; do not infer missing documents from inbox silence alone.