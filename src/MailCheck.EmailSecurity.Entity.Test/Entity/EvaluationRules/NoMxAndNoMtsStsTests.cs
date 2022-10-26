using MailCheck.Common.Contracts.Advisories;
using MailCheck.EmailSecurity.Entity.Domain;
using MailCheck.EmailSecurity.Entity.Domain.ExternalEntities;
using MailCheck.EmailSecurity.Entity.Entity;
using MailCheck.EmailSecurity.Entity.Entity.EvaluationRules;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MailCheck.EmailSecurity.Entity.Test.Entity.EvaluationRules
{
    [TestFixture]
    public class NoMxAndNoMtaStsTests
    {
        NoMxAndNoMtaSts _rule;

        [SetUp]
        public void SetUp()
        {
            _rule = new NoMxAndNoMtaSts();
        }

        [Test]
        public async Task WithMxWithMtaSts()
        {
            Dictionary<string, ExternalEntity> entities = new Dictionary<string, ExternalEntity>();

            ExternalEntity mxEntity = new ExternalEntity
            {
                EntityDetail = new MxEntityState
                {
                    HostMxRecords = new List<HostMxRecord>()
                    {
                        new HostMxRecord(),
                    }
                }
            };

            ExternalEntity mtaStsEntity = new ExternalEntity
            {
                EntityDetail = new MtaStsEntityState
                {
                    MtaStsRecords = new MtaStsRecords()
                    {
                        Records = new List<MtaStsRecord>()
                        {
                            new MtaStsRecord()
                        }
                    }
                }
            };

            entities.Add("MX", mxEntity);
            entities.Add("MTASTS", mtaStsEntity);

            var state = JObject.FromObject(new
            {
                Domain = "test.gov.uk",
                AdvisoryMessages = new List<AdvisoryMessage>(),
                CreatedAt = DateTime.MinValue,
                Entities = entities
            }).ToObject<EmailSecurityEntityState>();

            var result = await _rule.Evaluate(state);

            Assert.That(result.Count == 0);
        }

        [Test]
        public async Task NoMxNoMtaSts()
        {
            Dictionary<string, ExternalEntity> entities = new Dictionary<string, ExternalEntity>();

            ExternalEntity mxEntity = new ExternalEntity
            {
                EntityDetail = new MxEntityState
                {
                    HostMxRecords = new List<HostMxRecord>()
                }
            };

            ExternalEntity mtaStsEntity = new ExternalEntity
            {
                EntityDetail = new MtaStsEntityState
                {
                    MtaStsRecords = new MtaStsRecords()
                    {
                        Records = new List<MtaStsRecord>()
                    }
                }
            };

            entities.Add("MX", mxEntity);
            entities.Add("MTASTS", mtaStsEntity);

            var state = JObject.FromObject(new
            {
                Domain = "test.gov.uk",
                AdvisoryMessages = new List<AdvisoryMessage>(),
                CreatedAt = DateTime.MinValue,
                Entities = entities
            }).ToObject<EmailSecurityEntityState>();

            var result = await _rule.Evaluate(state);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(new Guid("FD2A5580-7EC9-44B4-BEE7-883D3F5E96E8"), result[0].Id);
            Assert.AreEqual(MessageType.info, result[0].MessageType);
        }

        [Test]
        public async Task NoMxWithMtaSts()
        {
            Dictionary<string, ExternalEntity> entities = new Dictionary<string, ExternalEntity>();

            ExternalEntity mxEntity = new ExternalEntity
            {
                EntityDetail = new MxEntityState
                {
                    HostMxRecords = new List<HostMxRecord>()
                }
            };

            ExternalEntity mtaStsEntity = new ExternalEntity
            {
                EntityDetail = new MtaStsEntityState
                {
                    MtaStsRecords = new MtaStsRecords()
                    {
                        Records = new List<MtaStsRecord>()
                        {
                            new MtaStsRecord()
                        }
                    }
                }
            };

            entities.Add("MX", mxEntity);
            entities.Add("MTASTS", mtaStsEntity);

            var state = JObject.FromObject(new
            {
                Domain = "test.gov.uk",
                AdvisoryMessages = new List<AdvisoryMessage>(),
                CreatedAt = DateTime.MinValue,
                Entities = entities
            }).ToObject<EmailSecurityEntityState>();

            var result = await _rule.Evaluate(state);

            Assert.That(result.Count == 0);
        }

        [Test]
        public async Task WithMxNoMtaSts()
        {
            Dictionary<string, ExternalEntity> entities = new Dictionary<string, ExternalEntity>();

            ExternalEntity mxEntity = new ExternalEntity
            {
                EntityDetail = new MxEntityState
                {
                    HostMxRecords = new List<HostMxRecord>()
                    {
                        new HostMxRecord(),
                    }
                }
            };

            ExternalEntity mtaStsEntity = new ExternalEntity
            {
                EntityDetail = new MtaStsEntityState
                {
                    MtaStsRecords = new MtaStsRecords()
                    {
                        Records = new List<MtaStsRecord>()
                    }
                }
            };

            entities.Add("MX", mxEntity);
            entities.Add("MTASTS", mtaStsEntity);

            var state = JObject.FromObject(new
            {
                Domain = "test.gov.uk",
                AdvisoryMessages = new List<AdvisoryMessage>(),
                CreatedAt = DateTime.MinValue,
                Entities = entities
            }).ToObject<EmailSecurityEntityState>();

            var result = await _rule.Evaluate(state);

            Assert.That(result.Count == 1);
            Assert.AreEqual(new Guid("8ec28d23-67b4-4e62-aca4-094eae3ebeae"), result[0].Id);
            Assert.AreEqual(MessageType.warning, result[0].MessageType);
        }

        [Test]
        public async Task WithNoMxNoMtaStsReal()
        {
            var x = @"
{
    ""Domain"": ""0191serco.corp.peterborough.gov.uk"",
    ""Entities"": {
        ""MX"": {
            ""EntityDetail"": {
                ""id"": ""0191serco.corp.peterborough.gov.uk"",
                ""mxState"": 4,
                ""hostMxRecords"": [],
                ""lastUpdated"": ""2022-02-28T11:14:14.054Z"",
                ""error"": {
                    ""id"": ""8ea38a56-dcfb-4a28-b632-85c9cf0cd27c"",
                    ""source"": ""MxPoller"",
                    ""messageType"": 2,
                    ""text"": ""Failed MX hosts query for 0191serco.corp.peterborough.gov.uk with error Non-Existent Domain"",
                    ""markDown"": """",
                    ""messageDisplay"": 0
                }
            },
            ""ChangedAt"": ""2022-02-28T11:14:14.354Z"",
            ""ReasonForChange"": ""MxRecordsPolled""
        },
        ""MTASTS"": {
            ""EntityDetail"": {
                ""id"": ""0191serco.corp.peterborough.gov.uk"",
                ""version"": 4586,
                ""mtaStsState"": ""Evaluated"",
                ""created"": ""2020-12-17T14:20:45.398Z"",
                ""mtaStsRecords"": {
                    ""domain"": ""0191serco.corp.peterborough.gov.uk"",
                    ""records"": [],
                    ""messageSize"": 123
                },
                ""messages"": [],
                ""lastUpdated"": ""2022-02-28T14:21:12.483Z"",
                ""policy"": {
                    ""rawValue"": null,
                    ""keys"": [],
                    ""errors"": []
                }
            },
            ""ChangedAt"": ""2022-02-28T14:21:12.996Z"",
            ""ReasonForChange"": ""MtaStsRecordsEvaluated""
        }
    },
    ""AdvisoryMessages"": [],
    ""Version"": 2829,
    ""CreatedAt"": ""2021-04-28T12:56:13.607Z"",
    ""UpdatedAt"": ""2022-02-28T14:21:13.192Z""
}";
            var state = JsonConvert.DeserializeObject<EmailSecurityEntityState>(x);

            var result = await _rule.Evaluate(state);

            Assert.That(result.Count == 1);
            var adv = result[0] as EmailSecAdvisoryMessage;
            Assert.AreEqual(new Guid("FD2A5580-7EC9-44B4-BEE7-883D3F5E96E8"), adv.Id);
            Assert.AreEqual("mailcheck.mtasts.noMxAndNoMtaSta", adv.Name);
            Assert.AreEqual(MessageType.info, adv.MessageType);
        }
    }
}
