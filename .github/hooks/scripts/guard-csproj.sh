#!/usr/bin/env bash
# guard-csproj.sh — preToolUse hook
# Warns when an agent is about to edit DotSerial.csproj

INPUT=$(cat)
TOOL_NAME=$(echo "$INPUT" | grep -o '"toolName":"[^"]*"' | cut -d'"' -f4)
TOOL_ARGS=$(echo "$INPUT" | grep -o '"toolArgs":"[^"]*"' | cut -d'"' -f4)

if [[ "$TOOL_NAME" == "edit" || "$TOOL_NAME" == "create" ]]; then
  if echo "$TOOL_ARGS" | grep -q "DotSerial\.csproj"; then
    echo '{"permissionDecision":"ask","permissionDecisionReason":"You are about to edit DotSerial.csproj. Verify: (1) TargetFrameworks = net10.0;net10.0-android;net10.0-ios, (2) Compile Remove blocks are correct for each platform file, (3) no manual <Version> property is set."}'
    exit 0
  fi
fi
