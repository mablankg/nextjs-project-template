using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;

namespace TelegramBot
{
    public class Bot
    {
        private readonly TelegramBotClient _botClient;
        private readonly IConfiguration _config;
        private readonly ApiService _apiService;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public Bot(IConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            var token = _config["Telegram:BotToken"] ?? throw new InvalidOperationException("Bot token not found in configuration");
            _botClient = new TelegramBotClient(token);
            _apiService = new ApiService(_config);
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public async Task StartAsync()
        {
            try
            {
                var me = await _botClient.GetMeAsync(_cancellationTokenSource.Token);
                Console.WriteLine($"Bot iniciado: @{me.Username}");

                var receiverOptions = new ReceiverOptions
                {
                    AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery }
                };

                _botClient.StartReceiving(
                    updateHandler: HandleUpdateAsync,
                    pollingErrorHandler: HandlePollingErrorAsync,
                    receiverOptions: receiverOptions,
                    cancellationToken: _cancellationTokenSource.Token
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao iniciar o bot: {ex.Message}");
                throw;
            }
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            try
            {
                if (update.Message is Message message && message.Text is string messageText)
                {
                    var chatId = message.Chat.Id;

                    switch (messageText.ToLower())
                    {
                        case "/start":
                            await SendWelcomeMessageAsync(chatId, cancellationToken);
                            break;
                        case "/ticker":
                            await SendTickerOptionsAsync(chatId, cancellationToken);
                            break;
                        default:
                            await _botClient.SendTextMessageAsync(
                                chatId: chatId,
                                text: "Comando não reconhecido. Use /start para começar ou /ticker para selecionar uma ação.",
                                cancellationToken: cancellationToken);
                            break;
                    }
                }
                else if (update.CallbackQuery is CallbackQuery callbackQuery)
                {
                    await HandleCallbackQueryAsync(callbackQuery, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao processar atualização: {ex.Message}");
            }
        }

        private async Task HandleCallbackQueryAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken)
        {
            if (callbackQuery.Data is null)
                return;

            try
            {
                var ticker = callbackQuery.Data;
                await _botClient.AnswerCallbackQueryAsync(
                    callbackQueryId: callbackQuery.Id,
                    text: $"Buscando dados para {ticker}...",
                    cancellationToken: cancellationToken);

                var stockData = await _apiService.GetTickerDataAsync(ticker);
                var formattedTable = TableFormatter.FormatTable(stockData);

                if (callbackQuery.Message is not null)
                {
                    await _botClient.SendTextMessageAsync(
                        chatId: callbackQuery.Message.Chat.Id,
                        text: $"Dados para {ticker}:\n{formattedTable}",
                        parseMode: ParseMode.Markdown,
                        cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                if (callbackQuery.Message is not null)
                {
                    await _botClient.SendTextMessageAsync(
                        chatId: callbackQuery.Message.Chat.Id,
                        text: $"Erro ao buscar dados: {ex.Message}",
                        cancellationToken: cancellationToken);
                }
            }
        }

        private async Task SendWelcomeMessageAsync(long chatId, CancellationToken cancellationToken)
        {
            var welcomeMessage = "Bem-vindo ao Bot de Cotações! 📈\n\n" +
                               "Use o comando /ticker para selecionar uma ação e ver seus dados.";

            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: welcomeMessage,
                cancellationToken: cancellationToken);
        }

        private async Task SendTickerOptionsAsync(long chatId, CancellationToken cancellationToken)
        {
            var tickerOptions = _config.GetSection("TickerOptions").Get<string[]>();
            
            if (tickerOptions == null)
            {
                tickerOptions = new[] { "AAPL", "GOOGL", "MSFT", "AMZN", "TSLA" };
            }

            if (!tickerOptions.Any())
            {
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Erro: Nenhuma opção de ticker configurada.",
                    cancellationToken: cancellationToken);
                return;
            }

            var buttons = tickerOptions.Select(ticker =>
                InlineKeyboardButton.WithCallbackData(text: ticker, callbackData: ticker));

            var keyboard = new InlineKeyboardMarkup(
                buttons.Select(button => new[] { button }));

            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Selecione uma ação para ver seus dados:",
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);
        }

        private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var errorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Erro na API do Telegram:\n{apiRequestException.ErrorCode}\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(errorMessage);
            return Task.CompletedTask;
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
        }
    }
}
