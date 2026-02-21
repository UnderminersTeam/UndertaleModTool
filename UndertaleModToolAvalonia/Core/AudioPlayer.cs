using System;
using System.Runtime.InteropServices;
using SDL3;

namespace UndertaleModToolAvalonia;

public class AudioPlayer : IDisposable
{
    static Action<Action> mainThreadAction = null!;

    static IntPtr mixer = IntPtr.Zero;

    IntPtr audio;
    IntPtr track;

    IntPtr dataArray;

    readonly Mixer.TrackStoppedCallback trackStoppedCallback;
    GCHandle trackStoppedCallbackHandle;

    public AudioPlayer(byte[] data)
    {
        // Don't allow this be deallocated until the sound stops.
        trackStoppedCallback = new(OnTrackStoppped);
        trackStoppedCallbackHandle = GCHandle.Alloc(trackStoppedCallback);

        // Load audio
        GCHandle dataHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
        IntPtr io = SDL.IOFromConstMem(dataHandle.AddrOfPinnedObject(), (nuint)data.Length);

        audio = Mixer.LoadAudioIO(mixer, io, predecode: true, closeio: true);
        if (audio == IntPtr.Zero)
            throw new InvalidOperationException($"{SDL.GetError()}");

        dataHandle.Free();

        // Create track and play
        track = Mixer.CreateTrack(mixer);
        if (track == IntPtr.Zero)
            throw new InvalidOperationException($"{SDL.GetError()}");

        if (!Mixer.SetTrackAudio(track, audio))
            throw new InvalidOperationException($"{SDL.GetError()}");

        if (!Mixer.PlayTrack(track, 0))
            throw new InvalidOperationException($"{SDL.GetError()}");

        if (!Mixer.SetTrackStoppedCallback(track, trackStoppedCallback, IntPtr.Zero))
            throw new InvalidOperationException($"{SDL.GetError()}");
    }

    public static void Init(Action<Action> _mainThreadAction)
    {
        if ((SDL.WasInit(SDL.InitFlags.Audio) & SDL.InitFlags.Audio) == 0)
        {
            if (!SDL.Init(SDL.InitFlags.Audio))
                throw new InvalidOperationException($"{SDL.GetError()}");

            if (!Mixer.Init())
                throw new InvalidOperationException($"{SDL.GetError()}");
        }

        if (mixer == IntPtr.Zero)
        {
            mixer = Mixer.CreateMixerDevice(SDL.AudioDeviceDefaultPlayback, IntPtr.Zero);
            if (mixer == IntPtr.Zero)
                throw new InvalidOperationException($"{SDL.GetError()}");
        }

        mainThreadAction = _mainThreadAction;
    }

    public void Stop()
    {
        Dispose();
    }

    public void Dispose()
    {
        // If those are null, nothing happens. They also don't call the track stopped callback.
        Mixer.DestroyTrack(track);
        Mixer.DestroyAudio(audio);

        if (dataArray != IntPtr.Zero)
            Marshal.FreeHGlobal(dataArray);

        if (trackStoppedCallbackHandle.IsAllocated)
            trackStoppedCallbackHandle.Free();

        track = IntPtr.Zero;
        audio = IntPtr.Zero;
        dataArray = IntPtr.Zero;

        GC.SuppressFinalize(this);
    }

    void OnTrackStoppped(IntPtr userdata, IntPtr track)
    {
        // The callback happens in a separate thread, so we defer to the main thread.
        mainThreadAction(() =>
        {
            Dispose();
        });
    }
}
