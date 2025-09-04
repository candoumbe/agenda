using System;
using System.Linq.Expressions;
using Serialize.Linq.Serializers;
using Xunit.Sdk;
using JsonSerializer = Serialize.Linq.Serializers.JsonSerializer;

namespace Agenda.API.UnitTests.Helpers;

public class XunitSerializableExpression<T>: IXunitSerializable
{
    public Expression<Func<T, bool>> Value { get; set; }

    private readonly ExpressionSerializer _serializer = new(new JsonSerializer());


    /// <inheritdoc />
    public void Deserialize(IXunitSerializationInfo info)
    {
        Value = _serializer.DeserializeText(info.GetValue<string>(nameof(Value))) as Expression<Func<T, bool>>;
    }

    /// <inheritdoc />
    public void Serialize(IXunitSerializationInfo info) => info.AddValue(nameof(Value), _serializer.SerializeText(Value));


    public static implicit operator Expression<Func<T, bool>>(XunitSerializableExpression<T> expressionSerializable) => expressionSerializable.Value;

    public static implicit operator XunitSerializableExpression<T>(Expression<Func<T, bool>> expression) => new() { Value = expression };
}