export async function onRequestGet(context) {
  const url = new URL(context.request.url);
  const rawQ = (url.searchParams.get("q") || "").trim();
  const limit = Math.min(parseInt(url.searchParams.get("limit") || "25", 10) || 25, 40);

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

  // 1. OAPEN & DOAB server-side (zero browser CORS blocks)
  const fetchDspaceBooks = async (baseUrl, sourceName) => {
    try {
      const res = await fetch(`${baseUrl}/rest/search?query=${encodeURIComponent(rawQ)}&expand=metadata,bitstreams&limit=12`, {
        headers: { "Accept": "application/json", "User-Agent": "EveryPageLibrary/1.0" },
        signal: AbortSignal.timeout(5500)
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

  // 2. OpenAlex Books server-side (bypasses browser 429 rate-limit pools)
  const fetchOpenAlexBooks = async () => {
    try {
      const api = `https://api.openalex.org/works?search=${encodeURIComponent(rawQ)}&filter=has_fulltext:true,is_oa:true,type:book&per-page=12&mailto=everypage@pages.dev`;
      const res = await fetch(api, {
        headers: { "User-Agent": "EveryPageLibrary/1.0 (mailto:everypage@pages.dev)" },
        signal: AbortSignal.timeout(5500)
      });
      if (!res.ok) return;
      const data = await res.json();
      for (const w of (data.results || [])) {
        const oaUrl = w?.open_access?.oa_url;
        if (!oaUrl || !w.title) continue;
        const authors = (w.authorships || []).slice(0, 2).map(a => a?.author?.display_name).filter(Boolean).join(", ");
        addItem({
          title: w.title.trim(),
          url: oaUrl,
          snippet: authors ? `Open-access book by ${authors} (${w.publication_year || ""}).` : "Open-access full-text book via OpenAlex.",
          host: authors || "OpenAlex Books",
          format: oaUrl.toLowerCase().includes(".epub") ? "epub" : "pdf"
        });
      }
    } catch {}
  };

  // 3. Internet Archive verified Text-PDFs (strictly excluding inlibrary/printdisabled/restricted)
  const fetchArchiveDirectPdfs = async () => {
    try {
      const q = `(${rawQ}) AND mediatype:texts AND format:"Text PDF" AND NOT collection:inlibrary AND NOT collection:printdisabled AND NOT access-restricted-item:true`;
      const api = `https://archive.org/advancedsearch.php?q=${encodeURIComponent(q)}&fl[]=identifier&fl[]=title&fl[]=creator&sort[]=downloads+desc&rows=12&output=json`;
      const res = await fetch(api, { signal: AbortSignal.timeout(5500) });
      if (!res.ok) return;
      const data = await res.json();
      for (const d of (data?.response?.docs || [])) {
        if (!d.identifier || !d.title) continue;
        const title = Array.isArray(d.title) ? d.title[0] : d.title;
        const creator = Array.isArray(d.creator) ? d.creator[0] : (d.creator || "Internet Archive");
        addItem({
          title: String(title).trim(),
          url: `https://archive.org/download/${encodeURIComponent(d.identifier)}/${encodeURIComponent(d.identifier)}.pdf`,
          snippet: `Direct public-domain PDF by ${creator}.`,
          host: String(creator),
          format: "pdf"
        });
      }
    } catch {}
  };

  await Promise.allSettled([
    fetchDspaceBooks("https://library.oapen.org", "OAPEN Library"),
    fetchDspaceBooks("https://directory.doabooks.org", "DOAB Academic"),
    fetchOpenAlexBooks(),
    fetchArchiveDirectPdfs()
  ]);

  return new Response(JSON.stringify({ results: items.slice(0, limit) }), { headers: corsHeaders });
}