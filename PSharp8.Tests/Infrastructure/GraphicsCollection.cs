using Xunit;

namespace PSharp8.Tests.Infrastructure;

/// <summary>
/// Defines the "Graphics" xUnit collection, which shares a single
/// <see cref="GraphicsFixture"/> instance across all test classes that declare
/// <c>[Collection("Graphics")]</c>.  The fixture is created once before the
/// first test in the collection and disposed after the last.
/// </summary>
[CollectionDefinition("Graphics")]
public class GraphicsCollection : ICollectionFixture<GraphicsFixture> { }
