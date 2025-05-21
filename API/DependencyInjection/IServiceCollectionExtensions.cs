using EasySMS.API.Auth;
using EasySMS.API.Azure.Services.BlobStorage;
using EasySMS.API.Azure.Services.ConfigurationManager;
using EasySMS.API.Azure.Services.GraphApi;
using EasySMS.API.Azure.Services.ServiceBus;
using EasySMS.API.Azure.Services.TableStorage;
using EasySMS.API.Common.Models;
using EasySMS.API.Functions.RequestValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace EasySMS.API.DependencyInjection
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddApiServices(this IServiceCollection services)
        {
            _ = services
                .AddSingleton(TimeProvider.System)
                .AddScoped<JsonWebTokenHandler>()
                .AddScoped<IHeaderHandler, HeaderHandler>()
                .AddGraphApiService()
                .AddBlobService()
                .AddTableService()
                .AddASBService()
                .AddOpenIdConnectConfiguration()
                .AddAuthorizer()
 
                .AddSingleton<HttpClient>()
          
                .AddScoped<IConfigurationManagerService, ConfigurationManagerService>();

            _ = services
                .AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>())
 
                .AddSingleton<IEntityTypeProvider>(
                    new EntityTypeProvider(
                        EntityTypeProvider.GetEntityTypesInAssemblyContaining<Program>()
                    )
                );

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

        public static IServiceCollection AddTableService(this IServiceCollection services)
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

            return services.AddScoped<IAzureTableStorageService, AzureTableStorageService>();
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
