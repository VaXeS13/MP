using MP.Samples;
using Xunit;

namespace MP.EntityFrameworkCore.Applications;

[Collection(MPTestConsts.CollectionDefinitionName)]
public class EfCoreSampleAppServiceTests : SampleAppServiceTests<MPEntityFrameworkCoreTestModule>
{

}
