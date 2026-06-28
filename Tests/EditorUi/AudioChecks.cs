using System.IO;
using System.Text;
using VerisEditor.Workspace;

internal static class AudioChecks
{
    public static void Run(EditorWorkspace workspace)
    {
        var path = Path.Combine(Path.GetDirectoryName(workspace.ProjectFile)!, "tone.wav");
        using (var writer = new BinaryWriter(File.Create(path)))
        {
            writer.Write(Encoding.ASCII.GetBytes("RIFF")); writer.Write(88236); writer.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
            writer.Write(16); writer.Write((short)1); writer.Write((short)1); writer.Write(44100); writer.Write(88200); writer.Write((short)2); writer.Write((short)16);
            writer.Write(Encoding.ASCII.GetBytes("data")); writer.Write(88200);
            for (var index = 0; index < 44100; ++index) writer.Write((short)(100 * Math.Sin(index * 440 * 2 * Math.PI / 44100)));
        }
        workspace.ImportAsset(path);
        var asset = workspace.Catalog.Assets.Single(asset => asset.Kind == "audio");
        workspace.SelectedAsset = asset;
        workspace.UseSelectedAsset();
        Program.Assert(workspace.AudioJson.Contains(asset.Id.ToString()), "Assigning an audio asset must persist its GUID on the selected entity.");
        workspace.PlayAudio(); workspace.PauseAudio(); workspace.ResumeAudio(); workspace.StopAudio();
        workspace.Undo();
        Program.Assert(workspace.AudioJson == "null", "Undo must remove the audio source.");
        workspace.Redo();
        Program.Assert(workspace.SaveScene("audio.verisscene") && workspace.Catalog.HasSavedReference(asset), "Saved audio sources must protect referenced assets from deletion.");
        Program.Assert(workspace.OpenScene("audio.verisscene", () => UnsavedChoice.Discard), "Audio scenes must reopen.");
    }
}
