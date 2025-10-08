using System.Threading.Tasks;

namespace MP.Domain.Data;

public interface IMPDbSchemaMigrator
{
    Task MigrateAsync();
}
