namespace Agenda.Pipelines;

using Nuke.Common.Tooling;

using System.ComponentModel;

[TypeConverter(typeof(TypeConverter<DataStoreProvider>))]
public class DataStoreProvider : Enumeration
{
    /// <summary>
    /// Sqlite database engine
    /// </summary>
    public static readonly DataStoreProvider Sqlite = new() { Value = nameof(Sqlite) };

    /// <summary>
    /// Postgres database engine
    /// </summary>
    public static readonly DataStoreProvider Postgres = new() { Value = nameof(Postgres) };

    ///<inheritdoc/>
    public static implicit operator string(DataStoreProvider provider) => provider.Value;
}