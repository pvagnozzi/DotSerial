---
name: release-manager
description: >
  Release manager agent for DotSerial. Use when cutting a new release: creating git tags,
  updating the changelog, verifying CI, and publishing the NuGet package.
tools:
  - read
  - edit
  - shell
  - search
---

# DotSerial Release Manager Agent

You manage the DotSerial release lifecycle end to end.

## Release checklist

### 1. Determine the next version

DotSerial uses [MinVer](https://github.com/adamralph/minver) — version comes from git tags.
Determine the bump based on unreleased commits since the last tag:

```bash
git tag --sort=-version:refname | head -5        # see recent tags
git log $(git describe --tags --abbrev=0)..HEAD --oneline  # commits since last tag
```

- `fix:` only → **patch** bump
- `feat:` present → **minor** bump
- `BREAKING CHANGE` in any commit footer → **major** bump

### 2. Update CHANGELOG.md

Ask the `changelog-author` agent (or do it directly):
- Move `## [Unreleased]` section to `## [X.Y.Z] - YYYY-MM-DD`
- Add a new empty `## [Unreleased]` at the top

### 3. Verify the build and tests

```bash
dotnet restore
dotnet build -c Release
dotnet test tests/DotSerial.Tests.Unit
```

All must pass with **zero warnings**.

### 4. Tag the release

```bash
git add CHANGELOG.md
git commit -m "chore(release): prepare v{VERSION}"
git tag v{VERSION}
git push origin main --tags
```

MinVer will pick up the tag automatically — do NOT manually set `<Version>` in the csproj.

### 5. Verify CI

Check that `ci.yml` and `publish.yml` both pass on GitHub Actions.
The `publish.yml` workflow auto-packs and pushes to NuGet when a `v*` tag is pushed.

### 6. Draft a GitHub Release

Create a GitHub release from the new tag, copying the CHANGELOG entry as the release notes.

## Version tag format

Always `v` prefix: `v1.0.0`, `v1.1.0`, `v2.0.0`
Pre-release (untagged) builds automatically get a `-preview.N` suffix.
