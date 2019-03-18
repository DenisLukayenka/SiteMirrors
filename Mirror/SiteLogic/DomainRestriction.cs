using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiteLogic
{
    public enum DomainRestriction
    {
        NoRestriction,
        CurrentDomain,
        NotHigherCurrentUrl
    }
}
