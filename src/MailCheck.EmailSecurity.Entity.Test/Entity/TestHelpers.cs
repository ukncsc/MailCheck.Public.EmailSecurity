using System;
using System.Collections.Generic;
using Amazon.Runtime.Internal.Transform;
using MailCheck.Common.Contracts.Advisories;
using MailCheck.EmailSecurity.Entity.Domain;
using MailCheck.EmailSecurity.Entity.Domain.ExternalEntities;
using MailCheck.EmailSecurity.Entity.Entity;
using Newtonsoft.Json.Linq;

namespace MailCheck.EmailSecurity.Entity.Test.Entity
{
    public static class TestHelpers
    {
        private static readonly CertificateResults NoCertErrors = new CertificateResults { Errors = new List<Error>() };
        private static readonly TlsEvaluatedResult Tls12FailResult = new TlsEvaluatedResult { Result = EvaluatorResult.FAIL };
        private static readonly CertificateResults WithCertErrors = new CertificateResults { Errors = new List<Error> { new Error{ Message = "testcerterror" } } };
        private static readonly TlsEvaluatedResult Tls12PassResult = new TlsEvaluatedResult { Result = EvaluatorResult.PASS };

        private static readonly List<AdvisoryMessage> SimplifiedValidCertAdvisories = new List<AdvisoryMessage>{ new AdvisoryMessage(new Guid(), MessageType.warning, "cert advisory", "cert advisory") };
        private static readonly List<AdvisoryMessage> SimplifiedNoCertAdvisories = new List<AdvisoryMessage>();
        private static readonly List<AdvisoryMessage> SimplifiedValidTlsResult = new List<AdvisoryMessage> { new AdvisoryMessage(new Guid(), MessageType.success, "", "") };
        private static readonly List<AdvisoryMessage> SimplifiedErrorCertAdvisories = new List<AdvisoryMessage> { new AdvisoryMessage(new Guid(), MessageType.error, "cert error", "cert error") };
        private static readonly List<AdvisoryMessage> SimplifiedInvalidTlsResult = new List<AdvisoryMessage> { new AdvisoryMessage(new Guid(), MessageType.error, "", ""), new AdvisoryMessage(new Guid(), MessageType.error, "", "") };

        public static readonly AdvisoryMessage MtaStsAdvisory = new AdvisoryMessage(Guid.NewGuid(), MessageType.warning, "testadvisory", "testmarkdown");
        public static EmailSecurityEntityState GetNoIssuesHostState(string id = "testhostgood.test.com.")
        {
            TlsRecords tls12Pass = new TlsRecords
            {
                Tls12Available = new TlsRecord {TlsEvaluatedResult = Tls12PassResult}
            };

            TlsEntityState noErrorState = new TlsEntityState 
            { 
                CertificateResults = NoCertErrors,
                TlsRecords = tls12Pass
            };
            
            ExternalEntity noErrorEntity = new ExternalEntity { EntityDetail = noErrorState };
            return JObject.FromObject(new
            {
                Domain = id,
                Entities = new Dictionary<string, ExternalEntity> {new KeyValuePair<string, ExternalEntity>("TLS", noErrorEntity)}
            }).ToObject<EmailSecurityEntityState>();
        }

        public static EmailSecurityEntityState GetCertIssuesHostState(string id = "testhostcerterror.test.com.")
        {
            TlsRecords tls12Pass = new TlsRecords
            {
                Tls12Available = new TlsRecord {TlsEvaluatedResult = Tls12PassResult}
            };

            TlsEntityState certErrorState = new TlsEntityState 
            {
                CertificateResults = WithCertErrors,
                TlsRecords = tls12Pass
            };
            ExternalEntity certErrorEntity = new ExternalEntity { EntityDetail = certErrorState };
            
            return JObject.FromObject(new
            {
                Domain = id,
                Entities = new Dictionary<string, ExternalEntity> {new KeyValuePair<string, ExternalEntity>("TLS", certErrorEntity)}
            }).ToObject<EmailSecurityEntityState>();
        }

