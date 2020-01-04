using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace FHEnhancer
{
    public class SiteMapBuilder
    {
        private static readonly string lastMod = DateTime.Today.ToString("yyyy-MM-dd");
        private readonly Uri _canonicalDomain;

        public SiteMapBuilder(IConfiguration configuration)
        {
            _canonicalDomain = new Uri(configuration["CanonicalDomain"]);
        }
        
        public string BuildSiteMap(IEnumerable<string> pageNames)
        {
            var sb = new StringBuilder();

            sb.AppendLine(@"<?xml version=""1.0"" encoding=""UTF-8""?>
            <urlset xmlns=""http://www.sitemaps.org/schemas/sitemap/0.9"" 
              xmlns:image=""http://www.google.com/schemas/sitemap-image/1.1"" 
              xmlns:video=""http://www.google.com/schemas/sitemap-video/1.1"">");

            foreach (var siteMapUrl in pageNames.Select(GetSiteMapUrl))
            {
                sb.AppendLine(string.Format(
                    "<url><loc>{0}</loc><lastmod>{1}</lastmod><priority>{2}</priority><changefreq>{3}</changefreq></url>",
                    siteMapUrl.Loc, siteMapUrl.LastMod, siteMapUrl.Priority, siteMapUrl.ChangeFreq));
            }

            sb.AppendLine("</urlset>");

            return sb.ToString();
        }

        private SiteMapUrl GetSiteMapUrl(string pageName)
        {
            var loc = new Uri(_canonicalDomain, pageName).ToString();

            var priority = GetPriority(pageName);
            var changeFreq = GetChangeFreq(pageName);

            return new SiteMapUrl
            {
                Loc = loc,
                LastMod = lastMod,
                Priority = priority,
                ChangeFreq = changeFreq
            };
        }

        private static string GetChangeFreq(string pageName)
        {
            if (pageName.StartsWith("index", StringComparison.OrdinalIgnoreCase) ||
                pageName.StartsWith("toc", StringComparison.OrdinalIgnoreCase) ||
                pageName.StartsWith("_nameindex", StringComparison.OrdinalIgnoreCase))
            {
                return "weekly";
            }

            return "monthly";
        }

        private static string GetPriority(string pageName)
        {
            if (pageName.StartsWith("index", StringComparison.OrdinalIgnoreCase))
            {
                return "1.0";
            }

            if (pageName.StartsWith("toc", StringComparison.OrdinalIgnoreCase) ||
                pageName.StartsWith("_nameindex", StringComparison.OrdinalIgnoreCase))
            {
                return "0.80";
            }

            return "0.5";
        }

        public class SiteMapUrl
        {
            public string Loc { get; set; }

            public string LastMod { get; set; }

            public string ChangeFreq { get; set; }

            public string Priority { get; set; }
        }
    }
}