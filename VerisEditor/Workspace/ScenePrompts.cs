using System.Windows;

namespace VerisEditor.Workspace
{
    public static class ScenePrompts
    {
        public static UnsavedChoice Choose(Window? owner, string sceneTitle)
        {
            var message = $"Save changes to {sceneTitle.TrimEnd(' ', '*')} before continuing?\n\nYes saves the scene. No discards changes. Cancel keeps editing.";
            var decision = owner == null
                ? MessageBox.Show(message, "Unsaved scene", MessageBoxButton.YesNoCancel, MessageBoxImage.Question)
                : MessageBox.Show(owner, message, "Unsaved scene", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            return decision switch { MessageBoxResult.Yes => UnsavedChoice.Save, MessageBoxResult.No => UnsavedChoice.Discard, _ => UnsavedChoice.Cancel };
        }
    }
}
