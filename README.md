RWS — Revenant Workspace Sidekick
Audit your AI-coded Windows app before you ship it.

rws is a small, fast hardening scanner for Windows desktop apps. It looks for security and deployment mistakes that are easy to miss when a project has been heavily AI-assisted or rapidly vibe-coded.

It scans a directory, applies focused rules, and reports findings with a simple severity score and letter grade.

What rws checks
rws is designed for Windows desktop app projects, especially:

WPF
WinUI
MSIX
.NET desktop apps
It audits common high-risk surfaces such as:

MSIX / app manifest configuration
Registry writes and elevation assumptions
Dangerous process and assembly loading patterns
Embedded secrets in common .NET project files and config surfaces
What rws does not try to do
rws is intentionally not a full application security platform. It does not:

perform binary inspection or disassembly
run dynamic analysis
inspect filesystem ACLs in depth
call third-party services
validate installer signing chains
provide CI/CD integrations beyond machine-readable output
analyze every language or runtime
Features
Scan a directory for security and hardening issues
Detect risky .NET/Windows app patterns
Output results in:
console
JSON
HTML
Show a severity summary and letter grade
Support baseline files for suppressing known findings
Support git-aware scanning modes like diff-only scanning
Optional pre-commit hook workflow
CLI
Scan
rws scan [path] [options]
Examples:

rws scan
rws scan .\myapp
rws scan .\myapp --format json
rws scan .\myapp --format html --output report.html
rws scan .\myapp --offline
rws scan .\myapp --roast
Baseline
Manage a known-findings baseline:

rws baseline update [path]
rws baseline status [path]
rws baseline clear [path]
Git hook
Install or remove the pre-commit hook:

rws hook install
rws hook uninstall
Output formats
rws supports:

console
json
html
Depending on the command and flags, you can write output to stdout or to a file.

Severity and grading
Each finding includes:

rule ID
title
severity
file
line number, when applicable
why it matters
suggested fix
The scanner also produces a simple score / letter grade so you can quickly judge how risky a project looks.

Project structure
This repository is organized into a few small components:

RevenantWorkspaceSidekick.Cli
CLI entry point and command routing
RevenantWorkspaceSidekick.Core
file walking, rule engine, scoring, baseline management
RevenantWorkspaceSidekick.Rules
rule implementations
RevenantWorkspaceSidekick.Tests
unit and integration tests
Default scan surfaces
By default, rws scans common project and configuration files such as:

**/*.cs
**/*.xaml
**/*.resx
**/*.csproj
**/*.props
**/*.targets
App.config
appsettings*.json
*.config
*.json
*.xml
**/Package.appxmanifest
It ignores common build and dependency folders such as:

bin/
obj/
.git/
.idea/
.vs/
packages/
node_modules/
Rule groups
rws uses focused rule groups for Windows desktop app hardening:

RWS-MSIX-* — MSIX and manifest issues
RWS-REG-* — registry and elevation issues
RWS-EXEC-* — process and assembly loading issues
RWS-SEC-* — embedded secret and config exposure issues
Example finding
[HIGH] RWS-REG-002 Writable HKLM registry modification detected
File: Services/RegistryService.cs:88

Why this matters:
Writing to HKLM requires elevation and can fail silently or create unsafe privilege assumptions.

Fix:
Move the write to HKCU when appropriate, or explicitly check for/admin-gate elevation before executing this code path.
Build and run
Add your normal .NET build/run instructions here if you want a more complete user-facing README.

Typical workflow:

dotnet build
dotnet test
dotnet run --project src/RevenantWorkspaceSidekick.Cli -- scan .
License
MIT License
