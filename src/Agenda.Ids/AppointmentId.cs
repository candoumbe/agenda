using System;
using System.Diagnostics.CodeAnalysis;
using Fluxera.StronglyTypedId;

namespace Agenda.Ids
{
    public class AppointmentId : StronglyTypedId<AppointmentId, Guid>
    {
        ///<inheritdoc/>
        public AppointmentId(Guid value) : base(value)
        {
        }

        public static AppointmentId New() => new AppointmentId(Guid.NewGuid());

#if NETSTANDARD2_1
        /// <summary>
        /// Try to parse <paramref name="input"/> in order to produce <paramref name="output"/>
        /// </summary>
        /// <param name="input">The input to parse</param>
        /// <param name="output">The output</param>
        /// <returns><see langword="true"/> when <paramref name="input"/> was successfully parsed and <see langword="false"/> otherwise.</returns>
        public static bool TryParse(string input, [NotNullWhen(true)] out AppointmentId output)
        {
            output = null;
            bool parsed = false;

            if (Guid.TryParse(input, out Guid result) && result != Guid.Empty)
            {
                output = new AppointmentId(result);
                parsed = true;
            }

            return parsed;
        }
#endif
    }

}