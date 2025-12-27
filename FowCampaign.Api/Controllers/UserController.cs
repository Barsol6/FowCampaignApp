using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FowCampaign.Api.DTO;
using FowCampaign.Api.Modules.Account;
using FowCampaign.Api.Modules.Database.Repositories;
using FowCampaign.Api.Modules.User;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;


namespace Mixi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly PasswordHash _passwordHash;
    private readonly IUserRepository _userRepository;


    public UserController(IUserRepository userRepository, PasswordHash passwordHash, IConfiguration configuration)
    {
        _userRepository = userRepository;
        _passwordHash = passwordHash;
        _configuration = configuration;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto user)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        if (await _userRepository.CheckIfExistsAsync(user.Username))
        {
            return Conflict(new { message = "Account already exists" });
        }

        var hashedPassword = _passwordHash.HashPasswords(user.Password, user.Username);

        var newUser = new User
        {
            Username = user.Username,
            Password = hashedPassword,
            Role= user.Role,
            Color = user.Role switch
            {
                "Axis" => "Black",
                "Ally" => "White",
                _ => "Grey"
            }
            
        };

        await _userRepository.AddUserAsync(newUser);

        return Ok(new { message = "Account created" });
        ;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto user)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var existingUser = await _userRepository.GetUserAsync(user.Username);
        
        if (existingUser is null)
            return NotFound(new { message = "Account does not exist" });

        var passwordCheck = _passwordHash.CheckPassword(user.Password, user.Username);

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
        
        Response.Cookies.Delete("authToken");
        return Ok(new { message = "Logged out" });
    }
    
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var username = User.Identity?.Name;
        if (username is null) return Unauthorized();
        
        return Ok(new { username = username, isAuthenticated = true});
    }
    
    private string GenerateJwtToken(string username)
    {
        var securitykey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
        var credentials = new SigningCredentials(securitykey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, username)
        };

        var token = new JwtSecurityToken(
            _configuration["Jwt:Issuer"],
            _configuration["Jwt:Audience"],
            claims,
            expires: DateTime.Now.AddHours(24),
            signingCredentials: credentials
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}