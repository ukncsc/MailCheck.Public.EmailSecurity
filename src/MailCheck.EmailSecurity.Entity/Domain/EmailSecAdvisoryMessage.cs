using System;
using MailCheck.Common.Contracts.Advisories;

namespace MailCheck.EmailSecurity.Entity.Domain
{
    public class EmailSecAdvisoryMessage : AdvisoryMessage
    {
        public string Name { get; }

        public EmailSecAdvisoryMessage(Guid id, string name, MessageType messageType, string text, string markDown)
            : base(id, messageType, text, markDown)
        {
            Name = name;
        }
    }
}
