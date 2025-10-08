using MP.Localization;
using Volo.Abp.Application.Services;

namespace MP;

/* Inherit your application services from this class.
 */
public abstract class MPAppService : ApplicationService
{
    protected MPAppService()
    {
        LocalizationResource = typeof(MPResource);
    }
}
