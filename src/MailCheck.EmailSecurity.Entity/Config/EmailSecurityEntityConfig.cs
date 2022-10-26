using MailCheck.Common.Environment.Abstractions;

namespace MailCheck.EmailSecurity.Entity.Config
{
    public interface IEmailSecurityEntityConfig
    {
        string SnsTopicArn { get; }
        string WebUrl { get; }
    }

    public class EmailSecurityEntityConfig : IEmailSecurityEntityConfig
    {
        public EmailSecurityEntityConfig(IEnvironmentVariables environmentVariables)
        {
            SnsTopicArn = environmentVariables.Get("SnsTopicArn");
            WebUrl = environmentVariables.Get("WebUrl");
        }

        public string SnsTopicArn { get; }
        public string WebUrl { get; }
    }
}
