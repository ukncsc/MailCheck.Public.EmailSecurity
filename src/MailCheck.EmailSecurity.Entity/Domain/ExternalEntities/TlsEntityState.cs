using System;
using System.Collections.Generic;
using MailCheck.Common.Contracts.Advisories;

namespace MailCheck.EmailSecurity.Entity.Domain.ExternalEntities
{
    public class TlsEntityState
    {
        public string Id { get; }
        
        public object TlsState { get; set; }

        public DateTime Created { get; }

        public CertificateResults CertificateResults { get; set; }

        public TlsRecords TlsRecords { get; set; }
        
        public int FailureCount { get; set; }
        
        public DateTime? LastUpdated { get; set; }
    }

    public class SimplifiedEmailSecTlsEntityState
    {
        public string Hostname { get; set; }

        public List<AdvisoryMessage> CertAdvisories { get; set; }

        public DateTime CertsLastUpdated { get; set; }

        public List<AdvisoryMessage> TlsAdvisories { get; set; }

        public DateTime TlsLastUpdated { get; set; }
    }

    public class TlsRecords
    {
        public TlsRecord Tls12AvailableWithBestCipherSuiteSelected { get; }
        public TlsRecord Tls12AvailableWithBestCipherSuiteSelectedFromReverseList { get; }
        public TlsRecord Tls12AvailableWithSha2HashFunctionSelected { get; }
        public TlsRecord Tls12AvailableWithWeakCipherSuiteNotSelected { get; }
        public TlsRecord Tls11AvailableWithBestCipherSuiteSelected { get; }
        public TlsRecord Tls11AvailableWithWeakCipherSuiteNotSelected { get; }
        public TlsRecord Tls10AvailableWithBestCipherSuiteSelected { get; }
        public TlsRecord Tls10AvailableWithWeakCipherSuiteNotSelected { get; }
        public TlsRecord Ssl3FailsWithBadCipherSuite { get; }
        public TlsRecord TlsSecureEllipticCurveSelected { get; }
        public TlsRecord TlsSecureDiffieHellmanGroupSelected { get; }
        public TlsRecord TlsWeakCipherSuitesRejected { get; }
        public TlsRecord Tls12Available { get; set; }
        public TlsRecord Tls11Available { get; }
        public TlsRecord Tls10Available { get; }
        public List<TlsRecord> Records { get; set; }
    }

    public class TlsRecord
    {
        public TlsEvaluatedResult TlsEvaluatedResult { get; set; }
        public object BouncyCastleTlsTestResult { get; set; }
    }

    public class TlsEvaluatedResult
    {
        public string Description { get; }

        public EvaluatorResult? Result { get; set; }
    }

    public enum EvaluatorResult
    {
        UNKNOWN = -1,
        PASS = 0,
        PENDING = 1,
        INCONCLUSIVE = 2,
        WARNING = 3,
        FAIL = 4,
        INFORMATIONAL = 5
    }

    public class CertificateResults
    {
        public List<Certificate> Certificates { get; set; }

        public List<Error> Errors { get; set; }
    }

    public class Error
    {
        public object ErrorType { get; set; }

        public string Message { get; set; }
    }

    public class Certificate
    {
        public string ThumbPrint { get; }
        public string Issuer { get; }
        public string Subject { get; }
        public DateTime ValidFrom { get; }
        public DateTime ValidTo { get; }
        public string KeyAlgoritm { get; }
        public int KeyLength { get; }
        public string SerialNumber { get; }
        public string Version { get; }
        public string SubjectAlternativeName { get; }
        public string CommonName { get; }
    }
}