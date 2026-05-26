# CLAUDE.md – Revenant Hardening (RSH) v0.1 → v0.5

You are working on **Revenant Hardening (RSH)**, a security and deployment sanity scanner for **AI‑coded apps** (supporting C#, Python, Java, and TypeScript/JavaScript).

RSH is a **micro‑tool**, not a platform.

- It scans a directory.
- It detects critical language-specific and infrastructure-specific security and hardening flaws.
- It prints clean findings and a simple score/letter grade.
- It outputs to **console**, **JSON**, and **HTML**.

**v0.5 is complete**. The next language additions will continue sequentially.

---

## 1. Product goal

**One‑sentence positioning**

> Audit your AI‑coded Windows app before you ship it.

Stronger version:

> You vibe‑coded a Windows app. Now make sure it won’t ship with a backdoor, a bad manifest, or a stupid registry write.

**Primary users**

- Solo/indie devs using AI assistants (Claude Code, Cursor, Copilot, etc.) to build WPF/WinUI/MSIX desktop apps for Windows.
- Small teams shipping internal desktop utilities.

**What RSH v0.1 does**

- Scans **Windows desktop app** projects (WPF/WinUI/MSIX/.NET).
- Audits:
  - MSIX / manifest configuration.
  - Registry writes and elevation assumptions.
  - Dangerous process/assembly loading patterns.
  - Embedded secrets in common .NET project surfaces.

**What RSH v0.1 does NOT do (reserved for v0.2)**

- XAML injection pattern detection.
- Deep P/Invoke / marshaling analysis.
- Full ACL / filesystem permission analysis.
- Binary inspection or disassembly.
- Runtime verification / dynamic analysis.
- Network calls to third‑party services.
- CI integrations beyond emitting JSON (no GitHub app, no pipelines).
- Multi‑language analysis beyond the .NET/Windows surfaces listed below.
- Installer signing chain validation.

---

## 2. Tech stack and architecture

**Tech stack**

- Language: **C#**
- Runtime: **.NET** (target LTS version; ask if not specified).
- Analysis: **Roslyn** for C# AST/semantic analysis.
- Parsing: standard .NET XML/JSON APIs for manifests and config.

**High‑level architecture**

Projects:

- `RevenantHardening.Cli`
  - Console entry point.
  - Command routing (`RSH scan`).
  - Argument parsing and configuration.
  - Chooses reporters (console/json/html).

- `RevenantHardening.Core`
  - File walker (include/exclude globs).
  - Rule engine and result aggregation.
  - Common models (`Finding`, `Severity`, `RuleMetadata`, etc.).
  - Scoring/letter‑grade logic.

- `RevenantHardening.Rules`
  - Implementations of rule groups:
    - `RSH-MSIX-*`
    - `RSH-REG-*`
    - `RSH-EXEC-*`
    - `RSH-SEC-*`

- `RevenantHardening.Tests`
  - Unit tests for rules and scoring.
  - Integration tests for `RSH scan` over synthetic sample projects.

**Folder structure (example)**

- `/src/RevenantHardening.Cli/`
- `/src/RevenantHardening.Core/`
- `/src/RevenantHardening.Rules/`
- `/tests/RevenantHardening.Tests/`
- `/tests/fixtures/` – small synthetic projects that intentionally trigger rules.

Ask before deviating significantly from this structure.

---

## 3. v0.1 scope – rule groups and surfaces

RSH v0.1 implements **four** rule groups with **17** rules total.

### 3.1 File types and surfaces to scan

Include (by default):

- C# source: `**/*.cs`
- XAML: `**/*.xaml`
- Resources: `**/*.resx`
- Project/props/targets: `**/*.csproj`, `**/*.props`, `**/*.targets`
- Config: `App.config`, `appsettings*.json`, `*.config`, `*.json`, `*.xml`
- MSIX manifests: `**/Package.appxmanifest`

Exclude by default:

- `bin/`, `obj/`, `.git/`, `.idea/`, `.vs/`
- `packages/`, `node_modules/`, other obvious dependency directories.
- Compiled binaries (`*.dll`, `*.exe`) for v0.1.

Allow overrides via `--include` and `--exclude` globs.

### 3.2 Rule groups

#### A. MSIX / manifest audit – `RSH-MSIX-*`

Goal: catch **over‑broad or obviously unsafe MSIX settings**.

Initial rules:

- `RSH-MSIX-001` — Over‑broad package capability
  - Detect suspicious or overly broad capabilities (e.g., capabilities that are rarely needed for typical WPF/WinUI apps).
  - Examples: capabilities granting broad system access when the project appears to be a simple desktop utility.

- `RSH-MSIX-002` — `runFullTrust` enabled
  - Flag manifests with `runFullTrust` set when it is unnecessary or not clearly justified.

- `RSH-MSIX-003` — Suspicious test/debug signing artifact
  - Detect obvious test/debug certs, sideload‑only hints, or mismatched publisher IDs that suggest debug packaging leaking to prod.

- `RSH-MSIX-004` — Custom URI protocol in manifest
  - Detect `<uap:Protocol>` registration in `Package.appxmanifest`. Any custom URI scheme creates an external invocation surface; flag for argument‑validation review.

#### B. Registry + elevation audit – `RSH-REG-*`

Goal: detect **unsafe registry writes and elevation assumptions**.

Initial rules:

- `RSH-REG-001` — HKLM write detected
  - Detect writes to `HKLM` or `HKCR` (e.g., `Registry.LocalMachine`, `Registry.ClassesRoot`) from regular application flows.

- `RSH-REG-002` — Writable registry handle against protected hive
  - Detect use of `RegistryKey.OpenSubKey(..., writable: true)` or equivalent against protected hives.

- `RSH-REG-003` — Elevation‑sensitive write without elevation guard
  - Detect registry writes to protected locations where there is **no clear elevation check** or manifest alignment (e.g., code assumes elevation that may not exist).

- `RSH-REG-004` — `RegistryKey.SetAccessControl` on protected hive
  - Detect calls to `SetAccessControl` on HKLM/HKCR keys. AI‑generated code often calls this to "fix" access‑denied errors without understanding the privilege‑escalation risk.

#### C. Dangerous execution / load audit – `RSH-EXEC-*`

Goal: catch **process launches and assembly loads using user‑influenced input or risky settings**.

Initial rules:

- `RSH-EXEC-001` — `Process.Start` with user‑derived input
  - `Process.Start(...)` where the executable or arguments are derived from user‑influenced data (CLI args, environment vars, config, UI input, etc.).

- `RSH-EXEC-002` — `UseShellExecute = true` in risky context
  - Detect `ProcessStartInfo.UseShellExecute = true` when combined with user‑derived data, increasing risk of shell injection.

- `RSH-EXEC-003` — `Assembly.LoadFrom` with non‑literal path
  - `Assembly.LoadFrom` / `Assembly.LoadFile` where the path is not a simple literal and is influenced by external input or concatenated strings.

- `RSH-EXEC-004` — Custom URI command registration pattern detected
  - Detect patterns of registering custom URI handlers (e.g., via registry) that might allow launching executables with unsanitized arguments.

- `RSH-EXEC-005` — `Process.Start` with interpolated/formatted string argument (Critical)
  - Detect `Process.Start(...)` where any argument is an interpolated string (`$"..."`), a `string.Format(...)` call, or a string concatenation containing a non‑literal operand. These are near‑certain injection vectors and are escalated to Critical severity. EXEC‑001 defers to this rule for those patterns.

#### D. Embedded secrets scan – `RSH-SEC-*`

Goal: detect **hardcoded secrets and credentials** in .NET project surfaces.

Initial rules:

- `RSH-SEC-001` — Hardcoded API key/token pattern
  - Regex‑based detection of obvious API keys, tokens, and secrets in `.cs`, `.xaml`, `.resx`, `.csproj`, `.props`, `.targets`, and config files.

- `RSH-SEC-002` — Suspicious secret in resource/config file
  - Detect credential‑like values (passwords, connection strings, client secrets) in `.resx`, `App.config`, `appsettings*.json`, and similar.

- `RSH-SEC-003` — Credential‑like value in project metadata
  - Detect secrets embedded in project files (e.g., `csproj` properties holding tokens, passwords, or keys).

- `RSH-SEC-004` — Hardcoded connection string in source code (Critical)
  - Detect string literals in `.cs` files that contain ADO.NET connection strings with embedded passwords, or database URIs (MongoDB, PostgreSQL, MySQL) with embedded credentials. Checks Roslyn string literal tokens only — avoids matching comments or disabled code.

---

## 3a. v0.2 planned scope

The following are **planned for v0.2** and must NOT be implemented in v0.1:

- **XAML injection** — detect binding expressions or loaded XAML paths that could inject markup from user input.
- **P/Invoke / marshaling audit** — detect unsafe P/Invoke patterns (e.g., `CharSet.None`, unchecked buffer sizes, `BOOL` vs `bool` mismatches).
- **ACL / filesystem permission analysis** — detect `Directory.SetAccessControl`, `File.SetAccessControl`, or `DirectorySecurity` patterns granting broad access.
- **Binary inspection** — scan compiled `*.dll`/`*.exe` artifacts for known‑bad patterns or embedded secrets (requires PE parsing, out of scope for text‑only scan).

Do not start any v0.2 work without an explicit instruction to begin v0.2.

---

## 4. Finding model and scoring

### 4.1 Finding structure

Every finding includes:

- `ruleId` – e.g., `RSH-REG-002`
- `title` – short description.
- `severity` – `Low | Medium | High | Critical`.
- `file` – path relative to scan root.
- `line` – line number (if applicable).
- `why` – short explanation of **why this matters**.
- `fix` – short **how to fix it** guidance.

Example finding (console):

```text
[HIGH] RSH-REG-002 Writable HKLM registry modification detected
File: Services/RegistryService.cs:88

Why this matters:
Writing to HKLM requires elevation and can fail silently or create unsafe privilege assumptions.

Fix:
Move the write to HKCU when appropriate, or explicitly check for/admin‑gate elevation before executing this code path.
```

### 4.2 Scoring and letter grades

Start at **100 points** and subtract per finding:

- `Critical`: −25
- `High`: −15
- `Medium`: −8
- `Low`: −3

Clamp score at 0.

Letter grades:

- `A`: 90–100
- `B`: 80–89
- `C`: 70–79
- `D`: 60–69
- `F`: below 60

Show in summary:

- Score (0–100).
- Letter grade.
- Counts of findings by severity.
- (Optional) guessed project type: “Windows/.NET desktop” based on files found.

---

## 5. CLI contract

v0.1 exposes **one** primary command: `scan`.

Examples:

```bash
rsh scan
rsh scan .\myapp
rsh scan .\myapp\ --format json
rsh scan .\myapp\ --format html --output report.html
rsh scan .\myapp\ --offline
rsh scan .\MyApp\ --roast
```

Flags:

- `--format console|json|html`
  - Default: `console`.

- `--output <path>`
  - For `json` or `html`, path to write output file.
  - If not specified, write to `stdout` where appropriate.

- `--offline`
  - For v0.1, **all checks are offline**. This flag exists for future compatibility and should not change behavior yet.

- `--roast`
  - Use slightly more opinionated/roasty console wording.
  - JSON/HTML output remain neutral.

- `--severity low|medium|high|critical`
  - Minimum severity to display/report.
  - Default: show all severities.

- `--include <glob>` / `--exclude <glob>`
  - Override default include/exclude patterns.

Do **not** add more commands in v0.1 (no `RSH rules`, no `RSH init-ci`, etc.).

---

## 6. Testing strategy

### 6.1 Unit tests

For each rule:

- Add tests that:
  - **Trigger** the rule on realistic “AI‑slop” examples (e.g., code that an AI assistant would likely generate).
  - **Do not trigger** on similar but safe code.

- Use small inline snippets or fixture files under `/tests/fixtures/` as appropriate.

### 6.2 Integration tests

Create at least one small fake Windows app fixture per rule group (or per cluster):

- A WPF/WinUI/MSIX project with:
  - Over‑broad manifest capabilities.
  - Unsafe registry writes/elevation assumptions.
  - Dangerous `Process.Start` / `Assembly.LoadFrom`.
  - Hardcoded secrets in config/resources.

Add integration tests that:

- Run `RSH scan` against these fixtures.
- Assert:
  - Certain rule IDs appear.
  - Grade and severity counts are as expected.

---

## 7. Coding standards and constraints

- Use modern C# style (nullable reference types enabled).
- Prefer **clear, boring code** over clever tricks.
- Avoid unnecessary third‑party dependencies:
  - Standard library + Roslyn are preferred.
  - Ask before introducing any new library.

- Performance:
  - Single Roslyn compilation per solution where possible.
  - Parallelize file analysis where safe.
  - Skip `bin/`, `obj/`, and large binary files to avoid slowdown.

- Robustness:
  - Scanner must **not crash** on unusual projects.
  - On errors, emit a warning and continue scanning other files.

---

## 8. How to work as Claude Code

You are an assistant in a Claude Code workspace, not an end user.

**General rules**

- Start complex tasks in **Plan mode**:
  - Outline the steps and files you will create or modify.
  - Wait for user approval before making changes.

- Respect v0.1 scope:
  - Do **not** add new feature areas (e.g., P/Invoke depth, ACLs, CI tooling) unless the user explicitly updates this spec.
  - If you believe a new rule or feature is important, propose it in the plan and wait for confirmation.

- For each new rule:
  - Implement rule logic.
  - Add tests (positive + negative).
  - Update any rule catalogs or documentation as needed.

- If you are unsure:
  - Ask clarifying questions rather than guessing.

**Typical tasks you will be asked to do**

- Scaffold the solution and projects from this spec.
- Implement `rsh scan` CLI and wire it to the core engine.
- Implement individual rule groups and their tests.
- Add synthetic “cursed” example apps that intentionally trigger RSH findings.
- Tweak reporters and scoring according to this document.

Adhere closely to this spec. When in doubt, choose the option that keeps RSH **small, fast, and focused** for v0.1.
