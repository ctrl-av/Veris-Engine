using System.IO;
using System.Threading;
using System.Windows.Threading;

namespace VerisEditor.Workspace
{
    public sealed partial class EditorWorkspace
    {
        private FileSystemWatcher? _assetWatcher;
        private DispatcherTimer? _assetTimer;
        private long _lastAssetChange;
        private string _assetDiagnostics = "";
        public string AssetDiagnostics => _assetDiagnostics;

        public void ReconcileAssets() => Run(() =>
        {
            var failures = Catalog.Reconcile(ReloadAsset);
            var diagnostics = string.Join(Environment.NewLine, failures.Values);
            if (_assetDiagnostics != diagnostics && diagnostics.Length != 0) EditorLog.Info("Asset references: " + diagnostics);
            _assetDiagnostics = diagnostics;
            OnPropertyChanged(nameof(AssetDiagnostics));
            RefreshAssetList();
            RefreshOutput();
        });

        private void WatchAssets()
        {
            _assetWatcher = new FileSystemWatcher(ContentRoot) { IncludeSubdirectories = true, NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.DirectoryName };
            _assetWatcher.Changed += (_, _) => Interlocked.Exchange(ref _lastAssetChange, Environment.TickCount64);
            _assetWatcher.Created += (_, _) => Interlocked.Exchange(ref _lastAssetChange, Environment.TickCount64);
            _assetWatcher.Deleted += (_, _) => Interlocked.Exchange(ref _lastAssetChange, Environment.TickCount64);
            _assetWatcher.Renamed += (_, _) => Interlocked.Exchange(ref _lastAssetChange, Environment.TickCount64);
            _assetWatcher.Error += (_, _) => Interlocked.Exchange(ref _lastAssetChange, Environment.TickCount64);
            _assetTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _assetTimer.Tick += (_, _) =>
            {
                if (IsDisposed) return;
                var changed = Interlocked.Read(ref _lastAssetChange);
                if (changed == 0 || Environment.TickCount64 - changed < 500) return;
                if (Interlocked.CompareExchange(ref _lastAssetChange, 0, changed) == changed) ReconcileAssets();
            };
            _assetWatcher.EnableRaisingEvents = true;
            _assetTimer.Start();
            ReconcileAssets();
        }
    }
}
