---
name: nuget-pack
description: >
  Pack and verify the DotSerial NuGet package. Use when asked to pack, publish,
  or verify the NuGet output for the DotSerial library.
allowed-tools: shell
---

# DotSerial NuGet Pack Skill

DotSerial uses **MinVer** for automatic versioning from git tags.

## Pack the NuGet package locally

```bash
dotnet pack src/DotSerial -c Release -o ./artifacts
```

This produces:
- `artifacts/DotSerial.<version>.nupkg` — the main package
- `artifacts/DotSerial.<version>.snupkg` — the symbol package (for source debugging)

## Inspect the package contents

```bash
# List files inside the nupkg (it is a zip)
unzip -l ./artifacts/DotSerial.*.nupkg
```

## Versioning rules (MinVer)

- Version is derived from the nearest git tag with `v` prefix: e.g., `v1.2.0` → `1.2.0`
- Untagged commits get a pre-release suffix: e.g., `1.2.0-preview.3`
- Do **NOT** manually set `<Version>` or `<VersionPrefix>` in the csproj

## Create a release tag

```bash
git tag v1.2.0
git push origin v1.2.0
```

The CI `publish.yml` workflow will then automatically pack and push to NuGet.

## Verify the package before publishing

Check that:
1. All three TFMs are present: `net10.0`, `net10.0-android`, `net10.0-ios`
2. `README.md` is embedded as the package readme
3. The `.snupkg` is generated alongside the `.nupkg`
