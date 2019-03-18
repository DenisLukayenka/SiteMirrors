using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiteLogic.Domains
{
    public class HigherUrlDomainChecker : IDomainChecker
    {
        public bool CheckDomain(Uri baseUri, Uri checkUri)
        {
            return baseUri.IsBaseOf(checkUri);
        }
    }
}
