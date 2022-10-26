using System.Collections.Generic;
using MailCheck.Common.Contracts.Advisories;
using MailCheck.Common.Messaging.Abstractions;
using MailCheck.DomainStatus.Contracts;
using MailCheck.EmailSecurity.Entity.Domain;
using Microsoft.Extensions.Logging;
using MailCheck.EmailSecurity.Entity.Config;
using MailCheck.EmailSecurity.Entity.Domain.ExternalEntities;
using Newtonsoft.Json.Linq;

namespace MailCheck.EmailSecurity.Entity.Entity.DomainStatus
{
    public interface IDomainStatusPublisher
    {
        void Publish(EmailSecurityEntityState state);
    }

    public class DomainStatusPublisher : IDomainStatusPublisher
    {
        private readonly IMessageDispatcher _dispatcher;
        private readonly IEmailSecurityEntityConfig _emailSecurityEntityConfig;
        private readonly IDomainStatusEvaluator _domainStatusEvaluator;
        private readonly ILogger<DomainStatusPublisher> _log;
        public DomainStatusPublisher(IMessageDispatcher dispatcher, IEmailSecurityEntityConfig mtaStsEntityConfig, IDomainStatusEvaluator domainStatusEvaluator, ILogger<DomainStatusPublisher> log)
        {
            _dispatcher = dispatcher;
            _emailSecurityEntityConfig = mtaStsEntityConfig;
            _domainStatusEvaluator = domainStatusEvaluator;
            _log = log;
        }

        public void Publish(EmailSecurityEntityState state)
        {
            List<AdvisoryMessage> advisoryMessages = new List<AdvisoryMessage>();

            advisoryMessages.AddRange(state.AdvisoryMessages);

            if (state.Entities.ContainsKey("MTASTS"))
            {
                ExternalEntity mtaStsExternal = state.Entities["MTASTS"];
                JObject mtaStsEntityJObject = (JObject)(mtaStsExternal?.EntityDetail);
                MtaStsEntityState mtaStsEntity = mtaStsEntityJObject?.ToObject<MtaStsEntityState>();

                if (mtaStsEntity != null)
                {
                    if (mtaStsEntity.Messages?.Count > 0)
                    {
                        advisoryMessages.AddRange(mtaStsEntity.Messages);
                    }

                    if (mtaStsEntity.Policy?.Errors?.Count > 0)
                    {
                        advisoryMessages.AddRange(mtaStsEntity.Policy.Errors);
                    }
                }
            }

            Status status = _domainStatusEvaluator.GetStatus(advisoryMessages);
            DomainStatusEvaluation domainStatusEvaluation =
                new DomainStatusEvaluation(state.Domain, "MTASTS", status);
            _log.LogInformation($"Publishing MTA-STS domain status for {state.Domain} of {status}.");
            _dispatcher.Dispatch(domainStatusEvaluation, _emailSecurityEntityConfig.SnsTopicArn);
        }
    }
}