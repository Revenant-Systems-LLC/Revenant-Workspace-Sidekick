# rws – Revenant Workspace Scout

You vibe‑coded a Windows app. rws is the sanity scanner that makes sure you don’t ship it with a backdoor, a bad manifest, or a stupid registry write.

- **Focus:** Windows desktop apps (WPF/WinUI/.NET/MSIX)
- **Checks:** MSIX/manifest, registry + elevation assumptions, dangerous process/assembly loads, embedded secrets in .NET project files
- **Usage:** `rws scan .` → get a score, a letter grade, and concrete fixes before you ship.[file:1]
