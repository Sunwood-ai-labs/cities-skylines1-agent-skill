param(
    [string]$RepoPath = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = "Stop"

$docs = Join-Path $RepoPath "docs"
if (!(Test-Path -LiteralPath $docs)) {
    throw "docs directory was not found: $docs"
}

$linkableExtensions = @(".md", ".mts", ".json", ".svg")
$files = Get-ChildItem -LiteralPath $docs -Recurse -File |
    Where-Object {
        $linkableExtensions -contains $_.Extension -and
        $_.FullName -notmatch "\\.vitepress\\dist\\" -and
        $_.FullName -notmatch "\\node_modules\\"
    }
$readmes = @("README.md", "README.ja.md", "CONTRIBUTING.md", "CONTRIBUTING.ja.md") |
    ForEach-Object { Join-Path $RepoPath $_ } |
    Where-Object { Test-Path -LiteralPath $_ } |
    ForEach-Object { Get-Item -LiteralPath $_ }
$files = @($files) + @($readmes)

$failed = $false
$linkPattern = '\[[^\]]+\]\(([^)]+)\)'
$imagePattern = 'src:\s+([^\s]+)'
$htmlLinkPattern = '\b(?:href|src)="([^"]+)"'
$vitepressLinkPattern = 'link:\s+[''"]([^''"]+)[''"]'

function Resolve-DocsLink([System.IO.FileInfo]$file, [string]$target) {
    $clean = $target.Trim().Trim('"').Trim("'")
    if ($clean -match '^<(.+)>$') {
        $clean = $matches[1].Trim()
    }
    elseif ($clean -match '[<>]') {
        return $null
    }

    if ($clean -match '^(https?:|mailto:|#)') {
        return $null
    }
    $fromDocsRoot = $false
    if ($clean.StartsWith('/')) {
        $publicCandidate = Join-Path (Join-Path $docs "public") $clean.TrimStart('/')
        if (Test-Path -LiteralPath $publicCandidate) {
            return $publicCandidate
        }
        $clean = $clean.TrimStart('/')
        $fromDocsRoot = $true
    }
    if ($clean.Contains('#')) {
        $clean = $clean.Split('#')[0]
    }
    if ($clean -eq '') {
        return $null
    }

    $isRooted = $false
    try {
        $isRooted = [System.IO.Path]::IsPathRooted($clean)
    }
    catch [System.ArgumentException] {
        return $null
    }

    $candidate = if ($fromDocsRoot) {
        Join-Path $docs $clean
    }
    elseif ($isRooted) {
        Join-Path $docs $clean.TrimStart('\')
    }
    else {
        Join-Path $file.DirectoryName $clean
    }

    if (Test-Path -LiteralPath $candidate -PathType Leaf) {
        return $candidate
    }

    if ([System.IO.Path]::GetExtension($candidate) -eq '') {
        $mdCandidate = "$candidate.md"
        $indexCandidate = Join-Path $candidate "index.md"
        if (Test-Path -LiteralPath $mdCandidate) {
            return $mdCandidate
        }
        return $indexCandidate
    }

    return $candidate
}

foreach ($file in $files) {
    $text = Get-Content -LiteralPath $file.FullName -Raw

    foreach ($match in [regex]::Matches($text, $linkPattern)) {
        $target = $match.Groups[1].Value
        $resolved = Resolve-DocsLink $file $target
        if ($resolved -and !(Test-Path -LiteralPath $resolved)) {
            Write-Warning "Broken docs link in $($file.FullName): $target -> $resolved"
            $failed = $true
        }
    }

    foreach ($match in [regex]::Matches($text, $imagePattern)) {
        $target = $match.Groups[1].Value
        $resolved = Resolve-DocsLink $file $target
        if ($resolved -and !(Test-Path -LiteralPath $resolved)) {
            Write-Warning "Broken docs asset reference in $($file.FullName): $target -> $resolved"
            $failed = $true
        }
    }

    foreach ($pattern in @($htmlLinkPattern, $vitepressLinkPattern)) {
        foreach ($match in [regex]::Matches($text, $pattern)) {
            $target = $match.Groups[1].Value
            $resolved = Resolve-DocsLink $file $target
            if ($resolved -and !(Test-Path -LiteralPath $resolved)) {
                Write-Warning "Broken docs reference in $($file.FullName): $target -> $resolved"
                $failed = $true
            }
        }
    }
}

if ($failed) {
    exit 1
}

Write-Host "Docs links OK"
