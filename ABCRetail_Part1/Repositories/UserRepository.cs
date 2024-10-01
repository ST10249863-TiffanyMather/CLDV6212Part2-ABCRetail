using Azure.Data.Tables;
using Azure;
using ABCRetail_Part1.Models;
using System.Threading.Tasks;

namespace ABCRetail_Part1.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly TableClient _tableClient;
        private const string tableName = "Users"; 

        //constructor initializes the TableClient 
        public UserRepository(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("AzureStorage"); 
            _tableClient = new TableClient(connectionString, tableName); 
            _tableClient.CreateIfNotExists(); 
        }

        //create a new user in Azure Table Storage
        public async Task<bool> CreateUserAsync(User user)
        {
            try
            {
                //add the user entity to the table
                await _tableClient.AddEntityAsync(user);
                return true; 
            }
            catch (RequestFailedException)
            {
                return false;
            }
        }
    }
}
