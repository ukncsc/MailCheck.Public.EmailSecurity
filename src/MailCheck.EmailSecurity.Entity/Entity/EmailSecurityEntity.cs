using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using MailCheck.Common.Contracts.Advisories;
using MailCheck.Common.Contracts.Messaging;
using MailCheck.Common.Messaging.Abstractions;
using MailCheck.Common.Processors.Evaluators;
using MailCheck.EmailSecurity.Entity.Dao;
using MailCheck.EmailSecurity.Entity.Domain;
using MailCheck.EmailSecurity.Entity.Domain.ExternalEntities;
using MailCheck.EmailSecurity.Entity.Entity.DomainStatus;
using MailCheck.EmailSecurity.Entity.Entity.Notifiers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace MailCheck.EmailSecurity.Entity.Entity
{
    public class EmailSecurityEntity :
        IHandle<EntityChanged>
    {
        private readonly IEmailSecurityEntityDao _dao;
        private readonly ILogger<EmailSecurityEntity> _log;
        private readonly IDomainStatusPublisher _domainStatusPublisher;
        private readonly IEvaluator<EmailSecurityEntityState> _evaluator;
        private readonly IChangeNotifiersComposite _changeNotifiersComposite;

        public EmailSecurityEntity(
            IEmailSecurityEntityDao dao,
            ILogger<EmailSecurityEntity> log,
            IDomainStatusPublisher domainStatusPublisher,
            IEvaluator<EmailSecurityEntityState> evaluator,
            IChangeNotifiersComposite changeNotifiersComposite)
        {
            _dao = dao;
            _log = log;
            _domainStatusPublisher = domainStatusPublisher;
            _evaluator = evaluator;
            _changeNotifiersComposite = changeNotifiersComposite;
        }

        public async Task Handle(EntityChanged message)
        {
            string domain = message.Id;
            string recordType = message.RecordType;

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            _log.LogInformation($"Handling {recordType} EntityChanged for {domain}");

            EmailSecurityEntityState state = await _dao.Read(domain);

            if (state == null)
            {
                state = new EmailSecurityEntityState
                {
                    Domain = domain,
                    State = EmailSecurityState.Created
                };

                await _dao.Create(state);
            }
            
            ExternalEntity entity = new ExternalEntity
            {
                EntityDetail = message.NewEntityDetail,
                ChangedAt = message.ChangedAt,
                ReasonForChange = message.ReasonForChange
            };

            if (state.Entities.ContainsKey(recordType))
            {
                state.Entities[recordType] = entity;
            }
            else
            {
                state.Entities.Add(recordType, entity);
            }

            state.Entities.TryGetValue("MX", out ExternalEntity mxExternal);
            state.Entities.TryGetValue("MTASTS", out ExternalEntity mtaStsExternal);

            MtaStsEntityState mtaStsEntity = (mtaStsExternal?.EntityDetail as JObject)?.ToObject<MtaStsEntityState>();
            MxEntityState mxEntity = (mxExternal?.EntityDetail as JObject)?.ToObject<MxEntityState>();

            if (mtaStsEntity == null || mxEntity == null)
            {
                state.State = EmailSecurityState.EvaluationPending;
            }
            else
            {
                EvaluationResult<EmailSecurityEntityState> evaluationResult = await _evaluator.Evaluate(state);

                _changeNotifiersComposite.Handle(domain, state.AdvisoryMessages.Select(msg => msg as AdvisoryMessage).ToList(), evaluationResult.AdvisoryMessages);

                state.AdvisoryMessages = evaluationResult.AdvisoryMessages.OfType<EmailSecAdvisoryMessage>().ToList();
                state.State = EmailSecurityState.Evaluated;
            }

            await _dao.Update(state);

            _log.LogInformation($"{recordType} entity persisted for {domain} in {stopwatch.ElapsedMilliseconds}");
            stopwatch.Stop();

            _domainStatusPublisher.Publish(state);
        }
    }
}
