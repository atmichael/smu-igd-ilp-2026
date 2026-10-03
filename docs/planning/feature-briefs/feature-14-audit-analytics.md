# Audit and Processing Analytics

**Priority:** Good-to-have (stretch goal)

As an accounts-payable lead, I want aggregate metrics for extraction quality, user overrides, verification failures, and processing errors so that I can monitor accuracy and identify recurring issues.

Report trends by document type, source, processing version, and time period where the underlying audit data supports them. Distinguish model-extracted values from corrected values and define accuracy using reviewed or otherwise verified outcomes. Analytics must use protected audit events and must not expose invoice contents or credentials to unauthorized users.

Clarify metric definitions, access roles, retention, and the verified data source used as ground truth. Basic audit-event capture is a Must-have prerequisite and is covered by the document-evidence storage feature; this feature adds aggregate reporting only.