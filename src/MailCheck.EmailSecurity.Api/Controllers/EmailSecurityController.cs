using System.Threading.Tasks;
using MailCheck.Common.Api.Authorisation.Filter;
using MailCheck.Common.Api.Authorisation.Service.Domain;
using MailCheck.Common.Api.Domain;
using MailCheck.EmailSecurity.Api.Domain;
using MailCheck.EmailSecurity.Api.Service;
using Microsoft.AspNetCore.Mvc;

namespace MailCheck.EmailSecurity.Api.Controllers
{
    [Route("/api/emailsecurity")]
    public class EmailSecurityController : Controller
    {
        private readonly IEmailSecurityService _emailSecurityService;
        
        public EmailSecurityController(IEmailSecurityService emailSecurityService)
        {
            _emailSecurityService = emailSecurityService;
        }
        
        [HttpGet("{domain}/advisories")]
        [MailCheckAuthoriseRole(Role.Standard)]
        public async Task<IActionResult> GetEmailSecurityForDomain(EmailSecurityDomainRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ErrorResponse(ModelState.Values));
            }

            EmailSecurityInfoResponse infoResponse = await _emailSecurityService.GetEmailSecurityForDomain(request.Domain);

            if (infoResponse == null)
            {
                return new ObjectResult(new ErrorResponse($"No Email Security found for {request.Domain}",
                    ErrorStatus.Information));
            }

            return new ObjectResult(infoResponse);
        }
    }
}