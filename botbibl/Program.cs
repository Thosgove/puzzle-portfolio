using practice.Repositories;
using practice.Settings;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddNewtonsoftJson();
builder.Services.AddSwaggerGen();

builder.Services.Configure<LibrarySettings>(
    builder.Configuration.GetSection("LibrarySettings"));

builder.Services.AddTransient<IBookRepository, BookRepository>();
builder.Services.AddTransient<ILibraryRepository, LibraryRepository>();

var telegramToken = "8240820235:AAGoOKgDf59Ip4tLvRs3DaAuURAlh-rJF_k";
builder.Services.AddSingleton<ITelegramBotClient>(sp => new TelegramBotClient(telegramToken));
builder.Services.AddTransient<IBookRepository, BookRepository>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

var dataPath = Path.Combine(Directory.GetCurrentDirectory(), "Data");
if (!Directory.Exists(dataPath))
    Directory.CreateDirectory(dataPath);

var booksPath = Path.Combine(dataPath, "books.json");
var librariesPath = Path.Combine(dataPath, "libraries.json");
if (!System.IO.File.Exists(booksPath)) System.IO.File.WriteAllText(booksPath, "[]");
if (!System.IO.File.Exists(librariesPath)) System.IO.File.WriteAllText(librariesPath, "[]");

var botClient = app.Services.GetRequiredService<ITelegramBotClient>();
using var cts = new CancellationTokenSource();

await botClient.DeleteWebhookAsync(cancellationToken: cts.Token);

var receiverOptions = new ReceiverOptions { AllowedUpdates = Array.Empty<UpdateType>() };

var menuKeyboard = new ReplyKeyboardMarkup(new[]
{
    new KeyboardButton[] { "Book Catalog", "Help" }
})
{
    ResizeKeyboard = true
};

