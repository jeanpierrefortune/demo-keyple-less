using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using Serilog.Events;
/// <summary>
/// The Misc class provides utility methods that are used across the application.
/// </summary>
namespace DemoKeypleLess.domain.utils
{
    public class Misc
    {

        /// <summary>
        /// Converts a raw JSON string into a prettily formatted JSON string.
        /// </summary>
        /// <param name="rawJson">The raw JSON string to be prettified.</param>
        /// <returns>
        /// A string representing the input JSON in a prettified format.
        /// If the input is not valid JSON, an exception will be thrown.
        /// </returns>
        public static string PrettyPrintJson(string rawJson)
        {
            // Parse raw JSON string into JToken
            var parsedJson = JToken.Parse(rawJson);

            // Serialize parsedJson back into string with formatting
            var prettyJson = JsonConvert.SerializeObject(parsedJson, Formatting.Indented);

            return prettyJson;
        }

        /// <summary>
        /// Displays the text in the specified color on the console and logs it with the specified log level using the provided logger.
        /// </summary>
        /// <param name="text">The text to be displayed and logged.</param>
        /// <param name="color">The color in which the text will be displayed on the console.</param>
        /// <param name="logLevel">The log level at which the text will be logged.</param>
        /// <param name="logger">The logger instance to use for logging.</param>
        public static void DisplayAndLog(string text, ConsoleColor color, LogEventLevel logLevel, ILogger logger)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ResetColor();
            logger.Write(logLevel, text);
        }
    }
}
