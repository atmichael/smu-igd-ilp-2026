# Raw Document Text Extraction

**Priority:** Must-have

As an accounts-payable user, I want readable text extracted from an accepted document so that its contents can be reviewed and used in later processing.

For digital PDFs, read the embedded text layer when usable. For scanned PDFs and camera images, normalize pages and use OCR. Preserve the original, page order, text locations where available, and a link from extracted text to its source. Report unreadable pages or regions; do not silently present incomplete extraction as complete.

Accept documents from the supported intake features. Keep document-type classification, invoice-field structuring, AP line-item conversion, and storage out of scope.