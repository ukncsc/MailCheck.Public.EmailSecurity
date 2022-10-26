using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FakeItEasy;
using MailCheck.Common.Api.Authorisation.Filter;
using MailCheck.Common.Messaging.Abstractions;
using MailCheck.EmailSecurity.Api.Controllers;
using MailCheck.EmailSecurity.Api.Dao;
using MailCheck.EmailSecurity.Api.Domain;
using MailCheck.EmailSecurity.Api.Service;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;

namespace MailCheck.EmailSecurity.Api.Test.Controllers
{
    [TestFixture]
    public class EmailSecurityControllerTests
    {
        private EmailSecurityController _sut;
        private IEmailSecurityService _emailSecurityService;

        [SetUp]
        public void SetUp()
        {
            _emailSecurityService = A.Fake<IEmailSecurityService>();
            _sut = new EmailSecurityController(_emailSecurityService);
        }

        [Test]
        public async Task ItShouldReturnNotFoundWhenThereIsNoEmailSecurityState()
        {
            A.CallTo(() => _emailSecurityService.GetEmailSecurityForDomain(A<string>._))
                .Returns(Task.FromResult<EmailSecurityInfoResponse>(null));

            IActionResult response =
                await _sut.GetEmailSecurityForDomain(new EmailSecurityDomainRequest {Domain = "ncsc.gov.uk"});

            Assert.That(response, Is.TypeOf(typeof(ObjectResult)));
        }

        [Test]
        public async Task ItShouldReturnTheFirstResultWhenTheEmailSecurityStateExists()
        {
            EmailSecurityInfoResponse state = new EmailSecurityInfoResponse("ncsc.gov.uk");

            A.CallTo(() => _emailSecurityService.GetEmailSecurityForDomain(A<string>._))
                .Returns(Task.FromResult(state));

            ObjectResult response =
                (ObjectResult) await _sut.GetEmailSecurityForDomain(new EmailSecurityDomainRequest
                    {Domain = "ncsc.gov.uk"});

            Assert.AreSame(response.Value, state);
        }

        [Test]
        public void AllEndpointsHaveAuthorisation()
        {
            IEnumerable<MethodInfo> controllerMethods =
                _sut.GetType().GetMethods().Where(x => x.DeclaringType == typeof(EmailSecurityController));

            foreach (MethodInfo methodInfo in controllerMethods)
            {
                Assert.That(methodInfo.CustomAttributes.Any(x =>
                    x.AttributeType == typeof(MailCheckAuthoriseResourceAttribute) ||
                    x.AttributeType == typeof(MailCheckAuthoriseRoleAttribute)));
            }
        }
    }
}