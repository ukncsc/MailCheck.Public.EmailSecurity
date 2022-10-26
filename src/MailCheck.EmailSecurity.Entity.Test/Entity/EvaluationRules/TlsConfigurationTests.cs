using System;
using System.Collections.Generic;
using System.Linq;
using FakeItEasy;
using MailCheck.Common.Contracts.Advisories;
using MailCheck.EmailSecurity.Entity.Dao;
using MailCheck.EmailSecurity.Entity.Domain;
using MailCheck.EmailSecurity.Entity.Domain.ExternalEntities;
using MailCheck.EmailSecurity.Entity.Entity;
using MailCheck.EmailSecurity.Entity.Entity.EvaluationRules;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace MailCheck.EmailSecurity.Entity.Test.Entity.EvaluationRules
{
    [TestFixture]
    public class TlsConfigurationTests
    {
        private TlsConfiguration _tlsConfiguration;
        private IEmailSecurityEntityDao _dao;

        [SetUp]
        public void SetUp()
        {
            _dao = A.Fake<IEmailSecurityEntityDao>();
            _tlsConfiguration = new TlsConfiguration(_dao);
        }

        [TestCaseSource(nameof(ExerciseTlsConfigurationTestPermutations))]
        public void ExerciseTlsConfigurationTest(TlsConfigurationTestCase test)
        {
            List<Key> keys = test.MtaStsList.Select(_ => new Key {Type = "MxKey", Value = _}).ToList();
            keys.Add(new Key{Type = "ModeKey", Value = test.MtaStsMode});
            MtaStsPolicyResult policyResult = new MtaStsPolicyResult
            {
                Keys = keys,
                Errors = test.PolicyErrors
            };
            MtaStsEntityState mtaStsEntityDetail = new MtaStsEntityState
            {
                Policy = policyResult,
                Messages = test.MtaStsAdvisories
            };
            
            ExternalEntity mtaStsExternalEntity = new ExternalEntity
            {
                EntityDetail = mtaStsEntityDetail
            };
            
            Dictionary<string, ExternalEntity> entities = new Dictionary<string, ExternalEntity>();
            
            entities.Add("MTASTS", mtaStsExternalEntity);
            
            EmailSecurityEntityState state = JObject.FromObject(new
            {
                Entities = entities
            }).ToObject<EmailSecurityEntityState>();
            
            test.MtaStsList.ForEach(_ => A.CallTo(() => _dao.Read(_)).Returns(test.HostStates.Find(h => h.Domain == _)));
            var result = _tlsConfiguration.Evaluate(state);
            
            Assert.AreEqual(result.Result.Count, test.AdvisoriesExpected);
            if (test.AdvisoriesExpected > 0)
            {
                Assert.AreEqual(result.Result[0].MessageType, test.AdvisoryType);
                Assert.AreEqual(result.Result[0].Text, test.AdvisoryText);
                Assert.AreEqual(result.Result[0].MarkDown, test.AdvisoryMarkdown);
            }
        }

        private static IEnumerable<TlsConfigurationTestCase> ExerciseTlsConfigurationTestPermutations()
        {
            
            
            TlsConfigurationTestCase test1 = new TlsConfigurationTestCase
            {
                HostStates = new List<EmailSecurityEntityState> { TestHelpers.GetNoIssuesHostState() },
                AdvisoriesExpected = 0,
                MtaStsList = new List<string> { "testhostgood.test.com." },
                MtaStsAdvisories = new List<AdvisoryMessage> { TestHelpers.MtaStsAdvisory },
                Description = "Incorrectly configured MTA-STS returns no advisories",
                MtaStsMode = "testing"
            };
            
            TlsConfigurationTestCase test2 = new TlsConfigurationTestCase
            {
                HostStates = new List<EmailSecurityEntityState> { TestHelpers.GetNoIssuesHostState() },
                AdvisoriesExpected = 0,
                MtaStsList = new List<string> { "testhostgood.test.com." },
                MtaStsAdvisories = new List<AdvisoryMessage>(),
                Description = "In enforce mode and no issues returns no advisories",
                MtaStsMode = "enforce"
            };
            
            TlsConfigurationTestCase test3 = new TlsConfigurationTestCase
            {
                HostStates = new List<EmailSecurityEntityState> { TestHelpers.GetNoIssuesHostState(), TestHelpers.GetCertIssuesHostState() },
                AdvisoriesExpected = 1,
                MtaStsList = new List<string> { "testhostgood.test.com.", "testhostcerterror.test.com." },
                MtaStsAdvisories = new List<AdvisoryMessage>(),
                Description = "In testing mode, some hosts with cert issues returns a warning advisory",
                MtaStsMode = "testing",
                AdvisoryType = MessageType.warning,
                AdvisoryText = RulesResourcesTest.TestingSomeError,
                AdvisoryMarkdown = RulesResourcesTest.TestingSomeErrorMarkdown
            };
            
            TlsConfigurationTestCase test4 = new TlsConfigurationTestCase
            {
                HostStates = new List<EmailSecurityEntityState> { TestHelpers.GetTlsIssuesHostState(), TestHelpers.GetCertIssuesHostState() },
                AdvisoriesExpected = 1,
                MtaStsList = new List<string> { "testhosttlserror.test.com.", "testhostcerterror.test.com." },
                MtaStsAdvisories = new List<AdvisoryMessage>(),
                Description = "In testing mode, all hosts with cert issues returns a warning advisory",
                MtaStsMode = "testing",
                AdvisoryType = MessageType.warning,
                AdvisoryText = RulesResourcesTest.TestingNoneError,
                AdvisoryMarkdown = RulesResourcesTest.TestingNoneErrorMarkdown
            };
            
            TlsConfigurationTestCase test5 = new TlsConfigurationTestCase
            {
                HostStates = new List<EmailSecurityEntityState>(),
                AdvisoriesExpected = 0,
                MtaStsList = new List<string> { "testhosttlserror.test.com.", "testhostcerterror.test.com." },
                MtaStsAdvisories = new List<AdvisoryMessage>(),
                Description = "In testing mode, no mx hosts listed in policy file have states in DB should return no advisories",
                MtaStsMode = "testing",
            };
            
            TlsConfigurationTestCase test6 = new TlsConfigurationTestCase
            {
                HostStates = new List<EmailSecurityEntityState> { TestHelpers.GetNoIssuesHostState(), TestHelpers.GetCertIssuesHostState() },
                AdvisoriesExpected = 1,
                MtaStsList = new List<string> { "testhostgood.test.com.", "testhostcerterror.test.com." },
                MtaStsAdvisories = new List<AdvisoryMessage>(),
                Description = "In enforce mode, some hosts with cert issues returns an error advisory",
                MtaStsMode = "enforce",
                AdvisoryType = MessageType.error,
                AdvisoryText = RulesResourcesTest.EnforceSomeError,
                AdvisoryMarkdown = RulesResourcesTest.EnforceSomeErrorMarkdown
            };
            
            TlsConfigurationTestCase test7 = new TlsConfigurationTestCase
            {
                HostStates = new List<EmailSecurityEntityState> { TestHelpers.GetTlsIssuesHostState(), TestHelpers.GetCertIssuesHostState() },
                AdvisoriesExpected = 1,
                MtaStsList = new List<string> { "testhosttlserror.test.com.", "testhostcerterror.test.com." },
                MtaStsAdvisories = new List<AdvisoryMessage>(),
                Description = "In enforce mode, all hosts with cert issues returns an error advisory",
                MtaStsMode = "enforce",
                AdvisoryType = MessageType.error,
                AdvisoryText = RulesResourcesTest.EnforceNoneError,
                AdvisoryMarkdown = RulesResourcesTest.EnforceNoneErrorMarkdown
            };
            
            TlsConfigurationTestCase test8 = new TlsConfigurationTestCase
            {
                HostStates = new List<EmailSecurityEntityState> { TestHelpers.GetNoIssuesHostState() },
                AdvisoriesExpected = 1,
                MtaStsList = new List<string> { "testhostgood.test.com." },
                MtaStsAdvisories = new List<AdvisoryMessage>(),
                Description = "With testing mode correctly set up, advisory should be raised telling you to go to enforce mode if there are no TLS errors",
                MtaStsMode = "testing",
                AdvisoryType = MessageType.info,
                AdvisoryText = RulesResourcesTest.GoToEnforce,
                AdvisoryMarkdown = RulesResourcesTest.GoToEnforceMarkdown
            };
            
            TlsConfigurationTestCase test9 = new TlsConfigurationTestCase
            {
                HostStates = new List<EmailSecurityEntityState> { TestHelpers.GetNoIssuesHostState() },
                AdvisoriesExpected = 1,
                MtaStsList = new List<string> { "testhostgood.test.com." },
                MtaStsAdvisories = new List<AdvisoryMessage> { new AdvisoryMessage(TlsConfiguration.MtaStsGoToEnforceId, MessageType.warning, "mtasts", "mtasts")},
                Description = "With testing mode correctly set up and existing amber advisory, advisory should be raised telling you to go to enforce mode if there are no TLS errors",
                MtaStsMode = "testing",
                AdvisoryType = MessageType.info,
                AdvisoryText = RulesResourcesTest.GoToEnforce,
                AdvisoryMarkdown = RulesResourcesTest.GoToEnforceMarkdown
            };
            
            TlsConfigurationTestCase test10 = new TlsConfigurationTestCase
            {
                HostStates = new List<EmailSecurityEntityState> { TestHelpers.GetNoIssuesSimplifiedHostState() },
                AdvisoriesExpected = 0,
                MtaStsList = new List<string> { "testhostgood.test.com." },
                MtaStsAdvisories = new List<AdvisoryMessage> { TestHelpers.MtaStsAdvisory },
                Description = "Incorrectly configured MTA-STS returns no advisories",
                MtaStsMode = "testing"
            };

            TlsConfigurationTestCase test11 = new TlsConfigurationTestCase
            {
                HostStates = new List<EmailSecurityEntityState> { TestHelpers.GetNoIssuesSimplifiedHostState(), TestHelpers.GetNoCertAdvisoriesSimplifiedHostState() },
                AdvisoriesExpected = 0,
                MtaStsList = new List<string> { "testhostgood.test.com." },
                MtaStsAdvisories = new List<AdvisoryMessage>(),
                Description = "In enforce mode and no issues returns no advisories",
                MtaStsMode = "enforce"
            };

            TlsConfigurationTestCase test12 = new TlsConfigurationTestCase
            {
                HostStates = new List<EmailSecurityEntityState> { TestHelpers.GetNoIssuesSimplifiedHostState(), TestHelpers.GetCertIssuesSimplifiedHostState() },
                AdvisoriesExpected = 1,
                MtaStsList = new List<string> { "testhostgood.test.com.", "testhostcerterror.test.com." },
                MtaStsAdvisories = new List<AdvisoryMessage>(),
                Description = "In testing mode, some hosts with cert issues returns a warning advisory",
                MtaStsMode = "testing",
                AdvisoryType = MessageType.warning,
                AdvisoryText = RulesResourcesTest.TestingSomeError,
                AdvisoryMarkdown = RulesResourcesTest.TestingSomeErrorMarkdown
            };

            TlsConfigurationTestCase test13 = new TlsConfigurationTestCase
            {
                HostStates = new List<EmailSecurityEntityState> { TestHelpers.GetCertIssuesSimplifiedHostState(), TestHelpers.GetCertIssuesSimplifiedHostState() },
                AdvisoriesExpected = 1,
                MtaStsList = new List<string> { "testhostcerterror.test.com.", "testhostcerterror.test.com." },
                MtaStsAdvisories = new List<AdvisoryMessage>(),
                Description = "In testing mode, all hosts with cert issues returns a warning advisory",
                MtaStsMode = "testing",
                AdvisoryType = MessageType.warning,
                AdvisoryText = RulesResourcesTest.TestingNoneError,
                AdvisoryMarkdown = RulesResourcesTest.TestingNoneErrorMarkdown
            };

            TlsConfigurationTestCase test14 = new TlsConfigurationTestCase
            {
                HostStates = new List<EmailSecurityEntityState>(),
                AdvisoriesExpected = 0,
                MtaStsList = new List<string> { "testhosttlserror.test.com.", "testhostcerterror.test.com." },
                MtaStsAdvisories = new List<AdvisoryMessage>(),
                Description = "In testing mode, no mx hosts listed in policy file have states in DB should return no advisories",
                MtaStsMode = "testing",
            };

            TlsConfigurationTestCase test15 = new TlsConfigurationTestCase
            {
                HostStates = new List<EmailSecurityEntityState> { TestHelpers.GetNoIssuesSimplifiedHostState(), TestHelpers.GetCertIssuesSimplifiedHostState() },
                AdvisoriesExpected = 1,
                MtaStsList = new List<string> { "testhostgood.test.com.", "testhostcerterror.test.com." },
                MtaStsAdvisories = new List<AdvisoryMessage>(),
                Description = "In enforce mode, some hosts with cert issues returns an error advisory",
                MtaStsMode = "enforce",
                AdvisoryType = MessageType.error,
                AdvisoryText = RulesResourcesTest.EnforceSomeError,
                AdvisoryMarkdown = RulesResourcesTest.EnforceSomeErrorMarkdown
            };

            TlsConfigurationTestCase test16 = new TlsConfigurationTestCase
            {
                HostStates = new List<EmailSecurityEntityState> { TestHelpers.GetTlsIssuesSimplifiedHostState(), TestHelpers.GetCertIssuesSimplifiedHostState() },
                AdvisoriesExpected = 1,
                MtaStsList = new List<string> { "testhosttlserror.test.com.", "testhostcerterror.test.com." },
                MtaStsAdvisories = new List<AdvisoryMessage>(),
                Description = "In enforce mode, all hosts with cert issues returns an error advisory",
                MtaStsMode = "enforce",
                AdvisoryType = MessageType.error,
                AdvisoryText = RulesResourcesTest.EnforceNoneError,
                AdvisoryMarkdown = RulesResourcesTest.EnforceNoneErrorMarkdown
            };

            TlsConfigurationTestCase test17 = new TlsConfigurationTestCase
            {
                HostStates = new List<EmailSecurityEntityState> { TestHelpers.GetNoIssuesSimplifiedHostState() },
                AdvisoriesExpected = 1,
                MtaStsList = new List<string> { "testhostgood.test.com." },
                MtaStsAdvisories = new List<AdvisoryMessage>(),
                Description = "With testing mode correctly set up, advisory should be raised telling you to go to enforce mode if there are no TLS errors",
                MtaStsMode = "testing",
                AdvisoryType = MessageType.info,
                AdvisoryText = RulesResourcesTest.GoToEnforce,
                AdvisoryMarkdown = RulesResourcesTest.GoToEnforceMarkdown
            };

            TlsConfigurationTestCase test18 = new TlsConfigurationTestCase
            {
                HostStates = new List<EmailSecurityEntityState> { TestHelpers.GetNoIssuesSimplifiedHostState() },
                AdvisoriesExpected = 1,
                MtaStsList = new List<string> { "testhostgood.test.com." },
                MtaStsAdvisories = new List<AdvisoryMessage> { new AdvisoryMessage(TlsConfiguration.MtaStsGoToEnforceId, MessageType.warning, "mtasts", "mtasts") },
                Description = "With testing mode correctly set up and existing amber advisory, advisory should be raised telling you to go to enforce mode if there are no TLS errors",
                MtaStsMode = "testing",
                AdvisoryType = MessageType.info,
                AdvisoryText = RulesResourcesTest.GoToEnforce,
                AdvisoryMarkdown = RulesResourcesTest.GoToEnforceMarkdown
            };

            TlsConfigurationTestCase test19 = new TlsConfigurationTestCase
            {
                HostStates = new List<EmailSecurityEntityState> { TestHelpers.GetTlsIssuesSimplifiedHostState() },
                AdvisoriesExpected = 1,
                MtaStsList = new List<string> { "testhosttlserror.test.com." },
                MtaStsAdvisories = new List<AdvisoryMessage>(),
                Description = "In testing mode, warning advisory should be raised if TLS errors are found",
                MtaStsMode = "testing",
                AdvisoryType = MessageType.warning,
                AdvisoryText = RulesResourcesTest.TestingNoneError,
                AdvisoryMarkdown = RulesResourcesTest.TestingNoneErrorMarkdown
            };

            TlsConfigurationTestCase test20 = new TlsConfigurationTestCase
            {
                HostStates = new List<EmailSecurityEntityState> { TestHelpers.GetNoIssuesSimplifiedHostState() },
                AdvisoriesExpected = 0,
                MtaStsList = new List<string> { "testhostgood.test.com." },
                MtaStsAdvisories = new List<AdvisoryMessage>(),
                Description = "With testing mode and policy errors, advisory should not be raised telling you to go to enforce mode if there are no TLS errors",
                MtaStsMode = "testing",
                PolicyErrors = new List<AdvisoryMessage> { new AdvisoryMessage(new Guid(), MessageType.error, "test", "test")}
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
        }
    }

    public class TlsConfigurationTestCase
    {
        public List<EmailSecurityEntityState> HostStates { get; set; }
        public int AdvisoriesExpected { get; set; }
        public List<string> MtaStsList { get; set; }
        public string Description { get; set; }
        public MessageType AdvisoryType { get; set; }
        public string AdvisoryText { get; set; }
        public string AdvisoryMarkdown { get; set; }
        public List<AdvisoryMessage> MtaStsAdvisories { get; set; }
        public string MtaStsMode { get; set; }
        public List<AdvisoryMessage> PolicyErrors { get; set; } = new List<AdvisoryMessage>();
        
        public override string ToString()
        {
            return Description;
        }
    }
}