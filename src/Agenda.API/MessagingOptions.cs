namespace Agenda.API
{
    /// <summary>
    /// Messaging options.
    /// </summary>
    public record MessagingOptions
    {
        /// <summary>
        /// Outbox tablename
        /// </summary>
        public string OutboxTablename { get; set; }
    }
}