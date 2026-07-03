namespace practice.Models;

public class Library
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<int> BookIds { get; set; } = new List<int>();
}
