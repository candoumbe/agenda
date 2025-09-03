using System;
using System.Linq.Expressions;
using System.Text.Json;
using Remote.Linq;
using Remote.Linq.Text.Json;
using Xunit.Sdk;

namespace Agenda.API.UnitTests.Helpers;

public class XunitSerializableExpression<T>() : IXunitSerializable
{
    public Expression<Func<T, bool>> Value { get; set; }

    private static readonly JsonSerializerOptions s_serializerSettings = new JsonSerializerOptions().ConfigureRemoteLinq();

    public void Deserialize(IXunitSerializationInfo info)
    {
        Value = JsonSerializer.Deserialize<Expression<Func<T, bool>>>(info.GetValue<string>(nameof(Value)), s_serializerSettings)!;
    }

    public void Serialize(IXunitSerializationInfo info)
    {
        info.AddValue(nameof(Value), JsonSerializer.Serialize(Value.ToRemoteLinqExpression(), s_serializerSettings));
    }

    public static implicit operator Expression<Func<T, bool>>(XunitSerializableExpression<T> expressionSerializable) => expressionSerializable.Value;

    public static implicit operator XunitSerializableExpression<T>(Expression<Func<T, bool>> expression) => new() { Value = expression };
}