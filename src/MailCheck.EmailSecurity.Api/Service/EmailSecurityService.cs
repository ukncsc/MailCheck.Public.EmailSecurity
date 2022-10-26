using System.Threading.Tasks;
using MailCheck.Common.Contracts.Messaging;
using MailCheck.Common.Messaging.Abstractions;
using MailCheck.EmailSecurity.Api.Dao;
using MailCheck.EmailSecurity.Api.Domain;
using MailCheck.EmailSecurity.Api.Config;

namespace MailCheck.EmailSecurity.Api.Service
{
    public interface IEmailSecurityService
    {
        Task<EmailSecurityInfoResponse> GetEmailSecurityForDomain(string requestDomain);
    }

    public class EmailSecurityService : IEmailSecurityService
    {
        private readonly IEmailSecurityApiDao _dao;
        private readonly IMessagePublisher _messagePublisher;
        private readonly IEmailSecurityApiConfig _config;

        public EmailSecurityService(IMessagePublisher messagePublisher, IEmailSecurityApiDao dao, IEmailSecurityApiConfig config)
        {
            _messagePublisher = messagePublisher;
            _dao = dao;
            _config = config;
        }

        public async Task<EmailSecurityInfoResponse> GetEmailSecurityForDomain(string requestDomain)
        {
            EmailSecurityInfoResponse infoResponse = await _dao.Read(requestDomain);

            if (infoResponse is null)
            {
                await _messagePublisher.Publish(new DomainMissing(requestDomain), _config.MicroserviceOutputSnsTopicArn);
            }

            return infoResponse;
        }
    }
}
