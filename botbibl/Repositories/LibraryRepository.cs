using System.Text.Json;
using Microsoft.Extensions.Options;
using practice.Models;
using practice.Settings;

namespace practice.Repositories;

public class LibraryRepository : ILibraryRepository
{
    private readonly IOptions<LibrarySettings> _settings;

    public LibraryRepository(IOptions<LibrarySettings> settings)
    {
        _settings = settings;
    }

    private List<Library> Load()
    {
        var path = _settings.Value.LibrariesFilePath;
        if (!File.Exists(path))
            return new List<Library>();

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<List<Library>>(json) ?? new List<Library>();
    }

    private void Save(List<Library> libraries)
    {
        var path = _settings.Value.LibrariesFilePath;
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(libraries, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }

    public List<Library> GetAll() => Load();

    public Library? GetById(int id) => Load().FirstOrDefault(l => l.Id == id);

    public Library Add(Library library)
    {
        var libs = Load();
        library.Id = libs.Count == 0 ? 1 : libs.Max(l => l.Id) + 1;
        library.BookIds ??= new List<int>();
        libs.Add(library);
        Save(libs);
        return library;
    }

    public void Update(Library library)
    {
        var libs = Load();
        var existing = libs.FirstOrDefault(l => l.Id == library.Id);
        if (existing == null) return;

        existing.Name = library.Name;
        existing.BookIds = library.BookIds ?? new List<int>();
        Save(libs);
    }

    public void Delete(int id)
    {
        var libs = Load();
        var lib = libs.FirstOrDefault(l => l.Id == id);
        if (lib != null)
        {
            libs.Remove(lib);
            Save(libs);
        }
    }

    public List<int> GetBookIds(int libraryId) =>
        GetById(libraryId)?.BookIds ?? new List<int>();

    public bool AddBookId(int libraryId, int bookId)
    {
        var libs = Load();
        var lib = libs.FirstOrDefault(l => l.Id == libraryId);
        if (lib == null) return false;

        if (!lib.BookIds.Contains(bookId))
            lib.BookIds.Add(bookId);

        Save(libs);
        return true;
    }

    public bool AddBookIds(int libraryId, List<int> bookIds)
    {
        var libs = Load();
        var lib = libs.FirstOrDefault(l => l.Id == libraryId);
        if (lib == null) return false;

        foreach (var id in bookIds.Distinct())
            if (!lib.BookIds.Contains(id))
                lib.BookIds.Add(id);

        Save(libs);
        return true;
    }

    public bool RemoveBookId(int libraryId, int bookId)
    {
        var libs = Load();
        var lib = libs.FirstOrDefault(l => l.Id == libraryId);
        if (lib == null) return false;

        if (!lib.BookIds.Contains(bookId)) return false;

        lib.BookIds.Remove(bookId);
        Save(libs);
        return true;
    }
}
