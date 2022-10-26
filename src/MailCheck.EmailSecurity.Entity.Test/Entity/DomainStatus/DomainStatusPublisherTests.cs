using System;
using System.Collections.Generic;
using FakeItEasy;
using MailCheck.Common.Contracts.Advisories;
using MailCheck.Common.Messaging.Abstractions;
using MailCheck.DomainStatus.Contracts;
using MailCheck.EmailSecurity.Entity.Config;
using MailCheck.EmailSecurity.Entity.Domain;
using MailCheck.EmailSecurity.Entity.Domain.ExternalEntities;
using MailCheck.EmailSecurity.Entity.Entity;
using MailCheck.EmailSecurity.Entity.Entity.DomainStatus;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace MailCheck.EmailSecurity.Entity.Test.Entity.DomainStatus
{
    [TestFixture]
    public class DomainStatusPublisherTests
    {
        private IEmailSecurityEntityConfig _config;
        private IMessageDispatcher _dispatcher;
        private ILogger<DomainStatusPublisher> _log;
        private IDomainStatusEvaluator _domainStatusEvaluator;
        private DomainStatusPublisher _domainStatusPublisher;

        [SetUp]
        public void SetUp()
        {
            _dispatcher = A.Fake<IMessageDispatcher>();
            _config = A.Fake<IEmailSecurityEntityConfig>();
            _domainStatusEvaluator = new DomainStatusEvaluator();
            _log = A.Fake<ILogger<DomainStatusPublisher>>();

            _domainStatusPublisher = new DomainStatusPublisher(_dispatcher, _config, _domainStatusEvaluator, _log);
        }

        [TestCase(MessageType.info, MessageType.info, MessageType.error, Status.Error)]
        [TestCase(MessageType.info, MessageType.info, MessageType.info, Status.Info)]
        [TestCase(MessageType.info, MessageType.info, MessageType.warning, Status.Warning)]
        [TestCase(MessageType.warning, MessageType.info, MessageType.error, Status.Error)]
        [TestCase(MessageType.warning, MessageType.info, MessageType.info, Status.Warning)]
        [TestCase(MessageType.error, MessageType.info, MessageType.warning, Status.Error)]
        [TestCase(MessageType.info, MessageType.error, MessageType.info, Status.Error)]
        [TestCase(MessageType.info, MessageType.error, MessageType.info, Status.Error)]
        [TestCase(MessageType.warning, MessageType.error, MessageType.warning, Status.Error)]
        public void ExtractsMessagesCorrectly(MessageType type1, MessageType type2, MessageType type3, Status result)
        {
            AdvisoryMessage advisoryMessage = new AdvisoryMessage(Guid.NewGuid(), type1, nameof(type1), nameof(type1));

            Dictionary<string, ExternalEntity> entities = new Dictionary<string, ExternalEntity>();

            List<HostMxRecord> hostMxRecords = new List<HostMxRecord>
            {
                new HostMxRecord { Id = "testhost1.com.", IpAddresses = new List<string>(), Preference = 0 },
                new HostMxRecord { Id = "testhost2.com.", IpAddresses = new List<string>(), Preference = 0 },
            };

            MxEntityState mxEntityState = new MxEntityState
            {
                HostMxRecords = hostMxRecords
            };

            ExternalEntity mxEntity = new ExternalEntity
            {
                EntityDetail = mxEntityState,
                ChangedAt = DateTime.UtcNow,
                ReasonForChange = "MxRecordsPolled"
            };

            MtaStsPolicyResult mtaStsPolicyResult = new MtaStsPolicyResult
            {
                Errors = new List<AdvisoryMessage> { new AdvisoryMessage(Guid.NewGuid(), type2, nameof(type2), nameof(type2)) }
            };

            MtaStsEntityState mtaStsEntityState = new MtaStsEntityState
            {
                Id = "test.gov.uk",
                Policy = mtaStsPolicyResult,
                Messages = new List<AdvisoryMessage> { new AdvisoryMessage(Guid.NewGuid(), type3, nameof(type3), nameof(type3)) }
            };

            ExternalEntity mtaStsEntity = new ExternalEntity
            {
                EntityDetail = mtaStsEntityState,
                ReasonForChange = "MtaStsPolicyFetched",
                ChangedAt = DateTime.UtcNow
            };

            entities.Add("MX", mxEntity);
            entities.Add("MTASTS", mtaStsEntity);

            EmailSecurityEntityState state = JObject.FromObject(new
            {
                AdvisoryMessages = new List<AdvisoryMessage> { advisoryMessage },
                Domain = "test.gov.uk",
                Entities = entities
            }).ToObject<EmailSecurityEntityState>();

            _domainStatusPublisher.Publish(state);

            A.CallTo(() => _dispatcher.Dispatch(A<DomainStatusEvaluation>.That.Matches(_ =>
                _.RecordType == "MTASTS" &&
                _.Status == result &&
                _.Id == "test.gov.uk"), A<string>._)).MustHaveHappenedOnceExactly();
        }
    }
}
