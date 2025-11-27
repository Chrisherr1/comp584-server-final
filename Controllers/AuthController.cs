using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Comp584ServerFinal.Data;
using Comp584ServerFinal.Data.Models;
using Comp584ServerFinal.DTO;
using Microsoft.AspNetCore.Authorization;

namespace Comp584ServerFinal.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly Comp584DbContext _db;
        private readonly IConfiguration _config;

        public AuthController(Comp584DbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public IActionResult Login(LoginRequest request)
        {
            // ✅ Look up user by Email instead of Username
            var user = _db.Users.SingleOrDefault(u => u.Email == request.Email);
            if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
                return Unauthorized();

            var claims = new[]
            {
                // ✅ Use Email as the identity claim
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var jwtKey = _config["Jwt:Key"];
            if (string.IsNullOrEmpty(jwtKey))
                throw new InvalidOperationException("JWT Key is missing from configuration.");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds);

            return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public IActionResult Register(RegisterRequest request)
        {
            if (_db.Users.Any(u => u.Email == request.Email))
                return BadRequest("Email already taken.");

            // Hash password using BCrypt
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var newUser = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = passwordHash,
                Role = "User"
            };

            _db.Users.Add(newUser);
            _db.SaveChanges();

            return Ok(new { message = "User registered successfully." });
        }

        private bool VerifyPassword(string password, string storedHash)
        {
            return BCrypt.Net.BCrypt.Verify(password, storedHash);
        }
    }
}
