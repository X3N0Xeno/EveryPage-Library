using System;
using System.Collections.Generic;
using System.Linq;

namespace EverydayLibrary.Models;

public sealed record BookFacetProfile(
    string Form,
    string Factuality,
    string Domain,
    string Audience,
    string Era,
    string Rights
);

public static class BookGenres
{
    // Axis 1: Form & Structure (How it is written)
    public static readonly IReadOnlyList<string> FormTags = new[]
    {
        "Prose-Narrative",
        "Verse-Poetry",
        "Drama-Script",
        "Sequential-Art",
        "Epistolary-Documentary",
        "Reference-Tabular",
        "Academic-Treatise"
    };

    // Axis 2: Factuality (Relationship to reality)
    public static readonly IReadOnlyList<string> FactualityTags = new[]
    {
        "Pure-Fiction",
        "Historical-Biographical-Fiction",
        "Objective-Nonfiction",
        "Speculative-Philosophical-Nonfiction",
        "Instructional-Applied"
    };

    // Axis 3: Primary Domain / Subject — Fiction
    public static readonly IReadOnlyList<string> FictionDomainTags = new[]
    {
        "Realist-Contemporary",
        "Historical-Period",
        "Speculative-SciFi",
        "Speculative-Fantasy",
        "Horror-Macabre",
        "Crime-Mystery-Espionage",
        "Romance-Relational",
        "Adventure-Survival"
    };

    // Axis 3: Primary Domain / Subject — Non-Fiction
    public static readonly IReadOnlyList<string> NonFictionDomainTags = new[]
    {
        "Formal-Sciences",
        "Natural-Sciences",
        "Applied-Sciences-Tech",
        "Social-Sciences",
        "Humanities-Arts",
        "Biography-Memoir",
        "Lifestyle-Practical"
    };

    public static readonly IReadOnlyList<string> DomainTags =
        FictionDomainTags.Concat(NonFictionDomainTags).ToArray();

    public const string General = "Humanities-Arts";

    public static IReadOnlyList<string> Selectable => DomainTags;

    public static string ToLabel(string? raw)
    {
        var normalized = ToStorage(raw);
        return normalized switch
        {
            "Realist-Contemporary" => "Realist & Contemporary",
            "Historical-Period" => "Historical & Period",
            "Speculative-SciFi" => "Science Fiction",
            "Speculative-Fantasy" => "Fantasy",
            "Horror-Macabre" => "Horror & Macabre",
            "Crime-Mystery-Espionage" => "Crime, Mystery & Espionage",
            "Romance-Relational" => "Romance & Relational",
            "Adventure-Survival" => "Adventure & Survival",
            "Formal-Sciences" => "Formal Sciences",
            "Natural-Sciences" => "Natural Sciences",
            "Applied-Sciences-Tech" => "Applied Sciences & Tech",
            "Social-Sciences" => "Social Sciences",
            "Humanities-Arts" => "Humanities & Arts",
            "Biography-Memoir" => "Biography & Memoir",
            "Lifestyle-Practical" => "Lifestyle & Practical",
            _ => normalized.Replace('-', ' ')
        };
    }

    public static readonly IReadOnlyList<string> Filters =
        new[] { "All" }.Concat(DomainTags).ToArray();

    // Axis 4: Target Audience
    public static readonly IReadOnlyList<string> AudienceTags = new[]
    {
        "Early-Reader",
        "Middle-Grade",
        "Young-Adult",
        "General-Adult",
        "Specialist-Scholarly"
    };

    // Axis 5: Era of Origin
    public static readonly IReadOnlyList<string> EraTags = new[]
    {
        "Ancient",
        "Medieval",
        "Early-Modern",
        "19th-Century",
        "20th-Century",
        "21st-Century"
    };

    // Axis 6: Rights & Access Status
    public static readonly IReadOnlyList<string> RightsTags = new[]
    {
        "Public-Domain",
        "Open-License",
        "Rights-Cleared-Free",
        "Controlled-Lending"
    };

    public static string ToStorage(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || raw.Equals("All", StringComparison.OrdinalIgnoreCase))
            return "Humanities-Arts";

        var exact = DomainTags.FirstOrDefault(t => t.Equals(raw.Trim(), StringComparison.OrdinalIgnoreCase));
        if (exact != null)
            return exact;

