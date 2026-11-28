$ErrorActionPreference = "Stop"
$utf8 = [System.Text.UTF8Encoding]::new($false)

$indexPath = "EveryPageLibrary\wwwroot\index.html"
$indexHtml = [System.IO.File]::ReadAllText($indexPath)
$indexHtml = [regex]::Replace(
    $indexHtml,
    '<script src="_framework/blazor\.webassembly\.js"[^>]*></script>\s*(<script>[\s\S]*?Blazor\.start\([\s\S]*?</script>)?',
    '<script src="_framework/blazor.webassembly.js"></script>'
)
[System.IO.File]::WriteAllText($indexPath, $indexHtml, $utf8)

$csprojPath = "EveryPageLibrary\EveryPageLibrary.csproj"
$csproj = [System.IO.File]::ReadAllText($csprojPath)
$csproj = $csproj -replace '\s*<BlazorCacheBootResources>false</BlazorCacheBootResources>', ''
$csproj = $csproj -replace '\s*<CompressionEnabled>false</CompressionEnabled>', ''
[System.IO.File]::WriteAllText($csprojPath, $csproj, $utf8)

Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 300
Remove-Item "EveryPageLibrary\bin", "EveryPageLibrary\obj", "publish" -Recurse -Force -ErrorAction SilentlyContinue

dotnet restore EveryPageLibrary\EveryPageLibrary.csproj --force
dotnet publish EveryPageLibrary\EveryPageLibrary.csproj -c Release -o publish --no-restore

New-Item -ItemType Directory -Path "publish\wwwroot" -Force | Out-Null
[System.IO.File]::WriteAllText("publish\wwwroot\_redirects", "/* /index.html 200`n", [System.Text.Encoding]::ASCII)

npx wrangler pages deploy publish/wwwroot --project-name=everypage-library --commit-dirty=true