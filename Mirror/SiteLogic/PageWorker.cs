using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CsQuery;
using CsQuery.Implementation;
using SiteLogic.Domains;

namespace SiteLogic
{
    public class PageWorker
    {
        private readonly Uri _baseUri;
        private readonly string _directoryPath;
        private readonly int _depth;
        private readonly Dictionary<string, int> _linksDepth;

        private readonly IDomainChecker _domainChecker;
        private readonly FileWorker _fileWorker;
        private readonly HttpRequestReader _httpReader;

        public PageWorker(string uri, string dirPath, int depth, IDomainChecker checker, ICollection<string> imageTypes)
        {
            _baseUri = new Uri(uri);
            _directoryPath = Path.Combine(dirPath, _baseUri.Host);
            _depth = depth;
            _domainChecker = checker;

            _linksDepth = new Dictionary<string, int>();
            _linksDepth.Add(_baseUri.AbsolutePath, 0);

            _fileWorker = new FileWorker(imageTypes);
            _httpReader = new HttpRequestReader();
        }

        public async Task CreateCopyAsync()
        {
            await CreateCopyAsync(_baseUri, 0);
        }

        private async Task CreateCopyAsync(Uri uri, int depth)
        {
            if (depth == _depth)
            {
                await SaveFileAsync(uri);
                return;
            }

            IEnumerable<Uri> links = await GetUrlsAsync(uri, _depth - depth);

            foreach (var link in links)
            {
                await CreateCopyAsync(link, depth + 1);
            }

            await SaveFileAsync(uri);
        }

        private Uri TryCreateUri(string uriString)
        {
            Uri uri = null;

            if (Uri.IsWellFormedUriString(uriString, UriKind.Absolute))
            {
                uri = new Uri(uriString);
            }
            else if (Uri.IsWellFormedUriString(uriString, UriKind.Relative))
            {
                string uriStringValue = $"{_baseUri.Scheme}://{_baseUri.Host}{uriString}";
                Uri.TryCreate(uriStringValue, UriKind.Absolute, out uri);
            }

            return uri;
        }

        private async Task SaveFileAsync(Uri uri)
        {
            var buffer = await _httpReader.ReadData(uri);

            if (buffer != null)
            {
                string dirPath = UriParser.GetCorrectDirectoryPath(uri, _directoryPath);
                string fileName = UriParser.GetCorrectName(uri);
                string fullPath = Path.Combine(dirPath, fileName.TrimStart('\\'));

                await _fileWorker.SaveToFileAsync(fullPath, buffer);
                Console.WriteLine($"Write {uri.AbsoluteUri}");
            }
        }

        private async Task<IEnumerable<Uri>> GetUrlsAsync(Uri uri, int depth)
        {
            string htmlPage = await _httpReader.ReadHtmlPage(uri);
            List<Uri> links = new List<Uri>();

            if (htmlPage != null)
            {
                CQ parser = CQ.Create(htmlPage);

                var element = parser.FirstElement();
                if (element != null)
                {
                    await FindLinksRecursiveAsync(element, depth, links);
                }
            }

            return links;
        }

        private async Task FindLinksRecursiveAsync(IDomElement element, int depth, ICollection<Uri> links)
        {
            if (element is null)
            {
                throw new ArgumentNullException(nameof(element) + " is null reference.");
            }

            string link = CheckForLink(element);

            if (link != null)
            {
                SaveLinkToDir(link, depth, links);
            }

            if (!element.HasChildren)
            {
                return;
            }

            foreach (var childElement in element.ChildElements)
            {
                await FindLinksRecursiveAsync(childElement, depth, links);
            }
        }

        private string CheckForLink(IDomElement element)
        {
            if (element is null)
            {
                throw new ArgumentNullException(nameof(element) + " is null reference.");
            }

            string link = null;

            if (element.HasAttribute("src"))
            {
                link = element.GetAttribute("src");
            }
            else if (element.HasAttribute("href"))
            {
                link = element.GetAttribute("href");
            }

            return link;
        }

        private void SaveLinkToDir(string link, int depth, ICollection<Uri> links)
        {
            if (link is null)
            {
                throw new ArgumentNullException(nameof(link) + " is null reference.");
            }

            if (_linksDepth is null)
            {
                throw new NullReferenceException(nameof(_linksDepth) + " is not initialized");
            }

            Uri uri = TryCreateUri(link);

            if (uri != null && (uri.Scheme == "http" || uri.Scheme == "https") && _domainChecker.CheckDomain(_baseUri, uri))
            {
                if (!_linksDepth.ContainsKey(uri.AbsolutePath))
                {

                    _linksDepth.Add(uri.AbsolutePath, depth);
                    links.Add(uri);

                }
                else if (_linksDepth[uri.AbsolutePath] > depth)
                {

                    _linksDepth[uri.AbsolutePath] = depth;
                    links.Add(uri);

                }
            }
        }
    }
}
