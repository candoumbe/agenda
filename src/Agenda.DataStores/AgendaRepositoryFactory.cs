
using Candoumbe.DataAccess.Abstractions;
using Candoumbe.DataAccess.EFStore;

namespace Agenda.DataStores;
public class AgendaRepositoryFactory : IRepositoryFactory<AgendaDataStore>
{
    public IRepository<TEntity> NewRepository<TEntity>(AgendaDataStore dbContext) where TEntity : class
    {
        return new EntityFrameworkRepository<TEntity, AgendaDataStore>(dbContext);
    }
}