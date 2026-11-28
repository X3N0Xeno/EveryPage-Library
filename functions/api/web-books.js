export async function onRequestGet(context) {
  const url = new URL(context.request.url);
  const rawQ = (url.searchParams.get("q") || "").trim();
  const limit = Math.min(parseInt(url.searchParams.get("limit") || "40", 10) || 40, 60);
  const serperKey = context.env?.SERPER_API_KEY || "";

  const corsHeaders = {
    "Content-Type": "application/json; charset=utf-8",
    "Access-Control-Allow-Origin": "*",
    "Cache-Control": "public, max-age=1800"
  };

  if (!rawQ) {
    return new Response(JSON.stringify({ results: [] }), { headers: corsHeaders });
  }

  const aliases = {
    "lotr": "Lord of the Rings",
    "got": "Game of Thrones",
    "asoiaf": "A Song of Ice and Fire",
    "hp": "Harry Potter",
    "bible": "Holy Bible",
    "narnia": "Chronicles of Narnia"
  };
  const expandedQ = aliases[rawQ.toLowerCase()] || rawQ;

  const seen = new Set();
  const items = [];
  const addItem = (item) => {
    if (!item || !item.url || seen.has(item.url)) return;
    if (!/^https?:\/\//i.test(item.url)) return;
    seen.add(item.url);
    items.push(item);
  };

  // Resolve a cover image by book title/query via Open Library Covers (free, no key) or Serper Images
  const resolveCoverUrl = async (queryText) => {
    try {
      const olRes = await fetch(`https://openlibrary.org/search.json?q=${encodeURIComponent(queryText)}&fields=cover_i&limit=3`, {
        signal: AbortSignal.timeout(4000)
      });
      if (olRes.ok) {
        const olData = await olRes.json();
        const hit = (olData.docs || []).find(d => d.cover_i);
        if (hit?.cover_i) {
          return `https://covers.openlibrary.org/b/id/${hit.cover_i}-L.jpg`;
        }
      }
    } catch {}

    if (serperKey) {
      try {
        const imgRes = await fetch("https://google.serper.dev/images", {
          method: "POST",
          headers: { "X-API-Key": serperKey, "Content-Type": "application/json" },
          body: JSON.stringify({ q: `${queryText} book cover`, num: 1 }),
          signal: AbortSignal.timeout(4000)
        });
        if (imgRes.ok) {
          const imgData = await imgRes.json();
          if (imgData?.images?.[0]?.imageUrl) return imgData.images[0].imageUrl;
        }
      } catch {}
    }
    return null;
  };

  // 1. Real Google Search via Serper.dev (finds every indexed .pdf and .epub across the entire web)
  const fetchGooglePdfs = async (ext) => {
    if (!serperKey) return;
    try {
      const res = await fetch("https://google.serper.dev/search", {
        method: "POST",
        headers: { "X-API-Key": serperKey, "Content-Type": "application/json" },
        body: JSON.stringify({ q: `${expandedQ} filetype:${ext}`, num: 20 }),
        signal: AbortSignal.timeout(6000)
      });
      if (!res.ok) return;
      const data = await res.json();
      for (const r of data.organic || []) {
        if (!r.link) continue;
        let host = "Web PDF";
        try { host = new URL(r.link).hostname.replace(/^www\./, ""); } catch {}
        addItem({
          title: (r.title || expandedQ).replace(/\s*\[PDF\]\s*/gi, "").trim(),
          url: r.link,
          snippet: r.snippet || `Direct ${ext.toUpperCase()} found via Google Search (${host}).`,
          host,
          format: ext
        });
      }
    } catch {}
  };

  // 2. Internet Archive Broadened Direct PDFs & EPUBs (searches title + creator across all PDF formats)
  const fetchArchiveDirect = async () => {
    try {
      const q = `(title:(${expandedQ}) OR (${expandedQ})) AND mediatype:texts AND format:(PDF OR EPUB) AND NOT collection:printdisabled`;
      const api = `https://archive.org/advancedsearch.php?q=${encodeURIComponent(q)}&fl[]=identifier&fl[]=title&fl[]=creator&fl[]=format&rows=20&output=json`;
      const res = await fetch(api, { signal: AbortSignal.timeout(6500) });
      if (!res.ok) return;
      const data = await res.json();
      for (const d of data?.response?.docs || []) {
        if (!d.identifier || !d.title) continue;
        const title = Array.isArray(d.title) ? d.title[0] : d.title;
        const creator = Array.isArray(d.creator) ? d.creator[0] : (d.creator || "Internet Archive");
        const formats = Array.isArray(d.format) ? d.format.join(" ").toLowerCase() : String(d.format || "").toLowerCase();
        const ext = formats.includes("pdf") ? "pdf" : "epub";
        const idEnc = encodeURIComponent(d.identifier);
        addItem({
          title: String(title).trim(),
          url: `https://archive.org/download/${idEnc}/${idEnc}.${ext}`,
          coverUrl: `https://archive.org/services/img/${idEnc}`,
          snippet: `Full-text ${ext.toUpperCase()} by ${creator} (Internet Archive).`,
          host: String(creator),
          format: ext
        });
      }
    } catch {}
  };

  // 3. OAPEN, DOAB & Wikibooks Open-Access Full-Text Books
  const fetchDspaceBooks = async (baseUrl, sourceName) => {
    try {
      const res = await fetch(`${baseUrl}/rest/search?query=${encodeURIComponent(expandedQ)}&expand=bitstreams&limit=8`, {
        headers: { "Accept": "application/json", "User-Agent": "EveryPageLibrary/1.0" },
        signal: AbortSignal.timeout(5000)
      });
      if (!res.ok) return;
      const arr = await res.json();
      if (!Array.isArray(arr)) return;
      for (const entry of arr) {
        const pdfBs = (entry.bitstreams || []).find(bs => bs.retrieveLink && ((bs.mimeType || "").includes("pdf") || bs.retrieveLink.toLowerCase().endsWith(".pdf")));
        if (!entry.name || !pdfBs) continue;
        addItem({
          title: entry.name.trim(),
          url: `${baseUrl}${pdfBs.retrieveLink}`,
          snippet: `Open-access PDF from ${sourceName}.`,
          host: sourceName,
          format: "pdf"
        });
      }
    } catch {}
  };

  const fetchWikibooks = async () => {
    try {
      const api = `https://en.wikibooks.org/w/api.php?action=query&list=search&srsearch=${encodeURIComponent(expandedQ)}&srnamespace=0&srlimit=6&format=json&origin=*`;
      const res = await fetch(api, { signal: AbortSignal.timeout(5000) });
      if (!res.ok) return;
      const data = await res.json();
      for (const hit of data?.query?.search || []) {
        if (!hit.title || hit.title.includes("/")) continue;
        const slug = encodeURIComponent(hit.title.replace(/ /g, "_"));
        addItem({
          title: hit.title.trim(),
          url: `https://en.wikibooks.org/api/rest_v1/page/pdf/${slug}`,
          snippet: "Open-license textbook & manual (Wikibooks PDF).",
          host: "Wikibooks",
          format: "pdf"
        });
      }
    } catch {}
  };

  const [sharedCover] = await Promise.all([
    resolveCoverUrl(expandedQ),
    Promise.allSettled([
      fetchGooglePdfs("pdf"),
      fetchGooglePdfs("epub"),
      fetchArchiveDirect(),
      fetchDspaceBooks("https://library.oapen.org", "OAPEN Library"),
      fetchDspaceBooks("https://directory.doabooks.org", "DOAB Academic"),
      fetchWikibooks()
    ])
  ]);

  // Attach cover art to every result that doesn't already have a thumbnail
  for (const item of items) {
    if (!item.coverUrl && sharedCover) {
      item.coverUrl = sharedCover;
    }
  }

  return new Response(JSON.stringify({ results: items.slice(0, limit) }), { headers: corsHeaders });
}