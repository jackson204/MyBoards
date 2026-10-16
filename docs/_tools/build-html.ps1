<#
.SYNOPSIS
    將 docs/ 底下的 Markdown 筆記轉成 HTML（輸出到 docs/html/，保留相同資料夾結構）。

.DESCRIPTION
    Markdown 原文會嵌入 template.html，開啟時在瀏覽器端用 marked 轉換、mermaid 畫圖、highlight.js 上色。
    Markdown 是唯一來源；修改 .md 後重新執行本腳本即可。開啟 HTML 需要網路（從 CDN 載入函式庫）。

.EXAMPLE
    ./docs/_tools/build-html.ps1                                   # 轉換全部
    ./docs/_tools/build-html.ps1 docs/notes/ef-core/connect-database.md   # 只轉換一個檔案
#>
param([string]$Path)

$ErrorActionPreference = 'Stop'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

$docsRoot = Split-Path $PSScriptRoot -Parent
$outRoot = Join-Path $docsRoot 'html'
$template = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'template.html'), $utf8NoBom)

$files = if ($Path) {
    Get-Item (Resolve-Path $Path)
} else {
    Get-ChildItem $docsRoot -Recurse -Filter *.md | Where-Object { -not $_.FullName.StartsWith($outRoot) }
}

foreach ($file in $files) {
    $relative = $file.FullName.Substring($docsRoot.Length + 1)
    $markdown = [IO.File]::ReadAllText($file.FullName, $utf8NoBom)

    if ($markdown -match '</script') {
        Write-Warning "略過 ${relative}：內容含有 </script，無法嵌入 HTML"
        continue
    }

    $titleLine = ($markdown -split "`r?`n") | Where-Object { $_ -match '^# ' } | Select-Object -First 1
    $title = if ($titleLine) { $titleLine.Substring(2).Trim() } else { $file.BaseName }
    $title = [System.Net.WebUtility]::HtmlEncode($title)

    $depth = ($relative -split '[\\/]').Count - 1
    $root = '../' * $depth

    $html = $template.Replace('{{TITLE}}', $title).Replace('{{ROOT}}', $root).Replace('{{MARKDOWN}}', $markdown)

    $outFile = Join-Path $outRoot ([IO.Path]::ChangeExtension($relative, '.html'))
    New-Item -ItemType Directory -Force (Split-Path $outFile -Parent) | Out-Null
    [IO.File]::WriteAllText($outFile, $html, $utf8NoBom)
    Write-Host "✓ $relative → html/$([IO.Path]::ChangeExtension($relative, '.html') -replace '\\', '/')"
}
