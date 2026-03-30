using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using SDL3;

namespace PSharp8.Tests.Infrastructure;

/// <summary>
/// xUnit class fixture that spins up a real FNA <see cref="Game"/>,
/// initializing both graphics (FNA3D) and audio (FAudio) subsystems.
/// </summary>
public sealed class FnaFixture : IDisposable
{
    private readonly TestGame _game;

    public FnaFixture()
    {
        ConfigureGraphicsBackend();

        _game = new TestGame();
        // Run() blocks until the SDL event loop exits.
        // TestGame.Initialize() calls Exit() after base.Initialize(), which creates
        // GraphicsDevice and then signals the loop to stop on its next iteration.
        _game.Run();
    }

    public GraphicsDevice GraphicsDevice => _game.GraphicsDevice;
    public GraphicsDeviceManager GraphicsDeviceManager => _game.GraphicsDeviceManager;
    public GameWindow Window => _game.Window;

    /// <summary>
    /// Creates a minimal silent <see cref="SoundEffect"/> in memory (no file I/O).
    /// Useful for audio tests that need dictionary entries without real audio assets.
    /// </summary>
    public static SoundEffect CreateSilentSoundEffect(int durationMs = 100, int sampleRate = 44100)
    {
        int sampleCount = sampleRate * durationMs / 1000;
        byte[] silence = new byte[sampleCount * 2]; // 16-bit mono = 2 bytes per sample
        return new SoundEffect(silence, sampleRate, AudioChannels.Mono);
    }

    public void Dispose() => _game.Dispose();

    private static void ConfigureGraphicsBackend()
    {
        // Project-level .runsettings pushes these into the testhost before startup.
        // Keep the same values here as a fallback for direct runs that bypass it.
        Environment.SetEnvironmentVariable("FNA_PLATFORM_BACKEND", "SDL3");
        Environment.SetEnvironmentVariable("FNA_NO_OPENGL_INTERCEPTION", "1");
        Environment.SetEnvironmentVariable("FNA3D_FORCE_DRIVER", "SDLGPU");
        Environment.SetEnvironmentVariable("SDL_GPU_DRIVER", "vulkan");

        SDL.SDL_SetHintWithPriority(
            "FNA_PLATFORM_BACKEND",
            "SDL3",
            SDL.SDL_HintPriority.SDL_HINT_OVERRIDE);
        SDL.SDL_SetHintWithPriority(
            "FNA_NO_OPENGL_INTERCEPTION",
            "1",
            SDL.SDL_HintPriority.SDL_HINT_OVERRIDE);
        SDL.SDL_SetHintWithPriority(
            "FNA3D_FORCE_DRIVER",
            "SDLGPU",
            SDL.SDL_HintPriority.SDL_HINT_OVERRIDE);
        SDL.SDL_SetHintWithPriority(
            SDL.SDL_HINT_GPU_DRIVER,
            "vulkan",
            SDL.SDL_HintPriority.SDL_HINT_OVERRIDE);
    }

    private sealed class TestGame : Game
    {
        public GraphicsDeviceManager GraphicsDeviceManager { get; }

        public TestGame()
        {
            // Registering GraphicsDeviceManager is sufficient; it wires itself to
            // the Game's Services and creates GraphicsDevice during Initialize().
            GraphicsDeviceManager = new GraphicsDeviceManager(this);
        }

        protected override void Initialize()
        {
            // base.Initialize() triggers GraphicsDeviceManager.Initialize(),
            // which calls CreateDevice() and populates Game.GraphicsDevice.
            base.Initialize();

            // Signal the SDL loop to exit on the next tick.
            // GraphicsDevice remains valid until Dispose() is called.
            Exit();
        }
    }
}
