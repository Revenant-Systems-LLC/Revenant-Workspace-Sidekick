# Revenant Workspace Sidekick (rws)

Audit your AI-coded Windows app before you ship it.

rws is a small, fast hardening scanner for Windows desktop apps. It looks for security and deployment mistakes that are easy to miss when a project has been heavily AI-assisted or rapidly vibe-coded. It scans a directory, applies focused rules, and reports findings with a severity score and a letter grade.

## What rws checks

Built for Windows desktop projects: WPF, WinUI, MSIX, and .NET desktop apps in general. It audits the surfaces that go wrong most often:

- MSIX and app manifest configuration
- Registry writes and elevation assumptions
- Dangerous process and assembly loading patterns
- Embedded secrets in .NET project files and config surfaces

## What rws does not try to do

rws is not a full application security platform. It does not perform binary inspection or disassembly, run dynamic analysis, inspect filesystem ACLs in depth, call third-party services, validate installer signing chains, analyze every language or runtime, or provide CI/CD integrations beyond machine-readable output.

## Features

- Scan a directory for security and hardening issues
- Detect risky .NET and Windows app patterns
- Console, JSON, and HTML output
- Severity summary and letter grade
- Baseline files for suppressing known findings
- Git-aware scanning modes such as diff-only
- Optional pre-commit hook

## CLI

Scan a project:

```
rws scan [path] [options]

rws scan
rws scan .\myapp
rws scan .\myapp --format json
rws scan .\myapp --format html --output report.html
rws scan .\myapp --roast
```

Manage a known-findings baseline:

```
rws baseline update [path]
rws baseline status [path]
rws baseline clear [path]
```

Install or remove the pre-commit hook:

```
rws hook install
rws hook uninstall
```

Output goes to stdout or to a file depending on the command and flags.

## Severity and grading

Each finding includes a rule ID, title, severity, file, line number when applicable, why it matters, and a suggested fix. The scanner also produces a score and letter grade so you can judge a project at a glance.

```
[HIGH] RWS-REG-002 Writable HKLM registry modification detected
File: Services/RegistryService.cs:88

Why this matters:
Writing to HKLM requires elevation and can fail silently or create unsafe privilege assumptions.

Fix:
Move the write to HKCU when appropriate, or explicitly check for/admin-gate elevation before executing this code path.
```

## Rule groups

| Prefix | Covers |
|---|---|
| `RWS-MSIX-*` | MSIX and manifest issues |
| `RWS-REG-*` | registry and elevation issues |
| `RWS-EXEC-*` | process and assembly loading issues |
| `RWS-SEC-*` | embedded secret and config exposure issues |

## Default scan surfaces

By default rws scans `**/*.cs`, `**/*.xaml`, `**/*.resx`, `**/*.csproj`, `**/*.props`, `**/*.targets`, `App.config`, `appsettings*.json`, `*.config`, `*.json`, `*.xml`, and `**/Package.appxmanifest`. It ignores `bin/`, `obj/`, `.git/`, `.idea/`, `.vs/`, `packages/`, and `node_modules/`.

## Project structure

```
src/
  RevenantWorkspaceSidekick.Cli     CLI entry point and command routing (net10.0)
  RevenantWorkspaceSidekick.Core    file walking, rule engine, scoring, baseline management
  RevenantWorkspaceSidekick.Rules   rule implementations
  RevenantWorkspaceSidekick.Ui      desktop front end (WPF, net10.0-windows)
tests/
  RevenantWorkspaceSidekick.Tests   unit and integration tests
  fixtures/                         sample projects the tests scan
docs/notes/                         UI findings and task plans
```

## Build and run

Requires the .NET 10 SDK.

```powershell
dotnet build
dotnet test
dotnet run --project src/RevenantWorkspaceSidekick.Cli -- scan .
```

## Secret scanning

Commits are gated by gitleaks via pre-commit, enforcing the Nomos Part VI secret baseline. One-time setup after cloning:

```powershell
winget install Gitleaks.Gitleaks
pre-commit install
```

If the hook blocks a commit, the block stands. Rotate the credential and recommit. Do not pass `--no-verify`.

## License

Proprietary. See [LICENSE](LICENSE). Paid, perpetual, single-user; no redistribution or resale.
