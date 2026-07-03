using System.Text.Json;
using Microsoft.Extensions.Options;
using practice.Models;
using practice.Settings;

namespace practice.Repositories;

public class BookRepository : IBookRepository
{
    private readonly IOptions<LibrarySettings> _settings;

    public BookRepository(IOptions<LibrarySettings> settings)
    {
        _settings = settings;
    }

    private List<Book> Load()
    {
        var path = _settings.Value.BooksFilePath;
        if (!File.Exists(path))
            return new List<Book>();

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<List<Book>>(json) ?? new List<Book>();
    }

    private void Save(List<Book> books)
    {
        var path = _settings.Value.BooksFilePath;
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(books, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }

    public List<Book> GetAll() => Load();

    public Book? GetById(int id) => Load().FirstOrDefault(b => b.Id == id);

    public List<Book> GetByAuthor(string authorName)
    {
        return Load()
            .Where(b => b.Author.Contains(authorName, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public Book Add(Book book)
    {
        var books = Load();
        book.Id = books.Count == 0 ? 1 : books.Max(b => b.Id) + 1;
        book.CreatedAt = DateTime.UtcNow;
        books.Add(book);
        Save(books);
        return book;
    }

    public void Update(Book book)
    {
        var books = Load();
        var existing = books.FirstOrDefault(b => b.Id == book.Id);
        if (existing == null) return;

        existing.Title = book.Title;
        existing.Author = book.Author;
        Save(books);
    }

    public void Delete(int id)
    {
        var books = Load();
        var book = books.FirstOrDefault(b => b.Id == id);
        if (book != null)
        {
            books.Remove(book);
            Save(books);
        }
    }
}