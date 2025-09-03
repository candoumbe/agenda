using System.Text.Json;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using Xunit.Sdk;

namespace Agenda.API.UnitTests.Helpers
{
    public class GenericSerializable<T> : IXunitSerializable
    {
        public T Value { get; set; }

        private readonly JsonSerializerOptions _serializerSettings;

        public GenericSerializable()
        {
            _serializerSettings = new JsonSerializerOptions(JsonSerializerDefaults.Web).ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
        }

        /// <inheritdoc />
        public void Deserialize(IXunitSerializationInfo info)
        {
            Value = JsonSerializer.Deserialize<T>(info.GetValue<string>(nameof(Value)), _serializerSettings)!;
        }

        /// <inheritdoc />
        public void Serialize(IXunitSerializationInfo info)
        {
            info.AddValue(nameof(Value), JsonSerializer.Serialize(Value, _serializerSettings));
        }
    }
}