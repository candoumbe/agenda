namespace Agenda.DataStores;

using Candoumbe.DataAccess.Abstractions;
using Candoumbe.DataAccess.EFStore;

public class AgendaRepositoryFactory : IRepositoryFactory<AgendaDataStore>
{
    public IRepository<TEntity> NewRepository<TEntity>(AgendaDataStore dbContext) where TEntity : class
    {
        return new EntityFrameworkRepository<TEntity, AgendaDataStore>(dbContext);
    }
}