
using Candoumbe.DataAccess.Abstractions;
using Candoumbe.DataAccess.EFStore;

namespace Agenda.DataStores;

/// <summary>
/// Factory for <see cref="AgendaDataStore"/>
/// </summary>
public class AgendaRepositoryFactory : IRepositoryFactory<AgendaDataStore>
{
    /// <inheritdoc />
    public IRepository<TEntity> NewRepository<TEntity>(AgendaDataStore dbContext) where TEntity : class
    {
        return new EntityFrameworkRepository<TEntity, AgendaDataStore>(dbContext);
    }
}