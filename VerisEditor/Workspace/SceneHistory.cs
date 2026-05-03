using VerisEditor.Interop;

namespace VerisEditor.Workspace
{
    public sealed class SceneHistory(NativeEngine engine, Func<ulong> selectedEntity, Action<ulong> restoreSelection) : ViewModelBase, IDisposable
    {
        private sealed record Edit(SceneSnapshot Before, SceneSnapshot After, ulong BeforeSelection, ulong AfterSelection, long BeforeRevision, long AfterRevision, string Name) : IDisposable
        {
            public void Dispose() { Before.Dispose(); After.Dispose(); }
        }

        private readonly List<Edit> _undo = new();
        private readonly List<Edit> _redo = new();
        private SceneSnapshot? _pending;
        private ulong _beforeSelection;
        private long _nextRevision;
        public long Revision { get; private set; }
        public bool CanUndo => _undo.Count != 0;
        public bool CanRedo => _redo.Count != 0;
        public int UndoCount => _undo.Count;
        public event EventHandler? Changed;

        public void Execute(string name, Action operation)
        {
            BeginEdit();
            try { operation(); CommitEdit(name); }
            catch
            {
                if (_pending != null) { engine.RestoreScene(_pending); restoreSelection(_beforeSelection); }
                CancelEdit();
                throw;
            }
        }

        public void BeginEdit()
        {
            if (_pending != null) throw new InvalidOperationException("Complete the current edit before beginning another.");
            _pending = engine.CaptureScene();
            _beforeSelection = selectedEntity();
        }

        public void CommitEdit(string name)
        {
            if (_pending == null) return;
            if (engine.MatchesScene(_pending)) { CancelEdit(); return; }
            var after = engine.CaptureScene();
            var revision = checked(++_nextRevision);
            _undo.Add(new Edit(_pending, after, _beforeSelection, selectedEntity(), Revision, revision, name));
            _pending = null;
            foreach (var edit in _redo) edit.Dispose();
            _redo.Clear();
            if (_undo.Count > 100) { _undo[0].Dispose(); _undo.RemoveAt(0); }
            Revision = revision;
            NotifyChanged();
        }

        public void CancelEdit()
        {
            _pending?.Dispose();
            _pending = null;
        }

        public void Undo()
        {
            if (!CanUndo) return;
            var edit = _undo[^1];
            engine.RestoreScene(edit.Before);
            restoreSelection(edit.BeforeSelection);
            _undo.RemoveAt(_undo.Count - 1);
            _redo.Add(edit);
            Revision = edit.BeforeRevision;
            NotifyChanged();
        }

        public void Redo()
        {
            if (!CanRedo) return;
            var edit = _redo[^1];
            engine.RestoreScene(edit.After);
            restoreSelection(edit.AfterSelection);
            _redo.RemoveAt(_redo.Count - 1);
            _undo.Add(edit);
            Revision = edit.AfterRevision;
            NotifyChanged();
        }

        public void Clear()
        {
            CancelEdit();
            foreach (var edit in _undo) edit.Dispose();
            foreach (var edit in _redo) edit.Dispose();
            _undo.Clear();
            _redo.Clear();
            Revision = checked(++_nextRevision);
            NotifyChanged();
        }

        public void Dispose() => Clear();

        private void NotifyChanged()
        {
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
            OnPropertyChanged(nameof(Revision));
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}
