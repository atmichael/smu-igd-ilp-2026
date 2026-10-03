# Document Quality Feedback

**Priority:** Should-have

As an accounts-payable user, I want to know when a document image is unlikely to produce reliable text so that I can recapture or replace it before extraction is trusted.

Identify input problems such as blur, low resolution, poor orientation, cropped edges, glare, or unreadable pages. Show the affected page and a clear recommendation to recapture, replace, or continue with a warning. Preserve the original document; any normalized or enhanced image is a derived copy. Do not claim quality checks guarantee correct extraction.

Clarify minimum quality criteria, supported camera and scanned-image conditions, multi-page behavior, and whether low-quality pages block processing or enter human review. Keep model field-accuracy evaluation out of scope.