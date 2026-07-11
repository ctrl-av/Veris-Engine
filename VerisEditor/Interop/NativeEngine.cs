using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace VerisEditor.Interop
{
    public sealed class NativeEngine : IDisposable
    {
        private readonly EngineHandle _handle;

        public NativeEngine(uint width, uint height)
        {
            Check(NativeMethods.Create(width, height, out var handle));
            _handle = new EngineHandle(handle);
        }

        public ulong EntityCount
        {
            get { Check(NativeMethods.EntityCount(_handle, out var count)); return count; }
        }

        public ulong CreateEntity(string name)
        {
            ValidateEntityName(name);
            Check(NativeMethods.CreateEntity(_handle, name, out var entity));
            return entity;
        }

        public void DestroyEntity(ulong entity) => Check(NativeMethods.DestroyEntity(_handle, entity));
        public void SetContentRoot(string directory) => Check(NativeMethods.SetContentRoot(_handle, directory));
        public void LoadAssetCatalog(string manifest) => Check(NativeMethods.LoadAssetCatalog(_handle, manifest));
        public static void ValidateTextureFile(string source) => Check(NativeMethods.ValidateTexture(source));
        public void ReloadTexture(string path) => Check(NativeMethods.ReloadTexture(_handle, path));
        public void SetAnimation(ulong entity, string json) => Check(NativeMethods.SetAnimation(_handle, entity, json));
        public string Animation(ulong entity) => ReadUtf8((byte[]? buffer, uint capacity, out uint required) => NativeMethods.Animation(_handle, entity, buffer, capacity, out required));
        public void AdvanceAnimation(ulong entity, double seconds) => Check(NativeMethods.AdvanceAnimation(_handle, entity, seconds));
        public void SetAudio(ulong entity, string json) => Check(NativeMethods.SetAudio(_handle, entity, json));
        public string Audio(ulong entity) => ReadUtf8((byte[]? buffer, uint capacity, out uint required) => NativeMethods.Audio(_handle, entity, buffer, capacity, out required));
        public void PlayAudio(ulong entity) => Check(NativeMethods.PlayAudio(_handle, entity));
        public void PauseAudio(ulong entity) => Check(NativeMethods.PauseAudio(_handle, entity));
        public void ResumeAudio(ulong entity) => Check(NativeMethods.ResumeAudio(_handle, entity));
        public void StopAudio(ulong entity) => Check(NativeMethods.StopAudio(_handle, entity));
        public int AudioStatus(ulong entity) { Check(NativeMethods.AudioStatus(_handle, entity, out var status)); return status; }
        public static void ValidateAudioFile(string path) => Check(NativeMethods.ValidateAudio(path));
        public void ReloadAudio(string path) => Check(NativeMethods.ReloadAudio(_handle, path));
        public void SetPhysics(ulong entity, string json) => Check(NativeMethods.SetPhysics(_handle, entity, json));
        public string Physics(ulong entity) => ReadUtf8((byte[]? buffer, uint capacity, out uint required) => NativeMethods.Physics(_handle, entity, buffer, capacity, out required));
        public void FixedUpdate(double seconds) => Check(NativeMethods.FixedUpdate(_handle, seconds));
        public void SetGravity(float x, float y) => Check(NativeMethods.SetGravity(_handle, x, y));
        public void ApplyImpulse(ulong entity, float x, float y) => Check(NativeMethods.ApplyImpulse(_handle, entity, x, y));
        public void SetPhysicsDebug(bool enabled) => Check(NativeMethods.SetPhysicsDebug(_handle, enabled ? 1 : 0));
        public string Contacts() => ReadUtf8((byte[]? buffer, uint capacity, out uint required) => NativeMethods.Contacts(_handle, buffer, capacity, out required));
        public (uint Stable, uint Legacy) AssetUsage(Guid id)
        {
            Check(NativeMethods.AssetUsage(_handle, id.ToString("D"), out var stable, out var legacy));
            return (stable, legacy);
        }
        public void SetSprite(ulong entity, string texture, SpriteProperties sprite)
        {
            if (string.IsNullOrWhiteSpace(texture) || texture.Contains('\0')) throw new ArgumentException("A content-relative texture path is required.", nameof(texture));
            Check(NativeMethods.SetSprite(_handle, entity, texture, in sprite));
        }
        public SpriteProperties? GetSprite(ulong entity)
        {
            Check(NativeMethods.GetSprite(_handle, entity, out var sprite, out var present));
            return present == 0 ? null : sprite;
        }
        public string SpritePath(ulong entity) => ReadUtf8((byte[]? buffer, uint capacity, out uint required) => NativeMethods.SpritePath(_handle, entity, buffer, capacity, out required));
        public void RemoveSprite(ulong entity) => Check(NativeMethods.RemoveSprite(_handle, entity));
        public ulong PickEntity(int x, int y)
        {
            Check(NativeMethods.PickEntity(_handle, x, y, out var entity));
            return entity;
        }
        public EntityPoint ParentCoordinates(ulong entity, int x, int y)
        {
            Check(NativeMethods.ParentCoordinates(_handle, entity, x, y, out var local));
            return local;
        }
        public EntityPoint EntityOrigin(ulong entity)
        {
            Check(NativeMethods.EntityOrigin(_handle, entity, out var pixel));
            return pixel;
        }
        public EntityPoint[] EntityCorners(ulong entity)
        {
            var corners = new EntityPoint[4];
            Check(NativeMethods.EntityCorners(_handle, entity, corners, (uint)corners.Length));
            return corners;
        }
        public ulong DuplicateEntity(ulong entity)
        {
            Check(NativeMethods.DuplicateEntity(_handle, entity, out var duplicate));
            return duplicate;
        }
        public void RenameEntity(ulong entity, string name)
        {
            ValidateEntityName(name);
            Check(NativeMethods.RenameEntity(_handle, entity, name));
        }
        public void SetParent(ulong entity, ulong parent) => Check(NativeMethods.SetParent(_handle, entity, parent));
        public ulong EntityParent(ulong entity)
        {
            Check(NativeMethods.EntityParent(_handle, entity, out var parent));
            return parent;
        }
        public IReadOnlyList<ulong> EntityIds()
        {
            var count = EntityCount;
            var entities = new List<ulong>();
            for (ulong index = 0; index < count; index++)
            {
                Check(NativeMethods.EntityAt(_handle, index, out var entity));
                entities.Add(entity);
            }
            return entities;
        }

        public string EntityName(ulong entity) => ReadUtf8((byte[]? buffer, uint capacity, out uint required) => NativeMethods.EntityName(_handle, entity, buffer, capacity, out required));

        public EntityTransform GetTransform(ulong entity)
        {
            Check(NativeMethods.GetTransform(_handle, entity, out var transform));
            return transform;
        }
        public void Resize(uint width, uint height) => Check(NativeMethods.Resize(_handle, width, height));
        public void Update() => Check(NativeMethods.Update(_handle));
        public void SetTransform(ulong entity, EntityTransform transform) => Check(NativeMethods.SetTransform(_handle, entity, in transform));
        public void Dispose() => _handle.Dispose();
        public SceneSnapshot CaptureScene()
        {
            Check(NativeMethods.CaptureScene(_handle, out var snapshot));
            return new SceneSnapshot(snapshot);
        }
        public string SerializeScene() => ReadUtf8((byte[]? buffer, uint capacity, out uint required) => NativeMethods.SerializeScene(_handle, buffer, capacity, out required));
        public void DeserializeScene(string json) => Check(NativeMethods.DeserializeScene(_handle, json));
        public void RestoreScene(SceneSnapshot snapshot) => Check(NativeMethods.RestoreScene(_handle, snapshot));
        public bool MatchesScene(SceneSnapshot snapshot)
        {
            Check(NativeMethods.MatchesScene(_handle, snapshot, out var matches));
            return matches != 0;
        }
        public void CopyFrame(byte[] rgba) => Check(NativeMethods.CopyFrame(_handle, rgba, (ulong)rgba.Length, out _));

        public byte[] CopyFrame()
        {
            Check(NativeMethods.CopyFrame(_handle, null, 0, out var required));
            if (required > (ulong)Array.MaxLength) throw new InvalidOperationException("Frame exceeds the managed array size limit.");
            var rgba = new byte[(int)required];
            Check(NativeMethods.CopyFrame(_handle, rgba, (ulong)rgba.Length, out _));
            return rgba;
        }

        private static void ValidateEntityName(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Contains('\0')) throw new ArgumentException("Entity names require visible text and no null characters.", nameof(name));
        }

        private delegate int Utf8Reader(byte[]? buffer, uint capacity, out uint required);

        private static string ReadUtf8(Utf8Reader read)
        {
            Check(read(null, 0, out var required));
            var utf8 = new byte[required];
            Check(read(utf8, required, out _));
            return Encoding.UTF8.GetString(utf8, 0, utf8.Length - 1);
        }

        private static void Check(int status)
        {
            if (status == 0) return;
            var required = NativeMethods.ErrorMessage(null, 0);
            var utf8 = new byte[required];
            NativeMethods.ErrorMessage(utf8, required);
            var exception = new NativeEngineException(status, Encoding.UTF8.GetString(utf8, 0, utf8.Length - 1));
            EditorLog.Error("Native engine operation failed.", exception);
            throw exception;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct EntityPoint(float x, float y)
    {
        public readonly float X = x;
        public readonly float Y = y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct EntityTransform(float positionX, float positionY, float scaleX, float scaleY, float rotation)
    {
        public readonly float PositionX = positionX;
        public readonly float PositionY = positionY;
        public readonly float ScaleX = scaleX;
        public readonly float ScaleY = scaleY;
        public readonly float Rotation = rotation;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SpriteProperties
    {
        public float OriginX;
        public float OriginY;
        public uint Tint;
        public int Layer;
        public int Visible;
        public int UseRectangle;
        public int RectangleX;
        public int RectangleY;
        public int RectangleWidth;
        public int RectangleHeight;
    }

    public sealed class NativeEngineException(int status, string message) : Exception(message)
    {
        public int Status { get; } = status;
    }

    internal sealed class EngineHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal EngineHandle(ulong handle) : base(true) => SetHandle(new IntPtr(checked((long)handle)));
        protected override bool ReleaseHandle() => NativeMethods.Destroy(unchecked((ulong)handle.ToInt64())) == 0;
    }

    public sealed class SceneSnapshot : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal SceneSnapshot(ulong snapshot) : base(true) => SetHandle(new IntPtr(checked((long)snapshot)));
        protected override bool ReleaseHandle() => NativeMethods.ReleaseSnapshot(unchecked((ulong)handle.ToInt64())) == 0;
    }

    internal static class NativeMethods
    {
        private const string Library = "VerisInterop";

        [DllImport(Library, EntryPoint = "veris_create", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int Create(uint width, uint height, out ulong engine);
        [DllImport(Library, EntryPoint = "veris_destroy", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int Destroy(ulong engine);
        [DllImport(Library, EntryPoint = "veris_capture_scene", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int CaptureScene(EngineHandle engine, out ulong snapshot);
        [DllImport(Library, EntryPoint = "veris_restore_scene", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int RestoreScene(EngineHandle engine, SceneSnapshot snapshot);
        [DllImport(Library, EntryPoint = "veris_matches_scene", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int MatchesScene(EngineHandle engine, SceneSnapshot snapshot, out int matches);
        [DllImport(Library, EntryPoint = "veris_release_snapshot", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int ReleaseSnapshot(ulong snapshot);
        [DllImport(Library, EntryPoint = "veris_serialize_scene", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SerializeScene(EngineHandle engine, [Out] byte[]? utf8, uint capacity, out uint required);
        [DllImport(Library, EntryPoint = "veris_deserialize_scene", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DeserializeScene(EngineHandle engine, [MarshalAs(UnmanagedType.LPUTF8Str)] string utf8);
        [DllImport(Library, EntryPoint = "veris_resize", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int Resize(EngineHandle engine, uint width, uint height);
        [DllImport(Library, EntryPoint = "veris_update", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int Update(EngineHandle engine);
        [DllImport(Library, EntryPoint = "veris_create_entity", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int CreateEntity(EngineHandle engine, [MarshalAs(UnmanagedType.LPUTF8Str)] string name, out ulong entity);
        [DllImport(Library, EntryPoint = "veris_destroy_entity", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DestroyEntity(EngineHandle engine, ulong entity);
        [DllImport(Library, EntryPoint = "veris_set_content_root", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SetContentRoot(EngineHandle engine, [MarshalAs(UnmanagedType.LPWStr)] string directory);
        [DllImport(Library, EntryPoint = "veris_load_asset_catalog", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int LoadAssetCatalog(EngineHandle engine, [MarshalAs(UnmanagedType.LPWStr)] string manifest);
        [DllImport(Library, EntryPoint = "veris_validate_texture", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int ValidateTexture([MarshalAs(UnmanagedType.LPWStr)] string source);
        [DllImport(Library, EntryPoint = "veris_reload_texture", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int ReloadTexture(EngineHandle engine, [MarshalAs(UnmanagedType.LPUTF8Str)] string path);
        [DllImport(Library, EntryPoint = "veris_set_animation", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SetAnimation(EngineHandle engine, ulong entity, [MarshalAs(UnmanagedType.LPUTF8Str)] string json);
        [DllImport(Library, EntryPoint = "veris_animation", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int Animation(EngineHandle engine, ulong entity, [Out] byte[]? utf8, uint capacity, out uint required);
        [DllImport(Library, EntryPoint = "veris_advance_animation", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int AdvanceAnimation(EngineHandle engine, ulong entity, double seconds);
        [DllImport(Library, EntryPoint = "veris_set_audio", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SetAudio(EngineHandle engine, ulong entity, [MarshalAs(UnmanagedType.LPUTF8Str)] string json);
        [DllImport(Library, EntryPoint = "veris_audio", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int Audio(EngineHandle engine, ulong entity, [Out] byte[]? utf8, uint capacity, out uint required);
        [DllImport(Library, EntryPoint = "veris_play_audio", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int PlayAudio(EngineHandle engine, ulong entity);
        [DllImport(Library, EntryPoint = "veris_pause_audio", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int PauseAudio(EngineHandle engine, ulong entity);
        [DllImport(Library, EntryPoint = "veris_resume_audio", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int ResumeAudio(EngineHandle engine, ulong entity);
        [DllImport(Library, EntryPoint = "veris_stop_audio", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int StopAudio(EngineHandle engine, ulong entity);
        [DllImport(Library, EntryPoint = "veris_audio_status", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int AudioStatus(EngineHandle engine, ulong entity, out int status);
        [DllImport(Library, EntryPoint = "veris_validate_audio", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int ValidateAudio([MarshalAs(UnmanagedType.LPWStr)] string path);
        [DllImport(Library, EntryPoint = "veris_reload_audio", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int ReloadAudio(EngineHandle engine, [MarshalAs(UnmanagedType.LPUTF8Str)] string path);
        [DllImport(Library, EntryPoint = "veris_set_physics", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SetPhysics(EngineHandle engine, ulong entity, [MarshalAs(UnmanagedType.LPUTF8Str)] string json);
        [DllImport(Library, EntryPoint = "veris_physics", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int Physics(EngineHandle engine, ulong entity, [Out] byte[]? utf8, uint capacity, out uint required);
        [DllImport(Library, EntryPoint = "veris_fixed_update", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int FixedUpdate(EngineHandle engine, double seconds);
        [DllImport(Library, EntryPoint = "veris_set_gravity", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SetGravity(EngineHandle engine, float x, float y);
        [DllImport(Library, EntryPoint = "veris_apply_impulse", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int ApplyImpulse(EngineHandle engine, ulong entity, float x, float y);
        [DllImport(Library, EntryPoint = "veris_set_physics_debug", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SetPhysicsDebug(EngineHandle engine, int enabled);
        [DllImport(Library, EntryPoint = "veris_contacts", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int Contacts(EngineHandle engine, [Out] byte[]? utf8, uint capacity, out uint required);
        [DllImport(Library, EntryPoint = "veris_asset_usage", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int AssetUsage(EngineHandle engine, [MarshalAs(UnmanagedType.LPUTF8Str)] string asset, out uint stable, out uint legacy);
        [DllImport(Library, EntryPoint = "veris_set_sprite", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SetSprite(EngineHandle engine, ulong entity, [MarshalAs(UnmanagedType.LPUTF8Str)] string texture, in SpriteProperties sprite);
        [DllImport(Library, EntryPoint = "veris_get_sprite", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int GetSprite(EngineHandle engine, ulong entity, out SpriteProperties sprite, out int present);
        [DllImport(Library, EntryPoint = "veris_sprite_path", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SpritePath(EngineHandle engine, ulong entity, [Out] byte[]? utf8, uint capacity, out uint required);
        [DllImport(Library, EntryPoint = "veris_remove_sprite", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int RemoveSprite(EngineHandle engine, ulong entity);
        [DllImport(Library, EntryPoint = "veris_pick_entity", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int PickEntity(EngineHandle engine, int x, int y, out ulong entity);
        [DllImport(Library, EntryPoint = "veris_parent_coordinates", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int ParentCoordinates(EngineHandle engine, ulong entity, int x, int y, out EntityPoint local);
        [DllImport(Library, EntryPoint = "veris_entity_origin", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int EntityOrigin(EngineHandle engine, ulong entity, out EntityPoint pixel);
        [DllImport(Library, EntryPoint = "veris_entity_corners", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int EntityCorners(EngineHandle engine, ulong entity, [Out] EntityPoint[] corners, uint count);
        [DllImport(Library, EntryPoint = "veris_duplicate_entity", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DuplicateEntity(EngineHandle engine, ulong entity, out ulong duplicate);
        [DllImport(Library, EntryPoint = "veris_rename_entity", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int RenameEntity(EngineHandle engine, ulong entity, [MarshalAs(UnmanagedType.LPUTF8Str)] string name);
        [DllImport(Library, EntryPoint = "veris_entity_parent", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int EntityParent(EngineHandle engine, ulong entity, out ulong parent);
        [DllImport(Library, EntryPoint = "veris_set_parent", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SetParent(EngineHandle engine, ulong entity, ulong parent);
        [DllImport(Library, EntryPoint = "veris_entity_count", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int EntityCount(EngineHandle engine, out ulong count);
        [DllImport(Library, EntryPoint = "veris_entity_at", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int EntityAt(EngineHandle engine, ulong index, out ulong entity);
        [DllImport(Library, EntryPoint = "veris_entity_name", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int EntityName(EngineHandle engine, ulong entity, [Out] byte[]? utf8, uint capacity, out uint required);
        [DllImport(Library, EntryPoint = "veris_get_transform", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int GetTransform(EngineHandle engine, ulong entity, out EntityTransform transform);
        [DllImport(Library, EntryPoint = "veris_set_transform", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SetTransform(EngineHandle engine, ulong entity, in EntityTransform transform);
        [DllImport(Library, EntryPoint = "veris_copy_frame", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int CopyFrame(EngineHandle engine, [Out] byte[]? rgba, ulong capacity, out ulong required);
        [DllImport(Library, EntryPoint = "veris_error_message", CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint ErrorMessage([Out] byte[]? utf8, uint capacity);
    }
}
