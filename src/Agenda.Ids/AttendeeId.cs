using System;
using System.Diagnostics.CodeAnalysis;
using Fluxera.StronglyTypedId;

namespace Agenda.Ids
{
    public class AttendeeId : StronglyTypedId<AttendeeId, Guid>
    {
        ///<inheritdoc/>
        public AttendeeId(Guid value) : base(value)
        {
        }

        /// <summary>
        /// Creates a new <see cref="AttendeeId"/>.
        /// </summary>
        /// <returns></returns>
#if NET
        public static AttendeeId New() => new AttendeeId(Guid.CreateVersion7());
#else
        public static AttendeeId New() => new AttendeeId(Guid.NewGuid());
#endif

#if NETSTANDARD2_1
        public static bool TryParse(string input, [NotNullWhen(true)] out AttendeeId output)
        {
            output = null;
            bool parsed = false;

            if (Guid.TryParse(input, out Guid result) && result != Guid.Empty)
            {
                output = new AttendeeId(result);
                parsed = true;
            }

            return parsed;
        }
#endif
    }

}