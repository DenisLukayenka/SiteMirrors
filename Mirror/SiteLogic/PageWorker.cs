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
        private int index = 0;

        public PageWorker(string uri, string dirPath, int depth)
        {
            _baseUri = new Uri(uri);
            _directoryPath = Path.Combine(dirPath, _baseUri.Host);
            _depth = depth;

            _linksDepth = new Dictionary<string, int>();
            _linksDepth.Add(_baseUri.AbsolutePath, 0);
        }

        public async Task CreateCopyAsync()
        {
            CreateCopyAsync(_baseUri, 0).Wait();

            index = 0;
            foreach (var i in _linksDepth)
            {
                Console.WriteLine($"{index++}.  Write(depth {i.Value}): {i.Key}");
            }
        }

        private async Task CreateCopyAsync(Uri uri, int depth)
        {
            if (depth == _depth)
            {
                Console.WriteLine($"{index++}.  Write{depth}: {uri.AbsoluteUri ?? uri.AbsolutePath}");
                await SaveFileAsync(uri);
                return;
            }

            IEnumerable<Uri> links = await GetUrlsAsync(uri, _depth - depth);

            foreach (var link in links)
            {
                await CreateCopyAsync(link, depth + 1);
            }

            Console.WriteLine($"{index++}.  Write{depth}: {uri.AbsoluteUri ?? uri.AbsolutePath}");
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
            using (HttpClient client = new HttpClient())
            {
                var message = await client.GetAsync(uri);

                if (message.IsSuccessStatusCode)
                {
                    var buffer = await message.Content.ReadAsByteArrayAsync();

                    string dirPath = CreatePathDirectoryToFile(uri);
                    string fileName = CreateCorrectFileName(uri);

                    dirPath = string.IsNullOrEmpty(dirPath) ? "\\" : dirPath;
                    fileName = string.IsNullOrEmpty(fileName) ? "index.html" : fileName;

                    string fullPath = Path.Combine(dirPath, fileName.TrimStart('\\'));

                    using (var writer = new FileStream(fullPath, FileMode.Create))
                    {
                        await writer.WriteAsync(buffer, 0, buffer.Length);
                    }
                }
            }
        }

        private string CreatePathDirectoryToFile(Uri uri)
        {
            string resultPath = _directoryPath;

            var badPathSymbols = Path.GetInvalidPathChars();

            string uriPath = uri.AbsolutePath;
            foreach (var badPathSymbol in badPathSymbols)
            {
                uriPath = uriPath.Replace(badPathSymbol.ToString(), "");
            }

            int length = resultPath.Length + uriPath.Length;
            if (length > 240)
            {
                uriPath = uriPath.Substring(0, 240 - resultPath.Length);
            }

            uriPath = Path.GetDirectoryName(uriPath);
            uriPath = string.IsNullOrEmpty(uriPath) ? "\\" : uriPath;

            resultPath = Path.Combine(resultPath, uriPath.TrimStart('\\'));

            if (!string.IsNullOrEmpty(resultPath) && !Directory.Exists(resultPath))
            {
                Directory.CreateDirectory(resultPath);
            }

            return resultPath;
        }

        private string CreateCorrectFileName(Uri uri)
        {
            string uriPath = uri.AbsolutePath;

            string uriFileName = Path.GetFileName(uriPath);

            if (string.IsNullOrEmpty(uriFileName))
            {
                return "index.html";
            }

            var badNameSymbols = Path.GetInvalidFileNameChars();
            foreach (var symbol in badNameSymbols)
            {
                uriFileName = uriFileName.Replace(symbol.ToString(), "");
            }

            if (uriFileName.Length > 240)
            {
                string name = Path.GetFileNameWithoutExtension(uriFileName);
                string extension = Path.GetExtension(uriFileName);

                name = name.Substring(0, 240 - extension.Length);

                uriFileName = $"{name}{extension}";
            }

            return uriFileName;
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

        private async Task SaveToFile(Uri uri)
        {
            byte[] buffer = null;

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
