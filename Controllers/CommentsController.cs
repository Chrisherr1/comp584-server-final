using Microsoft.AspNetCore.Authorization; // 👈 NEW
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;             // 👈 NEW
using Comp584ServerFinal.Data.Models;

namespace Comp584ServerFinal.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CommentsController : ControllerBase
    {
        private readonly Comp584DbContext _context;

        // Sets up access to EF Core database context 
        // .net Core injects Comp584DbContext when controller is created so we can query database with _context
        public CommentsController(Comp584DbContext context)
        {
            _context = context;
        }

        // Public: anyone can view all comments
        [AllowAnonymous]
        [HttpGet]
        //Route: GET  /api/comments 
        //returns a list of all comments in the database
        public async Task<ActionResult<IEnumerable<Comment>>> GetComments()
        {
            return await _context.Comments
                .Include(c => c.Post) // loads related post for each comment
                .Include(c => c.User) // loads user who wrote the comment
                .ToListAsync();
        }

        // Public: anyone can view a specific comment
        [AllowAnonymous]
        [HttpGet("{id}")]
        //Route: GET /api/comments/{id}
        //returns a single comment by it's ID
        // Loads comment with post and user
        public async Task<ActionResult<Comment>> GetComment(int id)
        {
            var comment = await _context.Comments
                .Include(c => c.Post)
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == id);
            // if no comment exists with that Id 404 not found 
            // otherwirse return the comment
            if (comment == null) return NotFound();
            return comment;
        }

        // Authenticated: only logged-in users can create comments
        [Authorize]
        [HttpPost]
        //Route: POST /api/comments
        //Adds new comment to database
        public async Task<ActionResult<Comment>> CreateComment(Comment comment)
        {
            var username = User.Identity?.Name;
            var author = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (author == null) return Unauthorized();

            //takes comment object from request body
            comment.User = author;
            //saves it to _context.Comments
            _context.Comments.Add(comment);
            //Saves Asynch
            await _context.SaveChangesAsync();
        // returns 201 created when created
            return CreatedAtAction(nameof(GetComment), new { id = comment.Id }, comment);
        }

        // Authenticated: users can update their own comments, Admin/Moderator can update any
        [Authorize]
        [HttpPut("{id}")]
        //Route; PUT /api/comments/{id}
        //Updates an existing comment
        public async Task<IActionResult> UpdateComment(int id, Comment comment)
        {
            if (id != comment.Id) return BadRequest();
            // check if Id in the route matches Id in the request body
            var existingComment = await _context.Comments.Include(c => c.User).FirstOrDefaultAsync(c => c.Id == id);
            if (existingComment == null) return NotFound();

            var username = User.Identity?.Name;
            var role = User.FindFirstValue(ClaimTypes.Role);

            if (existingComment.User.Username != username && role != "Admin" && role != "Moderator")
                return Forbid();

            existingComment.Content = comment.Content;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Authenticated: users can delete their own comments, Admin/Moderator can delete any
        [Authorize]
        [HttpDelete("{id}")]
        // Route: DELETE /api/comments/{id}
        // Deletes a comment by ID
        public async Task<IActionResult> DeleteComment(int id)
        {   
            var comment = await _context.Comments.Include(c => c.User).FirstOrDefaultAsync(c => c.Id == id);
            if (comment == null) return NotFound();

            var username = User.Identity?.Name;
            var role = User.FindFirstValue(ClaimTypes.Role);

            if (comment.User.Username != username && role != "Admin" && role != "Moderator")
                return Forbid();

            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
