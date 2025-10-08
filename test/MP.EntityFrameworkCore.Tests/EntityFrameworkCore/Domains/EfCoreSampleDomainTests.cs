using MP.Samples;
using Xunit;

namespace MP.EntityFrameworkCore.Domains;

[Collection(MPTestConsts.CollectionDefinitionName)]
public class EfCoreSampleDomainTests : SampleDomainTests<MPEntityFrameworkCoreTestModule>
{

}
