using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CsQuery;
using CsQuery.Implementation;

namespace SiteLogic
{
    public class PageWorker
    {
        private Uri _baseUri;
        private string _directoryPath;
        private int _depth;
        private Dictionary<string, int> _linksDepth;

        private DomainRestriction _currentRestriction;
        private List<string> _possibleImageTypes;

        public PageWorker(string uri, string dirPath, int depth, DomainRestriction restriction, List<string> imageTypes)
        {
            _baseUri = new Uri(uri);
            _directoryPath = Path.Combine(dirPath, _baseUri.Host);
            _depth = depth;
            _currentRestriction = restriction;
            _possibleImageTypes = imageTypes;

            _linksDepth = new Dictionary<string, int>();
            _linksDepth.Add(_baseUri.AbsolutePath, 0);
        }

        public async Task CreateCopyAsync()
        {
            await CreateCopyAsync(_baseUri, 0);
        }

        private async Task CreateCopyAsync(Uri uri, int depth)
        {
            if (depth == _depth)
            {
                Console.WriteLine($"Write: {uri.AbsoluteUri}");
                await SaveFileAsync(uri);
                return;
            }

            IEnumerable<Uri> links = await GetUrlsAsync(uri, _depth - depth);

            foreach (var link in links)
            {
                await CreateCopyAsync(link, depth + 1);
            }

            await SaveFileAsync(uri);
            Console.WriteLine($"Write: {uri.AbsoluteUri}");
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
            using (HttpClient client = new HttpClient())
            {
                var message = await client.GetAsync(uri);

                if (message.IsSuccessStatusCode)
                {
                    var buffer = await message.Content.ReadAsByteArrayAsync();

                    string dirPath = UriParser.GetCorrectDirectoryPath(uri, _directoryPath);
                    string fileName = UriParser.GetCorrectName(uri);

                    dirPath = string.IsNullOrEmpty(dirPath) ? "\\" : dirPath;
                    fileName = string.IsNullOrEmpty(fileName) ? "index.html" : fileName;

                    string extension = Path.GetExtension(fileName);
                    if (string.IsNullOrEmpty(extension))
                    {
                        throw new NullReferenceException($"{nameof(extension)} is empty.");
                    }

                    if (_possibleImageTypes is null 
                        || _possibleImageTypes.Count == 0 
                        || _possibleImageTypes.Contains(extension))
                    {
                        string fullPath = Path.Combine(dirPath, fileName.TrimStart('\\'));

                        using (var writer = new FileStream(fullPath, FileMode.Create))
                        {
                            await writer.WriteAsync(buffer, 0, buffer.Length);
                        }
                    }
                }
            }
        }

        private async Task<IEnumerable<Uri>> GetUrlsAsync(Uri uri, int depth)
        {
            string htmlPage = await GetHtmlPageAsync(uri);
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

        private async Task<string> GetHtmlPageAsync(Uri uri)
        {
            string htmlPage = null;

            using (HttpClient client = new HttpClient())
            {
                var message = await client.GetAsync(uri);

                if (message.IsSuccessStatusCode)
                {
                    htmlPage = await message.Content.ReadAsStringAsync();
                }
            }

            return htmlPage;
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

            if (uri != null && (uri.Scheme == "http" || uri.Scheme == "https"))
            {
                if (!_linksDepth.ContainsKey(uri.AbsolutePath))
                {
                    switch (_currentRestriction)
                    {
                        case DomainRestriction.CurrentDomain:
                            if (uri.Host == _baseUri.Host)
                            {
                                _linksDepth.Add(uri.AbsolutePath, depth);
                                links.Add(uri);
                            }
                            break;

                        case DomainRestriction.NoRestriction:
                            _linksDepth.Add(uri.AbsolutePath, depth);
                            links.Add(uri);
                            break;

                        case DomainRestriction.NotHigherCurrentUrl:
                            if (_baseUri.IsBaseOf(uri))
                            {
                                _linksDepth.Add(uri.AbsolutePath, depth);
                                links.Add(uri);
                            }
                            break;
                    }
                }
                else if (_linksDepth[uri.AbsolutePath] > depth)
                {
                    switch (_currentRestriction)
                    {
                        case DomainRestriction.CurrentDomain:
                            if (uri.Host == _baseUri.Host)
                            {
                                _linksDepth[uri.AbsolutePath] = depth;
                                links.Add(uri);
                            }
                            break;

                        case DomainRestriction.NoRestriction:
                            _linksDepth[uri.AbsolutePath] = depth;
                            links.Add(uri);
                            break;

                        case DomainRestriction.NotHigherCurrentUrl:
                            if (_baseUri.IsBaseOf(uri))
                            {
                                _linksDepth[uri.AbsolutePath] = depth;
                                links.Add(uri);
                            }
                            break;
                    }
                }
            }
        }
    }
}
