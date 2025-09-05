using System;
using System.Linq.Expressions;
using Serialize.Linq.Serializers;
using Xunit.Sdk;
using JsonSerializer = Serialize.Linq.Serializers.JsonSerializer;

namespace Agenda.API.UnitTests.Helpers;

public class XunitSerializableExpression<T>: IXunitSerializable
{
    public Expression<Func<T, bool>> Value { get; set; }

    private readonly ExpressionSerializer _expressionSerializer;

    public XunitSerializableExpression()
    {
        JsonSerializer jsonSerializer = new JsonSerializer();
        _expressionSerializer = new ExpressionSerializer(jsonSerializer);
    }


    /// <inheritdoc />
    public void Deserialize(IXunitSerializationInfo info)
    {
        Value = _expressionSerializer.DeserializeText(info.GetValue<string>(nameof(Value))) as Expression<Func<T, bool>>;
    }

    /// <inheritdoc />
    public void Serialize(IXunitSerializationInfo info) => info.AddValue(nameof(Value), _expressionSerializer.SerializeText(Value));


    public static implicit operator Expression<Func<T, bool>>(XunitSerializableExpression<T> expressionSerializable) => expressionSerializable.Value;

    public static implicit operator XunitSerializableExpression<T>(Expression<Func<T, bool>> expression) => new() { Value = expression };
}