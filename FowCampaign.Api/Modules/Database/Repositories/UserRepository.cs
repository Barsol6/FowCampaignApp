using Microsoft.EntityFrameworkCore;

namespace FowCampaign.Api.Modules.Database.Repositories;

public class UserRepository:IUserRepository
{
    private readonly FowCampaignContext _dbContext;
    
    public UserRepository(FowCampaignContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<User.User> AddUserAsync(User.User user)
    {
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();
        return user;
    }
    
    public async Task<bool> CheckIfExistsAsync(string username)
    {
        var exists = _dbContext
            .Users.Any(x => x.Username.ToLower() == username);
        return exists;
    }
    public async Task<User.User> GetUserAsync(int id)
    {
        var user = await _dbContext.Users.FindAsync(id);
        return user;
    } 
    
    public async Task<User.User> GetUserAsync(string username)
    {
        var user = await _dbContext
            .Users.Where(u => u.Username == username)
            .FirstOrDefaultAsync();
        return user;
    }
}