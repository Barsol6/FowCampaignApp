#region

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentValidation;
using FowCampaign.Api.DTO;
using FowCampaign.Api.Modules.Account;
using FowCampaign.Api.Modules.Database.Entities.User;
using FowCampaign.Api.Modules.Database.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

#endregion

namespace FowCampaign.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController(
    IUserRepository userRepository,
    PasswordHash passwordHash,
    IConfiguration configuration,
    IValidator<RegisterDto> validator
) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto user)
    {
        var validationResult = await validator.ValidateAsync(user);

        if (!validationResult.IsValid) return BadRequest(validationResult.Errors);

        if (!ModelState.IsValid) return BadRequest(ModelState);

        if (await userRepository.CheckIfExistsAsync(user.Username))
            return Conflict(new { message = "CODENAME ALREADY TAKEN" });

        var hashedPassword = passwordHash.HashPasswords(user.Password, user.Username);

        var newUser = new User
        {
            Username = user.Username,
            Password = hashedPassword
        };

        await userRepository.AddUserAsync(newUser);

        return Ok(new { message = "Account created" });
        ;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto user)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var existingUser = await userRepository.GetUserAsync(user.Username);

        if (existingUser is null)
            return NotFound(new { message = "Account does not exist" });

        var passwordCheck = passwordHash.CheckPassword(user.Password, user.Username);

        if (passwordCheck.Result is false) return Unauthorized(new { message = "Invalid password" });

        var token = GenerateJwtToken(user.Username);

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.Now.AddDays(1)
        };

        Response.Cookies.Append("authToken", token, cookieOptions);

        return Ok(new { message = "Login successful", token });
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(-1)
        };
        Response.Cookies.Append("authToken", "", cookieOptions);
        return Ok(new { message = "Logged out" });
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var username = User.Identity?.Name;
        if (username is null) return Unauthorized();

        return Ok(new { username, isAuthenticated = true });
    }

    private string GenerateJwtToken(string username)
    {
        var securitykey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]));
        var credentials = new SigningCredentials(securitykey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, username)
        };

        var token = new JwtSecurityToken(
            configuration["Jwt:Issuer"],
            configuration["Jwt:Audience"],
            claims,
            expires: DateTime.Now.AddHours(24),
            signingCredentials: credentials
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}