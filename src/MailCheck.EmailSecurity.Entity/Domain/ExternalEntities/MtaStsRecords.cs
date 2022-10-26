using System;
using System.Collections.Generic;
using System.Text;

namespace MailCheck.EmailSecurity.Entity.Domain.ExternalEntities
{
    public class MtaStsRecords
    {
        public string Domain { get; set; }
        public List<MtaStsRecord> Records { get; set; }
        public int MessageSize { get; set; }
    }
}
