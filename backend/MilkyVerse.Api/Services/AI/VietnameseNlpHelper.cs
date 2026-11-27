using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using MilkyVerse.Api.Models;

namespace MilkyVerse.Api.Services.AI;

public class ParsedCartItemClause
{
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; } = 1;
    public string Size { get; set; } = "M";
    public string SugarLevel { get; set; } = "50%";
    public string IceLevel { get; set; } = "70%";
    public string SpicyLevel { get; set; } = "Không cay";
    public List<string> Toppings { get; set; } = new();
}

public class SemanticMenuIntent
{
    public List<string> MeaningfulKeywords { get; set; } = new();
    public List<string> OrGroups { get; set; } = new();
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? ProductType { get; set; }
    public bool NoCaffeine { get; set; }
    public bool VeganOrNoMilk { get; set; }
    public bool WarmOrNoIce { get; set; }
    public bool LowSugar { get; set; }
    public bool CoolingOrFruity { get; set; }
    public bool CreamyOrCheese { get; set; }
    public bool EnergyOrCaffeine { get; set; }
    public bool SpicyFood { get; set; }
    public bool CheapestSort { get; set; }
    public bool BestSellerSort { get; set; }
}

public static class VietnameseNlpHelper
{
    private static readonly HashSet<string> ConversationalStopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "tui", "toi", "minh", "em", "anh", "chi", "ban", "khach", "quan", "shop", "milky", "milkyverse",
        "oi", "nha", "nhe", "ne", "a", "ha", "ho", "di", "cho", "xin", "hoi", "tim", "kiem", "goi", "y",
        "tu", "van", "giup", "voi", "va", "hay", "hoac", "cung", "ma", "la", "co", "khong", "duoc", "dc",
        "ko", "k", "gi", "nao", "sao", "may", "loai", "mon", "ly", "coc", "phan", "suat", "chai", "thuc",
        "don", "menu", "hom", "nay", "troi", "qua", "lam", "rat", "hoi", "mot", "hai", "ba", "bon", "nam",
        "vai", "may", "muon", "thich", "can", "uong", "an", "ban", "dang", "bi", "tam", "khoang", "gia",
        "tien", "vnd", "dong", "nghin", "ngan", "ngin", "canh", "den", "toi", "duoi", "tren", "tu", "ngon",
        "nhat", "nhieu", "it", "xem", "danh", "sach", "nhung", "cac", "cai", "nay", "kia", "do", "day",
        "ve", "vi", "huong", "mui", "kieu", "the", "nao", "bao", "nhieu", "dat", "mua", "them", "lay"
    };

    /// <summary>
    /// Converts Windows IME decomposed Unicode (NFD, e.g. 'a' + '\u0300') into standard precomposed Unicode (NFC, 'à').
    /// </summary>
    public static string NormalizeNfc(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        return text.Normalize(NormalizationForm.FormC).Trim();
    }

    /// <summary>
    /// Removes all Vietnamese diacritics so unaccented or NFD/NFC mismatched queries still match reliably.
    /// </summary>
    public static string RemoveDiacritics(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        var nfd = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(nfd.Length);

        foreach (var ch in nfd)
        {
            var cat = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (cat != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(ch switch
                {
                    'đ' => 'd',
                    'Đ' => 'D',
                    _ => ch
                });
            }
        }

        return sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant().Trim();
    }

    /// <summary>
    /// Analyzes ANY free-form Vietnamese prompt to extract real menu filters & keywords across all 213 products.
    /// </summary>
    public static SemanticMenuIntent AnalyzeMenuQuery(string rawMessage)
    {
        var nfc = NormalizeNfc(rawMessage);
        var ascii = RemoveDiacritics(nfc);
        var (minPrice, maxPrice) = ExtractPriceRange(nfc);

        var intent = new SemanticMenuIntent
        {
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            NoCaffeine = ContainsAny(ascii, "khong caffeine", "ko caffeine", "mat ngu", "kho ngu", "buoi toi", "tre em", "ba bau", "khong tra"),
            VeganOrNoMilk = ContainsAny(ascii, "thuan chay", "vegan", "an chay", "khong sua", "ko sua", "lactose", "yen mach", "hanh nhan", "eat clean", "an kieng", "giam can"),
            WarmOrNoIce = ContainsAny(ascii, "bi ho", "dau hong", "rat co", "viem hong", "khan tieng", "cam lanh", "uong am", "khong da", "ko da", "troi lanh"),
            LowSugar = ContainsAny(ascii, "it ngot", "it duong", "khong duong", "ko duong", "an kieng", "giam can", "eat clean"),
            CoolingOrFruity = ContainsAny(ascii, "mat lanh", "thanh mat", "giai nhiet", "giai khat", "troi nong", "oi buc", "chua ngot", "trai cay", "thanh nhiet"),
            CreamyOrCheese = ContainsAny(ascii, "beo ngay", "kem cheese", "macchiato", "kem muoi", "kem trung", "pho mai"),
            EnergyOrCaffeine = ContainsAny(ascii, "buon ngu", "tinh tao", "deadline", "tap trung", "met moi", "hoc bai"),
            SpicyFood = ContainsAny(ascii, "an cay", "mi cay", "cay nong", "sot thai", "xien nuong", "xe luoi"),
            CheapestSort = ContainsAny(ascii, "re nhat", "gia re", "hat de", "sinh vien", "tiet kiem"),
            BestSellerSort = ContainsAny(ascii, "ban chay", "best seller", "noi bat", "hot nhat", "dac sac", "signature")
        };

        // Detect ProductType
        if (ContainsAny(ascii, "combo", "nhom ban", "hai nguoi", "mot nguoi"))
        {
            intent.ProductType = "COMBO";
        }
        else if (ContainsAny(ascii, "an vat", "do an", "mon an", "doi bung", "an kem", "ca vien", "bo vien", "tom vien", "muc vien", "ho lo", "chan ga", "canh ga", "ga lac", "popcorn", "khoai tay", "khoai lang", "pho mai que", "xuc xich", "nem chua", "ha cao", "banh trang", "trung chen", "trung cut", "mi cay", "mi tron", "com chien", "kimbap", "long non", "doi sun", "sun ga", "bap xao", "tokbokki", "kho ga"))
        {
            intent.ProductType = "FOOD";
        }
        else if (ContainsAny(ascii, "do uong", "nuoc uong", "uong gi", "tra sua", "tra trai cay", "nuoc ep", "soda", "chanh", "tac", "ca phe", "bac xiu", "matcha", "frappe", "da xay", "sua tuoi", "macchiato", "hong tra", "luc tra", "o long"))
        {
            intent.ProductType = "DRINK";
        }

        // Split OR clauses (e.g. "soda việt quất hoặc trà đào hay đá xay oreo")
        var orParts = Regex.Split(ascii, @"\b(?:hoac|hay)\b|,|/", RegexOptions.IgnoreCase)
            .Select(x => CleanKeywordsFromText(x))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();
        intent.OrGroups = orParts;

        var cleaned = CleanKeywordsFromText(ascii);
        intent.MeaningfulKeywords = cleaned
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length >= 2)
            .Distinct()
            .ToList();

        return intent;
    }

    public static string CleanKeywordsFromText(string asciiText)
    {
        if (string.IsNullOrWhiteSpace(asciiText)) return string.Empty;
        // Remove numbers attached to prices/percentages so they don't confuse keyword scoring
        var noNumbers = Regex.Replace(asciiText, @"\b\d+(?:[\.,]\d+)*(?:%|k|d|đ)?\b", " ");
        var rawTokens = Regex.Split(noNumbers, @"[^a-z0-9]+")
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToList();

        var kept = new List<string>();
        foreach (var tk in rawTokens)
        {
            // Keep key domain words even if short ("bo", "ga", "mi", "o")
            if (tk is "ga" or "bo" or "mi" or "ca" or "oi")
            {
                kept.Add(tk);
                continue;
            }
            if (tk.Length >= 2 && !ConversationalStopWords.Contains(tk))
            {
                kept.Add(tk);
            }
        }

        return string.Join(" ", kept);
    }

    /// <summary>
    /// Ranks all 213 database products dynamically against a natural query or SemanticMenuIntent.
    /// Searches Product Name, Category Name, Description, and Slug!
    /// </summary>
    public static List<(Product Product, int Score, string MatchReason)> ScoreAndRankProducts(
        IReadOnlyList<Product> allActiveProducts,
        SemanticMenuIntent intent,
        string? rawQuery = null)
    {
        var queryAscii = RemoveDiacritics(rawQuery);
        var candidates = allActiveProducts
            .Where(p => p.IsActive && p.StockStatus == 1)
            .AsEnumerable();

        if (!string.IsNullOrWhiteSpace(intent.ProductType))
        {
            var typed = candidates.Where(p => string.Equals(p.ProductType, intent.ProductType, StringComparison.OrdinalIgnoreCase)).ToList();
            if (typed.Any()) candidates = typed;
        }

        if (intent.MinPrice.HasValue && intent.MinPrice.Value > 0)
        {
            candidates = candidates.Where(p => p.BasePrice >= intent.MinPrice.Value);
        }

        if (intent.MaxPrice.HasValue && intent.MaxPrice.Value > 0)
        {
            candidates = candidates.Where(p => p.BasePrice <= intent.MaxPrice.Value);
        }

        if (intent.NoCaffeine)
        {
            var noCaff = candidates.Where(p =>
                p.ProductType == "DRINK" &&
                (p.Slug.Contains("nuoc-ep") ||
                 p.Slug.Contains("soda") ||
                 p.Slug.Contains("sua-tuoi") ||
                 p.Slug.Contains("sua-chua") ||
                 p.Slug.Contains("chanh") ||
                 p.Slug.Contains("tac") ||
                 p.Slug.Contains("cam"))).ToList();
            if (noCaff.Any()) candidates = noCaff;
        }

        if (intent.VeganOrNoMilk)
        {
            var vegan = candidates.Where(p =>
                p.Slug.Contains("yen-mach") ||
                p.Slug.Contains("oat") ||
                p.Slug.Contains("nuoc-ep") ||
                p.Slug.Contains("soda") ||
                p.Slug.Contains("chanh") ||
                p.Slug.Contains("o-long") ||
                p.Slug.Contains("tra-xanh") ||
                p.Slug.Contains("tra-den") ||
                p.Slug.Contains("tra-lai") ||
                (p.Category != null && RemoveDiacritics(p.Category.Name).Contains("nguyen chat"))).ToList();
            if (vegan.Any()) candidates = vegan;
        }

        var candidateList = candidates.ToList();
        var scored = new List<(Product Product, int Score, string MatchReason)>();

        foreach (var p in candidateList)
        {
            var nameAscii = RemoveDiacritics(p.Name);
            var catAscii = RemoveDiacritics(p.Category?.Name ?? "");
            var descAscii = RemoveDiacritics(p.Description);
            var slugAscii = (p.Slug ?? "").Replace("-", " ");

            int score = 0;
            string reason = p.Category?.Name ?? "Phù hợp yêu cầu của bạn";

            // 1. Exact/OR-group phrase matches (handles "soda việt quất hoặc trà đào")
            foreach (var grp in intent.OrGroups)
            {
                if (grp.Length < 3) continue;
                if (nameAscii.Contains(grp)) { score += 30; reason = $"Khớp trực tiếp với '{p.Name}'"; }
                else if (catAscii.Contains(grp)) { score += 18; reason = $"Thuộc nhóm '{p.Category?.Name}'"; }
                else if (descAscii.Contains(grp)) { score += 12; }
            }

            // 2. Meaningful individual keywords
            if (intent.MeaningfulKeywords.Any())
            {
                int nameHits = 0;
                int otherHits = 0;
                foreach (var kw in intent.MeaningfulKeywords)
                {
                    if (nameAscii.Contains(kw)) nameHits++;
                    else if (catAscii.Contains(kw) || slugAscii.Contains(kw)) otherHits++;
                    else if (descAscii.Contains(kw)) score += 3;
                }

                score += nameHits * 10 + otherHits * 5;
            }

            // 3. Semantic attribute boosts
            var slugStr = (p.Slug ?? "").ToLowerInvariant();
            if (intent.WarmOrNoIce && p.ProductType == "DRINK")
            {
                if (slugStr.Contains("cam-sa") || slugStr.Contains("mat-ong") || slugStr.Contains("tac") || slugStr.Contains("chanh") || slugStr.Contains("cam") || slugStr.Contains("hoa-cuc") || slugStr.Contains("sua-tuoi"))
                {
                    score += 20;
                    reason = "Thanh ấm cổ họng, khuyên dùng 0% đá (pha ấm)";
                }
            }

            if (intent.CoolingOrFruity && p.ProductType == "DRINK")
            {
                if (slugStr.Contains("dao") || slugStr.Contains("vai") || slugStr.Contains("dau") || slugStr.Contains("xoai") || slugStr.Contains("dua-hau") || slugStr.Contains("soda") || slugStr.Contains("nuoc-ep") || slugStr.Contains("chanh"))
                {
                    score += 15;
                    reason = "Thanh mát giải nhiệt từ trái cây tươi / soda";
                }
            }

            if (intent.CreamyOrCheese && p.ProductType == "DRINK")
            {
                if (slugStr.Contains("cheese") || slugStr.Contains("macchiato") || slugStr.Contains("kem") || slugStr.Contains("flan") || slugStr.Contains("pudding"))
                {
                    score += 15;
                    reason = "Vị kem béo ngậy sánh mịn chuẩn gu MilkyVerse";
                }
            }

            if (intent.EnergyOrCaffeine && p.ProductType == "DRINK")
            {
                if (slugStr.Contains("ca-phe") || slugStr.Contains("bac-xiu") || slugStr.Contains("matcha") || slugStr.Contains("o-long") || slugStr.Contains("hong-tra"))
                {
                    score += 15;
                    reason = "Giàu Caffeine & L-Theanine giúp tỉnh táo tức thì";
                }
            }

            if (intent.SpicyFood && p.ProductType == "FOOD")
            {
                if ((p.SpicyLevel ?? 0) > 0 || slugStr.Contains("cay") || slugStr.Contains("thai") || slugStr.Contains("tokbokki"))
                {
                    score += 15;
                    reason = "Món ăn vặt cay đậm vị cực cuốn";
                }
            }

            if (intent.MeaningfulKeywords.Count == 0 && (intent.MinPrice.HasValue || intent.MaxPrice.HasValue || intent.NoCaffeine || intent.VeganOrNoMilk || !string.IsNullOrWhiteSpace(intent.ProductType)))
            {
                // Pure filter query (e.g. "đồ uống từ 25k đến 40k" or "món ăn vặt")
                score += p.IsBestSeller ? 8 : 5;
            }

            if (score > 0)
            {
                if (p.IsBestSeller) score += 2;
                scored.Add((p, score, reason));
            }
        }

        if (intent.CheapestSort)
        {
            return scored.OrderBy(x => x.Product.BasePrice).ThenByDescending(x => x.Score).ToList();
        }

        return scored
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Product.IsBestSeller)
            .ThenBy(x => x.Product.BasePrice)
            .ToList();
    }

    private static bool ContainsAny(string text, params string[] phrases)
        => phrases.Any(p => text.Contains(p, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Extracts price range (minPrice, maxPrice) from natural Vietnamese expressions:
    /// - "từ 25 nghìn đến 40 nghìn", "25.000 -> 40.000", "25k - 45k", "từ 25 ngìn tới 40 nghìn"
    /// - "dưới 40k", "có 40.000đ", "tầm 35 ngàn"
    /// - "trên 40k", "từ 40k trở lên"
    /// </summary>
    public static (decimal? MinPrice, decimal? MaxPrice) ExtractPriceRange(string rawMessage)
    {
        var nfc = NormalizeNfc(rawMessage).ToLowerInvariant();
        var ascii = RemoveDiacritics(nfc);

        var tokenRegex = new Regex(
            @"(?<num>\d{1,3}(?:[\.,]\d{3})+|\d{2,6})\s*(?<unit>k|nghin|ngan|ngin|canh|d|vnd|dong)?\b",
            RegexOptions.IgnoreCase);

        var amounts = new List<decimal>();
        foreach (Match m in tokenRegex.Matches(ascii))
        {
            var rawNum = m.Groups["num"].Value.Replace(".", "").Replace(",", "");
            var unit = m.Groups["unit"].Value.ToLowerInvariant();

            if (!decimal.TryParse(rawNum, out var val)) continue;

            var afterIdx = m.Index + m.Length;
            if (afterIdx < ascii.Length && ascii[afterIdx] == '%') continue;

            if (val < 1000)
            {
                if (unit is "k" or "nghin" or "ngan" or "ngin" or "canh")
                {
                    val *= 1000;
                }
                else if (val >= 15 && val <= 300 && (ascii.Contains("nghin") || ascii.Contains("ngan") || ascii.Contains("ngin") || ascii.Contains("->") || ascii.Contains("tu ") || ascii.Contains("den ") || ascii.Contains("toi ") || ascii.Contains("duoi ")))
                {
                    val *= 1000;
                }
                else
                {
                    continue;
                }
            }

            if (val >= 10000 && val <= 1000000)
            {
                amounts.Add(val);
            }
        }

        if (amounts.Count >= 2)
        {
            var min = Math.Min(amounts[0], amounts[1]);
            var max = Math.Max(amounts[0], amounts[1]);
            return (min, max);
        }

        if (amounts.Count == 1)
        {
            var single = amounts[0];
            if (ascii.Contains("tren ") || ascii.Contains("tro len") || ascii.Contains(">"))
            {
                return (single, 500000);
            }
            return (0, single);
        }

        return (null, null);
    }

    public static Product? FindBestMatchingProduct(string queryText, IReadOnlyList<Product> products)
    {
        if (string.IsNullOrWhiteSpace(queryText) || products.Count == 0) return null;

        var nfcQuery = NormalizeNfc(queryText).ToLowerInvariant();
        var asciiQuery = RemoveDiacritics(nfcQuery);

        foreach (var p in products.OrderByDescending(x => x.Name.Length))
        {
            var pNfc = NormalizeNfc(p.Name).ToLowerInvariant();
            var pAscii = RemoveDiacritics(pNfc);
            if (nfcQuery.Contains(pNfc) || asciiQuery.Contains(pAscii))
            {
                return p;
            }
        }

        (string AliasAscii, string TargetAscii)[] commonAliases =
        {
            ("ca vien chien", "ca vien chien truyen thong"),
            ("ca vien", "ca vien chien truyen thong"),
            ("bo vien", "bo vien chien"),
            ("tom vien", "tom vien chien"),
            ("muc vien", "muc vien chien"),
            ("banh trang tron", "banh trang tron"),
            ("banh trang nuong", "banh trang nuong"),
            ("chan ga sot thai", "chan ga rut xuong sot thai"),
            ("chan ga sa tac", "chan ga ngam sa tac"),
            ("chan ga", "chan ga"),
            ("mi cay", "mi cay"),
            ("tokbokki", "tokbokki"),
            ("pho mai que", "pho mai que"),
            ("nem chua ran", "nem chua ran"),
            ("khoai tay chien", "khoai tay"),
            ("canh ga", "canh ga chien"),
            ("tra sua truyen thong", "tra sua truyen thong"),
            ("tra sua tran chau den", "tra sua tran chau den"),
            ("tra sua tran chau trang", "tra sua tran chau trang"),
            ("tra sua hoang kim", "tra sua tran chau hoang kim"),
            ("tra sua duong den", "tra sua duong den"),
            ("sua tuoi tran chau duong den", "sua tuoi tran chau duong den"),
            ("tra sua matcha", "tra sua matcha"),
            ("matcha latte", "matcha latte"),
            ("tra sua khoai mon", "tra sua khoai mon"),
            ("tra sua socola", "tra sua socola"),
            ("tra dao cam sa", "tra dao cam sa"),
            ("tra dao", "tra dao cam sa"),
            ("tra vai", "tra vai"),
            ("tra o long kem cheese", "tra o long kem cheese"),
            ("ca phe muoi", "ca phe muoi"),
            ("ca phe sua", "ca phe sua"),
            ("bac xiu", "bac xiu")
        };

        foreach (var (alias, target) in commonAliases)
        {
            if (asciiQuery.Contains(alias))
            {
                var found = products.FirstOrDefault(p => RemoveDiacritics(p.Name).Contains(target));
                if (found != null) return found;
            }
        }

        var cleaned = CleanKeywordsFromText(asciiQuery);
        var queryTokens = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        if (queryTokens.Count == 0) return null;

        Product? best = null;
        double bestScore = 0;

        foreach (var p in products)
        {
            var pAscii = RemoveDiacritics(p.Name);
            var pTokens = pAscii.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            int matchedCount = queryTokens.Count(qt => pTokens.Any(pt => pt == qt));
            if (matchedCount == 0) continue;

            double score = (double)matchedCount * 2.0 - (pTokens.Length - matchedCount) * 0.25;
            if (p.IsBestSeller) score += 0.2;

            if (score > bestScore && matchedCount >= Math.Min(2, queryTokens.Count))
            {
                bestScore = score;
                best = p;
            }
        }

        return best;
    }

    public static List<ParsedCartItemClause> ParseMultiItemCartRequest(string rawMessage, IReadOnlyList<Product> activeProducts)
    {
        var nfc = NormalizeNfc(rawMessage);
        var results = new List<ParsedCartItemClause>();

        var mainProduct = FindBestMatchingProduct(nfc, activeProducts);
        var subClauses = Regex.Split(nfc, @",|\bvà\b|\bvới\b|\bcùng\b|\bthêm\b|\+", RegexOptions.IgnoreCase)
            .Select(c => c.Trim())
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .ToList();

        if (subClauses.Count >= 2)
        {
            Product? lastSeenProduct = mainProduct;

            for (int i = 0; i < subClauses.Count; i++)
            {
                var clause = subClauses[i];
                var clauseAscii = RemoveDiacritics(clause);
                var clauseProduct = FindBestMatchingProduct(clause, activeProducts);

                if (clauseProduct != null)
                {
                    lastSeenProduct = clauseProduct;
                }

                if (i == 0 && clauseProduct != null && subClauses.Count >= 3)
                {
                    var secondProd = FindBestMatchingProduct(subClauses[1], activeProducts);
                    var thirdProd = FindBestMatchingProduct(subClauses[2], activeProducts);
                    if (secondProd == null && thirdProd == null &&
                        (RemoveDiacritics(subClauses[1]).Contains("1 ly") || RemoveDiacritics(subClauses[1]).Contains("mot ly")))
                    {
                        continue;
                    }
                }

                var targetProduct = clauseProduct ?? lastSeenProduct;
                if (targetProduct == null) continue;

                if (clauseProduct == null && !ContainsModifierOrQuantity(clauseAscii))
                {
                    continue;
                }

                results.Add(BuildClauseItem(clause, targetProduct));
            }
        }

        if (results.Count == 0 && mainProduct != null)
        {
            results.Add(BuildClauseItem(nfc, mainProduct));
        }

        return results;
    }

    private static bool ContainsModifierOrQuantity(string clauseAscii)
    {
        return Regex.IsMatch(clauseAscii, @"\b(\d+|mot|hai|ba|bon|nam)\s*(ly|phan|coc|suat)?\b") ||
               clauseAscii.Contains("duong") ||
               clauseAscii.Contains("ngot") ||
               clauseAscii.Contains("da") ||
               clauseAscii.Contains("size") ||
               clauseAscii.Contains("binh thuong");
    }

    private static ParsedCartItemClause BuildClauseItem(string clauseText, Product product)
    {
        var ascii = RemoveDiacritics(clauseText);

        int qty = ExtractClauseQuantity(ascii);
        string size = Regex.IsMatch(ascii, @"\bsize\s*l\b|\bly\s*lon\b") ? "L" : "M";

        string sugar = "50%";
        if (ascii.Contains("0%") || ascii.Contains("khong duong") || ascii.Contains("khong ngot")) sugar = "0%";
        else if (ascii.Contains("30%") || ascii.Contains("it duong") || ascii.Contains("it ngot")) sugar = "30%";
        else if (ascii.Contains("70%") || ascii.Contains("ngot diu")) sugar = "70%";
        else if (ascii.Contains("100%") || ascii.Contains("binh thuong") || ascii.Contains("chuan vi") || ascii.Contains("nhieu duong")) sugar = "100%";

        string ice = "70%";
        if (ascii.Contains("0% da") || ascii.Contains("khong da") || ascii.Contains("uong am") || ascii.Contains("nong")) ice = "0%";
        else if (ascii.Contains("30% da") || ascii.Contains("it da")) ice = "30%";
        else if (ascii.Contains("50% da") || ascii.Contains("da vua")) ice = "50%";
        else if (ascii.Contains("100% da") || ascii.Contains("nhieu da")) ice = "100%";

        string spicy = "Không cay";
        var spicyMatch = Regex.Match(ascii, @"cap\s*(\d)");
        if (spicyMatch.Success) spicy = $"Cấp {spicyMatch.Groups[1].Value}";
        else if (ascii.Contains("sieu cay")) spicy = "Cấp 7 - Siêu cay";
        else if (ascii.Contains("cay vua")) spicy = "Cấp 2 - Cay vừa";
        else if (ascii.Contains("cay nhe") || ascii.Contains("it cay")) spicy = "Cấp 1 - Cay nhẹ";

        var toppings = new List<string>();
        (string KeyAscii, string NameVi)[] knownToppings =
        {
            ("tran chau hoang kim", "Trân châu hoàng kim"),
            ("tran chau den", "Trân châu đen"),
            ("tran chau trang", "Trân châu trắng"),
            ("tran chau duong den", "Trân châu đường đen"),
            ("kem cheese", "Kem cheese"),
            ("kem muoi", "Kem muối"),
            ("pudding", "Pudding"),
            ("thach dua", "Thạch dừa"),
            ("thach nha dam", "Thạch nha đam"),
            ("flan", "Flan"),
            ("suong sao", "Sương sáo"),
            ("oreo", "Oreo"),
            ("dau do", "Đậu đỏ")
        };

        foreach (var (keyAscii, nameVi) in knownToppings)
        {
            var prodAscii = RemoveDiacritics(product.Name);
            if (ascii.Contains(keyAscii) && (!prodAscii.Contains(keyAscii) || ascii.Contains("topping") || ascii.Contains("them " + keyAscii)))
            {
                toppings.Add(nameVi);
            }
        }

        return new ParsedCartItemClause
        {
            Product = product,
            Quantity = qty,
            Size = product.ProductType == "FOOD" ? "Standard" : size,
            SugarLevel = product.ProductType == "FOOD" ? "" : sugar,
            IceLevel = product.ProductType == "FOOD" ? "" : ice,
            SpicyLevel = spicy,
            Toppings = toppings
        };
    }

    public static int ExtractClauseQuantity(string ascii)
    {
        var numMatch = Regex.Match(ascii, @"\b(?<q>\d{1,2})\s*(?:ly|phan|coc|suat|hop|met|dia|chen|mon|tra|ca|banh|mi|chan|khoai|ga|bo|tom|muc|sua|nuoc|ca phe|combo)?\b");
        if (numMatch.Success && int.TryParse(numMatch.Groups["q"].Value, out var q) && q >= 1 && q <= 30)
        {
            var afterIdx = numMatch.Index + numMatch.Groups["q"].Length;
            var rest = afterIdx < ascii.Length ? ascii[afterIdx..].TrimStart() : "";
            if (!rest.StartsWith("%") && !rest.StartsWith("k ") && !rest.StartsWith("nghin") && !rest.StartsWith("ngan") && !rest.StartsWith("ngin") && !rest.StartsWith(".000"))
            {
                return q;
            }
        }

        if (Regex.IsMatch(ascii, @"\bhai\b", RegexOptions.IgnoreCase)) return 2;
        if (Regex.IsMatch(ascii, @"\bba\b", RegexOptions.IgnoreCase)) return 3;
        if (Regex.IsMatch(ascii, @"\bbon\b", RegexOptions.IgnoreCase)) return 4;
        if (Regex.IsMatch(ascii, @"\bnam\b", RegexOptions.IgnoreCase)) return 5;
        return 1;
    }
}
