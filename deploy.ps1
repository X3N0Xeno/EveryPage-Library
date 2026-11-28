$ErrorActionPreference = "Stop"
$utf8 = [System.Text.UTF8Encoding]::new($false)

# 1. Restore standard Blazor boot script in EveryPageLibrary/wwwroot/index.html
$indexPath = "EveryPageLibrary\wwwroot\index.html"
$indexHtml = [System.IO.File]::ReadAllText($indexPath)
$indexHtml = [regex]::Replace(
    $indexHtml,
    '<script src="_framework/blazor\.webassembly\.js"[^>]*></script>\s*(<script>[\s\S]*?Blazor\.start\([\s\S]*?</script>)?',
    '<script src="_framework/blazor.webassembly.js"></script>'
)
[System.IO.File]::WriteAllText($indexPath, $indexHtml, $utf8)

# 2. Clean extra properties from EveryPageLibrary.csproj
$csprojPath = "EveryPageLibrary\EveryPageLibrary.csproj"
$csproj = [System.IO.File]::ReadAllText($csprojPath)
$csproj = $csproj -replace '\s*<BlazorCacheBootResources>false</BlazorCacheBootResources>', ''
$csproj = $csproj -replace '\s*<CompressionEnabled>false</CompressionEnabled>', ''
[System.IO.File]::WriteAllText($csprojPath, $csproj, $utf8)

# 3. Nuke bin, obj, and publish to eliminate mixed dotnet.js / dotnet.runtime.js versions
Remove-Item "EveryPageLibrary\bin", "EveryPageLibrary\obj", "publish" -Recurse -Force -ErrorAction SilentlyContinue

# 4. Build clean Release bundle
dotnet publish EveryPageLibrary -c Release -o publish

# 5. Add Cloudflare Pages SPA fallback
[System.IO.File]::WriteAllText("publish\wwwroot\_redirects", "/* /index.html 200`n", [System.Text.Encoding]::ASCII)

# 6. Deploy to Cloudflare Pages
npx wrangler pages deploy publish/wwwroot --project-name=everypage-library --commit-dirty=true