        public static EmailSecurityEntityState GetTlsIssuesHostState(string id = "testhosttlserror.test.com.")
        {
            TlsRecords tls12Fail = new TlsRecords
            {
                Tls12Available = new TlsRecord {TlsEvaluatedResult = Tls12FailResult}
            };

            TlsEntityState tlsErrorState = new TlsEntityState
            {
                CertificateResults = NoCertErrors,
                TlsRecords = tls12Fail
            };
            
            ExternalEntity tlsErrorEntity = new ExternalEntity { EntityDetail = tlsErrorState };
            
            return JObject.FromObject(new
            {
                Domain = id,
                Entities = new Dictionary<string, ExternalEntity> {new KeyValuePair<string, ExternalEntity>("TLS", tlsErrorEntity)}
            }).ToObject<EmailSecurityEntityState>();
        }

        public static EmailSecurityEntityState GetNoIssuesSimplifiedHostState(string id = "testhostgood.test.com.")
        {
            SimplifiedEmailSecTlsEntityState noErrorState = new SimplifiedEmailSecTlsEntityState
            {
                CertAdvisories = SimplifiedValidCertAdvisories,
                TlsAdvisories = SimplifiedValidTlsResult
            };

            ExternalEntity noErrorEntity = new ExternalEntity { EntityDetail = noErrorState };
            return JObject.FromObject(new
            {
                Domain = id,
                Entities = new Dictionary<string, ExternalEntity> { new KeyValuePair<string, ExternalEntity>("SIMPLIFIEDTLS", noErrorEntity) }
            }).ToObject<EmailSecurityEntityState>();
        }

        public static EmailSecurityEntityState GetNoCertAdvisoriesSimplifiedHostState(string id = "testhostgood.test.com.")
        {
            SimplifiedEmailSecTlsEntityState noErrorState = new SimplifiedEmailSecTlsEntityState
            {
                CertAdvisories = SimplifiedNoCertAdvisories,
                TlsAdvisories = SimplifiedValidTlsResult
            };

            ExternalEntity noErrorEntity = new ExternalEntity { EntityDetail = noErrorState };
            return JObject.FromObject(new
            {
                Domain = id,
                Entities = new Dictionary<string, ExternalEntity> { new KeyValuePair<string, ExternalEntity>("SIMPLIFIEDTLS", noErrorEntity) }
            }).ToObject<EmailSecurityEntityState>();
        }

        public static EmailSecurityEntityState GetCertIssuesSimplifiedHostState(string id = "testhostcerterror.test.com.")
        {
            SimplifiedEmailSecTlsEntityState certErrorState = new SimplifiedEmailSecTlsEntityState
            {
                CertAdvisories = SimplifiedErrorCertAdvisories,
                TlsAdvisories = SimplifiedValidTlsResult
            };

            ExternalEntity certErrorEntity = new ExternalEntity { EntityDetail = certErrorState };

            return JObject.FromObject(new
            {
                Domain = id,
                Entities = new Dictionary<string, ExternalEntity> { new KeyValuePair<string, ExternalEntity>("SIMPLIFIEDTLS", certErrorEntity) }
            }).ToObject<EmailSecurityEntityState>();
        }

        public static EmailSecurityEntityState GetTlsIssuesSimplifiedHostState(string id = "testhosttlserror.test.com.")
        {
            SimplifiedEmailSecTlsEntityState tlsErrorState = new SimplifiedEmailSecTlsEntityState
            {
                CertAdvisories = SimplifiedValidCertAdvisories,
                TlsAdvisories = SimplifiedInvalidTlsResult
            };

            ExternalEntity tlsErrorEntity = new ExternalEntity { EntityDetail = tlsErrorState };

            return JObject.FromObject(new
            {
                Domain = id,
                Entities = new Dictionary<string, ExternalEntity> { new KeyValuePair<string, ExternalEntity>("SIMPLIFIEDTLS", tlsErrorEntity) }
            }).ToObject<EmailSecurityEntityState>();
        }
    }
}