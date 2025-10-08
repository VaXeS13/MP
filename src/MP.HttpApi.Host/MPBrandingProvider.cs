using Microsoft.Extensions.Localization;
using MP.Localization;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Ui.Branding;

namespace MP;

[Dependency(ReplaceServices = true)]
public class MPBrandingProvider : DefaultBrandingProvider
{
    private IStringLocalizer<MPResource> _localizer;

    public MPBrandingProvider(IStringLocalizer<MPResource> localizer)
    {
        _localizer = localizer;
    }

    public override string AppName => _localizer["AppName"];
}
