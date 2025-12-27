using FowCampaign.Api.Modules.Database.Repositories;
using Microsoft.AspNetCore.Components;
namespace FowCampaign.Api.Modules.Account;

public class SignUp
{
    
    public SignUp(PasswordHash passwordHash, IUserRepository userRepository)
    {
        PasswordHash = passwordHash;
        UserRepository = userRepository;
    }
    
    [Inject] private IUserRepository UserRepository { get; set; }
    [Inject] private PasswordHash PasswordHash { get; set; }





    public async Task<SignUpResult> CreateAccount(string username, string password, string repeatpassword, string role)
    {
        if (password != repeatpassword)
        {
            return SignUpResult.PasswordsDontMatch;
        }
        var hashedPassword = PasswordHash.HashPasswords(password, username);

        var exists = await UserRepository.CheckIfExistsAsync(username);
        if (exists is true)
        {
            return SignUpResult.AccountExists;
        }

      


        var user = new User.User()
        {
            Username = username, 
            Password = password, 
            Role = role
        };
     
        await UserRepository.AddUserAsync(user);
        return SignUpResult.Success;
    }

    public enum SignUpResult
    {
        Success, 
        AccountExists,
        PasswordsDontMatch
    }


}