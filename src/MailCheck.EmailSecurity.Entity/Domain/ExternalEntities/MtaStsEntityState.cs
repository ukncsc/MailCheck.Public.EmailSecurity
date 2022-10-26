using System;
using System.Collections.Generic;
using MailCheck.Common.Contracts.Advisories;

namespace MailCheck.EmailSecurity.Entity.Domain.ExternalEntities
{
    public class MtaStsEntityState
    {
        public string Id { get; set; }
        public int Version { get; set; }
        public object MtaStsState { get; set; }
        public DateTime Created { get; set; }
        public MtaStsRecords MtaStsRecords { get; set; }
        public List<AdvisoryMessage> Messages { get; set; }
        public DateTime? LastUpdated { get; set; }
        public MtaStsPolicyResult Policy { get; set; }
    }
}
