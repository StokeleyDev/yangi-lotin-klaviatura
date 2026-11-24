using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;

namespace UzbekKeyboard;

internal static class SelectionConverter {
    private static int active;

    internal static async Task<bool> ConvertSelectionAsync(KeyboardMapper mapper, string modeSetting, Dispatcher dispatcher) {
        if (Interlocked.CompareExchange(ref active, 1, 0) != 0) return false;
        try {
            if (mapper.IsForegroundBlocked()) return false;

            (bool hadText, string? previousText) = await TryReadClipboardTextAsync(dispatcher);
            uint seqBefore = GetClipboardSequenceNumber();

            SendCtrlChord(0x43); // Ctrl + C

            bool changed = false;
            for (int i = 0; i < 10; i++) {
                await Task.Delay(25);
                if (GetClipboardSequenceNumber() != seqBefore) {
                    changed = true;
                    break;
                }
            }
            if (!changed) return false;

            (_, string? copied) = await TryReadClipboardTextAsync(dispatcher);
            if (string.IsNullOrEmpty(copied) || copied.Length > 200_000) {
                if (hadText && previousText != null) await TryWriteClipboardTextAsync(dispatcher, previousText);
                return false;
            }

            string converted = TextConverter.Convert(copied, modeSetting);
            if (string.Equals(converted, copied, StringComparison.Ordinal)) {
                if (hadText && previousText != null) await TryWriteClipboardTextAsync(dispatcher, previousText);
                return false;
            }

            if (!await TryWriteClipboardTextAsync(dispatcher, converted)) return false;
            await Task.Delay(20);
            SendCtrlChord(0x56); // Ctrl + V
            await Task.Delay(120);

            if (hadText && previousText != null)
                await TryWriteClipboardTextAsync(dispatcher, previousText);

            return true;
        } finally {
            Interlocked.Exchange(ref active, 0);
        }
    }

    private static async Task<(bool hasText, string? text)> TryReadClipboardTextAsync(Dispatcher dispatcher) {
        for (int attempt = 0; attempt < 5; attempt++) {
            try {
                return await dispatcher.InvokeAsync(() =>
                    Clipboard.ContainsText() ? (true, Clipboard.GetText()) : (false, (string?)null));
            } catch (ExternalException) {
                await Task.Delay(20);
            }
        }
        return (false, null);
    }

    private static async Task<bool> TryWriteClipboardTextAsync(Dispatcher dispatcher, string text) {
        for (int attempt = 0; attempt < 5; attempt++) {
            try {
                await dispatcher.InvokeAsync(() => Clipboard.SetText(text));
                return true;
            } catch (ExternalException) {
                await Task.Delay(20);
            }
        }
        return false;
    }

    private static void SendCtrlChord(ushort keyVk) {
        var inputs = new List<INPUT>();
        ushort[] modifiersToRelease = { 0xA0, 0xA1, 0xA2, 0xA3, 0xA4, 0xA5, 0x5B, 0x5C };
        foreach (ushort vk in modifiersToRelease) {
            if ((GetAsyncKeyState(vk) & 0x8000) != 0)
                AddKey(inputs, vk, keyUp: true);
        }
        AddKey(inputs, 0xA2, keyUp: false); // LCtrl down
        AddKey(inputs, keyVk, keyUp: false);
        AddKey(inputs, keyVk, keyUp: true);
        AddKey(inputs, 0xA2, keyUp: true);  // LCtrl up
        SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf<INPUT>());
    }

    private static void AddKey(List<INPUT> inputs, ushort vk, bool keyUp) {
        uint flags = keyUp ? 2u : 0u;
        if (vk is 0xA3 or 0xA5 or 0x5B or 0x5C) flags |= 1u;
        var input = new INPUT { type = 1 };
        input.union.ki.wVk = vk;
        input.union.ki.dwFlags = flags;
        inputs.Add(input);
    }

    [StructLayout(LayoutKind.Sequential)] private struct INPUT { public uint type; public InputUnion union; }
    [StructLayout(LayoutKind.Explicit)] private struct InputUnion { [FieldOffset(0)] public KEYBDINPUT ki; [FieldOffset(0)] public MOUSEINPUT mi; }
    [StructLayout(LayoutKind.Sequential)] private struct KEYBDINPUT { public ushort wVk, wScan; public uint dwFlags, time; public UIntPtr extra; }
    [StructLayout(LayoutKind.Sequential)] private struct MOUSEINPUT { public int dx, dy; public uint mouseData, dwFlags, time; public UIntPtr extra; }

    [DllImport("user32.dll")] private static extern uint GetClipboardSequenceNumber();
    [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int key);
    [DllImport("user32.dll", SetLastError = true)] private static extern uint SendInput(uint count, INPUT[] inputs, int size);
}
