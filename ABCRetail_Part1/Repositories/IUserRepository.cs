using System.Threading.Tasks;
using ABCRetail_Part1.Models;

namespace ABCRetail_Part1.Repositories
{
    public interface IUserRepository
    {
        Task<bool> CreateUserAsync(User user);
    }
}
