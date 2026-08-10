#nullable enable
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using Agenda.API.TypeMappers;
using AwesomeAssertions;
using Candoumbe.Types.Numerics;
using FakeItEasy;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using NJsonSchema.Generation;
using Xunit;
using Xunit.OpenCategories.V3;

namespace Agenda.API.UnitTests.TypeMappers;

[UnitTest]
public class NumberTypeSchemaTransformerShould
{
    public static TheoryData<IOpenApiSchemaTransformer, (Type mapperType, JsonSchemaType type, string? format, string min, string max)> SchemaTransformerCases
        => new()
        {
            {
                new NumberTypeSchemaTransformer<PositiveInteger, int>(),
                (
                    typeof(PositiveInteger),
                    JsonSchemaType.Integer,
                    null,
                    PositiveInteger.MinValue.Value.ToString(),
                    PositiveInteger.MaxValue.Value.ToString()
                )
            },
            {
                new NumberTypeSchemaTransformer<NonNegativeInteger, int>(),
                (
                    typeof(NonNegativeInteger),
                    JsonSchemaType.Integer,
                    null,
                    NonNegativeInteger.MinValue.Value.ToString(),
                    NonNegativeInteger.MaxValue.Value.ToString()
                )
            }
        };

    [Theory]
    [MemberData(nameof(SchemaTransformerCases))]
    public async Task Generate_expected_informations(IOpenApiSchemaTransformer sut, (Type mapperType, JsonSchemaType type, string? format, string min, string max) expected)
    {
        // Arrange
        OpenApiSchema schema = new();
        OpenApiSchemaTransformerContext context = CreateContext(expected.mapperType);

        // Act
        await sut.TransformAsync(schema, context, TestContext.Current.CancellationToken);

        // Assert
        schema.Type.Should().Be(expected.type);
        schema.Format.Should().Be(expected.format);
        schema.Minimum.Should().Be(expected.min);
        schema.Maximum.Should().Be(expected.max);
    }

    private static OpenApiSchemaTransformerContext CreateContext(Type mappedType)
    {
        JsonSerializerOptions serializerOptions = new()
        {
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        };

        JsonTypeInfo jsonTypeInfo = serializerOptions.GetTypeInfo(mappedType);

        OpenApiSchemaTransformerContext context = new()
        {
            DocumentName = "v1",
            ParameterDescription = null,
            JsonPropertyInfo = null,
            ApplicationServices = A.Fake<IServiceProvider>(),
            JsonTypeInfo = jsonTypeInfo
        };

        return context;
    }
}