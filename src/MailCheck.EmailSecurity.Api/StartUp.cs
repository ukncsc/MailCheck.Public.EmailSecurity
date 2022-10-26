using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService;
using Amazon.SimpleSystemsManagement;
using FluentValidation;
using FluentValidation.AspNetCore;
using MailCheck.Common.Api.Authentication;
using MailCheck.Common.Api.Authorisation.Service;
using MailCheck.Common.Api.Middleware;
using MailCheck.Common.Api.Middleware.Audit;
using MailCheck.Common.Data.Abstractions;
using MailCheck.Common.Data.Implementations;
using MailCheck.Common.Logging;
using MailCheck.Common.Messaging.Abstractions;
using MailCheck.Common.Messaging.Sns;
using MailCheck.Common.SSM;
using MailCheck.Common.Util;
using MailCheck.EmailSecurity.Api.Config;
using MailCheck.EmailSecurity.Api.Dao;
using MailCheck.EmailSecurity.Api.Domain;
using MailCheck.EmailSecurity.Api.Service;
using MailCheck.EmailSecurity.Api.Validation;
using MailCheck.EmailSecurity.Entity.Config;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.HealthChecks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace MailCheck.EmailSecurity.Api
{
    public class StartUp
    {
        public StartUp(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                //Converters = new List<JsonConverter> {new StringEnumConverter(), new TagConverter(), new KeyConverter()}
            };

            if (RunInDevMode())
            {
                services.AddCors(CorsOptions);
            }

            services
                .AddHealthChecks(checks =>
                    checks.AddValueTaskCheck("HTTP Endpoint", () =>
                        new ValueTask<IHealthCheckResult>(HealthCheckResult.Healthy("Ok"))))
                .AddTransient<IEmailSecurityApiDao, EmailSecurityApiDao>()
                .AddTransient<IConnectionInfoAsync, MySqlEnvironmentParameterStoreConnectionInfoAsync>()
                .AddSingleton<IAmazonSimpleSystemsManagement, CachingAmazonSimpleSystemsManagementClient>()
                .AddTransient<IValidator<EmailSecurityDomainRequest>, EmailSecurityDomainRequestValidator>()
                .AddTransient<IDomainValidator, DomainValidator>()
                .AddTransient<IMessagePublisher, SnsMessagePublisher>()
                .AddTransient<IEmailSecurityApiConfig, EmailSecurityApiConfig>()
                .AddTransient<IDocumentDbConfig, DocumentDbConfig>()
                .AddTransient<IMongoClientProvider, MongoClientProvider>()
                .AddTransient<IAmazonSimpleNotificationService, AmazonSimpleNotificationServiceClient>()
                .AddTransient<IEmailSecurityService, EmailSecurityService>()
                .AddAudit("Email-Security-Api")
                .AddMailCheckAuthenticationClaimsPrincipleClient()
                .AddSerilogLogging()
                .AddControllers(config =>
                {
                    AuthorizationPolicy policy = new AuthorizationPolicyBuilder()
                        .RequireAuthenticatedUser()
                        .Build();
                    config.Filters.Add(new AuthorizeFilter(policy));
                }).SetCompatibilityVersion(Microsoft.AspNetCore.Mvc.CompatibilityVersion.Version_3_0)
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Serialize;
                    options.SerializerSettings.Converters.Add(new StringEnumConverter());
                })
                .AddFluentValidation();

            services
                .AddAuthorization()
                .AddAuthentication(AuthenticationSchemes.Claims)
                .AddMailCheckClaimsAuthentication();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (RunInDevMode())
            {
                app.UseCors(CorsPolicyName);
            }

            app.UseMiddleware<AuditTimerMiddleware>()
                .UseMiddleware<OidcHeadersToClaimsMiddleware>()
                .UseMiddleware<ApiKeyToClaimsMiddleware>()
                .UseAuthentication()
                .UseMiddleware<AuditLoggingMiddleware>()
                .UseMiddleware<UnhandledExceptionMiddleware>()
                .UseRouting()
                .UseEndpoints(endpoints => {
                    endpoints.MapDefaultControllerRoute();
                    endpoints.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
                });
        }

        private bool RunInDevMode()
        {
            bool.TryParse(Environment.GetEnvironmentVariable("DevMode"), out bool isDevMode);
            return isDevMode;
        }

        private static Action<CorsOptions> CorsOptions => options =>
        {
            options.AddPolicy(CorsPolicyName, builder =>
                builder
                    .SetIsOriginAllowed(_ => true)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials());
        };

        private const string CorsPolicyName = "CorsPolicy";
    }
}