using EasySMS.API.Azure.Models;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace EasySMS.API.Azure.Services.GraphApi
{
    public interface IGraphApiService
    {
        Task<User?> GetB2CUserAsync(Account account);
    }

    public class GraphApiService(GraphServiceClient graphClient) : IGraphApiService
    {
        public async Task<User?> GetB2CUserAsync(Account account)
        {
            return await graphClient
                .Users[account.UserId]
                .GetAsync(
                    (requestConfiguration) =>
                    {
                        requestConfiguration.QueryParameters.Select =
                        [
                            "GivenName",
                            "Surname",
                            "Id"
                         
                        ];
                    }
                );
        }


    }
}
