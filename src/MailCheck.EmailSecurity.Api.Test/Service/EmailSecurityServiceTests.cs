using System.Threading.Tasks;
using FakeItEasy;
using MailCheck.Common.Contracts.Messaging;
using MailCheck.Common.Messaging.Abstractions;
using MailCheck.EmailSecurity.Api.Config;
using MailCheck.EmailSecurity.Api.Dao;
using MailCheck.EmailSecurity.Api.Domain;
using MailCheck.EmailSecurity.Api.Service;
using NUnit.Framework;

namespace MailCheck.EmailSecurity.Api.Test.Service
{
    [TestFixture]
    public class EmailSecurityServiceTests
    {
        private EmailSecurityService _emailSecurityService;
        private IMessagePublisher _messagePublisher;
        private IEmailSecurityApiDao _dao;
        private IEmailSecurityApiConfig _config;

        [SetUp]
        public void SetUp()
        {
            _messagePublisher = A.Fake<IMessagePublisher>();
            _dao = A.Fake<IEmailSecurityApiDao>();
            _config = A.Fake<IEmailSecurityApiConfig>();
            _emailSecurityService = new EmailSecurityService(_messagePublisher, _dao, _config);
        }

        [Test]
        public async Task PublishesDomainMissingMessageWhenDomainDoesNotExist()
        {
            A.CallTo(() => _dao.Read("testDomain"))
                .Returns(Task.FromResult<EmailSecurityInfoResponse>(null));

            EmailSecurityInfoResponse result = await _emailSecurityService.GetEmailSecurityForDomain("testDomain");

            A.CallTo(() => _messagePublisher.Publish(A<DomainMissing>._, A<string>._))
                .MustHaveHappenedOnceExactly();
            Assert.AreEqual(null, result);
        }

        [Test]
        public async Task DoesNotPublishDomainMissingMessageWhenDomainExists()
        {
            EmailSecurityInfoResponse emailSecurityInfoResponse = new EmailSecurityInfoResponse("");
            A.CallTo(() => _dao.Read("testDomain"))
                .Returns(Task.FromResult(emailSecurityInfoResponse));

            EmailSecurityInfoResponse result = await _emailSecurityService.GetEmailSecurityForDomain("testDomain");

            A.CallTo(() => _messagePublisher.Publish(A<DomainMissing>._, A<string>._))
                .MustNotHaveHappened();
            Assert.AreSame(emailSecurityInfoResponse, result);

        }
    }
}