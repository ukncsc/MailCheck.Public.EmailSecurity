using System.Collections.Generic;
using MailCheck.Common.Contracts.Advisories;

namespace MailCheck.EmailSecurity.Entity.Entity.Notifiers
{
    public class MessageEqualityComparer : IEqualityComparer<AdvisoryMessage>
    {
        public bool Equals(AdvisoryMessage x, AdvisoryMessage y)
        {
            return y != null && x != null && x.Id.Equals(y.Id);
        }

        public int GetHashCode(AdvisoryMessage obj)
        {
            return obj.Id.GetHashCode();
        }
    }
}