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
            Check(NativeMethods.CreateEntity(_handle, name, out var entity));
            return entity;
        }

        public void DestroyEntity(ulong entity) => Check(NativeMethods.DestroyEntity(_handle, entity));
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

        public string EntityName(ulong entity)
        {
            Check(NativeMethods.EntityName(_handle, entity, null, 0, out var required));
            var utf8 = new byte[required];
            Check(NativeMethods.EntityName(_handle, entity, utf8, required, out _));
            return Encoding.UTF8.GetString(utf8, 0, utf8.Length - 1);
        }

        public EntityTransform GetTransform(ulong entity)
        {
            Check(NativeMethods.GetTransform(_handle, entity, out var transform));
            return transform;
        }
        public void Resize(uint width, uint height) => Check(NativeMethods.Resize(_handle, width, height));
        public void Update() => Check(NativeMethods.Update(_handle));
        public void SetTransform(ulong entity, EntityTransform transform) => Check(NativeMethods.SetTransform(_handle, entity, in transform));
        public void Dispose() => _handle.Dispose();

        public byte[] CopyFrame()
        {
            Check(NativeMethods.CopyFrame(_handle, null, 0, out var required));
            if (required > (ulong)Array.MaxLength) throw new InvalidOperationException("Frame exceeds the managed array size limit.");
            var rgba = new byte[(int)required];
            Check(NativeMethods.CopyFrame(_handle, rgba, (ulong)rgba.Length, out _));
            return rgba;
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
    public readonly struct EntityTransform(float positionX, float positionY, float scaleX, float scaleY, float rotation)
    {
        public readonly float PositionX = positionX;
        public readonly float PositionY = positionY;
        public readonly float ScaleX = scaleX;
        public readonly float ScaleY = scaleY;
        public readonly float Rotation = rotation;
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

    internal static class NativeMethods
    {
        private const string Library = "VerisInterop";

        [DllImport(Library, EntryPoint = "veris_create", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int Create(uint width, uint height, out ulong engine);
        [DllImport(Library, EntryPoint = "veris_destroy", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int Destroy(ulong engine);
        [DllImport(Library, EntryPoint = "veris_resize", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int Resize(EngineHandle engine, uint width, uint height);
        [DllImport(Library, EntryPoint = "veris_update", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int Update(EngineHandle engine);
        [DllImport(Library, EntryPoint = "veris_create_entity", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int CreateEntity(EngineHandle engine, [MarshalAs(UnmanagedType.LPUTF8Str)] string name, out ulong entity);
        [DllImport(Library, EntryPoint = "veris_destroy_entity", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DestroyEntity(EngineHandle engine, ulong entity);
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
