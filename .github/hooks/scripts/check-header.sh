#!/usr/bin/env bash
# check-header.sh — postToolUse hook
# Warns if a newly written/edited .cs file is missing the required copyright header

INPUT=$(cat)
TOOL_NAME=$(echo "$INPUT" | grep -o '"toolName":"[^"]*"' | cut -d'"' -f4)

if [[ "$TOOL_NAME" != "edit" && "$TOOL_NAME" != "create" && "$TOOL_NAME" != "write" ]]; then
  exit 0
fi

# Extract path from toolArgs JSON (handles escaped quotes in the grep output)
FILE_PATH=$(echo "$INPUT" | python3 -c "
import sys, json, re
data = sys.stdin.read()
# toolArgs is a JSON string embedded inside the outer JSON
m = re.search(r'\"toolArgs\":\"(.*?)\"(?:,|\})', data)
if m:
    try:
        args = json.loads(m.group(1).replace('\\\\\"','\"').replace('\\\\\\\\','\\\\'))
        if isinstance(args, dict):
            print(args.get('path', ''))
    except Exception:
        pass
" 2>/dev/null)

if [[ -z "$FILE_PATH" || "${FILE_PATH##*.}" != "cs" ]]; then
  exit 0
fi

if [[ -f "$FILE_PATH" ]] && ! grep -q "Copyright (c) 2026 Piergiorgio Vagnozzi" "$FILE_PATH"; then
  echo "{\"warning\": \"$FILE_PATH is missing the required copyright header. Add it before committing.\"}"
fi
