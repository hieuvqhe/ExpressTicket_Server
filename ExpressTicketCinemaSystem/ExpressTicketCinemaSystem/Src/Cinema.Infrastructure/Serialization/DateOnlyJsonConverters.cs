using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Serialization
{
    public sealed class DateOnlyJsonConverter : JsonConverter<DateOnly>
    {
        private const string Format = "yyyy-MM-dd";
        public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => DateOnly.ParseExact(reader.GetString()!, Format, CultureInfo.InvariantCulture);

        public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.ToString(Format, CultureInfo.InvariantCulture));
    }

    public sealed class NullableDateOnlyJsonConverter : JsonConverter<DateOnly?>
    {
        private const string Format = "yyyy-MM-dd";
        public override DateOnly? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => string.IsNullOrEmpty(reader.GetString())
               ? null
               : DateOnly.ParseExact(reader.GetString()!, Format, CultureInfo.InvariantCulture);

        public override void Write(Utf8JsonWriter writer, DateOnly? value, JsonSerializerOptions options)
            => writer.WriteStringValue(value?.ToString(Format, CultureInfo.InvariantCulture));
    }
}
