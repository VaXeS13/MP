using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace MP.Domain.Data;

/* This is used if database provider does't define
 * IMPDbSchemaMigrator implementation.
 */
public class NullMPDbSchemaMigrator : IMPDbSchemaMigrator, ITransientDependency
{
    public Task MigrateAsync()
    {
        return Task.CompletedTask;
    }
}
