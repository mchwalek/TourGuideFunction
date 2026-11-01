using HtmlAgilityPack;
using System.Globalization;
using System.Text.RegularExpressions;

namespace TourGuideFunction.Excursions
{
    public static class ExcursionScraper
    {
        private static readonly Regex DatesRegex = new(@"(?<from>\d{2}\.\d{2}\.\d{4})\s*-\s*(?<to>\d{2}\.\d{2}\.\d{4})", RegexOptions.Compiled);
        private static readonly Regex PriceRegex = new(@"^(?<price>\d+)", RegexOptions.Compiled);
        private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);

        // shortcut: copy of r.pl/robots.txt disallow rules as of 2026-11, re-check robots.txt if r.pl changes it.
        private static readonly Regex DisallowedPathRegex = new(
            @"^/(api|szukaj|[^/]+/szukaj|rezerwacja|dodajopinie|opinie/dodaj|bilety-czarterowe/(szukaj|rezerwacja))(/|\?|$)|idZamowienia=",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static async Task<Excursion?> GetExcursionAsync(HttpClient httpClient, Uri uri)
        {
            if (uri.Host is not ("r.pl" or "www.r.pl") || DisallowedPathRegex.IsMatch(uri.PathAndQuery))
                throw new InvalidOperationException($"{uri} is not an r.pl page allowed by robots.txt.");

            var html = await httpClient.GetStringAsync(uri);
            return Parse(html);
        }

        /// <returns>null when the page has no bookable offer (no configurator).</returns>
        public static Excursion? Parse(string html)
        {
            var document = new HtmlDocument();
            document.LoadHtml(html);

            var name = OwnText(FindByTestId(document, "r-typography:kartaHotelu:tytul"))
                ?? throw new InvalidOperationException("Excursion title not found - page layout may have changed.");

            var dates = OwnText(FindByTestId(document, "r-select-button:kartaHotelu-konfigurator:termin")?.SelectSingleNode(".//h5"));
            var origin = OwnText(FindByTestId(document, "r-select-button:kartaHotelu-konfigurator:polaczenie")?.SelectSingleNode(".//h5"));
            var price = OwnText(FindByTestId(document, "r-typography:kartaHotelu-konfigurator-cenaPodsumowanie:cenaZaOsobe"));
            if (dates is null && origin is null && price is null)
                return null;
            if (dates is null || origin is null || price is null)
                throw new InvalidOperationException("Offer configurator incomplete - page layout may have changed.");

            var datesMatch = DatesRegex.Match(dates);
            var priceMatch = PriceRegex.Match(WhitespaceRegex.Replace(price, ""));
            if (!datesMatch.Success || !priceMatch.Success)
                throw new InvalidOperationException($"Unexpected dates '{dates}' or price '{price}'.");

            return new Excursion(
                name,
                ParseDate(datesMatch.Groups["from"].Value),
                ParseDate(datesMatch.Groups["to"].Value),
                origin,
                int.Parse(priceMatch.Groups["price"].Value, CultureInfo.InvariantCulture));
        }

        private static HtmlNode? FindByTestId(HtmlDocument document, string testId) =>
            document.DocumentNode.SelectSingleNode($"//*[@data-test-id='{testId}']");

        // Only the node's own text: details (nights, flight info, currency) sit in child elements.
        private static string? OwnText(HtmlNode? node)
        {
            if (node is null)
                return null;

            var text = string.Concat(node.ChildNodes.Where(x => x.NodeType == HtmlNodeType.Text).Select(x => x.InnerText));
            text = HtmlEntity.DeEntitize(text).Trim();
            return text.Length == 0 ? null : text;
        }

        private static DateOnly ParseDate(string value) =>
            DateOnly.ParseExact(value, "dd.MM.yyyy", CultureInfo.InvariantCulture);
    }
}
