using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiteLogic.Domains
{
    public class NoRestrictionDomainChecker : IDomainChecker
    {
        public bool CheckDomain(Uri baseUri, Uri checkUri)
        {
            return true;
        }
    }
}
