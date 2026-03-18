using AuthApp.Interface;
using AuthApp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AuthApp.Services
{
    public class AuthService:IAuthService
    {
        private readonly AuthAppContext authAppContext;
        private readonly IConfiguration _config;

        public AuthService(AuthAppContext authAppContext , IConfiguration config)
        {
            this.authAppContext = authAppContext;
            this._config = config;
        }

        public async Task<string?> LoginAsync(LoginDto model)
        {
            var user = await authAppContext.Users
                .FirstOrDefaultAsync(x => x.Email == model.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.Password))
                return null;

            return GenerateJwtToken(user);
        }

        public async Task<string> RegisterAsync(RegisterDto model)
        {
            var userExists = authAppContext.Users.Any(x => x.Email == model.Email);
            if (userExists)
                return "User already exists";

            var user = new User
            {
                Name = model.Name,
                Email = model.Email,
                Password = BCrypt.Net.BCrypt.HashPassword(model.Password)
            };

            authAppContext.Users.Add(user);
            await authAppContext.SaveChangesAsync();

            return "User registered successfully";
        }
        private string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Role, user.Role)
        };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Jwt:Key"])
            );

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(2),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
