using ABCRetail_Part1.Models;
using ABCRetail_Part1.Repositories;
using System.Linq;
using System.Threading.Tasks;
using BCrypt.Net;

namespace ABCRetail_Part1.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly TableStorageService _tableStorageService;

        public AuthService(IUserRepository userRepository, TableStorageService tableStorageService)
        {
            _userRepository = userRepository;
            _tableStorageService = tableStorageService;
        }

        //method to register a new user
        public async Task<bool> RegisterAsync(string email, string password, string userName)
        {
            string partitionKey = "UsersPartition";

            //check if the user already exists by email
            var allUsers = await _tableStorageService.GetAllUsersAsync();
            var existingUser = allUsers.FirstOrDefault(u => u.Email == email);
            if (existingUser != null)
                return false;

            //hash the password before storing it
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

            //generate new UserId
            int maxUserId = allUsers.Any() ? allUsers.Max(u => u.UserId) : 0;

            //ceate a new user 
            var user = new User
            {
                UserId = maxUserId + 1,
                RowKey = Guid.NewGuid().ToString(),
                PartitionKey = partitionKey,
                Email = email,
                UserName = userName,
                Password = passwordHash
            };

            //save the user to the repository
            return await _userRepository.CreateUserAsync(user);
        }

        //method to login an existing user
        public async Task<User> LoginAsync(string email, string password)
        {
            //fetch all users from table storage and get email
            var allUsers = await _tableStorageService.GetAllUsersAsync();
            var user = allUsers.FirstOrDefault(u => u.Email == email);

            if (user == null)
                return null;

            //verify the password
            bool isValid = BCrypt.Net.BCrypt.Verify(password, user.Password);
            return isValid ? user : null;
        }
    }
}
