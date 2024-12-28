namespace Agenda.API.Resources.Appointments.v1.Update;

using NodaTime;

public class PatchAppointmentRequest
{
    public string Location { get; set; }

    public string Subject { get; set; }

    public ZonedDateTime? StartDate { get; set; }

    public ZonedDateTime? EndDate { get; set; }
}