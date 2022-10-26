using System;
using System.Collections.Generic;

namespace MailCheck.EmailSecurity.Entity.Domain.ExternalEntities
{
    public class MxEntityState
    {
        public string Id { get; set; }
        public object MxState { get; set; }
        public List<HostMxRecord> HostMxRecords { get; set; }
        public DateTime? LastUpdated { get; set; }
        public object Error { get; set; }
    }
}