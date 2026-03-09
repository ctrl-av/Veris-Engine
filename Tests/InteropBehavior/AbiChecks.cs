using System.Runtime.InteropServices;
using System.Text;

internal static class AbiChecks
{
    public static void Run()
    {
        Require(Create(0, 2, out var invalid) == 1 && invalid == 0, "Failed native creation must clear the output handle.");
        Require(Create(2, 2, out var engine) == 0, "Native ABI fixture must initialize.");
        try
        {
            Require(EntityCount(engine, IntPtr.Zero) == 1, "Null output pointers must return invalid-argument status.");
            var buffer = Enumerable.Repeat((byte)177, 3).ToArray();
            Require(CopyFrame(engine, buffer, 3, out var required) == 3 && required == 16,
                "Insufficient buffers must return the complete required size.");
            Require(buffer.All(channel => channel == 177), "Insufficient buffers must remain untouched.");
            Require(CopyFrame(engine, null, 1, out _) == 1, "Nonzero capacity requires a valid buffer.");
        }
        finally { Require(Destroy(engine) == 0, "Native ABI fixture must release."); }
        Require(Update(engine) == 2 && Destroy(engine) == 2, "Stale native handles must be rejected safely.");
        var size = ErrorMessage(null, 0);
        var utf8 = new byte[size];
        Require(ErrorMessage(utf8, size) == size && Encoding.UTF8.GetString(utf8).Contains("released"), "Native diagnostics must survive a buffer-size query.");
        Require(Create(2, 2, out var next) == 0 && next != engine, "Released session handles must not be reused.");
        Require(Destroy(next) == 0, "Replacement fixture must release.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    [DllImport("VerisInterop", EntryPoint = "veris_create", CallingConvention = CallingConvention.Cdecl)]
    private static extern int Create(uint width, uint height, out ulong engine);
    [DllImport("VerisInterop", EntryPoint = "veris_destroy", CallingConvention = CallingConvention.Cdecl)]
    private static extern int Destroy(ulong engine);
    [DllImport("VerisInterop", EntryPoint = "veris_update", CallingConvention = CallingConvention.Cdecl)]
    private static extern int Update(ulong engine);
    [DllImport("VerisInterop", EntryPoint = "veris_entity_count", CallingConvention = CallingConvention.Cdecl)]
    private static extern int EntityCount(ulong engine, IntPtr count);
    [DllImport("VerisInterop", EntryPoint = "veris_copy_frame", CallingConvention = CallingConvention.Cdecl)]
    private static extern int CopyFrame(ulong engine, [Out] byte[]? rgba, ulong capacity, out ulong required);
    [DllImport("VerisInterop", EntryPoint = "veris_error_message", CallingConvention = CallingConvention.Cdecl)]
    private static extern uint ErrorMessage([Out] byte[]? utf8, uint capacity);
}
