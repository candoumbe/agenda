using System;
using Aqua.Dynamic;
using NodaTime;

namespace Agenda.API.UnitTests.Helpers
{
    internal class KnownDynamicObjectMapper() : DynamicObjectMapper(isKnownTypeProvider: new KnownType())
    {
        /// <inheritdoc />
        class KnownType : IIsKnownTypeProvider
        {
            public bool IsKnownType(Type type) => type?.FullName?.StartsWith("NodaTime", StringComparison.Ordinal) is true;
        }

        /// <inheritdoc />
        protected override DynamicObject MapToDynamicObjectGraph(object obj, Func<Type, bool> setTypeInformation)
        {
            switch (obj)
            {
                case Instant instant:
                    long ticks = instant.ToUnixTimeTicks();
                    return new DynamicObject(new PropertySet
                    {
                        { "ticks", ticks }
                    });
                default:
                    return base.MapToDynamicObjectGraph(obj, setTypeInformation);
            }
        }

        protected override object MapFromDynamicObjectGraph(object obj, Type targetType)
        {
            if (targetType == typeof(Instant) && obj is DynamicObject instant)
            {
                long ticks = instant.Get<long>("ticks");
                return Instant.FromUnixTimeTicks(ticks);
            }

            return base.MapFromDynamicObjectGraph(obj, targetType);
        }
    }
}