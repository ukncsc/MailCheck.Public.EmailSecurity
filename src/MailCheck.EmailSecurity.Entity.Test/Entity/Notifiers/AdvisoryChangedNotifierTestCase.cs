using System.Collections.Generic;
using MailCheck.Common.Contracts.Advisories;

namespace MailCheck.EmailSecurity.Entity.Test.Entity.Notifiers
{
    public class AdvisoryChangedNotifierTestCase
    {
        public List<AdvisoryMessage> CurrentMessages { get; set; }
        public List<AdvisoryMessage> NewMessages { get; set; }
        public int ExpectedAdded { get; set; }
        public int ExpectedRemoved { get; set; }
        public int ExpectedSustained { get; set; }
        public string Description { get; set; }
        
        public override string ToString()
        {
            return Description;
        }
    }
}