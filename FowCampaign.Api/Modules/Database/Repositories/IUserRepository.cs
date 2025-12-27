namespace FowCampaign.Api.Modules.Database.Repositories;

public interface IUserRepository
{
    public Task<User.User> GetUserAsync(int id);
    public Task<User.User> GetUserAsync(string username);
    public Task<User.User> AddUserAsync(User.User user);
    public Task<bool> CheckIfExistsAsync(string username);
    
}