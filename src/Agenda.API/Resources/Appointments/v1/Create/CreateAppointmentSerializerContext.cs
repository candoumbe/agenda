using System.Text.Json.Serialization;
using FastEndpoints;

namespace Agenda.API.Resources.Appointments.v1.Create
{
        [JsonSerializable(typeof(NewAppointmentInfo))]
        [JsonSerializable(typeof(Browsable<AppointmentInfo>))]
        [JsonSerializable(typeof(ProblemDetails))]
        public partial class CreateAppointmentSerializerContext : JsonSerializerContext
        {

        }
}