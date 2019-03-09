using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CsQuery;

namespace SiteLogic
{
    public class SiteWorker
    {
        private Uri _uriAddress;
        private string _directoryPath;
        private int _depth = 0;

        public SiteWorker(string uriAddress, string directoryPath, int depth)
        {
            _uriAddress = new Uri(uriAddress);
            _directoryPath = directoryPath;
            _depth = depth;
        }

        public async Task CreateLocalCopy()
        {
            //await WriteToFile(_uriAddress.ToString());

            await CreateLocalCopy(_uriAddress.ToString(), _depth);
        }

        private async Task CreateLocalCopy(string uriAddress, int depth)
        {
            if (depth == 0)
            {
                await WriteToFile(uriAddress);
                return;
            }

            List<string> links = await GetAllLinksAsync(uriAddress);

            foreach (var link in links)
            {
                if (Uri.TryCreate(link, UriKind.Absolute, out var uri))
                {
                    await CreateLocalCopy(link, depth - 1);
                }

                if (Uri.TryCreate(link, UriKind.Relative, out uri))
                {
                    await CreateLocalCopy(new Uri(uriAddress).AbsoluteUri + link.Remove(0, 1), depth - 1);
                }
            }
        }

        private async Task WriteToFile(string uriAddress)
        {
            HttpClient client = new HttpClient();

            string htmlText = await client.GetStringAsync(uriAddress);

            CQ parser = CQ.Create(htmlText);

            using (var writer = new StreamWriter(NormalizeFileName(parser.Find("title").Text())))
            {
                await writer.WriteAsync(htmlText);
            }
        }

        private async Task<List<string>> GetAllLinksAsync(string uri)
        {
            HttpClient client = new HttpClient();
            string htmlString = await client.GetStringAsync(uri);

            CQ parser = CQ.Create(htmlString);
            List<string> links = new List<string>();

            foreach (var link in parser.Find("a"))
            {
                if (link.FirstElementChild == null)
                {
                    links.Add(NormalizeUrl(link.GetAttribute("href")));
                }
            }

            return links;
        }

        private string NormalizeUrl(string url)
        {
            if (url != null && url.EndsWith(".html"))
            {
                return String.Concat(_uriAddress, url);
            }

            return url;
        }

        private string NormalizeFileName(string name)
        {
            name = name.Replace('<', '_')
                .Replace('>', '_').Replace('*', '_')
                .Replace('|', '_').Replace(':', '_')
                .Replace('?', '_').Replace('/', '_')
                .Replace('\\', '_');

            return String.Concat(_directoryPath, "\\", name, ".html");
        }
    }
}
