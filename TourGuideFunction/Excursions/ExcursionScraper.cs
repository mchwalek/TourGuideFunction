﻿using PuppeteerSharp;
using System.Text.RegularExpressions;

namespace TourGuideFunction.Excursions
{
    public static class ExcursionScraper
    {
        private static readonly Regex PriceRegex = new(@"^(?<price>\d+)", RegexOptions.Compiled);
        private static readonly Regex NightsRegex = new(@"\(.*\)$", RegexOptions.Compiled);

        public static async Task<Excursion> GetExcursionAsync(Uri uri)
        {
            await Puppeteer.CreateBrowserFetcher(new BrowserFetcherOptions()).DownloadAsync();
            var browser = await Puppeteer.LaunchAsync(new LaunchOptions { DefaultViewport = new ViewPortOptions { Width = 1200 } });
            var page = await browser.NewPageAsync();
            await page.GoToAsync(uri.ToString(), new NavigationOptions
            {
                WaitUntil = new[] { WaitUntilNavigation.Networkidle2 }
            });


            var name = await GetNameAsync(page);
            var (dateFrom, dateTo) = await GetDatesAsync(page);
            var originCity = await GetOriginCityAsync(page);
            var price = await GetPriceAsync(page);
            return new Excursion(name, dateFrom, dateTo, originCity, price);
        }

        private static async Task<string> GetNameAsync(IPage page)
        {
            const string script = "document.querySelector('.kh-header__title').innerText";
            return await page.EvaluateExpressionAsync<string>(script);
        }

        private static async Task<(DateTimeOffset, DateTimeOffset)> GetDatesAsync(IPage page)
        {
            const string script = "document.querySelector('.r-select-button-termin span').innerText";
            var rawResult = await page.EvaluateExpressionAsync<string>(script);
            rawResult = NightsRegex.Replace(rawResult, "");

            var parts = rawResult.Split(" - ");

            return (DateTimeOffset.Parse(parts[0]), DateTimeOffset.Parse(parts[1]));
        }
        private static async Task<string> GetOriginCityAsync(IPage page)
        {
            const string script = "document.querySelector('.kh__container-right > div > div:nth-child(3) span').innerText";
            return await page.EvaluateExpressionAsync<string>(script);
        }

        private static async Task<int> GetPriceAsync(IPage page)
        {
            const string script = "document.querySelector('.kh-konfigurator-cena__za-osobe').innerText";
            var rawResult = await page.EvaluateExpressionAsync<string>(script);
            rawResult = rawResult.Replace(" ", "");

            var match = PriceRegex.Match(rawResult);
            return int.Parse(match.Groups["price"].Value);
        }

    }
}