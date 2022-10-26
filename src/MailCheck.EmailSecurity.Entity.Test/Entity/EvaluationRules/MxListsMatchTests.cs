using System;
using System.Collections.Generic;
using System.Linq;
using MailCheck.Common.Contracts.Advisories;
using MailCheck.EmailSecurity.Entity.Domain;
using MailCheck.EmailSecurity.Entity.Domain.ExternalEntities;
using MailCheck.EmailSecurity.Entity.Entity;
using MailCheck.EmailSecurity.Entity.Entity.EvaluationRules;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace MailCheck.EmailSecurity.Entity.Test.Entity.EvaluationRules
{
    [TestFixture]
    public class MxListsMatchTests
    {
        private MxListsMatch _mxListsMatch;

        [SetUp]
        public void SetUp()
        {
            _mxListsMatch = new MxListsMatch();
        }

        [TestCaseSource(nameof(ExerciseMxListsMatchTestPermutations))]
        public void ComparesListsCorrectlyAndRaisesAdvisory(MxListsMatchTestCase test)
        {
            List<HostMxRecord> hostMxRecords = test.MxList.Select(_ => new HostMxRecord {Id = _}).ToList();
            MxEntityState mxEntityState = new MxEntityState
            {
                HostMxRecords = hostMxRecords
            };
            ExternalEntity mxExternal = new ExternalEntity
            {
                ChangedAt = DateTime.Now,
                EntityDetail = mxEntityState
            };
            List<Key> keys = test.MtaStsList.Select(_ => new Key {Type = "MxKey", Value = _}).ToList();
            MtaStsPolicyResult policyResult = new MtaStsPolicyResult
            {
                Keys = keys
            };
            MtaStsEntityState mtaStsEntityState = new MtaStsEntityState
            {
                Policy = policyResult
            };
            
            ExternalEntity mtaStsExternal = new ExternalEntity
            {
                EntityDetail = mtaStsEntityState
            };
            
            Dictionary<string, ExternalEntity> entities = new Dictionary<string, ExternalEntity>();

            entities.Add("MX", mxExternal);
            entities.Add("MTASTS", mtaStsExternal);
            
            EmailSecurityEntityState state = JObject.FromObject(new
            {
                Domain = "test.gov.uk",
                AdvisoryMessages = new List<AdvisoryMessage>(),
                CreatedAt = DateTime.MinValue,
                Entities = entities
            }).ToObject<EmailSecurityEntityState>();
            
            var result = _mxListsMatch.Evaluate(state);
            
            Assert.AreEqual(result.Result.Count, test.AdvisoriesExpected);
            if (test.AdvisoriesExpected > 0)
            {
                Assert.AreEqual(result.Result[0].MessageType, test.AdvisoryType);
                Assert.AreEqual(result.Result[0].Text, test.AdvisoryText);
                Assert.AreEqual(result.Result[0].MarkDown, test.AdvisoryMarkdown);
            }
        }

        private static IEnumerable<MxListsMatchTestCase> ExerciseMxListsMatchTestPermutations()
        {
            MxListsMatchTestCase test1 = new MxListsMatchTestCase
            {
                MxList = new List<string> {"test1.host.com.", "test2.host.com.", "test3.host.com."},
                MtaStsList = new List<string>(),
                AdvisoriesExpected = 0,
                Description = "No mtasts hosts, advisory will be raised by MTA-STS microservice instead."
            };
            MxListsMatchTestCase test2 = new MxListsMatchTestCase
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
            MxListsMatchTestCase test3 = new MxListsMatchTestCase
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
            MxListsMatchTestCase test4 = new MxListsMatchTestCase
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
            MxListsMatchTestCase test5 = new MxListsMatchTestCase
            {
                MxList = new List<string> {"test1.host.com.", "test2.host.com.", "test3.host.com."},
                MtaStsList = new List<string> {"test1.host.com.", "test2.host.com.", "test3.host.com."},
                AdvisoriesExpected = 0,
                Description = "All hosts match shouldn't accidentally raise an info advisory"
            };
            MxListsMatchTestCase test6 = new MxListsMatchTestCase
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
            MxListsMatchTestCase test7 = new MxListsMatchTestCase
            {
                MxList = new List<string> {"test1.host.com.", "test2.host.com.", "test3.host.com."},
                MtaStsList = new List<string> {"test1.host.com", "test2.host.com", "test3.host.com"},
                AdvisoriesExpected = 0,
                Description = "Policy hosts without fullstop shouldn't accidentally raise an info advisory"
            };
            MxListsMatchTestCase test8 = new MxListsMatchTestCase
            {
                MxList = new List<string> { "test1.host.com.", "test2.host.com.", "test3.host.com." },
                MtaStsList = new List<string> { "*.host.com" },
                AdvisoriesExpected = 0,
                Description = "Wildcard policy hosts should match MX hosts"
            };
            MxListsMatchTestCase test9 = new MxListsMatchTestCase
            {
                MxList = new List<string> { "test1.host.com.", "test2.host.com.", "test3.host.com." },
                MtaStsList = new List<string> { "*.com" },
                AdvisoriesExpected = 1,
                AdvisoryType = MessageType.error,
                AdvisoryText = RulesResourcesTest.NoMatchingMx,
                AdvisoryMarkdown = string.Format(RulesResourcesTest.NoMatchingMxMarkdown,
                    string.Join(", ", new List<string> { "test1.host.com", "test2.host.com", "test3.host.com" }),
                    string.Join(", ", new List<string> { "*.com" })),
                Description = "Wildcard policy hosts should only match leftmost label of MX hosts"
            };
            MxListsMatchTestCase test10 = new MxListsMatchTestCase
            {
                MxList = new List<string> { "test1.host.com.", "test2.bad.com."},
                MtaStsList = new List<string> { "test1.host.com", "*.host.com" },
                AdvisoriesExpected = 1,
                AdvisoryType = MessageType.info,
                AdvisoryText = RulesResourcesTest.SomeMatchingMx,
                AdvisoryMarkdown = string.Format(RulesResourcesTest.SomeMatchingMxMarkdown,
                    string.Join(", ", new List<string> { "test1.host.com", "test2.bad.com" }),
                    string.Join(", ", new List<string> { "test1.host.com", "*.host.com" })),
                Description = "Multiple wildcard matches are only counted once"
            };
            MxListsMatchTestCase test11 = new MxListsMatchTestCase
            {
                MxList = new List<string> {"test1.host.com", "test2.host.com", "test3.host.com"},
                MtaStsList = new List<string> {"test1.host.com", "test2.host.com", "test3.host.com"},
                AdvisoriesExpected = 0,
                Description = "Policy hosts and DNS hosts with fullstops shouldn't accidentally raise an info advisory"
            };
            MxListsMatchTestCase test12 = new MxListsMatchTestCase
            {
                MxList = new List<string> { "test1.host1.com.", "test2.host2.com."},
                MtaStsList = new List<string> { "*.host1.com", "*.host2.com" },
                AdvisoriesExpected = 0,
                Description = "Wildcard matches from different hosts which match should produce no advisories"
            };
            MxListsMatchTestCase test13 = new MxListsMatchTestCase
            {
                MxList = new List<string> { "test1.host1.com", "TEST2.HOST2.COM", "tEst3.HOsT3.cOm" },
                MtaStsList = new List<string> { "TEST1.HOST1.COM", "test2.host2.com", "TeSt3.hoSt3.COm" },
                AdvisoriesExpected = 0,
                Description = "Matching from mx host should be case insensitive"
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
        }
    }

    public class MxListsMatchTestCase
    {
        public List<string> MxList { get; set; }
        public int AdvisoriesExpected { get; set; }
        public List<string> MtaStsList { get; set; }
        public string Description { get; set; }
        public MessageType AdvisoryType { get; set; }
        public string AdvisoryText { get; set; }
        public string AdvisoryMarkdown { get; set; }
        
        public override string ToString()
        {
            return Description;
        }
    }
}