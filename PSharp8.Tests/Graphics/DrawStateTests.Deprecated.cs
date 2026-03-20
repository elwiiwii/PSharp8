/*using FluentAssertions;
using PSharp8.Graphics;
using PSharp8.Tests.Infrastructure;
using Xunit;

namespace PSharp8.Tests.Graphics;

[Collection("Graphics")]
public class DrawStateTests(GraphicsFixture fixture) : GraphicsTestBase(fixture)
{
	[Fact]
	public void DrawState_DoesNotExposePrintStateProperty_GivenPrintSessionOwnership()
	{
		var printStateProperty = typeof(DrawState).GetProperty("PrintState");

		printStateProperty.Should().BeNull();
	}

	[Fact]
	public void DrawState_ExposesPrintSessionProperty_GivenPrintSessionOwnership()
	{
		var printSessionProperty = typeof(DrawState).GetProperty("PrintSession");

		printSessionProperty.Should().NotBeNull();
	}

	[Fact]
	public void DrawState_ExposesBeginPrintMethod_GivenPrintLifecycleIsOwnedByDrawState()
	{
		var beginPrintMethod = typeof(DrawState).GetMethod("BeginPrint");

		beginPrintMethod.Should().NotBeNull();
	}

	[Fact]
	public void DrawState_ExposesEndPrintMethod_GivenPrintLifecycleIsOwnedByDrawState()
	{
		var endPrintMethod = typeof(DrawState).GetMethod("EndPrint");

		endPrintMethod.Should().NotBeNull();
	}

	[Fact]
	public void DrawState_ExposesTryHandlePrintControlCodeMethod_GivenControlCodeHandlingIsOwnedByDrawState()
	{
		var tryHandleMethod = typeof(DrawState).GetMethod("TryHandlePrintControlCode");

		tryHandleMethod.Should().NotBeNull();
	}
}*/