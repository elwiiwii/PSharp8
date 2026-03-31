using FluentAssertions;
using Moq;
using PSharp8.Audio;
using PSharp8.Input;
using PSharp8.Scene;
using Xunit;

namespace PSharp8.Tests.Scene;

public class SceneManagerTests
{

    // --------------------------------------------------------------
    #region Helpers
    // --------------------------------------------------------------

    private static SceneManager BuildWithScene(Action<ISceneSetup> configure)
    {
        var mock = new Mock<IInputManager>();
        var sut = new SceneManager(mock.Object);
        sut.ScheduleScene(() => new TestScene(configure));
        sut.InternalUpdate(TimeSpan.Zero); // applies the queued transition
        return sut;
    }

    private sealed class TestScene : IScene
    {
        private readonly Action<ISceneSetup> _configure;

        public TestScene(Action<ISceneSetup> configure) => _configure = configure;

        public string? Name => null;
        public void Init(ISceneSetup setup) => _configure(setup);
        public string? SpritesPath => null;
        public string? MapPath => null;
        public string? FlagDataPath => null;
        public IReadOnlyList<Soundtrack> Music => [];
        public IReadOnlyList<SfxPack> Sfx => [];
    }

    private static SceneManager BuildWithPushedScene(
        Action<ISceneSetup> configureA,
        Action<ISceneSetup> configureB)
    {
        var mock = new Mock<IInputManager>();
        var sut = new SceneManager(mock.Object);
        sut.ScheduleScene(() => new TestScene(configureA));
        sut.InternalUpdate(TimeSpan.Zero); // applies scene A
        sut.PushScene(() => new TestScene(configureB));
        sut.InternalUpdate(TimeSpan.Zero); // applies the push
        return sut;
    }

    // --------------------------------------------------------------
    #endregion
    #region Constructor
    // --------------------------------------------------------------

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenInputManagerIsNull()
    {
        var act = () => new SceneManager(inputManager: null!);

        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("inputManager");
    }

    // --------------------------------------------------------------
    #endregion
    #region Single callback
    // --------------------------------------------------------------

    [Fact]
    public void InternalUpdate_FiresCallback_WhenElapsedReachesInterval()
    {
        var callCount = 0;
        var sut = BuildWithScene(setup => setup.RegisterUpdate(() => callCount++, fps: 10));

        sut.InternalUpdate(TimeSpan.FromMilliseconds(100));

        callCount.Should().Be(1);
    }

    [Fact]
    public void InternalUpdate_DoesNotFireCallback_WhenElapsedBelowInterval()
    {
        var callCount = 0;
        var sut = BuildWithScene(setup => setup.RegisterUpdate(() => callCount++, fps: 10));

        sut.InternalUpdate(TimeSpan.FromMilliseconds(99));

        callCount.Should().Be(0);
    }

    [Fact]
    public void InternalUpdate_AccumulatesAcrossMultipleTicks()
    {
        var callCount = 0;
        var sut = BuildWithScene(setup => setup.RegisterUpdate(() => callCount++, fps: 10));

        sut.InternalUpdate(TimeSpan.FromMilliseconds(60)); // accumulated: 60ms — no fire
        sut.InternalUpdate(TimeSpan.FromMilliseconds(60)); // accumulated: 120ms — fires once

        callCount.Should().Be(1);
    }

    [Fact]
    public void InternalUpdate_FiresCallbackTwice_WhenElapsedCoversMultipleIntervals()
    {
        var callCount = 0;
        var sut = BuildWithScene(setup => setup.RegisterUpdate(() => callCount++, fps: 10));

        sut.InternalUpdate(TimeSpan.FromMilliseconds(250)); // 200ms consumed, 50ms remainder

        callCount.Should().Be(2);
    }

    [Fact]
    public void InternalUpdate_DoesNotFireCallback_BeforeSceneIsApplied()
    {
        var callCount = 0;
        var mock = new Mock<IInputManager>();
        var sut = new SceneManager(mock.Object);

        // Queue a scene but never call InternalUpdate — scene never becomes active
        sut.ScheduleScene(() => new TestScene(setup => setup.RegisterUpdate(() => callCount++, fps: 10)));

        callCount.Should().Be(0);
    }

