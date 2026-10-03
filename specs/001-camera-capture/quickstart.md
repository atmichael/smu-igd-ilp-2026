# Quickstart: Camera Document Capture Validation

This guide validates the camera flow after its client, API intake contract, application authorization, and evidence-storage dependencies are implemented.

## Prerequisites

- .NET 10 SDK and Node.js LTS installed.
- API configuration and the Feature 08 evidence-storage provider available.
- A camera-enabled iPhone/iPad running current stable Safari or Android device running current stable Chrome.
- A trusted HTTPS origin reachable by the mobile device. Camera access does not work from an ordinary HTTP Vite network URL; `--host` alone is not sufficient.

## Start the API

From the repository root:

```powershell
dotnet run --project .\src\server\ILP.Server\ILP.Server.csproj
```

Configure the development client origin in the API's CORS settings as required by the implementation.

## Start the client

```powershell
Set-Location .\src\client\web
npm.cmd ci
npm.cmd run dev
```

For physical-device camera checks, serve the client through a trusted HTTPS development host or HTTPS test deployment and configure the API base URL/CORS origin for that host. Keep API and client origins HTTPS during device testing.

## Automated checks

From `src/client/web`:

```powershell
npm.cmd run lint
npm.cmd run test
npm.cmd run build
```

From the repository root, after the API test project is added:

```powershell
dotnet test .\tests\ILP.Server.Tests\ILP.Server.Tests.csproj
```

## Manual device scenarios

1. Verify the supported device matrix before testing:
   - iPhone or iPad running current stable Safari on iOS/iPadOS
   - Android phone or tablet running current stable Chrome
   - HTTPS required for camera access; localhost is allowed for local development
2. Start a capture and grant camera permission. Verify that only video access is requested and the rear camera is preferred when available.
3. Capture a page. Verify preview appears and the camera indicator stops while reviewing the still image.
4. Retake a page and verify the rejected image is absent from the final submission.
5. Capture and accept three pages; verify order. Confirm a fourth page cannot be added until a page is removed.
6. Submit the capture. Verify one request contains all accepted pages in order, with `source=camera-capture`, and success returns one source-document ID.
7. Cancel before submission. Verify no intake request is sent, local previews are released, camera tracks are stopped, and prior application work remains intact.
8. Deny camera permission, test without an available camera, interrupt camera access, and simulate upload/network/storage failures. Verify clear recovery actions and no partial source document.
9. Attempt a malformed, unsupported, or over-limit request at the API boundary. Verify server-side rejection and no partial persistence.

Record exact device model, OS version, browser version, permission outcome, and scenario result for the supported-device matrix. Never use production invoices during validation.
