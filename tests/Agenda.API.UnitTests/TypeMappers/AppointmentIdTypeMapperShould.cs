using System;
using Agenda.API.TypeMappers;
using Agenda.Ids;
using FluentAssertions;
using NJsonSchema;
using NJsonSchema.Generation;
using NJsonSchema.Generation.TypeMappers;
using Xunit;

namespace Agenda.API.UnitTests.TypeMappers
{
    public class AppointmentIdTypeMapperShould
    {
        private readonly ITypeMapper _sut = new StronglyTypedIdMapper<AppointmentId, Guid>();

        [Fact]
        public void Generate_expected_informations()
        {
            // Arrange
            var rootObject = new { };
            JsonSchemaGeneratorSettings schemaGeneratorSettings = new SystemTextJsonSchemaGeneratorSettings();
            JsonSchemaGenerator schemaGenerator = new JsonSchemaGenerator(schemaGeneratorSettings);
            JsonSchemaResolver schemaResolver = new(rootObject, schemaGeneratorSettings);
            TypeMapperContext context = new(typeof(AppointmentId), schemaGenerator, schemaResolver, []);
            JsonSchema schema = new();

            // Act
            _sut.GenerateSchema(schema, context);

            // Assert
            _sut.MappedType.Should().Be<AppointmentId>();
            _sut.UseReference.Should().BeFalse();

            schema.Type.Should().Be(JsonObjectType.String);
            schema.Format.Should().Be(JsonFormatStrings.Guid);
        }
    }
}