    // --------------------------------------------------------------
    #endregion
    #region Multiple callbacks
    // --------------------------------------------------------------

    [Fact]
    public void InternalUpdate_TwoCallbacksAtDifferentFps_FireIndependently()
    {
        var aCount = 0;
        var bCount = 0;
        var sut = BuildWithScene(setup =>
        {
            setup.RegisterUpdate(() => aCount++, fps: 10); // fires every 100ms
            setup.RegisterUpdate(() => bCount++, fps: 5);  // fires every 200ms
        });

        sut.InternalUpdate(TimeSpan.FromMilliseconds(100)); // A fires (total: A=1, B=0)
        aCount.Should().Be(1);
        bCount.Should().Be(0);

        sut.InternalUpdate(TimeSpan.FromMilliseconds(100)); // A fires again, B fires once (total: A=2, B=1)
        aCount.Should().Be(2);
        bCount.Should().Be(1);
    }

    // --------------------------------------------------------------
    #endregion
    #region Schedule/Push/Pop behavior
    // --------------------------------------------------------------

    [Fact]
    public void ScheduleScene_SwitchesToNewScene_WhenTickedAfterSchedule()
    {
        var aCount = 0;
        var bCount = 0;
        var sut = BuildWithScene(setup => setup.RegisterUpdate(() => aCount++, fps: 10));

        sut.ScheduleScene(() => new TestScene(setup => setup.RegisterUpdate(() => bCount++, fps: 10)));
        sut.InternalUpdate(TimeSpan.FromMilliseconds(100));

        aCount.Should().Be(0);
        bCount.Should().Be(1);
    }

    [Fact]
    public void ScheduleScene_ResetsAccumulator_WhenReplacingScene()
    {
        var bCount = 0;
        var sut = BuildWithScene(setup => setup.RegisterUpdate(() => { }, fps: 10));

        sut.InternalUpdate(TimeSpan.FromMilliseconds(60)); // accumulates 60ms — does not fire
        sut.ScheduleScene(() => new TestScene(setup => setup.RegisterUpdate(() => bCount++, fps: 10)));
        sut.InternalUpdate(TimeSpan.FromMilliseconds(60)); // applies B; B has 60ms — below 100ms threshold

        bCount.Should().Be(0);
    }

    [Fact]
    public void ScheduleScene_ClearsEntireStack_WhenPushedScenesExist()
    {
        var aCount = 0;
        var bCount = 0;
        var cCount = 0;
        var sut = BuildWithPushedScene(
            configureA: setup => setup.RegisterUpdate(() => aCount++, fps: 10),
            configureB: setup => setup.RegisterUpdate(() => bCount++, fps: 10));

        sut.ScheduleScene(() => new TestScene(setup => setup.RegisterUpdate(() => cCount++, fps: 10)));
        sut.InternalUpdate(TimeSpan.FromMilliseconds(100));

        cCount.Should().Be(1);
        aCount.Should().Be(0);
        bCount.Should().Be(0);
    }

    [Fact]
    public void PushScene_NewSceneFires_WhenAtTopOfStack()
    {
        var bCount = 0;
        var sut = BuildWithPushedScene(
            configureA: setup => setup.RegisterUpdate(() => { }, fps: 10),
            configureB: setup => setup.RegisterUpdate(() => bCount++, fps: 10));

        sut.InternalUpdate(TimeSpan.FromMilliseconds(100));

        bCount.Should().Be(1);
    }

    [Fact]
    public void PushScene_IsDeferred_UntilNextTick()
    {
        var bCount = 0;
        var mock = new Mock<IInputManager>();
        var sut = new SceneManager(mock.Object);
        sut.ScheduleScene(() => new TestScene(setup => setup.RegisterUpdate(() => { }, fps: 10)));
        sut.InternalUpdate(TimeSpan.Zero); // applies scene A

        sut.PushScene(() => new TestScene(setup => setup.RegisterUpdate(() => bCount++, fps: 10)));
        // deliberately NOT ticking — push not yet applied

        bCount.Should().Be(0);
    }

