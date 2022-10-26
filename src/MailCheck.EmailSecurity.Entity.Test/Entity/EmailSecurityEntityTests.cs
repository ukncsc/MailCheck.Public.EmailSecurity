using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FakeItEasy;
using MailCheck.Common.Contracts.Advisories;
using MailCheck.Common.Contracts.Messaging;
using MailCheck.Common.Processors.Evaluators;
using MailCheck.EmailSecurity.Entity.Dao;
using MailCheck.EmailSecurity.Entity.Domain;
using MailCheck.EmailSecurity.Entity.Domain.ExternalEntities;
using MailCheck.EmailSecurity.Entity.Entity;
using MailCheck.EmailSecurity.Entity.Entity.DomainStatus;
using MailCheck.EmailSecurity.Entity.Entity.Notifiers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace MailCheck.EmailSecurity.Entity.Test
{
    [TestFixture]
    public class EmailSecurityEntityTests
    {
        private IEmailSecurityEntityDao _dao;
        private ILogger<EmailSecurityEntity> _log;
        private IDomainStatusPublisher _domainStatusPublisher;
        private IEvaluator<EmailSecurityEntityState> _evaluator;
        private IChangeNotifiersComposite _changeNotifiersComposite;
        
        private EmailSecurityEntity _entity;

        private Guid NoMatchId = new Guid("7fee88ac-2f32-47fa-be12-27066fe4887a");

        [SetUp]
        public void SetUp()
        {
            _dao = A.Fake<IEmailSecurityEntityDao>();
            _log = A.Fake<ILogger<EmailSecurityEntity>>();
            _domainStatusPublisher = A.Fake<IDomainStatusPublisher>();
            _evaluator = A.Fake<IEvaluator<EmailSecurityEntityState>>();
            _changeNotifiersComposite = A.Fake<IChangeNotifiersComposite>();

            _entity = new EmailSecurityEntity(_dao, _log, _domainStatusPublisher, _evaluator, _changeNotifiersComposite);
        }

        [Test]
        public async Task HandlesMxEntityChangedAsExpectedWhenStateAlreadyExists()
        {
            var mxEntityState = CreateMxEntityState();

            EntityChanged message = new EntityChanged("test.gov.uk")
            {
                RecordType = "MX",
                NewEntityDetail = mxEntityState,
                ReasonForChange = "MxRecordsPolled",
                ChangedAt = DateTime.UtcNow
            };
            message = JObject.FromObject(message).ToObject<EntityChanged>();

            var mtaStsEntityState = CreateMtaStsEntityState();
            
            ExternalEntity mtaStsEntity = new ExternalEntity()
            {
                ChangedAt = DateTime.UtcNow,
                EntityDetail = mtaStsEntityState,
                ReasonForChange = "MtaStsPolicyFetched"
            };
            mtaStsEntity = JObject.FromObject(mtaStsEntity).ToObject<ExternalEntity>();

            Dictionary<string, ExternalEntity> entities = new Dictionary<string, ExternalEntity>();

            entities.Add("MTASTS", mtaStsEntity);

            EmailSecurityEntityState currentState = new EmailSecurityEntityState
            {
                AdvisoryMessages = new List<EmailSecAdvisoryMessage>()
                {
                    new EmailSecAdvisoryMessage(
                        NoMatchId,
                        "mailcheck.mtasts.testName",
                        MessageType.error,
                        "None of the MX hosts listed in your policy file match the hosts found in your DNS MX records",
                        "You are at risk of not receiving email as the inbound sending SMTP servers will only trust the MX hosts listed in your MTA-STS policy file.\n\nHosts found in your MX DNS records: {1}\n\nHosts found in your MTA-STS policy file: {0}\n\nPlease ensure that hosts denoted with mx: in your policy file match what was found in your MX DNS records.")
                },
                Domain = "test.gov.uk",
                Entities = entities,
                CreatedAt = DateTime.MinValue,
                UpdatedAt = DateTime.MinValue
            };

            A.CallTo(() => _dao.Read("test.gov.uk")).Returns(currentState);

            await _entity.Handle(message);
           
            A.CallTo(() => _domainStatusPublisher.Publish(A<EmailSecurityEntityState>.That.Matches(_ => 
                _.Entities.ContainsKey("MTASTS") &&
                _.Entities.ContainsKey("MX") &&
                _.Domain == "test.gov.uk" &&
                _.CreatedAt == DateTime.MinValue &&
                _.State == EmailSecurityState.Evaluated &&
                _.AdvisoryMessages.Count == 0))).MustHaveHappenedOnceExactly();

            A.CallTo(() => _dao.Update(A<EmailSecurityEntityState>.That.Matches(_ =>
                _.Entities.ContainsKey("MTASTS") &&
                _.Entities.ContainsKey("MX") &&
                _.Domain == "test.gov.uk" &&
                _.CreatedAt == DateTime.MinValue &&
                _.State == EmailSecurityState.Evaluated &&
                _.AdvisoryMessages.Count == 0 ))).MustHaveHappenedOnceExactly();

            A.CallTo(() => _evaluator.Evaluate(A<EmailSecurityEntityState>.That.Matches(_ =>
                _.AdvisoryMessages.Count == 0 &&
                _.Entities.ContainsKey("MTASTS") &&
                _.Entities.ContainsKey("MX") &&
                _.Domain == "test.gov.uk" &&
                _.State == EmailSecurityState.Evaluated &&
                _.CreatedAt == DateTime.MinValue))).MustHaveHappenedOnceExactly();
        }
        
        [Test]
        public async Task HandlesMxEntityChangedAsExpectedWhenStateDoesNotExist()
        {
            var mxEntityState = CreateMxEntityState();

            EntityChanged message = new EntityChanged("test.gov.uk")
            {
                RecordType = "MX",
                NewEntityDetail = mxEntityState,
                ReasonForChange = "MxRecordsPolled",
                ChangedAt = DateTime.UtcNow
            };
            message = JObject.FromObject(message).ToObject<EntityChanged>();

            EmailSecurityEntityState state = null;
            
            A.CallTo(() => _dao.Read("test.gov.uk")).Returns(state);
            
            await _entity.Handle(message);

            A.CallTo(() => _domainStatusPublisher.Publish(A<EmailSecurityEntityState>.That.Matches(_ =>
                _.Entities.ContainsKey("MX") &&
                _.Domain == "test.gov.uk" &&
                _.Version == 1 &&
                _.State == EmailSecurityState.EvaluationPending &&
                _.AdvisoryMessages.Count == 0))).MustHaveHappenedOnceExactly();

            A.CallTo(() => _dao.Create(A<EmailSecurityEntityState>.That.Matches(_ =>
                _.Domain == "test.gov.uk" &&
                _.Version == 1 &&
                _.AdvisoryMessages.Count == 0
                ))).MustHaveHappenedOnceExactly();

            A.CallTo(() => _dao.Update(A<EmailSecurityEntityState>.That.Matches(_ =>
                _.Entities.ContainsKey("MX") &&
                _.Domain == "test.gov.uk" &&
                _.Version == 1 &&
                _.State == EmailSecurityState.EvaluationPending &&
                _.AdvisoryMessages.Count == 0))).MustHaveHappenedOnceExactly();
        }

        private static MtaStsEntityState CreateMtaStsEntityState()
        {
            MtaStsPolicyResult mtaStsPolicyResult = new MtaStsPolicyResult()
            {
                Errors = new List<AdvisoryMessage>(),
                RawValue = "version: STSv1\nmode: enforce\nmx: ncsc-gov-uk.mail.protection.outlook.com\nmax_age: 86400"
            };

            MtaStsEntityState mtaStsEntityState = new MtaStsEntityState()
            {
                Id = "test.gov.uk",
                LastUpdated = DateTime.MinValue,
                Version = 10,
                Policy = mtaStsPolicyResult
            };
            return mtaStsEntityState;
        }

        private static MxEntityState CreateMxEntityState()
        {
            List<HostMxRecord> mxRecords = new List<HostMxRecord>
            {
                new HostMxRecord()
                {
                    Preference = 0,
                    Id = "test.host1.com",
                    IpAddresses = new List<string>()
                },
                new HostMxRecord()
                {
                    Preference = 0,
                    Id = "test.host2.com",
                    IpAddresses = new List<string>()
                }
            };

            MxEntityState mxEntityState = new MxEntityState()
            {
                HostMxRecords = mxRecords,
                Id = "test.gov.uk"
            };
            return mxEntityState;
        }
    }
}
