using System;
using System.Linq.Expressions;
using Agenda.Ids;
using NodaTime;
using Serialize.Linq.Factories;
using Serialize.Linq.Serializers;
using Xunit.Abstractions;
using Xunit.Sdk;
using JsonSerializer = Serialize.Linq.Serializers.JsonSerializer;

namespace Agenda.API.UnitTests.Helpers;

public class XunitSerializableExpression<T>: IXunitSerializable
{
    public Expression<Func<T, bool>> Value { get; set; }

    private readonly ExpressionSerializer _expressionSerializer;

    public XunitSerializableExpression()
    {
        JsonSerializer jsonSerializer = new ();
        //jsonSerializer.AddKnownTypes([typeof(AppointmentId), typeof(AttendeeId), typeof(OffsetDateTime)]);
        _expressionSerializer = new ExpressionSerializer(jsonSerializer);
    }


    /// <inheritdoc />
    public void Deserialize(IXunitSerializationInfo info)
    {
        Value = (Expression<Func<T, bool>>)_expressionSerializer.DeserializeText(info.GetValue<string>(nameof(Value)));
    }

    /// <inheritdoc />
    public void Serialize(IXunitSerializationInfo info) => info.AddValue(nameof(Value), _expressionSerializer.SerializeText(Value));


    public static implicit operator Expression<Func<T, bool>>(XunitSerializableExpression<T> expressionSerializable) => expressionSerializable.Value;

    public static implicit operator XunitSerializableExpression<T>(Expression<Func<T, bool>> expression) => new() { Value = expression };
}