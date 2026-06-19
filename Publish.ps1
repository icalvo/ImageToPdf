Write-Host "Existing versions:"
$json = dotnet package search imagetopdf --exact-match --format json | ConvertFrom-Json
$json.searchResult | ForEach-Object { $_.packages | ForEach-Object { $_.version } }

$version = Read-Host "New Version"
if (-not $version) { throw "Version is required." }

$content = Get-Content ImageToPdf.cs -Raw
$newContent = $content -replace '(?m)^#:property Version=.*$', "#:property Version=$version"
if ($newContent -eq $content) { throw "Could not update Version in ImageToPdf.cs." }
Set-Content ImageToPdf.cs -Value $newContent -NoNewline

git add ImageToPdf.cs
git commit -m "Bump version to $version"
git push
