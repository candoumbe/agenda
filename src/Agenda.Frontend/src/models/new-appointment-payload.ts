import { Attendee } from './attendee';

/** Payload expected by POST /appointments. */
export interface NewAppointmentPayload {
  subject: string;
  location: string;
  startDate: string;
  endDate: string;
  attendees: Omit<Attendee, 'id'>[];
}
