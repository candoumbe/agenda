using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Text.Json;
using Fluxera.StronglyTypedId.SystemTextJson;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using Remote.Linq;
using Remote.Linq.Text.Json;
using Xunit.Sdk;

namespace Agenda.API.UnitTests.Helpers;

public class XunitSerializableExpression<T>: IXunitSerializable
{
    public Expression<Func<T, bool>> Value { get; set; }

    private readonly JsonSerializerOptions _serializerSettings;
    private readonly ExpressionTranslatorContext _expressionTranslatorContext;

    public XunitSerializableExpression()
    {

        _serializerSettings = new JsonSerializerOptions().ConfigureRemoteLinq();
        _serializerSettings.UseStronglyTypedId();
        _serializerSettings.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
        _expressionTranslatorContext = new ExpressionTranslatorContext(valueMapper: new KnownDynamicObjectMapper());
    }

    public void Deserialize(IXunitSerializationInfo info)
    {
        Console.WriteLine(info.GetValue<string>(nameof(Value)));
        Value = (Expression<Func<T, bool>>) JsonSerializer.Deserialize<Remote.Linq.Expressions.LambdaExpression>(info.GetValue<string>(nameof(Value)), _serializerSettings).ToLinqExpression(_expressionTranslatorContext)!;
    }

    public void Serialize(IXunitSerializationInfo info) =>
        info.AddValue(nameof(Value), JsonSerializer.Serialize(Value.ToRemoteLinqExpression(_expressionTranslatorContext), _serializerSettings));


    public static implicit operator Expression<Func<T, bool>>(XunitSerializableExpression<T> expressionSerializable) => expressionSerializable.Value;

    public static implicit operator XunitSerializableExpression<T>(Expression<Func<T, bool>> expression) => new() { Value = expression };
}