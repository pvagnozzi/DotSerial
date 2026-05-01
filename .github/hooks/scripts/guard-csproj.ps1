# guard-csproj.ps1 — preToolUse hook (PowerShell)
# Warns when an agent is about to edit DotSerial.csproj

$input_data = $input | Out-String
$toolName = if ($input_data -match '"toolName":"([^"]+)"') { $matches[1] } else { "" }
$toolArgs = if ($input_data -match '"toolArgs":"([^"]+)"') { $matches[1] } else { "" }

if (($toolName -eq "edit" -or $toolName -eq "create") -and $toolArgs -match "DotSerial\.csproj") {
    Write-Output '{"permissionDecision":"ask","permissionDecisionReason":"You are about to edit DotSerial.csproj. Verify: (1) TargetFrameworks = net10.0;net10.0-android;net10.0-ios, (2) Compile Remove blocks are correct for each platform file, (3) no manual <Version> property is set."}'
}
