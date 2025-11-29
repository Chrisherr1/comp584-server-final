using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Comp584ServerFinal.Data.Models;
using Comp584ServerFinal.DTO;

namespace Comp584ServerFinal.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PostsController : ControllerBase
    {
        private readonly Comp584DbContext _context;

        public PostsController(Comp584DbContext context)
        {
            _context = context;
        }

        // Allows Anyone to see ALL POSTS
        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PostDto>>> GetPosts()
        {
            return await _context.Posts
                .Include(p => p.Author)
                .Select(p => new PostDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    Content = p.Content ?? string.Empty,
                    AuthorName = p.Author.Username // ✅ show username in list
                })
                .ToListAsync();
        }

        // Allows ANYONE to see a specific post via id
        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<ActionResult<PostDto>> GetPost(int id)
        {
            var post = await _context.Posts
                .Include(p => p.Author)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post == null) return NotFound();

            return new PostDto
            {
                Id = post.Id,
                Title = post.Title,
                Content = post.Content ?? string.Empty,
                AuthorName = post.Author.Username // ✅ show username in detail
            };
        }

        // Allows users to create posts
        [Authorize]
        [HttpPost]
        public async Task<ActionResult<PostDto>> CreatePost(CreatePostDto dto)
        {
            var email = User.Identity?.Name;
            var author = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (author == null) return Unauthorized();

            var post = new Post
            {
                Title = dto.Title,
                Content = dto.Content,
                Author = author,
                AuthorId = author.Id,
                CreatedAt = DateTime.UtcNow
            };

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            var result = new PostDto
            {
                Id = post.Id,
                Title = post.Title,
                Content = post.Content ?? string.Empty,
                AuthorName = author.Username // ✅ show username in response
            };

            return CreatedAtAction(nameof(GetPost), new { id = post.Id }, result);
        }

        // Allows users to update their posts along with Admin
        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePost(int id, Post post)
        {
            if (id != post.Id) return BadRequest();

            var email = User.Identity?.Name;
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            var existingPost = await _context.Posts.Include(p => p.Author).FirstOrDefaultAsync(p => p.Id == id);
            if (existingPost == null) return NotFound();

            if (existingPost.Author.Email != email && userRole != "Admin")
                return Forbid();

            existingPost.Title = post.Title;
            existingPost.Content = post.Content;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // Allows users to Delete their own Posts
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePost(int id)
        {
            var post = await _context.Posts.Include(p => p.Author).FirstOrDefaultAsync(p => p.Id == id);
            if (post == null) return NotFound();

            var email = User.Identity?.Name;
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            if (post.Author.Email != email && userRole != "Admin")
                return Forbid();

            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
