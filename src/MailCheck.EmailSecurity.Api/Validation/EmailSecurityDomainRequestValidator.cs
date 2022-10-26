using FluentValidation;
using MailCheck.Common.Util;
using MailCheck.EmailSecurity.Api.Domain;

namespace MailCheck.EmailSecurity.Api.Validation
{
    public class EmailSecurityDomainRequestValidator : AbstractValidator<EmailSecurityDomainRequest>
    {
        public EmailSecurityDomainRequestValidator(IDomainValidator domainValidator)
        {
            CascadeMode = CascadeMode.StopOnFirstFailure;

            RuleFor(_ => _.Domain)
                .NotNull()
                .WithMessage("A \"domain\" field is required.")
                .NotEmpty()
                .WithMessage("The \"domain\" field should not be empty.")
                .Must(domainValidator.IsValidDomain)
                .WithMessage("The domains must be be a valid domain");
        }
    }
}
