using practice.Models;

namespace practice.Repositories;

public interface ILibraryRepository
{
    List<Library> GetAll();
    Library? GetById(int id);
    Library Add(Library library);
    void Update(Library library);
    void Delete(int id);
    List<int> GetBookIds(int libraryId);
    bool AddBookId(int libraryId, int bookId);
    bool AddBookIds(int libraryId, List<int> bookIds);
    bool RemoveBookId(int libraryId, int bookId);
}
