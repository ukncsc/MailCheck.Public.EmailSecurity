using MailCheck.Common.Environment.Abstractions;

namespace MailCheck.EmailSecurity.Api.Config
{
    public interface IEmailSecurityApiConfig
    {
        string SnsTopicArn { get; }
        string MicroserviceOutputSnsTopicArn { get; }

    }

    public class EmailSecurityApiConfig : IEmailSecurityApiConfig
    {
        public EmailSecurityApiConfig(IEnvironmentVariables environmentVariables)
        {
            SnsTopicArn = environmentVariables.Get("SnsTopicArn");
            MicroserviceOutputSnsTopicArn = environmentVariables.Get("SnsTopicArn");
        }

        public string SnsTopicArn { get; }
        public string MicroserviceOutputSnsTopicArn { get; }

    }
}
