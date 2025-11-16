using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GeneticAlgorithm.ExternalClasses
{
    public class CustomDateTimeConverter : JsonConverter<DateTime>
    {
        private readonly string[] formats =
        [
            "yyyy-MM-ddTHH:mm:ss",  // Standard ISO format
            "yyyy-MM-dd",           // Date format without time
            "MM/dd/yyyy"            // Custom format
        ];

        // Read the date from the JSON string
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string dateString = reader.GetString();

            foreach (var format in formats)
            {
                if (DateTime.TryParseExact(dateString, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
                {
                    return date;
                }
            }

            throw new FormatException($"Unable to parse date: {dateString}");
        }

        // Write the date to the JSON in a standard format
        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString("yyyy-MM-dd"));  // Use standard format for writing
        }
    }
}
