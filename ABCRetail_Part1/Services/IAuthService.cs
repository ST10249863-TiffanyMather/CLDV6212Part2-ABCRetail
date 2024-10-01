using ABCRetail_Part1.Models;
using System.Threading.Tasks;

namespace ABCRetail_Part1.Services
{
    public interface IAuthService
    {
        Task<bool> RegisterAsync(string email, string password, string fullName);
        Task<User> LoginAsync(string email, string password);
    }
}
