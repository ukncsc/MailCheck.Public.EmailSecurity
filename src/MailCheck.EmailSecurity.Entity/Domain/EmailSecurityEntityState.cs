using System;
using System.Collections.Generic;
using MailCheck.Common.Contracts.Advisories;
using MailCheck.EmailSecurity.Entity.Entity;

namespace MailCheck.EmailSecurity.Entity.Domain
{
    public class EmailSecurityEntityState
    {
        public string Domain { get; set; }
        public Dictionary<string, ExternalEntity> Entities { get; set; } = new Dictionary<string, ExternalEntity>();
        public List<EmailSecAdvisoryMessage> AdvisoryMessages { get; set; } = new List<EmailSecAdvisoryMessage>();
        public int Version { get; set; } = 1;
        public virtual EmailSecurityState State { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
