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
        /// Create a new <see cref="AttendeeId"/>.
        /// </summary>
        /// <returns></returns>
        public static AttendeeId New() => new AttendeeId(Guid.NewGuid());

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