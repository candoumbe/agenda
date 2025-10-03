using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using StronglyTypedIds;

namespace Agenda.Ids;

/// <summary>
/// A strongly typed attendee identifier.
/// </summary>
[StronglyTypedId("guid-v7", "guid-efcore")]
public partial struct AttendeeId;