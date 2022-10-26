using System;
using System.Collections.Generic;
using System.Linq;
using MailCheck.Common.Contracts.Advisories;
using MailCheck.Common.Contracts.Findings;
using MailCheck.Common.Messaging.Abstractions;
using MailCheck.Common.Processors.Notifiers;
using MailCheck.EmailSecurity.Entity.Config;
using MailCheck.EmailSecurity.Entity.Domain;
using Microsoft.Extensions.Logging;

namespace MailCheck.EmailSecurity.Entity.Entity.Notifiers
{
    public class FindingsChangedNotifier : IChangeNotifier
    {
        private readonly IMessageDispatcher _dispatcher;
        private readonly IEmailSecurityEntityConfig _emailSecurityEntityConfig;
        private readonly IFindingsChangedNotifier _findingsChangedCalculator;
        private readonly ILogger<FindingsChangedNotifier> _log;

        public FindingsChangedNotifier(IMessageDispatcher dispatcher, IEmailSecurityEntityConfig emailSecurityEntityConfig,
            IFindingsChangedNotifier findingsChangedCalculator, ILogger<FindingsChangedNotifier> log)
        {
            _dispatcher = dispatcher;
            _emailSecurityEntityConfig = emailSecurityEntityConfig;
            _findingsChangedCalculator = findingsChangedCalculator;
            _log = log;
        }

        public void Handle(string domain, List<AdvisoryMessage> currentMessages, List<AdvisoryMessage> newMessages)
        {
            FindingsChanged findingsChanged = _findingsChangedCalculator.Process(domain, "MTA-STS",
                ExtractFindingsFromMessages(domain, currentMessages?.OfType<EmailSecAdvisoryMessage>().ToList() ?? new List<EmailSecAdvisoryMessage>()),
                ExtractFindingsFromMessages(domain, newMessages?.OfType<EmailSecAdvisoryMessage>().ToList() ?? new List<EmailSecAdvisoryMessage>()));

            if(findingsChanged.Added?.Count > 0 || findingsChanged.Sustained?.Count > 0 || findingsChanged.Removed?.Count > 0)
            {
                _log.LogInformation($"Dispatching FindingsChanged for {domain}: {findingsChanged.Added?.Count} findings added, {findingsChanged.Sustained?.Count} findings sustained, {findingsChanged.Removed?.Count} findings removed");
                _dispatcher.Dispatch(findingsChanged, _emailSecurityEntityConfig.SnsTopicArn);
            }
            else
            {
                _log.LogInformation($"No Findings to dispatch for {domain}");
            }
        }

        private List<Finding> ExtractFindingsFromMessages(string domain, List<EmailSecAdvisoryMessage> rootMessages)
        {
            List<Finding> findings = rootMessages.Select(msg => new Finding
            {
                Name = msg.Name,
                SourceUrl = $"https://{_emailSecurityEntityConfig.WebUrl}/app/domain-security/{domain}/mta-sts",
                Title = msg.Text,
                EntityUri = $"domain:{domain}",
                Severity = AdvisoryMessageTypeToFindingSeverityMapping[msg.MessageType]
            }).ToList();

            return findings;
        }

        internal static readonly Dictionary<MessageType, string> AdvisoryMessageTypeToFindingSeverityMapping = new Dictionary<MessageType, string>
        {
            [MessageType.info] = "Informational",
            [MessageType.warning] = "Advisory",
            [MessageType.error] = "Urgent",
        };
    }
}
