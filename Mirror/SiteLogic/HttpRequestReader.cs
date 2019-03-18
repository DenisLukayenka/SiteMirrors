using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SiteLogic
{
    public class HttpRequestReader
    {
        public async Task<string> ReadHtmlPage(Uri uri)
        {
            if (uri is null)
            {
                throw new ArgumentNullException($"{nameof(uri)} is null reference. Can't read data from bad uri.");
            }

            var buffer = await ReadData(uri);

            string htmlPage = Encoding.UTF8.GetString(buffer);

            return htmlPage;
        }

        public async Task<byte[]> ReadData(Uri uri)
        {
            if (uri is null)
            {
                throw new ArgumentNullException($"{nameof(uri)} is null reference. Can't read data from bad uri.");
            }

            byte[] buffer = null;

            using (var client = new HttpClient())
            {
                var message = await client.GetAsync(uri);

                if (message.IsSuccessStatusCode)
                {
                    buffer = await message.Content.ReadAsByteArrayAsync();
                }
            }

            return buffer;
        }
    }
}
