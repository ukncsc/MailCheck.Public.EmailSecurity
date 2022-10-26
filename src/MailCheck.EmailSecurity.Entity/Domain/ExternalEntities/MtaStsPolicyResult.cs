using System.Collections.Generic;
using MailCheck.Common.Contracts.Advisories;

namespace MailCheck.EmailSecurity.Entity.Domain.ExternalEntities
{
    public class MtaStsPolicyResult
    {
        public string RawValue { get; set; }
        public List<Key> Keys { get; set; }
        public List<AdvisoryMessage> Errors { get; set; }
    }
}
