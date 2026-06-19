Write-Host "Existing versions:"
$json = dotnet package search imagetopdf --exact-match --format json | ConvertFrom-Json
$json.searchResult | ForEach-Object { $_.packages | ForEach-Object { $_.version } }
