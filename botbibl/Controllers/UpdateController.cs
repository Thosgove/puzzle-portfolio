using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using practice.Models;
using practice.Repositories;

namespace practice.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UpdateController : ControllerBase
{
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<UpdateController> _logger;
    private readonly IBookRepository _bookRepository;
    private readonly ILibraryRepository _libraryRepository;

    private static readonly ConcurrentDictionary<long, UserSession> _sessions = new();

    public UpdateController(
        ITelegramBotClient botClient,
        ILogger<UpdateController> logger,
        IBookRepository bookRepository,
        ILibraryRepository libraryRepository)
    {
        _botClient = botClient;
        _logger = logger;
        _bookRepository = bookRepository;
        _libraryRepository = libraryRepository;
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] Update update)
    {
        if (update == null)
            return BadRequest();

        try
        {
            if (update.Type == UpdateType.Message && update.Message?.Text != null)
            {
                var chatId = update.Message.Chat.Id;
                var userText = update.Message.Text.Trim();

                _logger.LogInformation("Получено сообщение от {ChatId}: {Text}", chatId, userText);

                var session = _sessions.GetOrAdd(chatId, _ => new UserSession());

                if (session.CurrentStep != UserStep.None)
                {
                    await ProcessUserStepAsync(chatId, userText, session);
                    return Ok();
                }

                switch (userText)
                {
                    case "/start":
                        await _botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Добро пожаловать в библиотечный бот! Выберите нужное действие на клавиатуре:",
                            replyMarkup: GetMainMenuKeyboard()
                        );
                        break;

                    case " Список библиотек":
                        var libraries = _libraryRepository.GetAll();
                        if (!libraries.Any())
                        {
                            await _botClient.SendTextMessageAsync(chatId, "Список библиотек пока пуст.");
                        }
                        else
                        {
                            var libListText = " *Доступные библиотеки:*\n\n" +
                                string.Join("\n", libraries.Select(l => $"• *{l.Name}* (ID: `{l.Id}`, книг: {l.BookIds.Count})"));

                            await _botClient.SendTextMessageAsync(chatId, libListText, parseMode: ParseMode.Markdown);
                        }
                        break;

                    case " Добавить книгу":
                        session.CurrentStep = UserStep.WaitingForBookTitle;
                        await _botClient.SendTextMessageAsync(chatId, "Введите название книги:", replyMarkup: new ReplyKeyboardRemove());
                        break;

                    case " Поиск по автору":
                        session.CurrentStep = UserStep.WaitingForAuthorSearch;
                        await _botClient.SendTextMessageAsync(chatId, "Введите имя автора для поиска:", replyMarkup: new ReplyKeyboardRemove());
                        break;

                    default:
                        await _botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Пожалуйста, используйте кнопки меню для управления ботом.",
                            replyMarkup: GetMainMenuKeyboard()
                        );
                        break;
                }
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка обработки update");
            return Ok();
        }
    }

    private async Task ProcessUserStepAsync(long chatId, string userText, UserSession session)
    {
        switch (session.CurrentStep)
        {
            case UserStep.WaitingForAuthorSearch:
                session.CurrentStep = UserStep.None;
                var foundBooks = _bookRepository.GetByAuthor(userText);

                if (!foundBooks.Any())
                {
                    await _botClient.SendTextMessageAsync(chatId, $"Книг автора \"{userText}\" не найдено.", replyMarkup: GetMainMenuKeyboard());
                    return;
                }

                var searchResult = $"*Найденные книги автора \"{userText}\":*\n\n" +
                    string.Join("\n", foundBooks.Select(b => $"• \"{b.Title}\""));

                await _botClient.SendTextMessageAsync(chatId, searchResult, parseMode: ParseMode.Markdown, replyMarkup: GetMainMenuKeyboard());
                break;

            case UserStep.WaitingForBookTitle:
                session.TempBookTitle = userText;
                session.CurrentStep = UserStep.WaitingForBookAuthor;
                await _botClient.SendTextMessageAsync(chatId, $"Отлично! Книга: \"{userText}\". Теперь введите её автора:");
                break;

            case UserStep.WaitingForBookAuthor:
                session.TempBookAuthor = userText;
                session.CurrentStep = UserStep.WaitingForLibraryId;
                await _botClient.SendTextMessageAsync(chatId, "Введите числовой ID библиотеки, в которую нужно добавить книгу:");
                break;

            case UserStep.WaitingForLibraryId:
                if (int.TryParse(userText, out int libraryId))
                {
                    var targetLibrary = _libraryRepository.GetById(libraryId);
                    if (targetLibrary == null)
                    {
                        await _botClient.SendTextMessageAsync(chatId, "Библиотека с таким ID не найдена. Введите корректный ID:");
                        return;
                    }

                    var newBook = new Book
                    {
                        Title = session.TempBookTitle,
                        Author = session.TempBookAuthor
                    };
                    var addedBook = _bookRepository.Add(newBook);

                    _libraryRepository.AddBookId(libraryId, addedBook.Id);

                    session.CurrentStep = UserStep.None;
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: $"Книга \"{addedBook.Title}\" автора {addedBook.Author} успешно добавлена в библиотеку \"{targetLibrary.Name}\"!",
                        replyMarkup: GetMainMenuKeyboard()
                    );
                }
                else
                {
                    await _botClient.SendTextMessageAsync(chatId, "Ошибка: ID должен быть целым числом. Пожалуйста, введите корректный ID библиотеки:");
                }
                break;
        }
    }

    private ReplyKeyboardMarkup GetMainMenuKeyboard()
    {
        return new ReplyKeyboardMarkup(new[]
        {
            new KeyboardButton[] { "Добавить книгу" },
            new KeyboardButton[] { "Список библиотек", "Поиск по автору" }
        })
        {
            ResizeKeyboard = true
        };
    }
}
