using System.Text.Json;
using Agenda.Ids;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using Xunit.Sdk;

namespace Agenda.API.UnitTests.Helpers;

public class GenericSerializable<T> : IXunitSerializable
{
    public T Value { get; set; }

    private readonly JsonSerializerOptions _serializerSettings;

    public GenericSerializable()
    {
        _serializerSettings = new JsonSerializerOptions()
            .ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
        _serializerSettings.Converters.Add(new AppointmentId.AppointmentIdSystemTextJsonConverter());
        _serializerSettings.Converters.Add(new AttendeeId.AttendeeIdSystemTextJsonConverter());
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

    public static implicit operator T(GenericSerializable<T> genericSerializable) => genericSerializable.Value;

    public static implicit operator GenericSerializable<T>(T value) => new() { Value = value };
}