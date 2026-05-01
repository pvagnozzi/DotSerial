---
name: add-copyright-header
description: >
  Verify and add the required copyright header to DotSerial C# source files.
  Use when creating a new .cs file or when a file is missing the header.
---

# DotSerial Copyright Header Skill

Every `.cs` file in the DotSerial repository **must** start with the following header block.
The header must be the very first content in the file, before `namespace`, `using`, or any other statement.

## Required header template

Replace `{FILENAME}` with the actual file name (including `.cs` extension) and
`{ONE LINE SUMMARY}` with a concise single-line description of the file's purpose.

```csharp
// -----------------------------------------------------------------------
// <copyright file="{FILENAME}" company="Piergiorgio Vagnozzi">
//     Copyright (c) 2026 Piergiorgio Vagnozzi. All rights reserved.
//     Licensed under the MIT License.
//     See LICENSE file in the project root for full license information.
// </copyright>
// <summary>
//     {ONE LINE SUMMARY}
// </summary>
// <created>2026-05-01</created>
// -----------------------------------------------------------------------
```

## How to check for missing headers

Search for files that do NOT start with the copyright block:

```bash
# Files in src/ missing the header
grep -rL "Copyright (c) 2026 Piergiorgio Vagnozzi" src/ --include="*.cs"

# Files in tests/ missing the header
grep -rL "Copyright (c) 2026 Piergiorgio Vagnozzi" tests/ --include="*.cs"
```

## Rules

- The `<created>` date is always `2026-05-01` (project inception date) — do not change it.
- The `company` attribute is always `Piergiorgio Vagnozzi`.
- The header is required for **all** `.cs` files: source, tests, generated (if any).
