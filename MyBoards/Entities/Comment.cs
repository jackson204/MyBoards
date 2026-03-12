namespace MyBoards.Entities;

public class Comment
{
    public int Id { get; set; }
    
    public string Message { get; set; }

    public string Auther { get; set; }  

    public DateTime CreatedDate { get; set; }

    public DateTime? UpdateDate { get; set; }
}