using System;
namespace MailCheck.EmailSecurity.Entity.Domain.ExternalEntities
{
    public class Key
    {
        public string Type { get; set; }
        public string Value { get; set; }
        public string RawValue { get; set; }
        public string Explanation { get; set; }
    }
}
