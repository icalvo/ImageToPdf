"Existing versions:"
dotnet package search imagetopdf --exact-match --format json | jq .searchResult[].packages[].version -r
$version = Read-Host "New Version"
gh workflow run 296520268 -f version=$version
gh run watch
