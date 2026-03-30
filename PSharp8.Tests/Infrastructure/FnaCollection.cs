using Xunit;

namespace PSharp8.Tests.Infrastructure;

/// <summary>
/// Defines the "Fna" xUnit collection, which shares a single
/// <see cref="FnaFixture"/> instance across all test classes that declare
/// <c>[Collection("Fna")]</c>.  The fixture is created once before the
/// first test in the collection and disposed after the last.
/// </summary>
[CollectionDefinition("Fna")]
public class FnaCollection : ICollectionFixture<FnaFixture> { }
