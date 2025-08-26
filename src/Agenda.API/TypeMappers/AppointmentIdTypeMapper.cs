using System.Numerics;
using Candoumbe.Types.Numerics;
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


    /// <summary>
    /// Mappers for <see cref="Number{TNumber}"/>s.
    /// </summary>
    /// <typeparam name="TValue">Type of the underlying number</typeparam>
    /// <typeparam name="TNumber"></typeparam>
    public class NumberTypeMapper<TNumber, TValue> : ITypeMapper
        where TNumber : Number<TValue>, IMinMaxValue<TNumber>
        where TValue : IComparable<TValue>, IMinMaxValue<TValue>
    {
        /// <inheritdoc />
        void ITypeMapper.GenerateSchema(JsonSchema schema, TypeMapperContext context)
        {
            switch (typeof(TNumber))
            {
                case var type when type == typeof(long):
                    (schema.Type, schema.Format, schema.Minimum, schema.Maximum) = (JsonObjectType.Number, JsonFormatStrings.Long, Convert.ToDecimal(TNumber.MinValue), Convert.ToDecimal(TNumber.MaxValue));
                    break;
                case var type when type == typeof(int):
                    (schema.Type, schema.Format, schema.Minimum, schema.Maximum) = (JsonObjectType.Integer, JsonFormatStrings.Integer, Convert.ToDecimal(TNumber.MinValue), Convert.ToDecimal(TNumber.MaxValue));
                    break;
                default:
                    (schema.Type, schema.Format) = (JsonObjectType.Number, JsonFormatStrings.Integer);
                    break;
            }
        }

        /// <inheritdoc />
        Type ITypeMapper.MappedType => typeof(TNumber);

        /// <inheritdoc />
        bool ITypeMapper.UseReference => false;
    }
}