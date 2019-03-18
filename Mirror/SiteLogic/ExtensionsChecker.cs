using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiteLogic
{
    public class ExtensionsChecker
    {
        private ICollection<string> _possibleExtensions;

        public ExtensionsChecker(ICollection<string> possibleExtensions)
        {
            _possibleExtensions = possibleExtensions ?? new List<string>();
            _possibleExtensions.Add(".html");
        }

        public bool CheckExtension(string path)
        {
            bool result = false;

            string extenstion = Path.GetExtension(path);

            if (_possibleExtensions.Count == 1 || _possibleExtensions.Contains(extenstion))
            {
                result = true;
            }

            return result;
        }
    }
}
