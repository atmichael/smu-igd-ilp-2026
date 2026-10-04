# Local Setup

## Repository

Install the .NET 10 SDK to run the console prototype. It currently expects this path because of hardcoded configuration:

```text
C:\code\smu-igd-ilp-2026\
```

Clone the repository there until that configuration is removed.

## React frontend

Install Node.js LTS, then from the repository root install the frontend dependencies:

```powershell
Set-Location .\src\client\web
npm.cmd ci
```

Start the Vite development server for use on this computer:

```powershell
npm.cmd run dev
```

Open the local URL printed by Vite, usually `http://localhost:5173`. To test from another device on the same network, start Vite with a network host instead:

```powershell
npm.cmd run dev -- --host 0.0.0.0
```

Use the network URL printed by Vite on the other device. Allow the development server through the firewall if prompted; do not use this option on an untrusted network.

Run the configured ESLint checks and create a production build with:

```powershell
npm.cmd run lint
npm.cmd run test
npm.cmd run build
```

In PowerShell, use `npm.cmd` instead of `npm` if the execution policy blocks the `npm.ps1` script.

## Run the camera-capture app locally

Install the .NET 10 SDK. Before starting the API, create the OpenRouter key file as described in the API key section below; the API will not start without it.

### 1. Start the database

API intake stores original images and PDFs in MySQL (table `source_document_content`, created on first use). Start it once per session, using either option:

- With Docker Desktop or Podman: `.\setup\Start-DevDependencies.ps1` (MySQL 8 from `setup/docker-compose.yml`).
- Without Docker or admin rights: `.\setup\Start-LocalDatabase.ps1`. It downloads MariaDB 11.4 LTS (MySQL-compatible) to `%LOCALAPPDATA%\ilp-mariadb` on first run, starts it on `127.0.0.1:3306`, and creates the same database and user. Stop it with `.\setup\Start-LocalDatabase.ps1 -Stop`; run it again after a reboot.

To run without a database, set `$env:EvidenceStorage__ContentProvider = "File"` before starting the API; files are then kept under `src/server/ILP.Server/App_Data/source-documents/`.

### 2. Start the API

From the repository root, stop any API that is already running (Ctrl+C), then:

```powershell
# First time, or after packages change: the company NuGet feed returns 401, so restore from nuget.org.
dotnet restore .\src\server\ILP.Server\ILP.Server.csproj --source https://api.nuget.org/v3/index.json
dotnet run --project .\src\server\ILP.Server\ILP.Server.csproj --launch-profile http --no-restore
```

`dotnet run` builds the project, so no separate build is needed. The API listens on `http://localhost:5035`.

### 3. Start the client and capture

In a second terminal, start the React client using the frontend steps above and open the URL printed by Vite, usually `http://localhost:5173`. Select **Start camera capture**, grant camera permission, then capture and review pages before submitting. Browsers allow camera access on localhost. For physical-device testing, use a trusted HTTPS origin; `--host 0.0.0.0` alone does not provide HTTPS or enable camera access.

### 4. Check what was stored

The submission response contains `sourceDocumentId`. With the local MariaDB, list the stored files:

```powershell
& "$env:LOCALAPPDATA\ilp-mariadb\mariadb-11.4.13-winx64\bin\mariadb.exe" --host=127.0.0.1 --user=root ilpdvapp --table --execute="SELECT source_document_id, file_name, content_type, size_bytes, created_at FROM source_document_content;"
```

Or read the file back through the API with the header `Authorization: Test x`: `GET /api/source-documents/{sourceDocumentId}/pages/1` for images, or `/file` for an uploaded PDF.

### Tests

Run the API tests from the repository root:

```powershell
dotnet test .\tests\ILP.Server.Tests\ILP.Server.Tests.csproj
```

The two MySQL store tests are skipped unless `ILP_TEST_MYSQL` is set, for example `$env:ILP_TEST_MYSQL = "Server=127.0.0.1;Port=3306;Database=ilpdvapp;User ID=ilpuser;Password=Pass123#"` with the database running. The client tests mock camera APIs and do not require a physical camera.

### Where data is kept

Original files are in MySQL as above. Source-document records, including idempotency keys, are under `src/server/ILP.Server/App_Data/source-document-records/`, and evidence packages (`/api/evidence-packages`) are one JSON file each under `App_Data/evidence-store/` (gitignored). The development database credentials in `appsettings.Development.json` are for the local database only; override them with `ConnectionStrings__IlpDatabase`. Sign-in is a development-only test scheme: send `Authorization: Test <any value>`; the API refuses to start outside the Development environment until access control (Feature 17) exists. Sample requests are in `src/server/ILP.Server/ILP.Server.http`.

## API key

For OpenRouter testing, put your API key on the first line of `deploy/secrets/openrouter-api.key`. The API and collector read this location from their `appsettings.json`; `ILP.Console` reads it from `App.config`. If you move the repository, update the configured key path. Keep the key local; files under `deploy/secrets/` are ignored by Git except `.gitkeep`.

## Test locally with Ollama

This is a manual prompt experiment; the console prototype is not currently wired to Ollama. Install [Ollama for Windows](https://ollama.com/download/windows), then run:

```powershell
ollama pull qwen2.5:7b
ollama run qwen2.5:7b
```

Paste invoice text and ask for the same fields you plan to evaluate through OpenRouter. Use `llama3.2:3b` instead on machines with less memory. Ollama's local API is available at `http://localhost:11434` while it is running.

The console also contains an unused embedded GGUF experiment with a hardcoded model path. It is separate from both Ollama and the current OpenRouter entry point.

## Test with OpenRouter

From the repository root, run the console against a sample invoice:

```powershell
dotnet run --project .\src\client\windows-console\ILP.Console\ILP.Console.csproj -- .\docs\sample\invoices\APSIM-001.pdf
```

The current console sends the PDF to OpenRouter using the `google/gemini-2.5-flash` model and prints its transcription. The model and prompt are currently hardcoded in `Program.cs`. Check the output against the source invoice. Ollama and OpenRouter are not yet selectable providers in one application, and their outputs are not automatically compared; standardize the prompt and output format before using them for an accuracy comparison.

## Spec Kit

Spec Kit is initialized in this repository for GitHub Copilot. Each developer installs the CLI once, with Python 3.11+ and [`uv`](https://docs.astral.sh/uv/getting-started/installation/):

```powershell
uv tool install specify-cli
specify version
```

Do not run `specify init` for another clone; `.specify/` and `.github/skills/` are already part of the repo. For each feature, use `/speckit-specify`, `/speckit-clarify`, `/speckit-plan`, `/speckit-tasks`, `/speckit-analyze`, `/speckit-implement`, and `/speckit-converge` in Copilot Chat. These commands check plans against the project constitution in `.specify/memory/constitution.md`; shared business terms and their owning features are listed in the [feature brief index](../planning/feature-briefs/feature-brief-index.md#owned-terms).