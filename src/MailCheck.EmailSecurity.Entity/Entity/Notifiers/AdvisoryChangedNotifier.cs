using System.Collections.Generic;
using System.Linq;
using MailCheck.Common.Messaging.Abstractions;
using MailCheck.EmailSecurity.Entity.Config;
using MailCheck.EmailSecurity.Entity.Entity.Notifications;
using Microsoft.Extensions.Logging;
using AdvisoryMessage = MailCheck.Common.Contracts.Advisories.AdvisoryMessage;

namespace MailCheck.EmailSecurity.Entity.Entity.Notifiers
{
    public class AdvisoryChangedNotifier : IChangeNotifier
    {
        private readonly IMessageDispatcher _dispatcher;
        private readonly IEmailSecurityEntityConfig _emailSecurityEntityConfig;
        private readonly IEqualityComparer<AdvisoryMessage> _messageEqualityComparer;
        private readonly ILogger<AdvisoryChangedNotifier> _log;

        public AdvisoryChangedNotifier(IMessageDispatcher dispatcher, IEmailSecurityEntityConfig emailSecurityEntityConfig,
            IEqualityComparer<AdvisoryMessage> messageEqualityComparer, ILogger<AdvisoryChangedNotifier> log)
        {
            _dispatcher = dispatcher;
            _emailSecurityEntityConfig = emailSecurityEntityConfig;
            _messageEqualityComparer = messageEqualityComparer;
            _log = log;
        }

        public void Handle(string domain, List<AdvisoryMessage> currentMessages, List<AdvisoryMessage> newMessages)
        {
            currentMessages = currentMessages ?? new List<AdvisoryMessage>();
            newMessages = newMessages ?? new List<AdvisoryMessage>();
            
            List<AdvisoryMessage> addedMessages =
                newMessages.Except(currentMessages, _messageEqualityComparer).ToList();
            if (addedMessages.Any())
            {
                MtaStsAdvisoryAdded advisoryAdded = new MtaStsAdvisoryAdded(domain, 
                    addedMessages);
                _dispatcher.Dispatch(advisoryAdded, _emailSecurityEntityConfig.SnsTopicArn);
                _log.LogInformation($"Dispatching {addedMessages.Count} added messages for {domain}");
            }

            List<AdvisoryMessage> removedMessages =
                currentMessages.Except(newMessages, _messageEqualityComparer).ToList();
            if (removedMessages.Any())
            {
                MtaStsAdvisoryRemoved advisoryRemoved = new MtaStsAdvisoryRemoved(domain, 
                    removedMessages);
                _dispatcher.Dispatch(advisoryRemoved, _emailSecurityEntityConfig.SnsTopicArn);
                _log.LogInformation($"Dispatching {removedMessages.Count} removed messages for {domain}");
            }

            List<AdvisoryMessage> sustainedMessages =
                currentMessages.Intersect(newMessages, _messageEqualityComparer).ToList();
            if (sustainedMessages.Any())
            {
                MtaStsAdvisorySustained advisorySustained = new MtaStsAdvisorySustained(domain, 
                    sustainedMessages);
                _dispatcher.Dispatch(advisorySustained, _emailSecurityEntityConfig.SnsTopicArn);
                _log.LogInformation($"Dispatching {sustainedMessages.Count} sustained messages for {domain}");
            }
        }
    }
}