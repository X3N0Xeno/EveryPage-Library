export async function onRequestGet(context) {
  const url = new URL(context.request.url);
  const rawQ = (url.searchParams.get("q") || "").trim();
  const limit = Math.min(parseInt(url.searchParams.get("limit") || "20", 10) || 20, 30);

  const corsHeaders = {
    "Content-Type": "application/json; charset=utf-8",
    "Access-Control-Allow-Origin": "*",
    "Cache-Control": "public, max-age=1800"
  };

  if (!rawQ) {
    return new Response(JSON.stringify({ results: [] }), { headers: corsHeaders });
  }

  const seen = new Set();
  const items = [];

  const addItem = (item) => {
    if (!item || !item.url || seen.has(item.url)) return;
    seen.add(item.url);
    items.push(item);
  };

  // 1. Query OAPEN & DOAB server-side (bypasses browser CORS 403 blocks)
  const fetchOapenLike = async (baseUrl, sourceName) => {
    try {
      const res = await fetch(`${baseUrl}/rest/search?query=${encodeURIComponent(rawQ)}&expand=metadata,bitstreams&limit=10`, {
        headers: { "Accept": "application/json", "User-Agent": "EveryPageLibrary/1.0" },
        signal: AbortSignal.timeout(5000)
      });
      if (!res.ok) return;
      const arr = await res.json();
      if (!Array.isArray(arr)) return;
      for (const entry of arr) {
        if (!entry.handle || !entry.name) continue;
        let pdfUrl = null;
        for (const bs of (entry.bitstreams || [])) {
          if (bs.retrieveLink && ((bs.mimeType || "").includes("pdf") || bs.retrieveLink.toLowerCase().endsWith(".pdf"))) {
            pdfUrl = `${baseUrl}${bs.retrieveLink}`;
            break;
          }
        }
        if (!pdfUrl) continue;
        addItem({
          title: entry.name.trim(),
          url: pdfUrl,
          snippet: `Peer-reviewed open-access PDF from ${sourceName}.`,
          host: sourceName,
          format: "pdf"
        });
      }
    } catch {}
  };

  // 2. Open-web direct PDF / EPUB search (excludes Scribd/Academia/ResearchGate paywalls)
  const fetchWebPdfMirrors = async () => {
    const searchQuery = `"${rawQ}" (filetype:pdf OR filetype:epub) -site:scribd.com -site:academia.edu -site:researchgate.net -site:studocu.com -site:coursehero.com`;
    const instances = [
      "https://searx.be/search",
      "https://search.sapti.me/search",
      "https://searx.tiekoetter.com/search",
      "https://priv.au/search"
    ];

    for (const base of instances) {
      try {
        const res = await fetch(`${base}?q=${encodeURIComponent(searchQuery)}&format=json&language=en`, {
          headers: { "User-Agent": "EveryPageLibrary/1.0" },
          signal: AbortSignal.timeout(5000)
        });
        if (!res.ok) continue;
        const data = await res.json();
        let found = 0;
        for (const r of (data.results || [])) {
          const link = (r.url || "").trim();
          const lower = link.toLowerCase();
          if (!lower.includes(".pdf") && !lower.includes(".epub")) continue;
          let host = "Open Web";
          try { host = new URL(link).hostname.replace(/^www\./, ""); } catch {}
          addItem({
            title: (r.title || rawQ).replace(/\s*\[PDF\]\s*/gi, "").trim(),
            url: link,
            snippet: (r.content || `Direct free file hosted on ${host}`).trim(),
            host,
            format: lower.includes(".pdf") ? "pdf" : "epub"
          });
          found++;
        }
        if (found > 0) return;
      } catch {}
    }

    // DuckDuckGo HTML fallback
    try {
      const res = await fetch(`https://html.duckduckgo.com/html/?q=${encodeURIComponent(searchQuery)}`, {
        headers: { "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36" },
        signal: AbortSignal.timeout(5500)
      });
      if (res.ok) {
        const html = await res.text();
        const linkRegex = /<a[^>]+class="result__a"[^>]+href="([^"]+)"[^>]*>([\s\S]*?)<\/a>/gi;
        let match;
        while ((match = linkRegex.exec(html)) !== null) {
          let href = match[1];
          const uddg = href.match(/[?&]uddg=([^&]+)/);
          if (uddg) href = decodeURIComponent(uddg[1]);
          const lower = href.toLowerCase();
          if (!lower.includes(".pdf") && !lower.includes(".epub")) continue;
          let host = "Open Web";
          try { host = new URL(href).hostname.replace(/^www\./, ""); } catch {}
          addItem({
            title: match[2].replace(/<[^>]+>/g, "").trim() || rawQ,
            url: href,
            snippet: `Direct ${lower.includes(".pdf") ? "PDF" : "EPUB"} hosted on ${host}`,
            host,
            format: lower.includes(".pdf") ? "pdf" : "epub"
          });
        }
      }
    } catch {}
  };

  await Promise.allSettled([
    fetchWebPdfMirrors(),
    fetchOapenLike("https://library.oapen.org", "OAPEN Library"),
    fetchOapenLike("https://directory.doabooks.org", "DOAB Academic")
  ]);

  return new Response(JSON.stringify({ results: items.slice(0, limit) }), { headers: corsHeaders });
}