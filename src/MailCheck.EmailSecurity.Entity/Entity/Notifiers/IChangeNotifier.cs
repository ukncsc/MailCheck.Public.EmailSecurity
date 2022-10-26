using System.Collections.Generic;
using MailCheck.Common.Contracts.Advisories;

namespace MailCheck.EmailSecurity.Entity.Entity.Notifiers
{
    public interface IChangeNotifier
    {
        void Handle(string domain, List<AdvisoryMessage> currentMessages, List<AdvisoryMessage> newMessages);
    }
}