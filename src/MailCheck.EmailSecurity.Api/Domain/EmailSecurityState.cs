using System;
using System.Collections.Generic;
using System.Text;

namespace MailCheck.EmailSecurity.Api.Domain
{
    public enum EmailSecurityState
    {
        Created,
        EvaluationPending,
        Evaluated
    }
}
