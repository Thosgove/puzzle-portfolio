using Microsoft.AspNetCore.Mvc;
using practice.Models;
using practice.Repositories;

namespace practice.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookController : ControllerBase
{
    private readonly IBookRepository _bookRepository;

    public BookController(IBookRepository bookRepository)
    {
        _bookRepository = bookRepository;
    }

    [HttpGet]
    public ActionResult<IEnumerable<Book>> GetAll()
    {
        return Ok(_bookRepository.GetAll());
    }

    [HttpGet("{id:int}")]
    public ActionResult<Book> GetById(int id)
    {
        var book = _bookRepository.GetById(id);
        if (book == null)
            return NotFound(new { message = $"Book with id={id} not found." });
        return Ok(book);
    }

    [HttpGet("search")]
    public ActionResult<IEnumerable<Book>> GetByAuthor([FromQuery] string author)
    {
        if (string.IsNullOrWhiteSpace(author))
            return BadRequest(new { message = "Author parameter is required." });

        return Ok(_bookRepository.GetByAuthor(author));
    }

    [HttpPost]
    public ActionResult<Book> Create([FromBody] Book book)
    {
        if (string.IsNullOrWhiteSpace(book.Title))
            return BadRequest(new { message = "Title is required." });
        if (string.IsNullOrWhiteSpace(book.Author))
            return BadRequest(new { message = "Author is required." });

        var created = _bookRepository.Add(book);
        return StatusCode(StatusCodes.Status201Created, new { Id = created.Id });
    }

    [HttpPut("{id:int}")]
    public IActionResult Update(int id, [FromBody] Book book)
    {
        if (string.IsNullOrWhiteSpace(book.Title) || string.IsNullOrWhiteSpace(book.Author))
            return BadRequest(new { message = "Title and Author are required." });

        var existing = _bookRepository.GetById(id);
        if (existing == null)
            return NotFound(new { message = $"Book with id={id} not found." });

        book.Id = id;
        _bookRepository.Update(book);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id)
    {
        var existing = _bookRepository.GetById(id);
        if (existing == null)
            return NotFound(new { message = $"Book with id={id} not found." });

        _bookRepository.Delete(id);
        return NoContent();
    }
}