        return InferDomain(raw);
    }

    public static BookFacetProfile Classify(Book book)
    {
        var haystack = $"{book.Title} {book.Category} {book.Synopsis} {book.SourceName}".ToLowerInvariant();
        var domain = ToStorage(string.IsNullOrWhiteSpace(book.Category) ? haystack : $"{book.Category} {haystack}");
        var form = InferForm(haystack, book.SourceName);
        var factuality = InferFactuality(haystack, domain, form);
        var audience = InferAudience(haystack, book.SourceName, form);
        var era = InferEra(book.PublicationYear, haystack);
        var rights = InferRights(book);

        return new BookFacetProfile(form, factuality, domain, audience, era, rights);
    }

    public static string InferForm(string haystack, string? sourceName = null)
    {
        if (haystack.Contains("comic") || haystack.Contains("graphic novel") || haystack.Contains("manga") || haystack.Contains("compendium"))
            return "Sequential-Art";

        if (haystack.Contains("poetry") || haystack.Contains("poems") || haystack.Contains("sonnet") || haystack.Contains("verse") || haystack.Contains("anthology of poems"))
            return "Verse-Poetry";

        if (haystack.Contains("drama") || haystack.Contains("stage play") || haystack.Contains("screenplay") || haystack.Contains("tragedy") || haystack.Contains("comedy in") || haystack.Contains("acts"))
            return "Drama-Script";

        if (haystack.Contains("letters") || haystack.Contains("correspondence") || haystack.Contains("diary") || haystack.Contains("diaries") || haystack.Contains("journal") || haystack.Contains("speeches") || haystack.Contains("transcript"))
            return "Epistolary-Documentary";

        if (haystack.Contains("dictionary") || haystack.Contains("encyclopedia") || haystack.Contains("atlas") || haystack.Contains("directory") || haystack.Contains("almanac") || haystack.Contains("glossary"))
            return "Reference-Tabular";

        if (string.Equals(sourceName, "DOAB Academic", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(sourceName, "Wikibooks", StringComparison.OrdinalIgnoreCase) ||
            haystack.Contains("textbook") || haystack.Contains("monograph") || haystack.Contains("dissertation") || haystack.Contains("treatise") || haystack.Contains("proceedings"))
            return "Academic-Treatise";

        return "Prose-Narrative";
    }

    public static string InferFactuality(string haystack, string domain, string form)
    {
        if (haystack.Contains("how to") || haystack.Contains("how-to") || haystack.Contains("cookbook") || haystack.Contains("recipe") ||
            haystack.Contains("manual") || haystack.Contains("guide to") || haystack.Contains("tutorial") || haystack.Contains("self-help") ||
            domain == "Lifestyle-Practical")
            return "Instructional-Applied";

        if (haystack.Contains("historical fiction") || haystack.Contains("biographical novel") || haystack.Contains("based on a true"))
            return "Historical-Biographical-Fiction";

        if (FictionDomainTags.Contains(domain))
            return "Pure-Fiction";

        if (haystack.Contains("philosoph") || haystack.Contains("theolog") || haystack.Contains("religion") || haystack.Contains("mytholog") ||
            haystack.Contains("esoteric") || haystack.Contains("sacred") || haystack.Contains("critique") || haystack.Contains("essay") ||
            haystack.Contains("ethics") || haystack.Contains("metaphysics"))
            return "Speculative-Philosophical-Nonfiction";

        return "Objective-Nonfiction";
    }

    public static string InferDomain(string haystack)
    {
        var text = haystack.ToLowerInvariant();

        if (text.Contains("science fiction") || text.Contains("sci-fi") || text.Contains("cyberpunk") || text.Contains("dystopia") || text.Contains("space opera") || text.Contains("time travel"))
            return "Speculative-SciFi";

        if (text.Contains("fantasy") || text.Contains("wizard") || text.Contains("dragon") || text.Contains("magic") || text.Contains("fairy tale") || text.Contains("mythic"))
            return "Speculative-Fantasy";

        if (text.Contains("horror") || text.Contains("gothic") || text.Contains("macabre") || text.Contains("ghost") || text.Contains("vampire") || text.Contains("supernatural dread"))
            return "Horror-Macabre";

        if (text.Contains("crime") || text.Contains("mystery") || text.Contains("detective") || text.Contains("espionage") || text.Contains("thriller") || text.Contains("murder"))
            return "Crime-Mystery-Espionage";

        if (text.Contains("romance") || text.Contains("love story") || text.Contains("courtship") || text.Contains("marriage"))
            return "Romance-Relational";

        if (text.Contains("adventure") || text.Contains("survival") || text.Contains("expedition") || text.Contains("voyage") || text.Contains("pirate") || text.Contains("western"))
            return "Adventure-Survival";

        if (text.Contains("historical") || text.Contains("19th century") || text.Contains("victorian") || text.Contains("regency") || text.Contains("medieval"))
            return "Historical-Period";

        if (text.Contains("realist") || text.Contains("contemporary fiction") || text.Contains("domestic fiction") || text.Contains("literary fiction") || text.Contains("novel"))
            return "Realist-Contemporary";

        if (text.Contains("mathematics") || text.Contains("algebra") || text.Contains("calculus") || text.Contains("logic") || text.Contains("statistics") || text.Contains("algorithms") || text.Contains("computer science"))
            return "Formal-Sciences";

        if (text.Contains("physics") || text.Contains("chemistry") || text.Contains("biology") || text.Contains("astronomy") || text.Contains("geology") || text.Contains("botany") || text.Contains("natural science"))
            return "Natural-Sciences";

        if (text.Contains("engineering") || text.Contains("medicine") || text.Contains("medical") || text.Contains("programming") || text.Contains("software") || text.Contains("technology") || text.Contains("agriculture"))
            return "Applied-Sciences-Tech";

        if (text.Contains("psychology") || text.Contains("sociology") || text.Contains("anthropology") || text.Contains("economics") || text.Contains("political") || text.Contains("law") || text.Contains("criminology"))
            return "Social-Sciences";

        if (text.Contains("biography") || text.Contains("memoir") || text.Contains("autobiography") || text.Contains("life of"))
            return "Biography-Memoir";

        if (text.Contains("cooking") || text.Contains("cookbook") || text.Contains("travel") || text.Contains("fitness") || text.Contains("health") || text.Contains("craft") || text.Contains("personal finance"))
            return "Lifestyle-Practical";

        return "Humanities-Arts";
    }

    public static string InferAudience(string haystack, string? sourceName, string form)
    {
        if (haystack.Contains("picture book") || haystack.Contains("early reader") || haystack.Contains("nursery") || haystack.Contains("bedtime"))
            return "Early-Reader";

        if (haystack.Contains("middle grade") || haystack.Contains("juvenile") || haystack.Contains("children") || haystack.Contains("wimpy kid"))
            return "Middle-Grade";

        if (haystack.Contains("young adult") || haystack.Contains("teen") || haystack.Contains("adolescent"))
            return "Young-Adult";

        if (form == "Academic-Treatise" || string.Equals(sourceName, "DOAB Academic", StringComparison.OrdinalIgnoreCase) ||
            haystack.Contains("dissertation") || haystack.Contains("monograph") || haystack.Contains("graduate"))
            return "Specialist-Scholarly";

        return "General-Adult";
    }

    public static string InferEra(string? publicationYear, string haystack)
    {
        if (!string.IsNullOrWhiteSpace(publicationYear))
        {
            var digits = new string(publicationYear.Where(char.IsDigit).Take(4).ToArray());
            if (int.TryParse(digits, out int year))
            {
                if (year < 500) return "Ancient";
                if (year <= 1499) return "Medieval";
                if (year <= 1799) return "Early-Modern";
                if (year <= 1899) return "19th-Century";
                if (year <= 1999) return "20th-Century";
                return "21st-Century";
            }
        }

        if (haystack.Contains("ancient") || haystack.Contains("homer") || haystack.Contains("plato") || haystack.Contains("aristotle") || haystack.Contains("sanskrit"))
            return "Ancient";
        if (haystack.Contains("medieval") || haystack.Contains("middle ages") || haystack.Contains("chaucer") || haystack.Contains("dante"))
            return "Medieval";
        if (haystack.Contains("shakespeare") || haystack.Contains("renaissance") || haystack.Contains("enlightenment") || haystack.Contains("18th century"))
            return "Early-Modern";
        if (haystack.Contains("21st century") || haystack.Contains("2020") || haystack.Contains("2010") || haystack.Contains("2000"))
            return "21st-Century";

        return "19th-Century";
    }

    public static string InferRights(Book book)
    {
        if (book.SourceName is "Project Gutenberg" or "Standard Ebooks" or "Sacred Texts")
            return "Public-Domain";

        if (book.SourceName is "DOAB Academic" or "Wikibooks")
            return "Open-License";

        if (book.IsIndexed)
            return "Rights-Cleared-Free";

        if (!string.IsNullOrWhiteSpace(book.PublicationYear))
        {
            var digits = new string(book.PublicationYear.Where(char.IsDigit).Take(4).ToArray());
            if (int.TryParse(digits, out int year) && year <= 1928)
                return "Public-Domain";
        }

        if (book.SourceName == "Open Library" && string.IsNullOrWhiteSpace(book.PdfUrl) && string.IsNullOrWhiteSpace(book.EpubUrl))
            return "Controlled-Lending";

        return "Public-Domain";
    }

    public static string ToSearchKeyword(string domainOrFilter) => domainOrFilter switch
    {
        "Realist-Contemporary" => "literary fiction novel",
        "Historical-Period" => "historical fiction",
        "Speculative-SciFi" => "science fiction",
        "Speculative-Fantasy" => "fantasy",
        "Horror-Macabre" => "horror gothic",
        "Crime-Mystery-Espionage" => "mystery detective crime",
        "Romance-Relational" => "romance",
        "Adventure-Survival" => "adventure",
        "Formal-Sciences" => "mathematics computer science logic",
        "Natural-Sciences" => "physics chemistry biology science",
        "Applied-Sciences-Tech" => "engineering medicine technology",
        "Social-Sciences" => "psychology sociology economics politics",
        "Humanities-Arts" => "history philosophy religion literature",
        "Biography-Memoir" => "biography memoir",
        "Lifestyle-Practical" => "cooking travel guide practical",
        _ => domainOrFilter
    };

    public static string ToInternetArchiveSubject(string domain) => ToSearchKeyword(domain);
    public static string ToGutendexTopic(string domain) => ToSearchKeyword(domain);
    public static string ToOpenLibrarySubject(string domain) => ToSearchKeyword(domain);
}