    [Fact]
    public void PopScene_RestoresPreviousScene_AfterPopping()
    {
        var aCount = 0;
        var sut = BuildWithPushedScene(
            configureA: setup => setup.RegisterUpdate(() => aCount++, fps: 10),
            configureB: setup => setup.RegisterUpdate(() => { }, fps: 10));

        sut.PopScene();
        sut.InternalUpdate(TimeSpan.Zero);  // applies the pop
        sut.InternalUpdate(TimeSpan.FromMilliseconds(100));

        aCount.Should().Be(1);
    }

    [Fact]
    public void PopScene_PoppedSceneNoLongerFires()
    {
        var bCount = 0;
        var sut = BuildWithPushedScene(
            configureA: setup => setup.RegisterUpdate(() => { }, fps: 10),
            configureB: setup => setup.RegisterUpdate(() => bCount++, fps: 10));

        sut.PopScene();
        sut.InternalUpdate(TimeSpan.Zero);  // applies the pop
        sut.InternalUpdate(TimeSpan.FromMilliseconds(100));

        bCount.Should().Be(0);
    }

    [Fact]
    public void PopScene_ResetsRestoredSceneAccumulator()
    {
        var aCount = 0;
        var mock = new Mock<IInputManager>();
        var sut = new SceneManager(mock.Object);
        sut.ScheduleScene(() => new TestScene(setup => setup.RegisterUpdate(() => aCount++, fps: 10)));
        sut.InternalUpdate(TimeSpan.Zero);                  // applies scene A

        sut.InternalUpdate(TimeSpan.FromMilliseconds(60)); // A acc = 60ms — does not fire

        sut.PushScene(() => new TestScene(setup => setup.RegisterUpdate(() => { }, fps: 10)));
        sut.InternalUpdate(TimeSpan.Zero);                  // applies the push

        sut.PopScene();
        sut.InternalUpdate(TimeSpan.Zero);                  // applies the pop — A restored with reset accumulator

        sut.InternalUpdate(TimeSpan.FromMilliseconds(60)); // A has only 60ms since restore — below 100ms

        aCount.Should().Be(0);
    }

    // --------------------------------------------------------------
    #endregion
    #region PauseBehavior routing
    // --------------------------------------------------------------

    [Fact]
    public void InternalUpdate_SkipsBackgroundCallback_WhenPauseBehaviorIsPause()
    {
        var aCount = 0;
        var sut = BuildWithPushedScene(
            configureA: setup => setup.RegisterUpdate(() => aCount++, fps: 10, PauseBehavior.Pause),
            configureB: setup => setup.RegisterUpdate(() => { }, fps: 10));

        sut.InternalUpdate(TimeSpan.FromMilliseconds(100));

        aCount.Should().Be(0);
    }

    [Fact]
    public void InternalUpdate_FiresBackgroundCallback_WhenPauseBehaviorIsContinue()
    {
        var aCount = 0;
        var sut = BuildWithPushedScene(
            configureA: setup => setup.RegisterUpdate(() => aCount++, fps: 10, PauseBehavior.Continue),
            configureB: setup => setup.RegisterUpdate(() => { }, fps: 10));

        sut.InternalUpdate(TimeSpan.FromMilliseconds(100));

        aCount.Should().Be(1);
    }

    [Fact]
    public void InternalUpdate_DoesNotBlockInputs_WhenBackgroundPauseBehaviorIsContinue()
    {
        var mock = new Mock<IInputManager>();
        mock.SetupProperty(m => m.InputBlocked);
        var sut = new SceneManager(mock.Object);
        var capturedInputBlocked = false;
        sut.ScheduleScene(() => new TestScene(setup =>
            setup.RegisterUpdate(() => capturedInputBlocked = mock.Object.InputBlocked, fps: 10, PauseBehavior.Continue)));
        sut.InternalUpdate(TimeSpan.Zero); // applies scene A

        sut.PushScene(() => new TestScene(setup => setup.RegisterUpdate(() => { }, fps: 10)));
        sut.InternalUpdate(TimeSpan.Zero); // applies push

        sut.InternalUpdate(TimeSpan.FromMilliseconds(100));

        capturedInputBlocked.Should().BeFalse();
    }

