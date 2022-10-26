using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MailCheck.Common.Contracts.Advisories;
using MailCheck.Common.Processors.Evaluators;
using MailCheck.EmailSecurity.Entity.Domain;
using MailCheck.EmailSecurity.Entity.Domain.ExternalEntities;
using Newtonsoft.Json.Linq;

namespace MailCheck.EmailSecurity.Entity.Entity.EvaluationRules
{
    public class MxListsMatch : IRule<EmailSecurityEntityState>
    {
        public int SequenceNo => 1;

        public bool IsStopRule => false;

        private Guid NoMatchId = new Guid("7fee88ac-2f32-47fa-be12-27066fe4887a");

        private Guid SomeMatchId = new Guid("123a26ee-e01c-4673-99cb-badbe8545ba6");


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

                List<string> mxFromPolicy = mtaStsEntity?.Policy?.Keys?
                    .Where(_ => _.Type == "MxKey")
                    .Select(_ => _?.Value.Trim().TrimEnd('.') ?? "")
                    .ToList() ?? new List<string>();
                List<string> mxFromDns = mxEntity?.HostMxRecords?
                    .Select(_ => _?.Id.Trim().TrimEnd('.') ?? "")
                    .ToList() ?? new List<string>();

                if (mxFromPolicy.Count == 0)
                {
                    return advisoryMessages;
                }

                if (mxFromDns.Count == 0)
                {
                    EmailSecAdvisoryMessage advisory = new EmailSecAdvisoryMessage(
                        NoMatchId, "mailcheck.mtasts.noMatchingMx", MessageType.info,
                        RulesResources.NoMatchingMx,
                        string.Format(RulesResources.NoMatchingMxMarkdown,
                            string.Join(", ", mxFromDns),
                            string.Join(", ", mxFromPolicy)));
                    advisoryMessages.Add(advisory);

                    return advisoryMessages;
                }

                List<string> matchingMxList = mxFromDns.Intersect(mxFromPolicy, StringComparer.InvariantCultureIgnoreCase).ToList();
                List<string> unmatchedMxList = mxFromDns.Except(matchingMxList, StringComparer.InvariantCultureIgnoreCase).ToList();
                int matchingMx = matchingMxList.Count;

                List<string> wildcards = mxFromPolicy
                    .Where(_ => _.StartsWith('*'))
                    .Select(_ => _.TrimStart('*')).ToList();

                int wildcardMatchCount = unmatchedMxList.Count(
                    dnsRecord => wildcards.Any(
                        wildcard => dnsRecord.EndsWith(wildcard, StringComparison.Ordinal) && dnsRecord.Count(_ => _ == '.') == wildcard.Count(_ => _ == '.')
                    )
                );

                matchingMx += wildcardMatchCount;

                if (matchingMx == 0)
                {
                    EmailSecAdvisoryMessage advisory = new EmailSecAdvisoryMessage(
                        NoMatchId, "mailcheck.mtasts.noMatchingMx", MessageType.error,
                        RulesResources.NoMatchingMx,
                        string.Format(RulesResources.NoMatchingMxMarkdown,
                            string.Join(", ", mxFromDns),
                            string.Join(", ", mxFromPolicy)));
                    advisoryMessages.Add(advisory);
                }

                if (matchingMx > 0 && matchingMx < mxFromDns.Count)
                {
                    EmailSecAdvisoryMessage advisory = new EmailSecAdvisoryMessage(
                        SomeMatchId, "mailcheck.mtasts.someMatchingMx", MessageType.info, RulesResources.SomeMatchingMx,
                        string.Format(RulesResources.SomeMatchingMxMarkdown,
                            string.Join(", ", mxFromDns),
                            string.Join(", ", mxFromPolicy)));
                    advisoryMessages.Add(advisory);
                }

                return advisoryMessages;
            }
            return advisoryMessages;
        }
    }
}
