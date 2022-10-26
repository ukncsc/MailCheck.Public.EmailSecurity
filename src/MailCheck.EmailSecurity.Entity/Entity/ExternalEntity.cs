using System;

namespace MailCheck.EmailSecurity.Entity.Entity
{
    public class ExternalEntity
    {
        public object EntityDetail { get; set; }
        public DateTime ChangedAt { get; set; }
        public string ReasonForChange { get; set; }
    }
}
