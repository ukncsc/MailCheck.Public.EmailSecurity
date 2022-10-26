using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MailCheck.Common.Contracts.Advisories;
using MailCheck.Common.Processors.Evaluators;
using MailCheck.EmailSecurity.Entity.Dao;
using MailCheck.EmailSecurity.Entity.Domain;
using MailCheck.EmailSecurity.Entity.Domain.ExternalEntities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MailCheck.EmailSecurity.Entity.Entity.EvaluationRules
{
    public class TlsConfiguration : IRule<EmailSecurityEntityState>
    {
        public int SequenceNo => 2;
        public bool IsStopRule => false;
        private readonly IEmailSecurityEntityDao _dao;

        private Guid testingSomeId = new Guid("122ebd52-3d93-46e9-95de-659bf89ca2b8");
        private Guid testingNoneId = new Guid("a7541767-ed48-4862-bab7-dd9344caa9aa");
        private Guid enforceSomeId = new Guid("b0e6747f-a6bd-42f8-b06d-e16fe24e76b4");
        private Guid enforceNoneId = new Guid("8b52ca95-ba29-4f64-bd0b-873d877ad3b5");
        private Guid goToEnforceId = new Guid("33c14e10-6223-46c0-abe0-82ef34a815be");
        
        public static Guid MtaStsGoToEnforceId = new Guid("5D35C65E-D853-43EF-B1DA-F3473603200B");

        public TlsConfiguration(IEmailSecurityEntityDao dao)
        {
            _dao = dao;
        }
        
        public async Task<List<AdvisoryMessage>> Evaluate(EmailSecurityEntityState state)
        {
            List<AdvisoryMessage> advisoryMessages = new List<AdvisoryMessage>();

            if (state.Entities.TryGetValue("MTASTS", out ExternalEntity mtaStsExternal))
            {
                JObject mtaStsEntityJObject = (JObject)mtaStsExternal?.EntityDetail;

                if (mtaStsEntityJObject == null)
                {
                    return advisoryMessages;
                }

                MtaStsEntityState mtaStsEntity = mtaStsEntityJObject.ToObject<MtaStsEntityState>();

                if (mtaStsEntity?.Messages?.Count == 0 || (mtaStsEntity?.Messages?.Count == 1 && mtaStsEntity.Messages?[0].Id == MtaStsGoToEnforceId))
                {
                    Key modeKey = mtaStsEntity.Policy?.Keys?.Find(_ => _.Type == "ModeKey");
                    if (modeKey != null)
                    {
                        string mode = modeKey.Value;
                        List<Key> mxKeys = mtaStsEntity.Policy?.Keys?.FindAll(_ => _.Type == "MxKey");
                        List<string> mxHosts = mxKeys.Select(_ => _.Value.EndsWith(".") ? _.Value : _.Value + ".").ToList();
                        if (mode == "testing")
                        {
                            List<AdvisoryMessage> testingAdvisories = await GetTestingAdvisories(state, mxHosts, mtaStsEntity.Policy);
                            advisoryMessages.AddRange(testingAdvisories);
                        }

                        if (mode == "enforce")
                        {
                            List<AdvisoryMessage> enforceAdvisories = await GetEnforceAdvisories(state, mxHosts);
                            advisoryMessages.AddRange(enforceAdvisories);
                        }
                    }
                }
            }

            return advisoryMessages;
        }

        private async Task<List<AdvisoryMessage>> GetTestingAdvisories(EmailSecurityEntityState state, List<string> mxHosts, MtaStsPolicyResult policy)
        {
            List<AdvisoryMessage> testingAdvisories = new List<AdvisoryMessage>();

            if (mxHosts.Count > 0)
            {
                int invalidCount = await GetInvalidCount(mxHosts);
                if (invalidCount > 0 && invalidCount < mxHosts.Count)
                {
                    testingAdvisories.Add(new EmailSecAdvisoryMessage(testingSomeId, "mailcheck.mtasts.testingModeSomeInvalid", MessageType.warning, RulesResources.TestingSomeError, RulesResources.TestingSomeErrorMarkdown));
                }
                else if (invalidCount == mxHosts.Count)
                {
                    testingAdvisories.Add(new EmailSecAdvisoryMessage(testingNoneId, "mailcheck.mtasts.testingModeAllInvalid", MessageType.warning, RulesResources.TestingNoneError, RulesResources.TestingNoneErrorMarkdown));
                }
                else if (invalidCount == 0)
                {
                    if (policy?.Errors?.Count == 0) testingAdvisories.Add(new EmailSecAdvisoryMessage(goToEnforceId, "mailcheck.mtasts.goToEnforce", MessageType.info, RulesResources.GoToEnforce, RulesResources.GoToEnforceMarkdown));
                }
            }
            
            return testingAdvisories;
        }
        
        private async Task<List<AdvisoryMessage>> GetEnforceAdvisories(EmailSecurityEntityState state, List<string> mxHosts)
        {
            List<AdvisoryMessage> enforceAdvisories = new List<AdvisoryMessage>();
                
            if (mxHosts.Count > 0)
            {
                int invalidCount = await GetInvalidCount(mxHosts);
                
                if (invalidCount > 0 && invalidCount < mxHosts.Count)
                {
                    enforceAdvisories.Add(new EmailSecAdvisoryMessage(enforceSomeId, "mailcheck.mtasts.enforceModeSomeInvalid", MessageType.error, RulesResources.EnforceSomeError, RulesResources.EnforceSomeErrorMarkdown));
                }
                else if (invalidCount == mxHosts.Count)
                {
                    enforceAdvisories.Add(new EmailSecAdvisoryMessage(enforceNoneId, "mailcheck.mtasts.enforceModeAllInvalid", MessageType.error, RulesResources.EnforceNoneError, RulesResources.EnforceNoneErrorMarkdown));
                }
            }
            return enforceAdvisories;
        }

        private async Task<int> GetInvalidCount(List<string> mxHosts)
        {
            int invalidCount = 0;

            foreach (var mxHost in mxHosts)
            {
                EmailSecurityEntityState hostState = await _dao.Read(mxHost);
                if (hostState != null)
                {
                    if (hostState.Entities.TryGetValue("SIMPLIFIEDTLS", out ExternalEntity simplifiedTlsExternal))
                    {
                        JObject tlsEntityJObject = (JObject)simplifiedTlsExternal?.EntityDetail;
                        if (tlsEntityJObject == null) return 0;

                        SimplifiedEmailSecTlsEntityState tlsEntity = tlsEntityJObject.ToObject<SimplifiedEmailSecTlsEntityState>();
                        if ((tlsEntity?.TlsAdvisories?.Any(x => x.MessageType == MessageType.error) ?? false) ||
                            (tlsEntity?.CertAdvisories?.Any(x => x.MessageType == MessageType.error) ?? false))
                        {
                            invalidCount++;
                        }
                    }
                    else if (hostState.Entities.TryGetValue("TLS", out ExternalEntity tlsExternal))
                    {
                        JObject tlsEntityJObject = (JObject)tlsExternal?.EntityDetail;
                        if (tlsEntityJObject == null) return 0;
                        TlsEntityState tlsEntity = tlsEntityJObject.ToObject<TlsEntityState>();
                        if (tlsEntity?.TlsRecords?.Tls12Available?.TlsEvaluatedResult?.Result !=
                            EvaluatorResult.PASS || tlsEntity?.CertificateResults?.Errors?.Count > 0)
                        {
                            invalidCount++;
                        }
                    }
                }
                else
                {
                    return -1;
                }
            }
            
            return invalidCount;
        }
    }
}