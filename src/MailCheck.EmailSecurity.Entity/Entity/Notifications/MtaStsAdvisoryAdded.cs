using System.Collections.Generic;
using MailCheck.Common.Messaging.Abstractions;
using MailCheck.Common.Contracts.Advisories;

namespace MailCheck.EmailSecurity.Entity.Entity.Notifications
{
    public class MtaStsAdvisoryAdded : Message
    {
        public MtaStsAdvisoryAdded(string id, List<AdvisoryMessage> messages) : base(id)
        {
            Messages = messages;
        }
        public List<AdvisoryMessage> Messages { get; }
    }
}