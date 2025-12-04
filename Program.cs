using System;
public class Book
{
    public string Title { get; set; }
    public string Author { get; set; }
    public int Year { get; set; }
    public Book(string title, string author, int year)
    {
        Title = title;
        Author = author;
        Year = year;
    }
    public void ShowInfo()
    {
        Console.WriteLine($"Название: {Title}, автор: {Author}, Год: {Year}");
    }
}
class Program
{
    static void Main(string[] args)
    {
        var book = new Book("1984", "Джордж Оруэлл", 1949);
        book.ShowInfo();
    }
}