using Xunit;

namespace MP.EntityFrameworkCore;

[CollectionDefinition(MPTestConsts.CollectionDefinitionName)]
public class MPEntityFrameworkCoreCollection : ICollectionFixture<MPEntityFrameworkCoreFixture>
{

}