botClient.StartReceiving(
    updateHandler: async (bot, update, token) =>
    {
    try
    {
        if (update.Message is { Text: { } messageText } message)
        {
            long chatId = message.Chat.Id;
            long userId = message.From?.Id ?? chatId;
            using var scope = app.Services.CreateScope();
            var bookRepository = scope.ServiceProvider.GetRequiredService<IBookRepository>();
            if (messageText == "Book Catalog") messageText = "/catalog";
            if (messageText == "Help") messageText = "/help";
            if (messageText.StartsWith("/"))
            {
                var parts = messageText.Split(' ', 2);
                var command = parts[0].ToLower();
                var argument = parts.Length > 1 ? parts[1].Trim() : string.Empty;

                switch (command)
                {
                    case "/start":
                        await bot.SendTextMessageAsync(
                            chatId,
                            "Hello! Welcome to the library bot.\n\nYou will only see the books that you personally added.",
                            replyMarkup: menuKeyboard,
                            cancellationToken: token);
                        break;

                    case "/help":
                        var helpText = "Available actions:\n" +
                                       "/start - Restart the bot\n" +
                                       "/catalog - Show your personal books from the global list\n" +
                                       "/book [id] - View specific book details (if it belongs to you)\n" +
                                       "/search [text] - Find books in your collection by title\n" +
                                       "/author [name] - Find your books by author\n" +
                                       "/add [title] - [author] - Add a new book to the global list\n" +
                                       "/delete [id] - Delete your book";

                        await bot.SendTextMessageAsync(chatId, helpText, replyMarkup: menuKeyboard, cancellationToken: token);
                        break;

                    case "/catalog":
                        var userBooks = bookRepository.GetAll()
                            .Where(b => b.UserId == userId)
                            .ToList();

                        if (!userBooks.Any())
                        {
                            await bot.SendTextMessageAsync(chatId, "You haven't added any books yet.", cancellationToken: token);
                        }
                        else
                        {
                            var catalogText = "Your Books:\n\n" +
                                string.Join("\n", userBooks.Select(b => $"ID: {b.Id} | {b.Title} (Author: {b.Author ?? "Unknown"})"));

                            await bot.SendTextMessageAsync(chatId, catalogText, cancellationToken: token);
                        }
                        break;

                    case "/book":
                        if (string.IsNullOrWhiteSpace(argument) || !int.TryParse(argument, out int bookId))
                        {
                            await bot.SendTextMessageAsync(chatId, "Please specify a valid numeric book ID.\nExample: `/book 1`", parseMode: ParseMode.Markdown, cancellationToken: token);
                            break;
                        }

                        var book = bookRepository.GetById(bookId);
                        if (book == null || book.UserId != userId)
                        {
                            await bot.SendTextMessageAsync(chatId, "Book not found or you do not have permission to view it.", cancellationToken: token);
                        }
                        else
                        {
                            var details = $"Book Details:\n\nID: {book.Id}\nTitle: {book.Title}\nAuthor: {book.Author ?? "Unknown"}";
                            await bot.SendTextMessageAsync(chatId, details, cancellationToken: token);
                        }
                        break;

                    case "/search":
                        if (string.IsNullOrWhiteSpace(argument))
                        {
                            await bot.SendTextMessageAsync(chatId, "You did not specify a title for the search.\nExample: `/search Harry Potter`", parseMode: ParseMode.Markdown, cancellationToken: token);
                            break;
                        }
                        var foundBooks = bookRepository.GetAll()
                            .Where(b => b.UserId == userId && b.Title != null && b.Title.Contains(argument, StringComparison.OrdinalIgnoreCase))
                            .ToList();

                        if (!foundBooks.Any())
                        {
                            await bot.SendTextMessageAsync(chatId, $"No books found containing \"{argument}\" in your collection.", cancellationToken: token);
                        }
                        else
                        {
                            var resultText = $"Found books ({foundBooks.Count}):\n\n" +
                                string.Join("\n", foundBooks.Select(b => $"ID: {b.Id} | {b.Title} - {b.Author}"));

                            await bot.SendTextMessageAsync(chatId, resultText, cancellationToken: token);
                        }
                        break;

                    case "/author":
                        if (string.IsNullOrWhiteSpace(argument))
                        {
                            await bot.SendTextMessageAsync(chatId, "Please specify an author name.\nExample: `/author Tolkien`", parseMode: ParseMode.Markdown, cancellationToken: token);
                            break;
                        }
                        var booksByAuthor = bookRepository.GetByAuthor(argument)
                            .Where(b => b.UserId == userId)
                            .ToList();

                        if (!booksByAuthor.Any())
                        {
                            await bot.SendTextMessageAsync(chatId, $"No books found by \"{argument}\" in your collection.", cancellationToken: token);
                        }
                        else
                        {
                            var authorResultText = $"Your books by {argument} ({booksByAuthor.Count}):\n\n" +
                                string.Join("\n", booksByAuthor.Select(b => $"ID: {b.Id} | {b.Title}"));

                            await bot.SendTextMessageAsync(chatId, authorResultText, cancellationToken: token);
                        }
                        break;

                    case "/add":
                        if (string.IsNullOrWhiteSpace(argument) || !argument.Contains("-"))
                        {
                            await bot.SendTextMessageAsync(chatId, "Invalid format. Use a dash to separate title and author.\nExample: `/add The Hobbit - J.R.R. Tolkien`", parseMode: ParseMode.Markdown, cancellationToken: token);
                            break;
                        }

                        var bookParts = argument.Split('-', 2);
                        var newTitle = bookParts[0].Trim();
                        var newAuthor = bookParts[1].Trim();

                        var newBook = new practice.Models.Book
                        {
                            Title = newTitle,
                            Author = newAuthor,
                            UserId = userId
                        };

                        bookRepository.Add(newBook);
                        await bot.SendTextMessageAsync(chatId, $"Success! \"{newTitle}\" has been added to your collection.", cancellationToken: token);
                        break;

                        case "/delete":
                            if (string.IsNullOrWhiteSpace(argument) || !int.TryParse(argument, out int deleteId))
                            {
                                await bot.SendTextMessageAsync(chatId, "Please specify a valid numeric book ID to delete.\nExample: `/delete 1`", parseMode: ParseMode.Markdown, cancellationToken: token);
                                break;
                            }

                            var checkBook = bookRepository.GetById(deleteId);
                            if (checkBook == null)
                            {
                                await bot.SendTextMessageAsync(chatId, $"Cannot delete. Book with ID {deleteId} does not exist.", cancellationToken: token);
                            }
                            else
                            {
                                bookRepository.Delete(deleteId);
                                await bot.SendTextMessageAsync(chatId, $"Success! Book with ID {deleteId} has been deleted.", cancellationToken: token);
                            }
                            break;

                        default:
                            await bot.SendTextMessageAsync(chatId, "Unknown command. Try /help", cancellationToken: token);
                            break;
                    }
                }
                else
                {
                    await bot.SendTextMessageAsync(chatId, $"To search for a book, use the command:\n`/search {messageText}`", parseMode: ParseMode.Markdown, cancellationToken: token);
                }
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[Message Processing Error]: {ex.Message}");
            Console.ResetColor();
        }
    },
    pollingErrorHandler: (bot, exception, token) =>
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[Telegram Polling Error]: {exception.Message}");
        Console.ResetColor();
        return Task.CompletedTask;
    },
    receiverOptions: receiverOptions,
    cancellationToken: cts.Token
);
app.Run();