namespace practice.Models;

public class Book
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public long UserId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public enum UserStep
{
    None,
    WaitingForBookTitle,
    WaitingForBookAuthor,
    WaitingForLibraryId,
    WaitingForAuthorSearch
}

public class UserSession
{
    public UserStep CurrentStep { get; set; } = UserStep.None;
    public string TempBookTitle { get; set; } = string.Empty;
    public string TempBookAuthor { get; set; } = string.Empty;
}
