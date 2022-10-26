using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FakeItEasy;
using MailCheck.Common.Contracts.Advisories;
using MailCheck.Common.Contracts.Messaging;
using MailCheck.Common.Processors.Evaluators;
using MailCheck.Common.Util;
using MailCheck.EmailSecurity.Entity.Config;
using MailCheck.EmailSecurity.Entity.Dao;
using MailCheck.EmailSecurity.Entity.Domain;
using MailCheck.EmailSecurity.Entity.Domain.ExternalEntities;
using MailCheck.EmailSecurity.Entity.Entity;
using MailCheck.EmailSecurity.Entity.Entity.DomainStatus;
using MailCheck.EmailSecurity.Entity.Entity.EvaluationRules;
using MailCheck.EmailSecurity.Entity.Entity.Notifiers;
using MailCheck.EmailSecurity.Entity.Test.Entity.EvaluationRules;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace MailCheck.EmailSecurity.Entity.Test.Entity.Integration
{
    [TestFixture(Category = "IntegrationCI")]
    public class EmailSecurityEntityIntegrationTest : BaseIntegrationTest
    {
        private IDocumentDbConfig _config;
        private ILogger<EmailSecurityEntity> _logger;
        private IDomainStatusPublisher _publisher;
        private IChangeNotifiersComposite _changeNotifiersComposite;
        private IClock _clock;
        private EmailSecurityEntity _entity;

        [SetUp]
        public void SetUp()
        {
            var loggerDao = A.Fake<ILogger<EmailSecurityEntityDao>>();
            _clock = A.Fake<IClock>();
            _config = A.Fake<IDocumentDbConfig>();
            _config.Database = "test";
            EmailSecurityEntityDao dao = new EmailSecurityEntityDao(_config, loggerDao, _clock, new MongoClientProvider(_config, Runner.ConnectionString));
            
            _logger = A.Fake<ILogger<EmailSecurityEntity>>();
            _publisher = A.Fake<IDomainStatusPublisher>();
            _changeNotifiersComposite = A.Fake<IChangeNotifiersComposite>();
            IRule<EmailSecurityEntityState> mxListMatch = new MxListsMatch();
            IRule<EmailSecurityEntityState> tlsConfiguration = new TlsConfiguration(dao);

            _entity = new EmailSecurityEntity(
                dao, 
                _logger, 
                _publisher, 
                new Evaluator<EmailSecurityEntityState>(
                    new List<IRule<EmailSecurityEntityState>>
                    {
                        mxListMatch,
                        tlsConfiguration
                    }),
                _changeNotifiersComposite
                );
        }
        
        [TestCaseSource(nameof(Test1Permutations))]
        public async Task EmailSecurityEntityIntegration(EntityIntegrationTestCase test)
        {
            A.CallTo(() => _clock.GetDateTimeUtc()).Returns(DateTime.Parse("2021-01-02T10:00:00Z"));
            
            List<HostMxRecord> hostMxRecords = test.MxList.Select(_ => new HostMxRecord {Id = _}).ToList();
            JObject mxEntityState = JObject.FromObject(new
            {
                HostMxRecords = hostMxRecords
            });
            EntityChanged message = new EntityChanged("test.gov.uk")
            {
                RecordType = "MX",
                NewEntityDetail = mxEntityState,
                ReasonForChange = "MxRecordsPolled",
                ChangedAt = DateTime.UtcNow
            };
            List<Key> keys = test.MtaStsList.Select(_ => new Key {Type = "MxKey", Value = _}).ToList();
            keys.Add(new Key{Type = "ModeKey", Value = test.MtaStsMode});
            MtaStsPolicyResult policyResult = new MtaStsPolicyResult
            {
                Keys = keys,
                Errors = new List<AdvisoryMessage>()
            };
            JObject mtaStsEntityState = JObject.FromObject(new
            {
                Policy = policyResult,
                Messages = test.MtaStsAdvisories
            });
            
            ExternalEntity mtaStsExternal = new ExternalEntity
            {
                ChangedAt = DateTime.UtcNow,
                EntityDetail = mtaStsEntityState,
                ReasonForChange = "MtaStsPolicyFetched"
            };

            Dictionary<string, ExternalEntity> entities = new Dictionary<string, ExternalEntity>
            {
                {"MTASTS", mtaStsExternal}
            };

            EmailSecurityEntityState state = JObject.FromObject(new
            {
                AdvisoryMessages = new List<EmailSecAdvisoryMessage>(),
                Domain = "test.gov.uk",
                Entities = entities,
                CreatedAt = DateTime.MinValue,
                UpdatedAt = DateTime.MinValue
            }).ToObject<EmailSecurityEntityState>();
            
            await StoreEntity(state);
            test.HostStates?.ForEach(async _ => await StoreEntity(_));
            await _entity.Handle(message);
            A.CallTo(() =>
                _publisher.Publish(A<EmailSecurityEntityState>.That.Matches(_ =>
                    CheckMatches(_.AdvisoryMessages, test)))).MustHaveHappenedOnceExactly();
        }

        private static bool CheckMatches(List<EmailSecAdvisoryMessage> messages, EntityIntegrationTestCase test)
        {
            bool expression = messages.Count > 0 ? 
                messages.Count == test.AdvisoriesExpected && 
                messages[0].Text == test.AdvisoryText && 
                messages[0].MarkDown == test.AdvisoryMarkdown && 
                messages[0].MessageType == test.AdvisoryType : messages.Count == test.AdvisoriesExpected;
            
            return expression;
        }

        private static IEnumerable<EntityIntegrationTestCase> Test1Permutations()
        {
            EntityIntegrationTestCase test1 = new EntityIntegrationTestCase
            {
                MxList = new List<string> {"test1.host.com.", "test2.host.com.", "test3.host.com."},
                MtaStsList = new List<string>(),
                AdvisoriesExpected = 0,
                Description = "No mtasts hosts, advisory will be raised by MTA-STS microservice instead."
            };
            EntityIntegrationTestCase test2 = new EntityIntegrationTestCase
            {
                MxList = new List<string> {"test1.host.com.", "test2.host.com.", "test3.host.com."},
                MtaStsList = new List<string> {"test4.host.com.", "test5.host.com.", "test6.host.com."},
                AdvisoriesExpected = 1,
                AdvisoryType = MessageType.error,
                AdvisoryText = RulesResourcesTest.NoMatchingMx,
                AdvisoryMarkdown = string.Format(RulesResourcesTest.NoMatchingMxMarkdown, 
                    string.Join(", ", new List<string> {"test1.host.com", "test2.host.com", "test3.host.com"}), 
                    string.Join(", ", new List<string> {"test4.host.com", "test5.host.com", "test6.host.com"})),
                Description = "All three hosts wrong should produce error"
            };
            EntityIntegrationTestCase test3 = new EntityIntegrationTestCase
            {
                MxList = new List<string> {"test1.host.com.", "test2.host.com.", "test3.host.com."},
                MtaStsList = new List<string> {"test1.host.com.", "test5.host.com.", "test6.host.com."},
                AdvisoriesExpected = 1,
                AdvisoryType = MessageType.info,
                AdvisoryText = RulesResourcesTest.SomeMatchingMx,
                AdvisoryMarkdown = string.Format(RulesResourcesTest.SomeMatchingMxMarkdown, 
                    string.Join(", ", new List<string> {"test1.host.com", "test2.host.com", "test3.host.com"}), 
                    string.Join(", ", new List<string> {"test1.host.com", "test5.host.com", "test6.host.com"})),
                Description = "One host correct should produce info"
            };
            EntityIntegrationTestCase test4 = new EntityIntegrationTestCase
            {
                MxList = new List<string> {"test1.host.com.", "test2.host.com.", "test3.host.com."},
                MtaStsList = new List<string> {"test1.host.com.", "test2.host.com."},
                AdvisoriesExpected = 1,
                AdvisoryType = MessageType.info,
                AdvisoryText = RulesResourcesTest.SomeMatchingMx,
                AdvisoryMarkdown = string.Format(RulesResourcesTest.SomeMatchingMxMarkdown, 
                    string.Join(", ", new List<string> {"test1.host.com", "test2.host.com", "test3.host.com"}), 
                    string.Join(", ", new List<string> {"test1.host.com", "test2.host.com"})),
                Description = "Non-matching host in mx list should produce an informational"
            };
            EntityIntegrationTestCase test5 = new EntityIntegrationTestCase
            {
                MxList = new List<string> {"test1.host.com.", "test2.host.com.", "test3.host.com."},
                MtaStsList = new List<string> {"test1.host.com.", "test2.host.com.", "test3.host.com."},
                AdvisoriesExpected = 0,
                Description = "All hosts match shouldn't accidentally raise an info advisory"
            };
            EntityIntegrationTestCase test6 = new EntityIntegrationTestCase
            {
                MxList = new List<string>(),
                MtaStsList = new List<string> {"test1.host.com.", "test2.host.com.", "test3.host.com."},
                AdvisoriesExpected = 1,
                AdvisoryType = MessageType.info,
                AdvisoryText = RulesResourcesTest.NoMatchingMx,
                AdvisoryMarkdown = string.Format(RulesResourcesTest.NoMatchingMxMarkdown, 
                    string.Join(", ", new List<string>()), 
                    string.Join(", ", new List<string> {"test1.host.com", "test2.host.com", "test3.host.com"})),
                Description = "No MX hosts but 3 policy hosts should raise an informational advisory"
            };
            EntityIntegrationTestCase test7 = new EntityIntegrationTestCase
            {
                MxList = new List<string> {"test1.host.com.", "test2.host.com.", "test3.host.com."},
                MtaStsList = new List<string> {"test1.host.com", "test2.host.com", "test3.host.com"},
                AdvisoriesExpected = 0,
                Description = "Policy hosts without fullstop shouldn't accidentally raise an info advisory"
            };
            EntityIntegrationTestCase test8 = new EntityIntegrationTestCase
            {
                HostStates = new List<EmailSecurityEntityState> { TestHelpers.GetNoIssuesHostState() },
                AdvisoriesExpected = 0,
                MtaStsList = new List<string> { "testhostgood.test.com." },
                MxList = new List<string> { "testhostgood.test.com." },
                MtaStsAdvisories = new List<AdvisoryMessage> { TestHelpers.MtaStsAdvisory },
                Description = "Incorrectly configured MTA-STS returns no advisories",
                MtaStsMode = "testing"
            };
            
            EntityIntegrationTestCase test9 = new EntityIntegrationTestCase
            {
                HostStates = new List<EmailSecurityEntityState> { TestHelpers.GetNoIssuesHostState() },
                AdvisoriesExpected = 0,
                MtaStsList = new List<string> { "testhostgood.test.com." },
                MxList = new List<string> { "testhostgood.test.com." },
                MtaStsAdvisories = new List<AdvisoryMessage>(),
                Description = "In enforce mode and no issues returns no advisories",
                MtaStsMode = "enforce"
            };
            
            EntityIntegrationTestCase test10 = new EntityIntegrationTestCase
            {
                HostStates = new List<EmailSecurityEntityState> { TestHelpers.GetNoIssuesHostState(), TestHelpers.GetCertIssuesHostState() },
                AdvisoriesExpected = 1,
                MtaStsList = new List<string> { "testhostgood.test.com.", "testhostcerterror.test.com." },
                MxList = new List<string> { "testhostgood.test.com.", "testhostcerterror.test.com." },
                MtaStsAdvisories = new List<AdvisoryMessage>(),
                Description = "In testing mode, some hosts with cert issues returns a warning advisory",
                MtaStsMode = "testing",
                AdvisoryType = MessageType.warning,
                AdvisoryText = RulesResourcesTest.TestingSomeError,
                AdvisoryMarkdown = RulesResourcesTest.TestingSomeErrorMarkdown
            };
            
            EntityIntegrationTestCase test11 = new EntityIntegrationTestCase
            {
                HostStates = new List<EmailSecurityEntityState> { TestHelpers.GetTlsIssuesHostState(), TestHelpers.GetCertIssuesHostState() },
                AdvisoriesExpected = 1,
                MtaStsList = new List<string> { "testhosttlserror.test.com.", "testhostcerterror.test.com." },
                MxList = new List<string> { "testhosttlserror.test.com.", "testhostcerterror.test.com." },
                MtaStsAdvisories = new List<AdvisoryMessage>(),
                Description = "In testing mode, all hosts with cert issues returns a warning advisory",
                MtaStsMode = "testing",
                AdvisoryType = MessageType.warning,
                AdvisoryText = RulesResourcesTest.TestingNoneError,
                AdvisoryMarkdown = RulesResourcesTest.TestingNoneErrorMarkdown
            };
            
            EntityIntegrationTestCase test12 = new EntityIntegrationTestCase
            {
                HostStates = new List<EmailSecurityEntityState>(),
                AdvisoriesExpected = 0,
                MtaStsList = new List<string> { "testhosttlserror.test.com.", "testhostcerterror.test.com." },
                MxList = new List<string> { "testhosttlserror.test.com.", "testhostcerterror.test.com." },
                MtaStsAdvisories = new List<AdvisoryMessage>(),
                Description = "In testing mode, no mx hosts listed in policy file have states in DB should return no advisories",
                MtaStsMode = "testing",
            };
            
            EntityIntegrationTestCase test13 = new EntityIntegrationTestCase
            {
                HostStates = new List<EmailSecurityEntityState> { TestHelpers.GetNoIssuesHostState(), TestHelpers.GetCertIssuesHostState() },
                AdvisoriesExpected = 1,
                MtaStsList = new List<string> { "testhostgood.test.com.", "testhostcerterror.test.com." },
                MxList = new List<string> { "testhostgood.test.com.", "testhostcerterror.test.com." },
                MtaStsAdvisories = new List<AdvisoryMessage>(),
                Description = "In enforce mode, some hosts with cert issues returns an error advisory",
                MtaStsMode = "enforce",
                AdvisoryType = MessageType.error,
                AdvisoryText = RulesResourcesTest.EnforceSomeError,
                AdvisoryMarkdown = RulesResourcesTest.EnforceSomeErrorMarkdown
            };
            
            EntityIntegrationTestCase test14 = new EntityIntegrationTestCase
            {
                HostStates = new List<EmailSecurityEntityState> { TestHelpers.GetTlsIssuesHostState(), TestHelpers.GetCertIssuesHostState() },
                AdvisoriesExpected = 1,
                MtaStsList = new List<string> { "testhosttlserror.test.com.", "testhostcerterror.test.com." },
                MxList = new List<string> { "testhosttlserror.test.com.", "testhostcerterror.test.com." },
                MtaStsAdvisories = new List<AdvisoryMessage>(),
                Description = "In enforce mode, all hosts with cert issues returns an error advisory",
                MtaStsMode = "enforce",
                AdvisoryType = MessageType.error,
                AdvisoryText = RulesResourcesTest.EnforceNoneError,
                AdvisoryMarkdown = RulesResourcesTest.EnforceNoneErrorMarkdown
            };
            
            EntityIntegrationTestCase test15 = new EntityIntegrationTestCase
            {
                HostStates = new List<EmailSecurityEntityState> { TestHelpers.GetNoIssuesHostState() },
                AdvisoriesExpected = 1,
                MtaStsList = new List<string> { "testhostgood.test.com." },
                MxList = new List<string> { "testhostgood.test.com." },
                MtaStsAdvisories = new List<AdvisoryMessage>(),
                Description = "With testing mode correctly set up, advisory should be raised telling you to go to enforce mode if there are no TLS errors",
                MtaStsMode = "testing",
                AdvisoryType = MessageType.info,
                AdvisoryText = RulesResourcesTest.GoToEnforce,
                AdvisoryMarkdown = RulesResourcesTest.GoToEnforceMarkdown
            };

            EntityIntegrationTestCase test16 = new EntityIntegrationTestCase
            {
                HostStates = new List<EmailSecurityEntityState> { TestHelpers.GetNoIssuesSimplifiedHostState() },
                AdvisoriesExpected = 0,
                MtaStsList = new List<string> { "testhostgood.test.com." },
                MxList = new List<string> { "testhostgood.test.com." },
                MtaStsAdvisories = new List<AdvisoryMessage> { TestHelpers.MtaStsAdvisory },
                Description = "Incorrectly configured MTA-STS returns no advisories",
                MtaStsMode = "testing"
            };
            
            EntityIntegrationTestCase test17 = new EntityIntegrationTestCase
            {
                HostStates = new List<EmailSecurityEntityState> { TestHelpers.GetNoIssuesSimplifiedHostState() },
                AdvisoriesExpected = 0,
                MtaStsList = new List<string> { "testhostgood.test.com." },
                MxList = new List<string> { "testhostgood.test.com." },
                MtaStsAdvisories = new List<AdvisoryMessage>(),
                Description = "In enforce mode and no issues returns no advisories",
                MtaStsMode = "enforce"
            };
            
            EntityIntegrationTestCase test18 = new EntityIntegrationTestCase
            {
                HostStates = new List<EmailSecurityEntityState> { TestHelpers.GetNoIssuesSimplifiedHostState(), TestHelpers.GetCertIssuesSimplifiedHostState() },
                AdvisoriesExpected = 1,
                MtaStsList = new List<string> { "testhostgood.test.com.", "testhostcerterror.test.com." },
                MxList = new List<string> { "testhostgood.test.com.", "testhostcerterror.test.com." },
                MtaStsAdvisories = new List<AdvisoryMessage>(),
                Description = "In testing mode, some hosts with cert issues returns a warning advisory",
                MtaStsMode = "testing",
                AdvisoryType = MessageType.warning,
                AdvisoryText = RulesResourcesTest.TestingSomeError,
                AdvisoryMarkdown = RulesResourcesTest.TestingSomeErrorMarkdown
            };
            
            EntityIntegrationTestCase test19 = new EntityIntegrationTestCase
            {
                HostStates = new List<EmailSecurityEntityState> { TestHelpers.GetTlsIssuesSimplifiedHostState(), TestHelpers.GetCertIssuesSimplifiedHostState() },
                AdvisoriesExpected = 1,
                MtaStsList = new List<string> { "testhosttlserror.test.com.", "testhostcerterror.test.com." },
                MxList = new List<string> { "testhosttlserror.test.com.", "testhostcerterror.test.com." },
                MtaStsAdvisories = new List<AdvisoryMessage>(),
                Description = "In testing mode, all hosts with cert issues returns a warning advisory",
                MtaStsMode = "testing",
                AdvisoryType = MessageType.warning,
                AdvisoryText = RulesResourcesTest.TestingNoneError,
                AdvisoryMarkdown = RulesResourcesTest.TestingNoneErrorMarkdown
            };
            
            EntityIntegrationTestCase test20 = new EntityIntegrationTestCase
            {
                HostStates = new List<EmailSecurityEntityState>(),
                AdvisoriesExpected = 0,
                MtaStsList = new List<string> { "testhosttlserror.test.com.", "testhostcerterror.test.com." },
                MxList = new List<string> { "testhosttlserror.test.com.", "testhostcerterror.test.com." },
                MtaStsAdvisories = new List<AdvisoryMessage>(),
                Description = "In testing mode, no mx hosts listed in policy file have states in DB should return no advisories",
                MtaStsMode = "testing",
            };
            
            EntityIntegrationTestCase test21 = new EntityIntegrationTestCase
            {
                HostStates = new List<EmailSecurityEntityState> { TestHelpers.GetNoIssuesSimplifiedHostState(), TestHelpers.GetCertIssuesSimplifiedHostState() },
                AdvisoriesExpected = 1,
                MtaStsList = new List<string> { "testhostgood.test.com.", "testhostcerterror.test.com." },
                MxList = new List<string> { "testhostgood.test.com.", "testhostcerterror.test.com." },
                MtaStsAdvisories = new List<AdvisoryMessage>(),
                Description = "In enforce mode, some hosts with cert issues returns an error advisory",
                MtaStsMode = "enforce",
                AdvisoryType = MessageType.error,
                AdvisoryText = RulesResourcesTest.EnforceSomeError,
                AdvisoryMarkdown = RulesResourcesTest.EnforceSomeErrorMarkdown
            };
            
            EntityIntegrationTestCase test22 = new EntityIntegrationTestCase
            {
                HostStates = new List<EmailSecurityEntityState> { TestHelpers.GetTlsIssuesSimplifiedHostState(), TestHelpers.GetCertIssuesSimplifiedHostState() },
                AdvisoriesExpected = 1,
                MtaStsList = new List<string> { "testhosttlserror.test.com.", "testhostcerterror.test.com." },
                MxList = new List<string> { "testhosttlserror.test.com.", "testhostcerterror.test.com." },
                MtaStsAdvisories = new List<AdvisoryMessage>(),
                Description = "In enforce mode, all hosts with cert issues returns an error advisory",
                MtaStsMode = "enforce",
                AdvisoryType = MessageType.error,
                AdvisoryText = RulesResourcesTest.EnforceNoneError,
                AdvisoryMarkdown = RulesResourcesTest.EnforceNoneErrorMarkdown
            };
            
            EntityIntegrationTestCase test23 = new EntityIntegrationTestCase
            {
                HostStates = new List<EmailSecurityEntityState> { TestHelpers.GetNoIssuesSimplifiedHostState() },
                AdvisoriesExpected = 1,
                MtaStsList = new List<string> { "testhostgood.test.com." },
                MxList = new List<string> { "testhostgood.test.com." },
                MtaStsAdvisories = new List<AdvisoryMessage>(),
                Description = "With testing mode correctly set up, advisory should be raised telling you to go to enforce mode if there are no TLS errors",
                MtaStsMode = "testing",
                AdvisoryType = MessageType.info,
                AdvisoryText = RulesResourcesTest.GoToEnforce,
                AdvisoryMarkdown = RulesResourcesTest.GoToEnforceMarkdown
            };

            yield return test1;
            yield return test2;
            yield return test3;
            yield return test4;
            yield return test5;
            yield return test6;
            yield return test7;
            yield return test8;
            yield return test9;
            yield return test10;
            yield return test11;
            yield return test12;
            yield return test13;
            yield return test14;
            yield return test15;
            yield return test16;
            yield return test17;
            yield return test18;
            yield return test19;
            yield return test20;
            yield return test21;
            yield return test22;
            yield return test23;
        }
    }

    public class EntityIntegrationTestCase
    {
        public List<string> MxList { get; set; }
        public int AdvisoriesExpected { get; set; }
        public List<string> MtaStsList { get; set; }
        public string Description { get; set; }
        public MessageType AdvisoryType { get; set; }
        public string AdvisoryText { get; set; }
        public string AdvisoryMarkdown { get; set; }
        public List<EmailSecurityEntityState> HostStates { get; set; }
        public List<AdvisoryMessage> MtaStsAdvisories { get; set; }
        public string MtaStsMode { get; set; } = "enforce";
        
        public override string ToString()
        {
            return Description;
        }
    }
}