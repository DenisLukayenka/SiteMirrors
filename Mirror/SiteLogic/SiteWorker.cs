using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CsQuery;
using CsQuery.ExtensionMethods.Internal;

namespace SiteLogic
{
    public class SiteWorker
    {
        private Uri _uriAddress;
        private string _directoryPath;
        private int _depth = 0;
        private HashSet<string> _addedUrls = new HashSet<string>();

        public SiteWorker(string uriAddress, string directoryPath, int depth)
        {
            _uriAddress = new Uri(uriAddress);
            _directoryPath = directoryPath;
            _depth = depth;
            _addedUrls.Add(_uriAddress.AbsolutePath);
        }

        public async Task CreateLocalCopy()
        {
            await CreateLocalCopy(_uriAddress, _depth);
        }

        private async Task CreateLocalCopy(Uri uriAddress, int depth)
        {
            if (depth == 0)
            {
                Console.WriteLine($"Write^ {uriAddress}");
                await WriteToFile(uriAddress);
                return;
            }

            List<Uri> links = await GetAllLinksAsync(uriAddress);

            foreach (var link in links)
            {
                await CreateLocalCopy(link, depth - 1);

                Console.WriteLine($"CreateLocalCopy: {depth} --- {uriAddress}");
            }
        }

        private async Task WriteToFile(Uri uriAddress)
        {
            using (HttpClient client = new HttpClient())
            {
                var messageResponse = await client.GetAsync(uriAddress);

                if (messageResponse.IsSuccessStatusCode)
                {
                    string html = await messageResponse.Content.ReadAsStringAsync();

                    CQ parser = CQ.Create(html);

                    var directoryInfo = new DirectoryInfo($"{_directoryPath}{uriAddress.Host}{uriAddress.AbsolutePath.Replace('/', '\\')}");

                    if (!directoryInfo.Exists)
                    {
                        Directory.CreateDirectory(directoryInfo.FullName);
                    }

                    string name = NormalizeFileName($"{parser.Find("title").Text()}");

                    using (var writer = new StreamWriter($"{directoryInfo.FullName}{name}"))
                    {
                        await writer.WriteAsync(html);
                    }

                    foreach (var image in parser.Find("img"))
                    {
                        string imageLink = image.GetAttribute("src");

                        int lastIndex = imageLink.LastIndexOf('/');

                        string imageDirPath = imageLink.Substring(0, lastIndex);
                        imageLink = imageLink.Substring(lastIndex);

                        if(Uri.TryCreate(imageDirPath, UriKind.Absolute, out Uri uri))
                        {
                            Uri cc = new Uri(uriAddress.Scheme + "://" + uriAddress.Host);

                            var aa = cc.MakeRelativeUri(uri);
                            imageDirPath = $"/{aa.ToString()}/";
                        }

                        string imagePath = $"{_directoryPath}{uriAddress.Host}{imageDirPath.Replace('/', '\\')}";
                        var dirInfo = new DirectoryInfo(imagePath);

                        if (!dirInfo.Exists)
                        {
                            Directory.CreateDirectory(dirInfo.FullName + "\\");
                        }

                        var imageMessage = await client.GetAsync($"{uriAddress.Scheme}://{uriAddress.Host}{imageDirPath}{imageLink}");

                        if (imageMessage.IsSuccessStatusCode)
                        {
                            using (var writer = new FileStream($"{dirInfo.FullName}{imageLink}", FileMode.Create))
                            {
                                var buffer = await imageMessage.Content.ReadAsByteArrayAsync();
                                await writer.WriteAsync(buffer, 0, buffer.Length);
                            }
                        }
                    }
                }
            }
        }

        private async Task<List<Uri>> GetAllLinksAsync(Uri uri)
        {
            HttpClient client = new HttpClient();
            string htmlString = await client.GetStringAsync(uri);

            CQ parser = CQ.Create(htmlString);
            List<Uri> links = new List<Uri>();

            foreach (var link in parser.Find("a"))
            {
                Uri linkUri = NormalizeUri(link.GetAttribute("href"), uri.AbsoluteUri);

                if (linkUri != null)
                {
                    links.Add(linkUri);
                }
            }

            return links;
        }

        private Uri NormalizeUri(string uri, string baseStr)
        {
            if (!_addedUrls.Contains(uri))
            {
                Uri baseUri = new Uri(baseStr);
                Uri newUri = null;

                if (Uri.IsWellFormedUriString(uri, UriKind.Relative) && baseUri.AbsolutePath != uri)
                {
                    if (uri[0] == '/')
                    {
                        newUri = new Uri(baseStr + uri.Remove(0, 1));
                    }
                    else
                    {
                        newUri = new Uri(baseStr + uri);
                    }
                }
                else if (Uri.IsWellFormedUriString(uri, UriKind.Absolute))
                {
                    newUri = new Uri(uri);
                }

                if (newUri != null && (newUri.Scheme == "http" || newUri.Scheme == "https"))
                {
                    if (!_addedUrls.Contains(newUri.AbsolutePath))
                    {
                        _addedUrls.Add(newUri.AbsolutePath);
                        return newUri;
                    }
                }
            }

            return null;
        }

        private string NormalizeFileName(string name)
        {
            name = name.Replace('<', '_')
                .Replace('>', '_').Replace('*', '_')
                .Replace('|', '_').Replace(':', '_')
                .Replace('?', '_').Replace('/', '_')
                .Replace('\\', '_');

            name = string.Concat(name, ".html");

            if (name.Length > 255)
            {
                return name.Remove(255, name.Length);
            }

            return name;
        }
    }
}
