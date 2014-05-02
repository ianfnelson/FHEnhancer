using System.Configuration;
using System.IO;
using System.Linq;
using HtmlAgilityPack;

namespace FHEnhancer
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var sourceDirectory = new DirectoryInfo(ConfigurationManager.AppSettings["SourceDirectory"]);
            var outputDirectory = new DirectoryInfo(ConfigurationManager.AppSettings["DestinationDirectory"]);

            CleanOutputDirectory(outputDirectory);
            CopyJpegs(sourceDirectory, outputDirectory);
            BuildPages(sourceDirectory, outputDirectory);
        }

        private static void BuildPages(DirectoryInfo sourceDirectory, DirectoryInfo outputDirectory)
        {
            var searchPatterns = new[] {"toc*.html", "ind*.html", "fam*.html", "_nameindex.html"};

            var pagesToModify =
                searchPatterns.SelectMany(
                    pat => sourceDirectory.EnumerateFileSystemInfos(pat, SearchOption.TopDirectoryOnly));

            foreach (var pageToModify in pagesToModify)
            {
                var pageParts = GetPageParts(pageToModify.FullName);

                var modifiedPage = new PageBuilder().BuildPage(pageParts.Title, pageParts.Content);

                File.WriteAllText(Path.Combine(outputDirectory.FullName, pageToModify.Name), modifiedPage);
            }

            // TODO - build index homepage from template.
        }

        private static PageParts GetPageParts(string path)
        {
            var doc = new HtmlDocument();
            doc.Load(path);

            var title = doc.DocumentNode.SelectSingleNode("/html/head/title").InnerText;
            var content = doc.DocumentNode.SelectSingleNode("/html/body/div[contains(@class,'fhcontent')]").InnerHtml;

            return new PageParts {Title = title, Content = content};
        }

        private static void CopyJpegs(DirectoryInfo sourceDirectory, DirectoryInfo outputDirectory)
        {
            foreach (var jpeg in sourceDirectory.EnumerateFileSystemInfos("*.jpg"))
            {
                File.Copy(jpeg.FullName, Path.Combine(outputDirectory.FullName, jpeg.Name));
            }
        }

        private static void CleanOutputDirectory(DirectoryInfo outputDirectory)
        {
            var searchPatterns = new[] {"*.jpg", "_*.html", "fam*.html", "ind*.html", "toc*.html"};

            var filesToDelete =
                searchPatterns.SelectMany(
                    pat => outputDirectory.EnumerateFileSystemInfos(pat, SearchOption.TopDirectoryOnly));

            foreach (var fileToDelete in filesToDelete)
            {
                fileToDelete.Delete();
            }
        }

        public class PageParts
        {
            public string Title { get; set; }
            public string Content { get; set; }
        }
    }
}