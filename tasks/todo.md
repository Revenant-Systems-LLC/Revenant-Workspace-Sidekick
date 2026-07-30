# Sidekick UI — Task Plan

Added 2026-07-29. Dave's instruction (verbatim, via Cowork desktop chat while stepping out): "You need to get Sidekick. It needs to have its own UI. I'm not sure. Work on that. I'll update you through dispatch. You're set to automatically approve." Scope and approach below are Claude's judgment calls — no one was available to ask, flagging everything for review.

## Decisions made (review these)

- **Framework: WPF**, not WinUI 3 or Avalonia. rws already targets WPF/WinUI apps as scan subjects, the whole toolchain is Windows/.NET-only, and WPF needs zero new build tooling beyond `UseWPF=true` on a new `net10.0-windows` project. Simplest option that fits the existing repo.
- **In-process, not CLI-wrapping.** The UI project references Core + Rules directly and calls `RuleEngine.Scan(RuleRegistry.All, options)` itself — the same call `ScanCommand.Execute` makes — instead of shelling out to `rws.exe` and parsing stdout. Reuses `JsonReporter`/`HtmlReporter` directly for export since they're pure `TextWriter` writers.
- **No new NuGet packages.** CLAUDE.md's coding standards say ask before adding a dependency. With no one to ask, skipped CommunityToolkit.Mvvm and hand-wrote a small `RelayCommand` + `ViewModelBase`. Trivial to swap for the toolkit later if preferred.
- **Scope trim on flags exposed:** the UI surfaces path, min severity, roast, student mode — the "explore a project interactively" flags. It does **not** expose `--diff`, `--history`/`--history-depth`, `--baseline`/`--update-baseline`, `--audit-deps`, or `--verify` (CI/automation-oriented, and `--verify` makes live outbound HTTP calls to confirm secrets — not something to fire by default from a GUI button without a very deliberate opt-in). Include/exclude glob editing also deferred from v1. All easy follow-ups.
- **No custom icon/branding yet.** Functional correctness first. `Core/Assets/RWS_GOLD.png` is sitting right there if you want it wired in as the window icon later.

## Plan

- [x] Research existing Core/Rules/Cli architecture (solution layout, models, RuleEngine/Scorer/ScanOptions, reporters, actual current rule count vs. CLAUDE.md's v0.1 spec)
- [x] Decide UI architecture (see Decisions above)
- [ ] Scaffold `RevenantWorkspaceSidekick.Ui` (WPF, net10.0-windows) — csproj, App, MainWindow, ViewModels, minimal MVVM plumbing
- [ ] Add project to `RevenantWorkspaceSidekick.slnx`
- [ ] Wire `MainViewModel` to `RuleEngine.Scan` / `RuleRegistry.All` / `Scorer` (confirm whether the engine or the reporter is what applies `--severity` filtering, so the on-screen score matches what's shown)
- [ ] Results grid (severity-colored badges), score/grade panel, warnings surface for scan failures
- [ ] Export buttons reusing `JsonReporter`/`HtmlReporter`
- [ ] `dotnet build` the full solution — confirm it compiles and nothing existing regresses
- [ ] Update Sidekick `CLAUDE.md` to record the UI as an intentional scope addition beyond v0.1
- [ ] Review section below

## Known pre-existing issues noticed during research (not fixed — flagging only, not what was asked)

- `RuleRegistry.All` is missing 3 rules that exist in source but were never wired in, so they currently never run: `GoSilentFailureRule` (RWS-GO-003), `PerlTaintModeRule` (RWS-PL-003), `RustCommandInjectionRule` (RWS-RS-003).
- `src/RevenantHardening.Companion/` is an orphaned WPF project sitting inside this repo but **not in the .slnx** — it's Workspace Warden's voice-companion app (prints "Revenant Workspace Warden initialized", uses NAudio/Whisper.net), not a Sidekick UI. Left untouched; not part of this task and not safe to guess about.
- Version strings disagree across the repo: `.csproj` says `1.0.0`, `Program.cs`/`SarifReporter` banner hardcodes `"v0.68"`, `CLAUDE.md` says "v0.5 is complete." Real current rule count is 96 across 21 language/category groups — far beyond the 17 rules in CLAUDE.md's v0.1 spec.
- `README.md` is stale — only documents the original 4 rule groups (MSIX/REG/EXEC/SEC), nothing about the 17 additional language groups added since.

## Review

**Status: done, verified, not yet visually confirmed.**

- Full solution (`Cli`, `Core`, `Rules`, `Ui`, `Tests`) builds clean — 0 warnings, 0 errors. Verified by copying the affected projects to local disk and running `dotnet build` there, since the sandbox's mount of this folder doesn't support the file operations NuGet/MSBuild need directly (FUSE bridge rejected temp-file writes under `obj/`). Also ran the full existing test suite: 196/196 passing, unchanged from before this work — confirms nothing regressed.
- What's **not** verified: actually opening the window and looking at it. That needs a real Windows session, which wasn't available. Next time you're at the machine: `cd src\RevenantWorkspaceSidekick.Ui` then `dotnet build` and `dotnet run` (your usual PowerShell loop) and just look at it. If the layout, colors, or any control looks or behaves wrong, tell me and I'll fix it — I can't see it from here.
- Everything under "Decisions made" above is a judgment call, not a request from you — review those first, since that's where I could most easily have guessed wrong.
- `CLAUDE.md` now has a dated addendum (§9) recording this work and the real current scope (96 rules, not 17; extra Cli commands; version-string disagreement across the repo) since the v0.1 header was materially stale.
