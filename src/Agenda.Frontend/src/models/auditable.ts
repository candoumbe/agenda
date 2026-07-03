
/**
 * Represents an entity that can be audited for changes.
 */
export interface Auditable {
  /** The date and time when the entity was created. */
  createdAt: Date;

  /** The user who created the entity. */
  createdBy: string | undefined;

  /** The date and time when the entity was last updated. */
  updatedAt?: Date | undefined;

  /** The user who last updated the entity. */
  updatedBy?: string | undefined;
}
