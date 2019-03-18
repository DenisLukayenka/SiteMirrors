using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiteLogic
{
    public class FileWorker
    {
        private ExtensionsChecker _checker;

        public FileWorker(ICollection<string> possibleExtensions)
        {
            _checker = new ExtensionsChecker(possibleExtensions);
        }

        public async Task SaveToFileAsync(string path, byte[] buffer)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException($"{nameof(path)} is null reference object.");
            }

            if (buffer is null)
            {
                throw new ArgumentNullException($"{nameof(buffer)} is null reference object.");
            }

            InitDirectory(path);

            if (_checker.CheckExtension(path))
            {
                using (var writer = new FileStream(path, FileMode.Create))
                {
                    await writer.WriteAsync(buffer, 0, buffer.Length);
                }
            }
        }

        public void InitDirectory(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException($"{nameof(path)} is null reference object.");
            }

            string directoryPath = Path.GetDirectoryName(path);
            directoryPath = string.IsNullOrEmpty(directoryPath) ? "\\" : directoryPath;

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }
    }
}
