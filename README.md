# EveryPage Library

**Everything Humanity Has Written, Open to Everyone.**

Live App: [https://everypage-library.pages.dev](https://everypage-library.pages.dev)

EveryPage Library is a federated digital library built with **Blazor WebAssembly (.NET 8)**, **Tailwind CSS**, and **Supabase**, designed with a tactile paper-and-ink comic strip aesthetic. It unifies community-contributed `.pdf` editions with open-access books aggregated in real time across seven open web catalogs.

---

## Key Features

- **7-Source Federated Catalog:** Searches community uploads alongside **Project Gutenberg (Gutendex)**, **Open Library**, **Internet Archive**, **Standard Ebooks**, **Sacred Texts**, **Wikibooks**, and **DOAB (Directory of Open Access Books)**.
- **6-Axis Card Catalog Taxonomy:** Filter editions across **Domain** (16 subjects), **Form & Structure**, **Factuality**, **Audience**, **Era**, and **Access / Rights** with memoized classification profiles.
- **1-Click Format Downloads & Auto-Indexing:** Resolves `.pdf`, `.epub`, and `.txt` editions across sources, persists catalog metadata, and tracks community download counts in Supabase.
- **Inline Document & Archive Reader:** Read community PDFs and Internet Archive editions directly inside the browser alongside a reader notes thread.
- **PDF-Only Community Shelf:** Signed-in contributors upload original works or rare editions strictly as `.pdf` files with bibliographic metadata and optional cover art.
- **Matchbox Ambience Audio Engine:** Persistent YouTube IFrame background audio player built into the sidebar for lofi beats, rain ambience, or custom YouTube links.
- **Role-Based Moderation & Audit Trail:** Auditors get exclusive access to the **Site Audit** sanctum to edit bibliographic metadata, soft-delete or restore catalog items, and inspect the append-only audit trail.

---

## Demo Accounts (Library Card Sign In)

Use the **Quick Demo Cards** inside the sign-in modal to test role-based features without registering:

- **Auditor (`Altair`):** Full catalog access plus the **Site Audit** moderation log, metadata editing, and soft-delete recovery.
- **Contributor (`Niccolo`):** Community `.pdf` shelf uploads and reader margin notes.

---

## Local Development

1. Configure your Supabase URL and publishable key in `EveryPageLibrary/wwwroot/appsettings.json`:
   ```json
   {
     "Supabase": {
       "Url": "https://YOUR_PROJECT_ID.supabase.co",
       "Key": "YOUR_SUPABASE_PUBLISHABLE_KEY"
     }
   }