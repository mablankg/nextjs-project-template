using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace TelegramBot
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                // Build configuration
                IConfiguration config = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .Build();

                // Validate configuration
                var botToken = config["Telegram:BotToken"];
                if (string.IsNullOrEmpty(botToken))
                {
                    throw new InvalidOperationException("Bot token not found in configuration. Please check appsettings.json");
                }

                // Create and start the bot
                Console.WriteLine("Iniciando o bot...");
                var bot = new Bot(config);
                await bot.StartAsync();

                Console.WriteLine("Bot está em execução. Pressione Ctrl+C para sair.");

                // Handle graceful shutdown
                var cts = new CancellationTokenSource();
                Console.CancelKeyPress += (s, e) =>
                {
                    Console.WriteLine("Desligando o bot...");
                    cts.Cancel();
                    e.Cancel = true;
                };

                // Keep the application running until cancellation is requested
                try
                {
                    await Task.Delay(Timeout.Infinite, cts.Token);
                }
                catch (TaskCanceledException)
                {
                    // Normal shutdown, no need to handle
                }

                bot.Stop();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro fatal: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                Environment.Exit(1);
            }
        }
    }
}
