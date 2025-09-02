using System;
using Fluxera.StronglyTypedId;
using NJsonSchema;
using NJsonSchema.Generation.TypeMappers;

namespace Agenda.API.TypeMappers
{
    /// <summary>
    /// Mappers for strongly typed <see cref="StronglyTypedId{TStronglyTypedId,TValue}"/>s.
    /// </summary>
    /// <typeparam name="TValue">Type of the underlying value</typeparam>
    /// <typeparam name="TStronglyTypedId">Type of the strongly typed id to map.</typeparam>
    public class StronglyTypedIdMapper<TStronglyTypedId, TValue> : ITypeMapper
        where TStronglyTypedId : StronglyTypedId<TStronglyTypedId, TValue> where TValue : IComparable, IComparable<TValue>, IEquatable<TValue>
    {
        /// <inheritdoc />
        void ITypeMapper.GenerateSchema(JsonSchema schema, TypeMapperContext context)
        {
            (schema.Type, schema.Format) = typeof(TValue) switch
            {
                var type when type == typeof(long) => (JsonObjectType.Number, JsonFormatStrings.Long),
                var type when type == typeof(Guid) => (JsonObjectType.String, JsonFormatStrings.Guid),
                _                                  => (JsonObjectType.String, null)
            };
        }

        Type ITypeMapper.MappedType => typeof(TStronglyTypedId);

        /// <inheritdoc />
        bool ITypeMapper.UseReference => false;
    }
}