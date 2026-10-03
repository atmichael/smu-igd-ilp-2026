# Local Setup

## Repository

Install the .NET 10 SDK to run the console prototype. It currently expects this path because of hardcoded configuration:

```text
C:\code\smu-igd-ilp-2026\
```

Clone the repository there until that configuration is removed.

## API key

For OpenRouter testing, put your API key on the first line of `deploy/secrets/openrouter-api.key`. `ILP.Console` reads this location from `App.config`. Keep the key local; files under `deploy/secrets/` are ignored by Git except `.gitkeep`.

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

Do not run `specify init` for another clone; `.specify/` and `.github/skills/` are already part of the repo. For each feature, use `/speckit-specify`, `/speckit-clarify`, `/speckit-plan`, `/speckit-tasks`, `/speckit-analyze`, `/speckit-implement`, and `/speckit-converge` in Copilot Chat. Maintainers should complete `.specify/memory/constitution.md`, which is currently a template.