using System;
using System.IO;
using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;

namespace FHEnhancer
{
    public class PageBuilder
    {
        private readonly string _template;
        private readonly Uri _canonicalDomain;
        private readonly string _sourceDirectory;

        public PageBuilder(IConfiguration configuration)
        {
            _sourceDirectory = configuration["SourceDirectory"];
            _canonicalDomain = new Uri(configuration["CanonicalDomain"]);
            _template = BuildTemplate();
        }

        private string BuildTemplate()
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

            template = template.Replace("{{CANONICAL_DOMAIN}}", _canonicalDomain.ToString());

            return template;
        }

        private Stats GetStats()
        {
            var statsDoc = new HtmlDocument();
            statsDoc.Load(Path.Combine(_sourceDirectory, "_statistics.html"));

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
            var page = _template.Replace("{{TITLE}}", title);
            page = page.Replace("{{CONTENT}}", content);
            page = page.Replace("{{CANONICAL_URL}}", new Uri(_canonicalDomain, fileName).ToString());
            page = page.Replace("{{CANONICAL_DOMAIN}}", _canonicalDomain.ToString());

            return page;
        }

        public class Stats
        {
            public string Pages { get; set; }

            public string People { get; set; }

            public string Pictures { get; set; }
        }
    }
}