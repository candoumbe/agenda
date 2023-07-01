﻿namespace Agenda.API.Resources.Appointments
{
    /// <summary>
    /// Base class for search requests
    /// </summary>
    public abstract record AbstractSearchRequest<T>
    {
        /// <summary>
        /// Index of the page
        /// </summary>
        public int Page { get; init; }

        /// <summary>
        /// Defines the number of items result set will contain at most.
        /// </summary>
        /// <remarks>
        /// This value is just a hint that the server may not fullfill.
        /// </remarks>
        public int PageSize { get; init; }

        /// <summary>
        /// Directive on how to sort results
        /// </summary>
        public string Sort { get; init; }
    }
}
