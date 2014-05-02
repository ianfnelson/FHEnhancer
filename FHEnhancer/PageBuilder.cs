using System;
using System.Configuration;
using System.IO;

namespace FHEnhancer
{
    public class PageBuilder
    {
        private static readonly string StaticTemplate;
        private static readonly Uri CanonicalDomain;

        static PageBuilder()
        {
            StaticTemplate = BuildStaticTemplate();
            CanonicalDomain = new Uri(ConfigurationManager.AppSettings["CanonicalDomain"]);
        }

        private static string BuildStaticTemplate()
        {
            var template = File.ReadAllText("./content/template.html");

            var now = DateTime.Now;

            template = template.Replace("{{TIMESTAMP}}", now.ToString("yyyyMMddHHmm"));
            template = template.Replace("{{CURRENT_YEAR}}", now.Year.ToString());
            template = template.Replace("{{LAST_UPDATED}}", now.ToString("dd MMMM yyyy"));

            // TODO - stats - pagecount, picture count, person count

            return template;
        }

        public string BuildPage(string title, string content, string fileName)
        {
            var page = StaticTemplate.Replace("{{TITLE}}", title);
            page = page.Replace("{{CONTENT}}", content);
            page = page.Replace("{{CANONICAL_URL}}", new Uri(CanonicalDomain, fileName).ToString());

            // TODO - advert

            return page;
        }
    }
}