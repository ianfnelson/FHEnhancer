using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace FHEnhancer
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var sw = new Stopwatch();
            sw.Start();

            var sourceDirectory = new DirectoryInfo(ConfigurationManager.AppSettings["SourceDirectory"]);
            var outputDirectory = new DirectoryInfo(ConfigurationManager.AppSettings["DestinationDirectory"]);

            CleanOutputDirectory(outputDirectory);
            CopyJpegs(sourceDirectory, outputDirectory);
            BuildPages(sourceDirectory, outputDirectory);

            sw.Stop();
            Console.WriteLine("Done - {0} seconds", sw.ElapsedMilliseconds / 1000.0);
            Console.ReadKey();
        }

        private static void BuildPages(DirectoryInfo sourceDirectory, DirectoryInfo outputDirectory)
        {
            Console.WriteLine("Building Pages...");

            var searchPatterns = new[] {"ind*.html", "toc*.html", "fam*.html", "_nameindex.html"};

            var pagesToModify =
                searchPatterns.SelectMany(
                    pat => sourceDirectory.EnumerateFileSystemInfos(pat, SearchOption.TopDirectoryOnly));

            var counter = 0;

            var timeSinceMessage = DateTime.Now;

            Parallel.ForEach(pagesToModify, pageToModify =>
            {
                var pageParts = GetPageParts(pageToModify.FullName);

                var modifiedPage = new PageBuilder().BuildPage(pageParts.Title, pageParts.Content, pageToModify.Name);

                File.WriteAllText(Path.Combine(outputDirectory.FullName, pageToModify.Name), modifiedPage);

                counter++;

                if ((DateTime.Now - timeSinceMessage).Milliseconds > 500)
                {
                    timeSinceMessage = DateTime.Now;
                    Console.WriteLine("...{0} pages created", counter);
                }
            });

            Console.WriteLine("...{0} pages created", counter);

            var indexPage = new PageBuilder().BuildPage("Nelson Family Tree", "<p>todo</p>", "index.html");
            File.WriteAllText(Path.Combine(outputDirectory.FullName, "index.html"), indexPage);
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

            foreach (var node in new[] {h1Node, pageTitleCentredNode, seeAlsoNode}.Where(node => node != null))
            {
                contentDiv.RemoveChild(node);
            }

            var content = contentDiv.InnerHtml;

            return new PageParts {Title = titleText, Content = content};
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

            var searchPatterns = new[] {"*.jpg", "_*.html", "fam*.html", "ind*.html", "toc*.html"};

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