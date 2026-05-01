---
name: issue-triage
description: >
  Automatically triage new issues opened in the DotSerial repository.
  Adds labels, checks for duplicates, and posts a helpful first response.
triggers:
  - event: issues
    types: [opened]
permissions:
  issues: write
  pull-requests: read
---

# DotSerial Issue Triage

When a new issue is opened, perform the following steps:

## 1. Read the issue

Read the issue title, body, and any attached code/logs carefully.

## 2. Classify and label

Apply one or more of the following labels based on the issue content:

| Label | When to apply |
|-------|---------------|
| `bug` | Reporter describes unexpected behavior with a reproducible scenario |
| `enhancement` | Feature request or improvement suggestion |
| `question` | User needs help or clarification (not a bug) |
| `platform:windows` | Issue is Windows-specific |
| `platform:linux` | Issue is Linux-specific |
| `platform:macos` | Issue is macOS-specific |
| `platform:android` | Issue is Android-specific |
| `platform:ios` | Issue is iOS-specific |
| `connection:bluetooth` | Issue involves Bluetooth connection type |
| `connection:network` | Issue involves Network (TCP) connection type |
| `documentation` | Issue is about missing or incorrect documentation |
| `duplicate` | Issue is a duplicate of an existing open issue |
| `needs-reproduction` | Bug report lacks a reproducible example |

## 3. Check for duplicates

Search open issues for similar titles or descriptions using the GitHub MCP tools.
If a duplicate is found, apply the `duplicate` label and post a comment pointing to the original.

## 4. Post a first response

Post a comment on the issue using this template, adapted to the specific content:

```
Thank you for opening this issue! 👋

[If bug]: Could you share a minimal code example that reproduces the problem?
Include the port name, OS, .NET version, and the exact exception or unexpected behavior.

[If enhancement]: This is on our radar — feel free to open a PR if you'd like to contribute!

[If question]: Here's a quick pointer: [relevant README section / code example]
```

## 5. Assign milestone (if applicable)

If the issue clearly maps to a planned version, assign it to the appropriate milestone.
