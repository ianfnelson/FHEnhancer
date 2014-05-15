using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace FHEnhancer
{
    public class PageBuilder
    {
        private static readonly Regex DisqusFileNameRegex = new Regex(@"^(fam|ind|toc)\d+.html$", RegexOptions.Compiled);
        private static readonly string DisqusMarkup;
        private static readonly string StaticTemplate;
        private static readonly Uri CanonicalDomain;
        private static readonly IList<Ad> Ads;
        private static readonly Random Rng = new Random(DateTime.Now.Millisecond);

        static PageBuilder()
        {
            StaticTemplate = BuildStaticTemplate();
            DisqusMarkup = BuildDisqusMarkup();
            Ads = BuildAds();
            CanonicalDomain = new Uri(ConfigurationManager.AppSettings["CanonicalDomain"]);
        }

        private static IList<Ad> BuildAds()
        {
            var ads = File.ReadAllLines("./content/ads.txt")
                .Select(l => l.Split('|'))
                .Select(x => new Ad {Title = x[0], Href = x[1], FileName = x[2]})
                .ToList();

            return ads;
        }

        private static string BuildDisqusMarkup()
        {
            return File.ReadAllText("./content/disqus.html");
        }

        private static string BuildStaticTemplate()
        {
            var template = File.ReadAllText("./content/template.html");

            var now = DateTime.Now;

            template = template.Replace("{{TIMESTAMP}}", now.ToString("yyyyMMddHHmm"));
            template = template.Replace("{{CURRENT_YEAR}}", now.Year.ToString());
            template = template.Replace("{{LAST_UPDATED}}", now.ToString("dd MMMM yyyy"));

            var stats = GetStats();

            template = template.Replace("{{PAGE_COUNT}}", stats.Pages);
            template = template.Replace("{{PERSON_COUNT}}", stats.People);
            template = template.Replace("{{PICTURE_COUNT}}", stats.Pictures);

            return template;
        }

        private static Stats GetStats()
        {
            var sourceDir = ConfigurationManager.AppSettings["SourceDirectory"];

            var statsDoc = new HtmlDocument();
            statsDoc.Load(Path.Combine(sourceDir, "_statistics.html"));

            var statsNode = statsDoc.DocumentNode.SelectSingleNode("//div[contains(@class,'FhStatsData')]");
            var pagesRow = statsNode.SelectSingleNode("//table/tr[1]");
            var peopleRow = statsNode.SelectSingleNode("//table/tr[2]");
            var picturesRow = statsNode.SelectSingleNode("//table/tr[3]");

            var stats = new Stats
            {
                Pages = GetStatFigure(pagesRow),
                People = GetStatFigure(peopleRow),
                Pictures = GetStatFigure(picturesRow)
            };

            return stats;
        }

        private static string GetStatFigure(HtmlNode statRow)
        {
            return statRow.ChildNodes[1].InnerText;
        }

        public string BuildPage(string title, string content, string fileName)
        {
            var page = StaticTemplate.Replace("{{TITLE}}", title);
            page = page.Replace("{{CONTENT}}", content);
            page = page.Replace("{{CANONICAL_URL}}", new Uri(CanonicalDomain, fileName).ToString());

            page = page.Replace("{{DISQUS}}", 
                DisqusFileNameRegex.IsMatch(fileName) ? DisqusMarkup : string.Empty);

            page = InsertAd(page);

            return page;
        }

        private string InsertAd(string page)
        {
            var rand = Rng.Next(0, Ads.Count);
            var ad = Ads[rand];

            page = page.Replace("{{AD_HREF}}", ad.Href);
            page = page.Replace("{{AD_TITLE}}", ad.Title);
            page = page.Replace("{{AD_IMG}}", ad.FileName);

            return page;
        }

        public class Stats
        {
            public string Pages { get; set; }

            public string People { get; set; }

            public string Pictures { get; set; }
        }

        public class Ad
        {
            public string Title { get; set; }

            public string FileName { get; set; }

            public string Href { get; set; }
        }
    }
}