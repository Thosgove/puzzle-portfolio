using Microsoft.AspNetCore.Mvc;
using practice.Models;
using practice.Repositories;

namespace practice.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LibraryController : ControllerBase
{
    private readonly ILibraryRepository _libraryRepository;
    private readonly IBookRepository _bookRepository;

    public LibraryController(ILibraryRepository libraryRepository, IBookRepository bookRepository)
    {
        _libraryRepository = libraryRepository;
        _bookRepository = bookRepository;
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        return Ok(_libraryRepository.GetAll());
    }

    [HttpGet("{id:int}")]
    public IActionResult GetById(int id)
    {
        var lib = _libraryRepository.GetById(id);
        if (lib == null)
            return NotFound(new { message = "Library not found." });
        return Ok(lib);
    }

    [HttpPost]
    public IActionResult Create([FromBody] Library lib)
    {
        if (string.IsNullOrWhiteSpace(lib.Name))
            return BadRequest(new { message = "Name is required." });

        var created = _libraryRepository.Add(lib);
        return StatusCode(201, new { id = created.Id });
    }

    [HttpGet("{id:int}/books")]
    public IActionResult GetBooks(int id)
    {
        var library = _libraryRepository.GetById(id);
        if (library == null)
            return NotFound(new { message = "Library not found." });

        var books = library.BookIds
            .Select(_bookRepository.GetById)
            .Where(b => b != null)
            .ToList();

        return Ok(books);
    }

    [HttpPost("{id:int}/books/{bookId:int}")]
    public IActionResult AddBook(int id, int bookId)
    {
        if (_bookRepository.GetById(bookId) == null)
            return BadRequest(new { message = "Book does not exist." });

        var ok = _libraryRepository.AddBookId(id, bookId);
        if (!ok)
            return NotFound(new { message = "Library not found." });

        return NoContent();
    }

    [HttpDelete("{id:int}/books/{bookId:int}")]
    public IActionResult RemoveBook(int id, int bookId)
    {
        var ok = _libraryRepository.RemoveBookId(id, bookId);
        if (!ok)
            return NotFound(new { message = "Library or book not found." });

        return NoContent();
    }
}
