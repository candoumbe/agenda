using System.ComponentModel;
using Nuke.Common.Tooling;

[TypeConverter(typeof(Enumeration.TypeConverter<DataStoreProvider>))]
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
    
    /// <summary>
    /// Implicit cast from <see cref="DataStoreProvider"/> to <see />
    /// </summary>
    /// <param name="provider"></param>
    /// <returns></returns>
    public static implicit operator string(DataStoreProvider provider) => provider.Value;
}