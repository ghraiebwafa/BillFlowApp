using Xunit;

namespace BillFlow.ManagementService.Tests;

[CollectionDefinition("ManagementApi")]
public sealed class ManagementApiCollection : ICollectionFixture<ManagementApiFixture>;
