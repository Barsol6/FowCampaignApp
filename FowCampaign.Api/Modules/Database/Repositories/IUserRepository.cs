using FowCampaign.Api.Modules.Database.Entities.User;

namespace FowCampaign.Api.Modules.Database.Repositories;

public interface IUserRepository
{
    public Task<User> GetUserAsync(int id);
    public Task<User> GetUserAsync(string username);
    public Task<User> AddUserAsync(User user);
    public Task<bool> CheckIfExistsAsync(string username);
    
}