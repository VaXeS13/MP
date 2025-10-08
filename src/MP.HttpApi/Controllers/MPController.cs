using MP.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace MP.Controllers;

/* Inherit your controllers from this class.
 */
public abstract class MPController : AbpControllerBase
{
    protected MPController()
    {
        LocalizationResource = typeof(MPResource);
    }
}
