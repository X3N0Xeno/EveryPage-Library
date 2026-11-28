export async function onRequestGet(context) {
  const url = new URL(context.request.url);
  const rawQ = (url.searchParams.get("q") || "").trim();
  const limit = Math.min(parseInt(url.searchParams.get("limit") || "15", 10) || 15, 25);

  const corsHeaders = {
    "Content-Type": "application/json; charset=utf-8",
    "Access-Control-Allow-Origin": "*",
    "Cache-Control": "public, max-age=1800"
  };

  if (!rawQ) {
    return new Response(JSON.stringify({ results: [] }), { headers: corsHeaders });
  }

  const searchQuery = `"${rawQ}" (filetype:pdf OR filetype:epub) -site:scribd.com -site:academia.edu -site:researchgate.net -site:studocu.com -site:coursehero.com`;

  const instances = [
    "https://searx.be/search",
    "https://search.sapti.me/search",
    "https://searx.tiekoetter.com/search",
    "https://priv.au/search"
  ];

  const seen = new Set();
  const items = [];

  for (const base of instances) {
    if (items.length >= limit) break;
    try {
      const target = `${base}?q=${encodeURIComponent(searchQuery)}&format=json&language=en`;
      const res = await fetch(target, {
        headers: { "User-Agent": "EveryPageLibrary/1.0 (+https://everypage-library.pages.dev)" },
        signal: AbortSignal.timeout(5500)
      });
      if (!res.ok) continue;

      const data = await res.json();
      for (const r of (data.results || [])) {
        const link = (r.url || "").trim();
        if (!link || seen.has(link)) continue;
        const lower = link.toLowerCase();
        const isPdf = lower.includes(".pdf");
        const isEpub = lower.includes(".epub");
        if (!isPdf && !isEpub) continue;

        seen.add(link);
        let host = "Open Web";
        try { host = new URL(link).hostname.replace(/^www\./, ""); } catch {}

        items.push({
          title: (r.title || rawQ).replace(/\s*\[PDF\]\s*/gi, "").trim(),
          url: link,
          snippet: (r.content || "").trim(),
          host,
          format: isPdf ? "pdf" : "epub"
        });
        if (items.length >= limit) break;
      }
      if (items.length > 0) break;
    } catch {
      // Try next public SearXNG instance
    }
  }

  // Fallback: DuckDuckGo HTML lite parser if SearXNG instances rate-limit
  if (items.length === 0) {
    try {
      const ddgUrl = `https://html.duckduckgo.com/html/?q=${encodeURIComponent(searchQuery)}`;
      const res = await fetch(ddgUrl, {
        headers: { "User-Agent": "Mozilla/5.0 (compatible; EveryPageLibrary/1.0)" },
        signal: AbortSignal.timeout(6000)
      });
      if (res.ok) {
        const html = await res.text();
        const linkRegex = /<a[^>]+class="result__a"[^>]+href="([^"]+)"[^>]*>([\s\S]*?)<\/a>/gi;
        let match;
        while ((match = linkRegex.exec(html)) !== null && items.length < limit) {
          let href = match[1];
          const uddg = href.match(/[?&]uddg=([^&]+)/);
          if (uddg) href = decodeURIComponent(uddg[1]);
          const lower = href.toLowerCase();
          if (!lower.includes(".pdf") && !lower.includes(".epub")) continue;
          if (seen.has(href)) continue;

          seen.add(href);
          let host = "Open Web";
          try { host = new URL(href).hostname.replace(/^www\./, ""); } catch {}
          const cleanTitle = match[2].replace(/<[^>]+>/g, "").trim() || rawQ;

          items.push({
            title: cleanTitle,
            url: href,
            snippet: `Direct ${lower.includes(".pdf") ? "PDF" : "EPUB"} hosted on ${host}`,
            host,
            format: lower.includes(".pdf") ? "pdf" : "epub"
          });
        }
      }
    } catch {}
  }

  return new Response(JSON.stringify({ results: items }), { headers: corsHeaders });
}