    [Fact]
    public void InternalUpdate_FiresBackgroundCallback_WhenPauseBehaviorIsContinueWithoutInputs()
    {
        var aCount = 0;
        var sut = BuildWithPushedScene(
            configureA: setup => setup.RegisterUpdate(() => aCount++, fps: 10, PauseBehavior.ContinueWithoutInputs),
            configureB: setup => setup.RegisterUpdate(() => { }, fps: 10));

        sut.InternalUpdate(TimeSpan.FromMilliseconds(100));

        aCount.Should().Be(1);
    }

    [Fact]
    public void InternalUpdate_BlocksInputsDuringBackgroundCallback_WhenContinueWithoutInputs()
    {
        var mock = new Mock<IInputManager>();
        mock.SetupProperty(m => m.InputBlocked);
        var sut = new SceneManager(mock.Object);
        var capturedInputBlocked = false;
        sut.ScheduleScene(() => new TestScene(setup =>
            setup.RegisterUpdate(() => capturedInputBlocked = mock.Object.InputBlocked, fps: 10, PauseBehavior.ContinueWithoutInputs)));
        sut.InternalUpdate(TimeSpan.Zero); // applies scene A

        sut.PushScene(() => new TestScene(setup => setup.RegisterUpdate(() => { }, fps: 10)));
        sut.InternalUpdate(TimeSpan.Zero); // applies push

        sut.InternalUpdate(TimeSpan.FromMilliseconds(100));

        capturedInputBlocked.Should().BeTrue();
    }

    [Fact]
    public void InternalUpdate_RestoresInputsAfterBackgroundCallback_WhenContinueWithoutInputs()
    {
        var mock = new Mock<IInputManager>();
        mock.SetupProperty(m => m.InputBlocked);
        var sut = new SceneManager(mock.Object);
        sut.ScheduleScene(() => new TestScene(setup =>
            setup.RegisterUpdate(() => { }, fps: 10, PauseBehavior.ContinueWithoutInputs)));
        sut.InternalUpdate(TimeSpan.Zero); // applies scene A

        sut.PushScene(() => new TestScene(setup => setup.RegisterUpdate(() => { }, fps: 10)));
        sut.InternalUpdate(TimeSpan.Zero); // applies push

        sut.InternalUpdate(TimeSpan.FromMilliseconds(100));

        mock.Object.InputBlocked.Should().BeFalse();
    }

    [Fact]
    public void InternalUpdate_TopSceneFires_EvenIfPauseBehaviorIsPause()
    {
        var aCount = 0;
        var sut = BuildWithScene(setup => setup.RegisterUpdate(() => aCount++, fps: 10, PauseBehavior.Pause));

        sut.InternalUpdate(TimeSpan.FromMilliseconds(100));

        aCount.Should().Be(1);
    }

    // --------------------------------------------------------------
    #endregion
    #region Handle mutability
    // --------------------------------------------------------------

    [Fact]
    public void InternalUpdate_StopsFireCallback_WhenHandleDisabled()
    {
        var count = 0;
        IFunctionHandle? handle = null;
        var sut = BuildWithScene(setup =>
        {
            handle = setup.RegisterUpdate(() => count++, fps: 10);
        });

        sut.InternalUpdate(TimeSpan.FromMilliseconds(100)); // fires once
        count.Should().Be(1);

        handle!.Enabled = false;
        sut.InternalUpdate(TimeSpan.FromMilliseconds(100)); // disabled — skipped

        count.Should().Be(1);
    }

    [Fact]
    public void InternalUpdate_ResumesFireCallback_WhenHandleReEnabled()
    {
        var count = 0;
        IFunctionHandle? handle = null;
        var sut = BuildWithScene(setup =>
        {
            handle = setup.RegisterUpdate(() => count++, fps: 10);
        });

        handle!.Enabled = false;
        sut.InternalUpdate(TimeSpan.FromMilliseconds(100)); // disabled — no fire

        handle.Enabled = true;
        sut.InternalUpdate(TimeSpan.FromMilliseconds(100)); // re-enabled — fires

        count.Should().Be(1);
    }

    [Fact]
    public void InternalUpdate_FiresAtNewRate_WhenFpsIncreasedMidRun()
    {
        var count = 0;
        IFunctionHandle? handle = null;
        var sut = BuildWithScene(setup =>
        {
            handle = setup.RegisterUpdate(() => count++, fps: 5); // 200ms interval
        });

        sut.InternalUpdate(TimeSpan.FromMilliseconds(100)); // acc=100ms, below 200ms — no fire
        count.Should().Be(0);

        handle!.Fps = 10; // new interval = 100ms
        sut.InternalUpdate(TimeSpan.FromMilliseconds(100)); // acc=200ms; fires twice (200≥100, 100≥100)

        count.Should().Be(2);
    }

