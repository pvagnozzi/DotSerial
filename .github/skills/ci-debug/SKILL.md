---
name: ci-debug
description: >
  Debug failing GitHub Actions CI runs for DotSerial. Use when a CI build or test
  is failing on GitHub Actions and you need to understand why and fix it.
allowed-tools: shell
---

# DotSerial CI Debug Skill

When a CI run fails, follow this systematic process using the GitHub MCP tools.

## Step 1 — Identify the failing run

Use GitHub MCP tools to list recent workflow runs:
- Tool: `list_workflow_runs` for the `pvagnozzi/DotSerial` repository
- Focus on: `ci.yml` and `publish.yml`

## Step 2 — Get the failure summary

Use: `summarize_job_log_failures` for the failing run ID to get a concise AI summary of what failed.
If more detail is needed: `get_job_logs` or `get_workflow_run_logs`.

## Step 3 — Common failure patterns

| Symptom | Likely cause | Fix |
|---------|-------------|-----|
| Build warns treated as errors | New warning in code | Fix the warning; don't suppress it |
| `CS1591` missing XML doc | New public member without doc comment | Add `<summary>` |
| `CS8600`/`CS8601` nullable | Nullable reference not handled | Fix nullability properly |
| `dotnet test` fails on Linux/macOS | Platform-specific code path | Check `RuntimeInformation` guards |
| NuGet restore fails | Version constraint conflict | Check `*.csproj` PackageReference versions |
| `publish.yml` fails | Tag format wrong or NuGet key missing | Tag must start with `v`; check `NUGET_API_KEY` secret |

## Step 4 — Reproduce locally

```bash
# Match what CI does:
dotnet restore
dotnet build --no-restore -c Release
dotnet test tests/DotSerial.Tests.Unit --no-build -c Release
```

## Step 5 — Check the matrix

CI runs on three OS: `ubuntu-latest`, `windows-latest`, `macos-latest`.
A failure on one OS but not others usually indicates:
- Path separator issue (use `Path.Combine`, never string concatenation with `/`)
- `RuntimeInformation.IsOSPlatform()` check missing
- Case-sensitive filesystem issue (Linux)

## CI workflow locations

- Build and test: `.github/workflows/ci.yml`
- NuGet publish: `.github/workflows/publish.yml`
