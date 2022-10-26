using System;
using System.Collections.Generic;
using MailCheck.Common.Contracts.Advisories;

namespace MailCheck.EmailSecurity.Api.Domain
{
    public class EmailSecurityInfoResponse
    {
        public EmailSecurityInfoResponse(string domain)
        {
            Domain = domain;
        }

        public string Domain { get; set; }
        public List<AdvisoryMessage> AdvisoryMessages { get; set; } = new List<AdvisoryMessage>();
        public int Version { get; set; } = 1;
        public EmailSecurityState State { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
