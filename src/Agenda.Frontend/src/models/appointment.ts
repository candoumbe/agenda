import { Attendee } from './attendee';
import { Auditable } from './auditable';

/**
 * Representation of an appointment.
 */
export interface Appointment extends Auditable {
  /** Unique identifier for the appointment. */
  id: string;

  /** Subject of the appointment. */
  subject: string;

  /** Location of the appointment. */
  location: string;

  /** Start and end date for the appointment. */
  startDate: Date;

  /** End date for the appointment. */
  endDate: Date;

  /** Attendees for the appointment. */
  attendees: Attendee[];
}
