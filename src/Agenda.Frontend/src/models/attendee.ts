export interface Attendee {
  /**
   * Unique identifier for the attendee.
   */
  id: string;

  /**
   * Name of the attendee
   */
  name: string;

  /**
   * Email address.
   */
  email: string | null;

  /**
   * Phone number.
   */
  phone: string | null;
}
