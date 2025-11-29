public class CommentDto
{
    public int Id { get; set; }
    public string Content { get; set; } = null!;
    public int PostId { get; set; }
    public string AuthorName { get; set; } = null!;
}
