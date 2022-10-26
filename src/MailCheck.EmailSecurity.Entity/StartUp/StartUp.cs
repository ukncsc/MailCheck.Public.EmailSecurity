using System.Collections;
using System.Collections.Generic;
using Amazon.SimpleNotificationService;
using Amazon.SimpleSystemsManagement;
using MailCheck.Common.Contracts.Advisories;
using MailCheck.Common.Data;
using MailCheck.Common.Data.Abstractions;
using MailCheck.Common.Processors.Notifiers;
using MailCheck.Common.Data.Implementations;
using MailCheck.Common.Environment.Abstractions;
using MailCheck.Common.Environment.Implementations;
using MailCheck.Common.Messaging.Abstractions;
using MailCheck.Common.Processors.Evaluators;
using MailCheck.Common.SSM;
using MailCheck.Common.Util;
using MailCheck.EmailSecurity.Entity.Config;
using MailCheck.EmailSecurity.Entity.Dao;
using MailCheck.EmailSecurity.Entity.Domain;
using MailCheck.EmailSecurity.Entity.Entity;
using MailCheck.EmailSecurity.Entity.Entity.DomainStatus;
using MailCheck.EmailSecurity.Entity.Entity.EvaluationRules;
using MailCheck.EmailSecurity.Entity.Entity.Notifiers;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using FindingsChangedNotifier = MailCheck.Common.Processors.Notifiers.FindingsChangedNotifier;
using LocalFindingsChangedNotifier = MailCheck.EmailSecurity.Entity.Entity.Notifiers.FindingsChangedNotifier;
using MessageEqualityComparer = MailCheck.EmailSecurity.Entity.Entity.Notifiers.MessageEqualityComparer;

namespace MailCheck.EmailSecurity.Entity.StartUp
{
    public class StartUp : IStartUp
    {
        public void ConfigureServices(IServiceCollection services)
        {
            JsonConvert.DefaultSettings = () => SerialisationConfig.Settings;

            services
                .AddSingleton<IClock, Clock>()
                .AddSingleton<IConnectionInfoAsync, MySqlEnvironmentParameterStoreConnectionInfoAsync>()
                .AddSingleton<IDatabase, DefaultDatabase<MySqlProvider>>()
                .AddSingleton<IEnvironment, EnvironmentWrapper>()
                .AddSingleton<IEnvironmentVariables, EnvironmentVariables>()
                .AddTransient<IAmazonSimpleSystemsManagement, CachingAmazonSimpleSystemsManagementClient>()
                .AddTransient<IAmazonSimpleNotificationService, AmazonSimpleNotificationServiceClient>()
                .AddTransient<IEmailSecurityEntityConfig, EmailSecurityEntityConfig>()
                .AddTransient<IEmailSecurityEntityDao, EmailSecurityEntityDao>()
                .AddTransient<IDomainStatusPublisher, DomainStatusPublisher>()
                .AddTransient<IDomainStatusEvaluator, DomainStatusEvaluator>()
                .AddTransient<IEvaluator<EmailSecurityEntityState>, Evaluator<EmailSecurityEntityState>>()
                .AddTransient<IRule<EmailSecurityEntityState>, NoMxAndNoMtaSts>()
                .AddTransient<IRule<EmailSecurityEntityState>, MxListsMatch>()
                .AddTransient<IRule<EmailSecurityEntityState>, TlsConfiguration>()
                .AddTransient<IDocumentDbConfig, DocumentDbConfig>()
                .AddTransient<IMongoClientProvider, MongoClientProvider>()
                .AddTransient<IChangeNotifier, AdvisoryChangedNotifier>()
                .AddTransient<IChangeNotifier, LocalFindingsChangedNotifier>()
                .AddTransient<IChangeNotifiersComposite, ChangeNotifiersComposite>()
                .AddTransient<IFindingsChangedNotifier, FindingsChangedNotifier>()
                .AddTransient<IEqualityComparer<AdvisoryMessage>, MessageEqualityComparer>()
                .AddTransient<EmailSecurityEntity>();
        }
    }
}