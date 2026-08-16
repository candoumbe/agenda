using System;
using Agenda.API.TypeMappers;
using AwesomeAssertions;
using Candoumbe.Types.Numerics;
using NJsonSchema;
using NJsonSchema.Generation;
using NJsonSchema.Generation.TypeMappers;
using Xunit;

namespace Agenda.API.UnitTests.TypeMappers
{
    public class NumberTypeMapperShould
    {

        public static TheoryData<ITypeMapper, (Type mapperType, JsonObjectType type, string format, decimal? min, decimal? max)> NumberTypeMapperCases
            => new()
            {
                {
                    new NumberTypeMapper<NonNegativeInteger, int>(),
                    (typeof(NonNegativeInteger), JsonObjectType.Integer, JsonFormatStrings.Integer, NonNegativeInteger.MinValue.Value, NonNegativeInteger.MaxValue.Value)
                },
                {
                    new NumberTypeMapper<PositiveInteger, int>(),
                    (typeof(PositiveInteger), JsonObjectType.Integer, JsonFormatStrings.Integer, PositiveInteger.MinValue.Value, PositiveInteger.MaxValue.Value)
                },
                {
                    new NumberTypeMapper<PositiveLong, long>(),
                    (typeof(PositiveLong), JsonObjectType.Number, JsonFormatStrings.Long, PositiveLong.MinValue.Value, PositiveLong.MaxValue.Value)
                }
            };

        [Theory]
        [MemberData(nameof(NumberTypeMapperCases))]
        public void Generate_expected_informations(ITypeMapper sut, (Type mapperType, JsonObjectType type, string format, decimal? min, decimal? max) expected)
        {
            // Arrange
            var rootObject = new { };

            SystemTextJsonSchemaGeneratorSettings schemaGeneratorSettings = new ();
            JsonSchemaResolver schemaResolver = new(rootObject, schemaGeneratorSettings);
            JsonSchemaGenerator schemaGenerator = new JsonSchemaGenerator(schemaGeneratorSettings);
            JsonSchema schema = new ();
            TypeMapperContext context = new (expected.mapperType, schemaGenerator, schemaResolver, []);

            // Act
            sut.GenerateSchema(schema, context);

            // Assert
            sut.MappedType.Should().Be(expected.mapperType);
            sut.UseReference.Should().BeFalse();

            schema.Type.Should().Be(expected.type);
            schema.Format.Should().Be(expected.format);
            schema.Minimum.Should().Be(expected.min);
            schema.Maximum.Should().Be(expected.max);
        }
    }
}