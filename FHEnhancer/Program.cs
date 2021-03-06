using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;

namespace FHEnhancer
{
    class Program
    {
        private static readonly string[] PageSearchPatterns =
        {
            "ind*.html", "toc*.html", "fam*.html", "_nameindex.html"
        };

        private static IConfigurationRoot _configuration;

        private static void Main(string[] args)
        {
            _configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, true)
                .Build();
            
            var sw = new Stopwatch();
            sw.Start();

            var sourceDirectory = new DirectoryInfo(_configuration["SourceDirectory"]);
            var outputDirectory = new DirectoryInfo(_configuration["DestinationDirectory"]);

            CleanOutputDirectory(outputDirectory);
            CopyJpegs(sourceDirectory, outputDirectory);
            BuildPages(sourceDirectory, outputDirectory);
            BuildSiteMaps(sourceDirectory, outputDirectory);

            sw.Stop();
            Console.WriteLine("Done - {0} seconds", sw.ElapsedMilliseconds/1000.0);
        }

        private static void BuildSiteMaps(DirectoryInfo sourceDirectory, DirectoryInfo outputDirectory)
        {
            Console.WriteLine("Building SiteMaps...");

            var pages =
                PageSearchPatterns.SelectMany(
                    pat => sourceDirectory.EnumerateFiles(pat, SearchOption.TopDirectoryOnly).Select(x => x.Name));

            var siteMapBuilder = new SiteMapBuilder(_configuration);
            var siteMap = siteMapBuilder.BuildSiteMap(pages);

            var originalFileName = Path.Combine(outputDirectory.FullName, "sitemap.xml");

            File.WriteAllText(originalFileName, siteMap);

            using (var originalFileStream = File.OpenRead(originalFileName))
            using (var compressedFileStream = File.Create(originalFileName + ".gz"))
            using (var compressionStream = new GZipStream(compressedFileStream, CompressionMode.Compress))
            {
                originalFileStream.CopyTo(compressionStream);
            }

            Console.WriteLine("...SiteMaps created");
        }

        private static void BuildPages(DirectoryInfo sourceDirectory, DirectoryInfo outputDirectory)
        {
            Console.WriteLine("Building Pages...");

            var pagesToModify =
                PageSearchPatterns.Except(new[] { "index.html" }).SelectMany(
                    pat => sourceDirectory.EnumerateFiles(pat, SearchOption.TopDirectoryOnly));

            var counter = 0;

            var timeSinceMessage = DateTime.Now;
            var monitor = new object();

            var pageBuilder = new PageBuilder(_configuration);
            
            Parallel.ForEach(pagesToModify, pageToModify =>
            {
                var pageParts = GetPageParts(pageToModify.FullName);

                var modifiedPage = pageBuilder.BuildPage(pageParts.Title, pageParts.Content, pageToModify.Name);

                File.WriteAllText(Path.Combine(outputDirectory.FullName, pageToModify.Name), modifiedPage);

                Interlocked.Increment(ref counter);

                lock (monitor)
                {
                    if ((DateTime.Now - timeSinceMessage).Milliseconds > 500)
                    {
                        timeSinceMessage = DateTime.Now;
                        Console.WriteLine("...{0} pages created", counter);
                    }
                }
            });

            var homepageContent = File.ReadAllText("./content/homepage.html");
            var indexPage = pageBuilder.BuildPage("Family Tree of Ian Nelson and Jocelyn McGhee", homepageContent, "index.html");
            File.WriteAllText(Path.Combine(outputDirectory.FullName, "index.html"), indexPage);
            counter++;

            Console.WriteLine("...{0} pages created", counter);
        }

        private static PageParts GetPageParts(string path)
        {
            var doc = new HtmlDocument();
            doc.Load(path);

            var contentDiv = doc.DocumentNode.SelectSingleNode("/html/body/div[contains(@class,'fhcontent')]");

            var h1Node = contentDiv.SelectSingleNode("h1[contains(@class,'FhHdg1')]");
            var pageTitleCentredNode = contentDiv.SelectSingleNode("p[contains(@class,'FhPageTitleCentred')]");

            var titleText = (h1Node ?? pageTitleCentredNode ??
                             doc.DocumentNode.SelectSingleNode("/html/head/title")).InnerText;

            var seeAlsoNode = contentDiv.SelectSingleNode("div[contains(@class,'FhSeeAlso')]");

            foreach (var node in new[] { h1Node, pageTitleCentredNode, seeAlsoNode }.Where(node => node != null))
            {
                contentDiv.RemoveChild(node);
            }

            var content = contentDiv.InnerHtml;

            return new PageParts { Title = titleText, Content = content };
        }

        private static void CopyJpegs(DirectoryInfo sourceDirectory, DirectoryInfo outputDirectory)
        {
            Console.WriteLine("Copying JPEGs...");

            var counter = 0;

            foreach (var jpeg in sourceDirectory.EnumerateFileSystemInfos("*.jpg"))
            {
                File.Copy(jpeg.FullName, Path.Combine(outputDirectory.FullName, jpeg.Name));
                counter++;
            }

            Console.WriteLine("...{0} JPEGs copied", counter);
        }

        private static void CleanOutputDirectory(DirectoryInfo outputDirectory)
        {
            Console.WriteLine("Cleaning output directory...");

            var searchPatterns = new[] { "*.jpg", "_*.html", "fam*.html", "ind*.html", "toc*.html", "sitemap.xml", "sitemap.xml.gz" };

            var filesToDelete =
                searchPatterns.SelectMany(
                    pat => outputDirectory.EnumerateFileSystemInfos(pat, SearchOption.TopDirectoryOnly));

            var counter = 0;

            foreach (var fileToDelete in filesToDelete)
            {
                fileToDelete.Delete();
                counter++;
            }

            Console.WriteLine("...{0} files deleted", counter);
        }

        public class PageParts
        {
            public string Title { get; set; }
            public string Content { get; set; }
        }
    }
}