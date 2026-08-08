using System.Diagnostics;
using System.IO;
using VerisEditor.Interop;

namespace VerisEditor.Workspace
{
    public enum PlaybackState { Stopped, Playing, Paused }

    public sealed partial class EditorWorkspace
    {
        private NativeEngine? _playEngine;
        private long _lastPlayTick;
        private double _playAccumulator;
        public PlaybackState Playback { get; private set; }
        public bool IsPlayingSession => _playEngine != null;
        public bool CanEditScene => !IsPlayingSession;
        public bool CanUndo => CanEditScene && History.CanUndo;
        public bool CanRedo => CanEditScene && History.CanRedo;
        public string RuntimeDiagnostics { get; private set; } = "";

        public void Play() => Run(() =>
        {
            if (Playback == PlaybackState.Playing) return;
            Tools.Cancel();
            if (_playEngine == null) CreatePlayEngine();
            else _playEngine.ResumeAllAudio();
            BeginPlayback();
        });

        private void BeginPlayback()
        {
            Playback = PlaybackState.Playing;
            _lastPlayTick = Stopwatch.GetTimestamp();
            _playAccumulator = 0;
            NotifyPlayback();
        }

        private void CreatePlayEngine()
        {
            var runtime = new NativeEngine((uint)Project.WindowWidth, (uint)Project.WindowHeight);
            try
            {
                runtime.SetContentRoot(ContentRoot);
                runtime.LoadAssetCatalog(Catalog.ManifestPath);
                runtime.SetScriptRoot(Path.Combine(Path.GetDirectoryName(ProjectFile)!, Project.GameCodeDirectory));
                runtime.ConfigureInput(Path.GetDirectoryName(ProjectFile)!);
                runtime.DeserializeScene(_engine.SerializeScene());
                runtime.SetPhysicsDebug(PhysicsDebug);
                _playEngine = runtime;
            }
            catch { runtime.Dispose(); throw; }
        }

        public void Pause() => Run(() =>
        {
            if (Playback != PlaybackState.Playing) return;
            _playEngine!.PauseAllAudio();
            _playEngine.ReleaseInput();
            Playback = PlaybackState.Paused;
            NotifyPlayback();
        });

        public void StepPlay() => Run(() =>
        {
            if (Playback != PlaybackState.Paused) throw new ArgumentException("Pause the game before stepping.");
            _playEngine!.GameStep(1.0 / 60);
            _playEngine.PauseAllAudio();
            RefreshRuntimeDiagnostics();
        });

        public void StopPlay()
        {
            _playEngine?.Dispose();
            _playEngine = null;
            Playback = PlaybackState.Stopped;
            _playAccumulator = 0;
            if (!IsDisposed && HasSelection) Inspector.Load(SelectedEntityId);
            NotifyPlayback();
        }

        public void SetPlayInput(string binding, bool down) => Run(() => _playEngine?.NamedInput(binding, down));
        public void ReleasePlayInput() => _playEngine?.ReleaseInput();

        private void AdvancePlay()
        {
            if (Playback != PlaybackState.Playing) return;
            var now = Stopwatch.GetTimestamp();
            _playAccumulator += Math.Min((now - _lastPlayTick) / (double)Stopwatch.Frequency, 0.1);
            _lastPlayTick = now;
            while (_playAccumulator >= 1.0 / 60)
            {
                _playEngine!.GameStep(1.0 / 60);
                _playAccumulator -= 1.0 / 60;
            }
            RefreshRuntimeDiagnostics();
        }
        private void RefreshRuntimeDiagnostics()
        {
            RuntimeDiagnostics = _playEngine!.ScriptLog();
            ContactDetails = _playEngine.Contacts();
            OnPropertyChanged(nameof(RuntimeDiagnostics));
            OnPropertyChanged(nameof(ContactDetails));
        }
        private void RequireEditing()
        {
            if (IsPlayingSession) throw new ArgumentException("Stop the game before editing or saving the scene.");
        }
        private void NotifyPlayback()
        {
            OnPropertyChanged(nameof(Playback));
            OnPropertyChanged(nameof(IsPlayingSession));
            OnPropertyChanged(nameof(CanEditScene));
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
        }
    }
}
