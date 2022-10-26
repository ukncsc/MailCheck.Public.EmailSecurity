using System.Collections.Generic;

namespace MailCheck.EmailSecurity.Entity.Domain.ExternalEntities
{
    public class HostMxRecord
    {
        public int? Preference { get; set; }
        public List<string> IpAddresses { get; set; }
        public string Id { get; set; }
    }
}