    [Fact]
    public void InternalUpdate_DoesNotFireEarly_WhenFpsDecreasedMidRun()
    {
        var count = 0;
        IFunctionHandle? handle = null;
        var sut = BuildWithScene(setup =>
        {
            handle = setup.RegisterUpdate(() => count++, fps: 10); // 100ms interval
        });

        sut.InternalUpdate(TimeSpan.FromMilliseconds(60)); // acc=60ms — no fire

        handle!.Fps = 2; // new interval = 500ms
        sut.InternalUpdate(TimeSpan.FromMilliseconds(60)); // acc=120ms, below 500ms — no fire

        count.Should().Be(0);
    }

    [Fact]
    public void InternalUpdate_StartsFireBackgroundCallback_WhenPauseBehaviorChangedToContinue()
    {
        IFunctionHandle? handleA = null;
        var aCount = 0;
        var mock = new Mock<IInputManager>();
        var sut = new SceneManager(mock.Object);
        sut.ScheduleScene(() => new TestScene(setup =>
        {
            handleA = setup.RegisterUpdate(() => aCount++, fps: 10, PauseBehavior.Pause);
        }));
        sut.InternalUpdate(TimeSpan.Zero); // applies scene A

        sut.PushScene(() => new TestScene(setup => setup.RegisterUpdate(() => { }, fps: 10)));
        sut.InternalUpdate(TimeSpan.Zero); // applies push; A is now background

        sut.InternalUpdate(TimeSpan.FromMilliseconds(100)); // A has Pause — skipped
        aCount.Should().Be(0);

        handleA!.PauseBehavior = PauseBehavior.Continue;
        sut.InternalUpdate(TimeSpan.FromMilliseconds(100)); // A now continues

        aCount.Should().Be(1);
    }

    // --------------------------------------------------------------
    #endregion
    #region Resolution deferral
    // --------------------------------------------------------------

    [Fact]
    public void Resolution_IsActiveImmediately_WhenSetDuringInit()
    {
        SceneSetup? captured = null;
        _ = BuildWithScene(setup =>
        {
            setup.Resolution = (256, 256);
            captured = (SceneSetup)setup;
        });

        captured!.ActiveResolution.Should().Be((256, 256));
    }

    [Fact]
    public void Resolution_IsNotYetActive_WhenChangedAfterInit()
    {
        SceneSetup? captured = null;
        _ = BuildWithScene(setup =>
        {
            captured = (SceneSetup)setup;
        });

        captured!.Resolution = (400, 300);

        captured.ActiveResolution.Should().Be((128, 128));
    }

    [Fact]
    public void Resolution_BecomesActive_OnNextUpdate()
    {
        SceneSetup? captured = null;
        var sut = BuildWithScene(setup =>
        {
            captured = (SceneSetup)setup;
        });

        captured!.Resolution = (400, 300);
        sut.InternalUpdate(TimeSpan.Zero);

        captured.ActiveResolution.Should().Be((400, 300));
    }

    [Fact]
    public void Resolution_IsIndependentPerScene()
    {
        SceneSetup? setupA = null;
        SceneSetup? setupB = null;
        var mock = new Mock<IInputManager>();
        var sut = new SceneManager(mock.Object);
        sut.ScheduleScene(() => new TestScene(s =>
        {
            s.Resolution = (128, 128);
            setupA = (SceneSetup)s;
        }));
        sut.InternalUpdate(TimeSpan.Zero); // applies A
        sut.PushScene(() => new TestScene(s =>
        {
            s.Resolution = (256, 256);
            setupB = (SceneSetup)s;
        }));
        sut.InternalUpdate(TimeSpan.Zero); // applies push

        setupA!.ActiveResolution.Should().Be((128, 128));
        setupB!.ActiveResolution.Should().Be((256, 256));
    }

    // --------------------------------------------------------------
    #endregion
    // --------------------------------------------------------------
}
