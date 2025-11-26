using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Comp584ServerFinal.Data.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Comp584ServerFinal.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly Comp584DbContext _context;

        public UsersController(Comp584DbContext context)
        {
            _context = context;
        }

        // Only Admin can grab all the users in the database 
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            return await _context.Users
                .Include(u => u.Posts)
                .Include(u => u.Comments)
                .ToListAsync();
        }

        // Users can view themselves and admin can view anyone
        [Authorize]
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            var user = await _context.Users
                .Include(u => u.Posts)
                .Include(u => u.Comments)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) return NotFound();

            var username = User.Identity?.Name;
            var role = User.FindFirstValue(ClaimTypes.Role);

            if (user.Username != username && role != "Admin")
                return Forbid();

            return user;
        }

        // Admin is the only one that can create users outright without login
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult<User>> CreateUser(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }

        // Users can update themselves but the Admin can update ANYONE
        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, User user)
        {
            if (id != user.Id) return BadRequest();

            var existingUser = await _context.Users.FindAsync(id);
            if (existingUser == null) return NotFound();

            var username = User.Identity?.Name;
            var role = User.FindFirstValue(ClaimTypes.Role);

            if (existingUser.Username != username && role != "Admin")
                return Forbid();

            existingUser.Email = user.Email;
            existingUser.Username = user.Username;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // Users can delete themselves and Admin can delete ANYONE
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            var username = User.Identity?.Name;
            var role = User.FindFirstValue(ClaimTypes.Role);

            if (user.Username != username && role != "Admin")
                return Forbid();

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
