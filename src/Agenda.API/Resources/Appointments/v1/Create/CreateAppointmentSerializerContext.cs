using System.Text.Json.Serialization;

namespace Agenda.API.Resources.Appointments.v1.Create
{
        [JsonSerializable(typeof(NewAppointmentInfo))]
        [JsonSerializable(typeof(Browsable<AppointmentInfo>))]
        public partial class CreateAppointmentSerializerContext : JsonSerializerContext
        {

        }
}