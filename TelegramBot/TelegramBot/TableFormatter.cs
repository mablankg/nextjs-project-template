using System;
using System.Text;
using Newtonsoft.Json.Linq;

namespace TelegramBot
{
    public static class TableFormatter
    {
        public static string FormatTable(JObject jsonData)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("```");
                sb.AppendLine("+----------------------+----------------------+");
                sb.AppendLine("|      Campo          |       Valor         |");
                sb.AppendLine("+----------------------+----------------------+");

                foreach (var property in jsonData.Properties())
                {
                    string name = property.Name;
                    string value = property.Value?.ToString() ?? "N/A";
                    
                    // Truncate long values to fit in the table
                    if (value.Length > 20)
                    {
                        value = value.Substring(0, 17) + "...";
                    }

                    sb.AppendLine($"| {name,-20} | {value,-20} |");
                }

                sb.AppendLine("+----------------------+----------------------+");
                sb.AppendLine("```");
                return sb.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error formatting table: {ex.Message}");
                return "Erro ao formatar os dados.";
            }
        }
    }
}
