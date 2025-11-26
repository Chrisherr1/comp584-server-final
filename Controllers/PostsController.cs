using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Comp584ServerFinal.Data.Models;

namespace Comp584ServerFinal.Controllers
{
    [ApiController]
    [Route("api/[controller]")]

    //sets up access to EF core DataBase context(_context)
    public class PostsController : ControllerBase
    {
        private readonly Comp584DbContext _context;

        public PostsController(Comp584DbContext context)
        {
            _context = context;
        }

        //Allows Anyone to see ALL POSTS
        [AllowAnonymous]
        [HttpGet]
        //Route: GET/api/posts
        // Returns all posts,including authors and comments
        public async Task<ActionResult<IEnumerable<Post>>> GetPosts()
        {
            return await _context.Posts
                .Include(p => p.Author)
                .Include(p => p.Comments)
                .ToListAsync();
        }

        // Allows ANYONE to see a specific post via id
        [AllowAnonymous]
        [HttpGet("{id}")]
        //Route: GET /api/posts/{id}
        //returns a single post by ID, with author and comments
        public async Task<ActionResult<Post>> GetPost(int id)
        {
            var post = await _context.Posts
                .Include(p => p.Author)
                .Include(p => p.Comments)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post == null) return NotFound();
            return post;
        }

        // Allows users to create posts
        [Authorize]
        [HttpPost]
        // Route: POST /api/posts
        // saves and sends post to DataBase
        public async Task<ActionResult<Post>> CreatePost(Post post)
        {
            // Attach the logged-in user as the Author
            var username = User.Identity?.Name;
            var author = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (author == null) return Unauthorized();

            post.Author = author;
            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPost), new { id = post.Id }, post);
        }

        //Allows users to update their posts along with Admin
        [Authorize]
        [HttpPut("{id}")]
        //Route: PUT /api/posts/{id}
        //loads existing post from database & allows update posts
        public async Task<IActionResult> UpdatePost(int id, Post post)
        {
            if (id != post.Id) return BadRequest();

            // Ownership check: only author or admin can update
            var username = User.Identity?.Name;
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            var existingPost = await _context.Posts.Include(p => p.Author).FirstOrDefaultAsync(p => p.Id == id);
            if (existingPost == null) return NotFound();

            if (existingPost.Author.Username != username && userRole != "Admin")
                return Forbid();

            existingPost.Title = post.Title;
            existingPost.Content = post.Content;

            await _context.SaveChangesAsync();
            return NoContent();
        }
        
        // Allows users to Delete their own Posts
        [Authorize]
        [HttpDelete("{id}")]
        //Route: DELETE /api/posts/{id}
        // loads post via ID and allows deletion depending on role
        public async Task<IActionResult> DeletePost(int id)
        {
            var post = await _context.Posts.Include(p => p.Author).FirstOrDefaultAsync(p => p.Id == id);
            if (post == null) return NotFound();

            var username = User.Identity?.Name;
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            // Allow if user is the author OR has Admin role
            if (post.Author.Username != username && userRole != "Admin")
                return Forbid();

            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
