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
            int bb = 0;
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
            }

            await WriteToFile(uriAddress);
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

                    await SaveFileAsync(uriAddress, await messageResponse.Content.ReadAsStreamAsync());

                    await DownloadImagesAsync(client, html, uriAddress);
                }
            }
        }

        private async Task SaveFileAsync(Uri uriAddress, Stream writer)
        {
            string dirPath = "";
            string name = "";

            UriBuilder uriBuilder = new UriBuilder(uriAddress);

            dirPath = Path.GetDirectoryName(uriBuilder.Path);
            name = Path.GetFileName(uriAddress.AbsolutePath);

            string pathToFolder = Path.Combine(_directoryPath, uriAddress.Host, dirPath.TrimStart('\\'));
            if (!Directory.Exists(pathToFolder))
            {
                Directory.CreateDirectory(pathToFolder);
            }

            byte[] buffer = new byte[writer.Length];
            await writer.ReadAsync(buffer, 0, buffer.Length);

            try
            {
                using (var stream = new FileStream(Path.Combine(pathToFolder, name.TrimStart('\\')), FileMode.Create))
                {
                    await stream.WriteAsync(buffer, 0, buffer.Length);
                }
            }
            catch
            {
                int b = 10;
            }
            
        }

        private async Task DownloadImagesAsync(HttpClient client, string html, Uri uriAddress)
        {
            CQ parser = CQ.Create(html);

            foreach (var image in parser.Find("img"))
            {
                string strLink = image.GetAttribute("src");

                if (!_addedUrls.Contains(strLink))
                {
                    Console.WriteLine(strLink);

                    _addedUrls.Add(strLink);

                    string imageLinkPath = "";
                    string imageDirPath = "";
                    string imageName = "";

                    if (Uri.IsWellFormedUriString(strLink, UriKind.Relative))
                    {
                        imageDirPath = Path.GetDirectoryName(strLink);

                        imageLinkPath = imageDirPath.Length != 0 ? imageDirPath.Replace('\\', '/') : "/";
                    }
                    else if (Uri.IsWellFormedUriString(strLink, UriKind.Absolute))
                    {
                        var ub = new UriBuilder(strLink);

                        imageDirPath = Path.GetDirectoryName(ub.Path);
                        imageLinkPath = imageDirPath.Length != 0 ? imageDirPath.Replace('\\', '/') : "/";
                    }
                    else
                    {
                        continue;
                    }

                    imageName = Path.GetFileName(strLink);

                    string imagePath = Path.Combine(_directoryPath, uriAddress.Host, imageDirPath);

                    UriBuilder uriBuilder = new UriBuilder(uriAddress.Scheme,
                        uriAddress.Host,
                        uriAddress.Port,
                        $"{imageLinkPath}/{imageName}");

                    var imageMessage = await client.GetAsync(uriBuilder.Uri);

                    if (imageMessage.IsSuccessStatusCode)
                    {
                        var dirInfo = new DirectoryInfo(Path.Combine(_directoryPath, uriAddress.Host, imagePath.TrimStart('\\')));

                        if (!dirInfo.Exists)
                        {
                            Directory.CreateDirectory(dirInfo.FullName + "\\");
                        }

                        using (var writer = new FileStream(Path.Combine(dirInfo.FullName, imageName),
                            FileMode.Create))
                        {
                            var buffer = await imageMessage.Content.ReadAsByteArrayAsync();
                            await writer.WriteAsync(buffer, 0, buffer.Length);
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

            if (name.Length > 255)
            {
                return name.Remove(255, name.Length);
            }

            return name;
        }
    }
}
