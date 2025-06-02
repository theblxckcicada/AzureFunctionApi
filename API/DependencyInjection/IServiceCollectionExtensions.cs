using AnimalKingdom.API.DependencyInjection;
using DMIX.API.Auth;
using DMIX.API.Azure.Services.BlobStorage;
using DMIX.API.Azure.Services.ConfigurationManager;
using DMIX.API.Azure.Services.GraphApi;
using DMIX.API.Azure.Services.ServiceBus;
using DMIX.API.Azure.Services.TableStorage;
using DMIX.API.Common.Models;
using DMIX.API.Handlers;
using DMIX.API.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace DMIX.API.DependencyInjection
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddApiServices(this IServiceCollection services)
        {
            // Core services
            services.AddSingleton(TimeProvider.System);
            services.AddScoped<JsonWebTokenHandler>();
            services.AddScoped<IHeaderHandler, HeaderHandler>();

            // Azure/External Services
            services.AddGraphApiService()
                    .AddBlobService()
                    .AddStorageService()
                    .AddASBService();

            // Auth and Config
            services.AddOpenIdConnectConfiguration()
                    .AddAuthorizer();

            // Replace raw HttpClient registration with factory
            services.AddHttpClient();

            // App-specific services
            services.AddScoped<IConfigurationManagerService, ConfigurationManagerService>();

            // MediatR (make sure the right assemblies are used)
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());

            // Register entity handlers (if not already done in AddStorageService)
            services.AddEntityHandlers();
            // services.AddStorageEntityHandlers(); // Consider removing if redundant

            return services;
        }



        public static IServiceCollection AddBlobService(this IServiceCollection services)
        {
            _ = services
                .AddOptions<BlobServiceClientSettings>()
                .Configure<IConfiguration>(
                    static (settings, configuration) =>
                    {
                        configuration.Bind(settings);
                    }
                );

            _ = services.AddScoped(static services =>
                new BlobServiceClientFactory(
                    services.GetRequiredService<IOptions<BlobServiceClientSettings>>()
                ).Build()
            );

            return services.AddScoped<IAzureBlobStorageService, AzureBlobStorageService>();
        }


        public static IServiceCollection AddGraphApiService(this IServiceCollection services)
        {
            _ = services
                .AddOptions<GraphApiServiceClientSettings>()
                .Configure<IConfiguration>(
                    static (settings, configuration) =>
                    {
                        configuration.Bind(settings);
                    }
                );

            _ = services.AddScoped(implementationFactory: static services =>
                new GraphApiServiceClientFactory(
                    services.GetRequiredService<IOptions<GraphApiServiceClientSettings>>()
                ).Build()
            );

            return services.AddScoped<IGraphApiService, GraphApiService>();
        }

        public static IServiceCollection AddStorageService(this IServiceCollection services)
        {
            _ = services
                .AddOptions<TableServiceClientSettings>()
                .Configure<IConfiguration>(
                    static (settings, configuration) =>
                    {
                        configuration.Bind(settings);
                    }
                );

            _ = services.AddScoped(static services =>
                new TableServiceClientFactory(
                    services.GetRequiredService<IOptions<TableServiceClientSettings>>()
                ).Build()
            );

            services
               .AddScoped(typeof(IStorageEntityHandler<,>), typeof(StorageEntityHandler<,>))
                .AddScoped(typeof(StorageEntityHandler<,>))
               .AddScoped(typeof(EntityHandler<,>))
               .AddScoped(typeof(IAzureTableStorageService<,>), typeof(AzureTableStorageService<,>))
               .AddScoped(typeof(AzureTableStorageService<,>));

            services.Add(
            new ServiceDescriptor(
                    typeof(IEntityBaseKeyGenerator<Guid>),
                typeof(GuidKeyGenerator),
                ServiceLifetime.Scoped)
            );

            return services;
        }

        public static IServiceCollection AddASBService(this IServiceCollection services)
        {
            _ = services
                .AddOptions<ASBServiceClientSettings>()
                .Configure<IConfiguration>(
                    static (settings, configuration) =>
                    {
                        configuration.Bind(settings);
                    }
                );

            _ = services.AddScoped(static services =>
                new ASBServiceClientFactory(
                    services.GetRequiredService<IOptions<ASBServiceClientSettings>>()
                ).Build()
            );

            return services.AddScoped<IASBService, ASBService>();
        }

        public static IServiceCollection AddOpenIdConnectConfiguration(
            this IServiceCollection services
        )
        {
            _ = services
                .AddOptions<ConfigurationManagerServiceClientSettings>()
                .Configure<IConfiguration>(
                    static (settings, configuration) =>
                    {
                        configuration.Bind(settings);
                    }
                );

            _ = services.AddScoped(static provider =>
            {
                var factory = new ConfigurationManagerServiceClientFactory(
                    provider.GetRequiredService<
                        IOptions<ConfigurationManagerServiceClientSettings>
                    >()
                );

                return factory.Build();
            });

            _ = services.AddScoped(
                static provider =>
                {
                    var factory = new ConfigurationManagerServiceClientFactory(
                        provider.GetRequiredService<
                            IOptions<ConfigurationManagerServiceClientSettings>
                        >()
                    );

                    var configurationManagers = provider.GetRequiredService<
                        Dictionary<string, IConfigurationManager<OpenIdConnectConfiguration>>
                    >();

                    var selectedConfigurationManager =
                        configurationManagers[nameof(AppAuthorization.Microsoft)];

                    return selectedConfigurationManager;
                }
            );

            return services;
        }

        public static IServiceCollection AddAuthorizer(this IServiceCollection services)
        {
            _ = services
                .AddOptions<AuthSettings>()
                .Configure<IConfiguration>(
                    static (settings, configuration) =>
                    {
                        configuration.Bind(settings);
                    }
                );

            return services.AddScoped<IAuthorizer, Authorizer>();
        }
    }
}
