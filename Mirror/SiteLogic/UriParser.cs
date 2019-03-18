using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiteLogic
{
    public static class UriParser
    {
        public static string GetCorrectName(Uri uri)
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

        public static string GetCorrectDirectoryPath(Uri uri, string directoryPath)
        {
            string resultPath = directoryPath;

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
    }
}
