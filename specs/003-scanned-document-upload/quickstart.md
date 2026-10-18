# Quickstart: Validate Scanned Document Upload

How to prove the feature works end to end. Contract details: [contracts/shared-intake-changes.md](contracts/shared-intake-changes.md). Entity details: [data-model.md](data-model.md).

## Prerequisites

- .NET 10 SDK and Node.js with npm.
- The API runs in the Development environment (the development test sign-in is the only authentication until Feature 17).
- The web client dev server proxies `/api` to `http://localhost:5035` (see `src/client/web/vite.config.ts`).
- Sample files: valid PDF (one and several pages), valid JPEG and PNG, a renamed non-PDF (for example a text file named `.pdf`), a password-protected PDF, a damaged PDF, an empty file, and a file just over 20 MB. Sample invoices are in `docs/sample/invoices/`.

## Run locally

```powershell
# API (Development environment)
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project src/server/ILP.Server

# Web client (separate terminal)
cd src/client/web
npm install
npm run dev
```

For phones and tablets on the same network, open the address printed by `npm run dev` (it listens on the network) and accept the development certificate.

## Automated checks

```powershell
dotnet test tests/ILP.Server.Tests
cd src/client/web; npm test; npm run lint; npm run build
```

Expected: server tests cover the 20 MiB boundary (accepted at 20 x 1024 x 1024 bytes, `413` at one byte more), empty file (`400`), mismatched content and password-protected PDF (`415`), `origin.upload.originalFileName` recorded and sanitized (path-like names reduced to the final segment), camera capture still accepted at its existing limits, identical retry (`201`, same ID), and key conflict (`409`). Client tests cover quick checks, key reuse on retry, rejection messages, and the success actions.

## Manual scenarios

| # | Scenario | Expected result |
|---|---|---|
| 1 | Select a valid multi-page PDF and submit | Name and passing quick checks shown before submit; confirmation shows the source document ID; "Upload another" and view/download of the original are offered; the downloaded file is byte-identical; the record has `channel` = `file-upload` and the page count |
| 2 | Select a valid JPEG, then a PNG | Each is received as one source document with `pageCount` 1 |
| 3 | Replace the selection before submitting | Only the replacement is submitted |
| 4 | Select a `.txt` file | Blocked before submit with the list of accepted types |
| 5 | Select a text file renamed `.pdf` | Quick checks pass; server rejects it (`415`) with a clear message; nothing stored |
| 6 | Select a password-protected PDF, then a damaged PDF | Each rejected with the "could not be read" message and a next action; nothing stored |
| 7 | Select an empty file | Blocked before submit; server also rejects `400` if sent directly |
| 8 | Select a file just over 20 MB | Blocked before submit with the 20 MB limit; sent directly, the server returns `413` |
| 9 | Submit, interrupt the network after sending, then retry | One source document exists; the retry returns the original result |
| 10 | Upload a file named like `..\..\evil.pdf` | Shown as the final segment only; stored under server-generated names; no path effect |
| 11 | Remove the credentials, submit | `401` explained; selection kept |
| 12 | Complete scenario 1 using only the keyboard | Every control reachable, visible focus, status announced without relying on color |
| 13 | On desktop, drag one valid PDF onto the upload screen | The screen highlights while dragging; on drop the file is selected with its name, size, and quick-check result; nothing is submitted until Submit; dropping a file outside the screen never opens it in the browser tab |
| 14 | On desktop, drag two files onto the screen (with one file already selected) | "Only one file can be uploaded at a time" is shown and the selected file is unchanged |

## Browser and device matrix (FR-018, SC-008)

Run scenarios 1, 2, 4, 6, and 8 on current stable Chrome, Edge, Firefox, and Safari on desktop, plus scenarios 13 and 14 on desktop only, and scenarios 1, 2, 4, 6, and 8 on Safari on iOS/iPadOS and Chrome on Android (select a PDF and an image already stored on the device; no drag hint should appear on phones and tablets). Check that there is no horizontal scrolling at a 320 CSS pixel viewport width, that controls are at least 44 × 44 CSS pixels, that the picker does not force the camera, and that contrast meets WCAG 2.2 AA.

## Done when

- All automated checks pass.
- Manual scenarios 1-14 behave as expected and the matrix is complete.
- `source-document-intake.md` contains the edits listed in the contract changes page.
