using MailCheck.Common.Contracts.Advisories;
using MailCheck.Common.Processors.Evaluators;
using MailCheck.EmailSecurity.Entity.Domain;
using MailCheck.EmailSecurity.Entity.Domain.ExternalEntities;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MailCheck.EmailSecurity.Entity.Entity.EvaluationRules
{
    public class NoMxAndNoMtaSts : IRule<EmailSecurityEntityState>
    {
        public int SequenceNo => 0;

        public bool IsStopRule => false;

        private Guid MxAndNoMtaStaId = new Guid("FD2A5580-7EC9-44B4-BEE7-883D3F5E96E8");
        private Guid NoMtaStsId = new Guid("8ec28d23-67b4-4e62-aca4-094eae3ebeae");

        public Task<List<AdvisoryMessage>> Evaluate(EmailSecurityEntityState state)
        {
            return Task.FromResult(EvaluateInternal(state));
        }

        internal List<AdvisoryMessage> EvaluateInternal(EmailSecurityEntityState state)
        {
            List<AdvisoryMessage> advisoryMessages = new List<AdvisoryMessage>();

            if (state.Entities.TryGetValue("MX", out ExternalEntity mxExternal) && state.Entities.TryGetValue("MTASTS", out ExternalEntity mtaStsExternal))
            {
                MtaStsEntityState mtaStsEntity = (mtaStsExternal?.EntityDetail as JObject)?.ToObject<MtaStsEntityState>();
                if (mtaStsEntity == null)
                {
                    return advisoryMessages;
                }

                MxEntityState mxEntity = (mxExternal?.EntityDetail as JObject)?.ToObject<MxEntityState>();
                if (mxEntity == null)
                {
                    return advisoryMessages;
                }

                if (mxEntity.HostMxRecords?.Count == 0 && mtaStsEntity.MtaStsRecords?.Records?.Count == 0)
                {
                    EmailSecAdvisoryMessage advisory = new EmailSecAdvisoryMessage(
                        MxAndNoMtaStaId, "mailcheck.mtasts.noMxAndNoMtaSta", MessageType.info,
                        RulesResources.NoMxAndNoMtaSts, null);
                    advisoryMessages.Add(advisory);
                }
                else if (mxEntity.HostMxRecords?.Count > 0 && mtaStsEntity.MtaStsRecords?.Records?.Count == 0)
                {
                    EmailSecAdvisoryMessage advisory = new EmailSecAdvisoryMessage(
                        NoMtaStsId, "mailcheck.mtasts.noMtaSts", MessageType.warning,
                        RulesResources.NoMtsStsError, RulesResources.NoMtsStsErrorMarkdown);
                    advisoryMessages.Add(advisory);
                }
            }

            return advisoryMessages;
        }
    }
}
