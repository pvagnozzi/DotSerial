# check-header.ps1 — postToolUse hook (PowerShell)
# Warns if a newly written/edited .cs file is missing the required copyright header

$input_data = $input | Out-String
$toolName = if ($input_data -match '"toolName":"([^"]+)"') { $matches[1] } else { "" }

if ($toolName -notmatch "edit|create|write") { exit 0 }

try {
    # Extract toolArgs JSON string
    if ($input_data -match '"toolArgs":"((?:[^"\\]|\\.)*)"') {
        $toolArgsRaw = $matches[1] -replace '\\"', '"' -replace '\\\\', '\'
        $toolArgs = $toolArgsRaw | ConvertFrom-Json -ErrorAction SilentlyContinue
        $filePath = $toolArgs.path
        if ($filePath -and $filePath.EndsWith(".cs") -and (Test-Path $filePath)) {
            $content = Get-Content $filePath -Raw -ErrorAction SilentlyContinue
            if ($content -and -not ($content -match "Copyright \(c\) 2026 Piergiorgio Vagnozzi")) {
                Write-Output "{`"warning`": `"$filePath is missing the required copyright header. Add it before committing.`"}"
            }
        }
    }
} catch {
    # Silently ignore parse errors
}
