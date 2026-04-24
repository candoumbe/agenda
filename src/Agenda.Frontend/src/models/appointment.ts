import { Attendee } from './attendee';

/**
 * Representation of an appointment.
 */
export interface Appointment {
  /** Unique identifier for the appointment. */
  id: string;

  /** Subject of the appointment. */
  subject: string;

  /** Location of the appointment. */
  location: string;

  /** Start and end date for the appointment. */
  startDate: string;

  /** End date for the appointment. */
  endDate: string;

  /** Attendees for the appointment. */
  attendees: Attendee[];
}
