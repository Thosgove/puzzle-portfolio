using practice.Models;

namespace practice.Repositories;

public interface IBookRepository
{
    List<Book> GetAll();
    Book? GetById(int id);
    List<Book> GetByAuthor(string authorName);
    Book Add(Book book);
    void Update(Book book);
    void Delete(int id);
}
