namespace MyBoards.Entities;

public class Comment
{
    public string Message { get; set; }

    public string Author { get; set; }
    
    public DateTime CreateDate { get; set; }

    public DateTime? UpdateDate { get; set; }
    
}