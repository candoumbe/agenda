using System;
using System.Text.Json;
using StronglyTypedIds;


namespace Agenda.Ids;

/// <summary>
/// A strongly typed appointment identifier.
/// </summary>
[StronglyTypedId("guid-v7", "guid-efcore")]
// ReSharper disable once StructCanBeMadeReadOnly
public partial struct AppointmentId;