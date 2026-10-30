using System.ComponentModel;
using System.Runtime.InteropServices;
namespace UzbekKeyboard;
internal sealed class KeyboardMapper {
 private const int WH_KEYBOARD_LL=13, WM_KEYDOWN=0x100, WM_SYSKEYDOWN=0x104, LLKHF_INJECTED=0x10;
 private readonly HookProc callback; private readonly HookProc mouseCallback; private IntPtr hook; private IntPtr mouseHook;
 private bool rightAltHeld;
 private char previous; private IntPtr previousWindow; private string? undo;
 internal ushort? LastInputLanguage { get; private set; }


 internal void ResetPending() { previous='\0'; undo=null; }
 internal bool Enabled => hook != IntPtr.Zero;
 internal KeyboardMapper() { callback=OnKey; mouseCallback=OnMouse; }
 internal void Start() { if (Enabled) return; hook=SetWindowsHookEx(WH_KEYBOARD_LL, callback, GetModuleHandle(null), 0); if (hook==IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error()); mouseHook=SetWindowsHookEx(14, mouseCallback, GetModuleHandle(null), 0); if (mouseHook==IntPtr.Zero) { var error=Marshal.GetLastWin32Error(); UnhookWindowsHookEx(hook); hook=IntPtr.Zero; throw new Win32Exception(error); } }
 internal void Stop() { if (Enabled) { UnhookWindowsHookEx(hook); hook=IntPtr.Zero; } if (mouseHook!=IntPtr.Zero) { UnhookWindowsHookEx(mouseHook); mouseHook=IntPtr.Zero; } rightAltHeld=false; previous='\0'; undo=null; previousWindow=IntPtr.Zero; }
 private IntPtr OnMouse(int code, IntPtr message, IntPtr data) { if (code>=0 && (message.ToInt64()==0x201 || message.ToInt64()==0x204 || message.ToInt64()==0x207)) { previous='\0'; undo=null; } return CallNextHookEx(mouseHook,code,message,data); }
 private IntPtr OnKey(int code, IntPtr message, IntPtr data) {
  if (code<0) return CallNextHookEx(hook,code,message,data);
  var key=Marshal.PtrToStructure<Kbd>(data);
  if ((key.flags & LLKHF_INJECTED)!=0) return CallNextHookEx(hook,code,message,data);
  bool isDown=message.ToInt64()==WM_KEYDOWN || message.ToInt64()==WM_SYSKEYDOWN;
  if (key.vk==0xA5 || (key.vk==0x12 && (key.flags & 1)!=0)) rightAltHeld=isDown;
  if (!isDown) return CallNextHookEx(hook,code,message,data);
  var window=GetForegroundWindow(); if (window==IntPtr.Zero) return CallNextHookEx(hook,code,message,data);
  var thread=GetWindowThreadProcessId(window,out _); long layout=GetKeyboardLayout(thread).ToInt64(); LastInputLanguage=(ushort)(layout & 0xffff);
  if ((layout & 0xffff)!=0x0443) { previous='\0'; undo=null; previousWindow=IntPtr.Zero; return CallNextHookEx(hook,code,message,data); }
  if (window != previousWindow) { previous='\0'; undo=null; previousWindow=window; }
  bool shift=Down(0x10), ctrl=Down(0x11), alt=Down(0x12), altgr=rightAltHeld || Down(0xA5), win=Down(0x5B)||Down(0x5C);
  if (win || (ctrl && !altgr) || (alt && !altgr)) { previous='\0'; undo=null; return CallNextHookEx(hook,code,message,data); }
  bool upper=shift ^ ((GetKeyState(0x14)&1)!=0);
  if (altgr) { previous='\0'; undo=null; return CallNextHookEx(hook,code,message,data); }
  if (key.vk==0x08 && undo!=null) {
   string restore=undo; undo=null; previous='\0';
   if (SendText(restore,true)) return new IntPtr(1);
   return CallNextHookEx(hook,code,message,data);
  }
  undo=null;
  char current = key.vk >= 0x41 && key.vk <= 0x5A ? (char)(upper ? key.vk : key.vk+32) : key.vk==0xDE && !shift ? '\'' : '\0';
  char first=char.ToLowerInvariant(previous), second=char.ToLowerInvariant(current);
  char replacement=(first,second) switch { ('s','h')=>'ş', ('c','h')=>'ç', ('o','\'')=>'ö', ('g','\'')=>'ğ', _=>'\0' };
  if (replacement!='\0') {
   if (char.IsUpper(previous)) replacement=char.ToUpperInvariant(replacement);
   string original=string.Concat(previous,current); previous='\0';
   if (SendText(replacement.ToString(),true)) { undo=original; return new IntPtr(1); }
  }
  previous=current;
  return CallNextHookEx(hook,code,message,data);
 }
 private static bool SendText(string text,bool backspace,bool releaseModifiers=false) {
  var inputs=new List<INPUT>();
  var modifiers=new List<ushort>();
  if (releaseModifiers) foreach (ushort vk in new ushort[] { 0xA2,0xA3,0xA4,0xA5 }) if (Down(vk)) {
   modifiers.Add(vk); var up=new INPUT { type=1 }; up.union.ki.wVk=vk; up.union.ki.dwFlags=(uint)(2 | (vk==0xA3 || vk==0xA5 ? 1 : 0)); inputs.Add(up);
  }
  if (backspace) { var down=new INPUT {type=1}; down.union.ki.wVk=0x08; inputs.Add(down); var up=down; up.union.ki.dwFlags=2; inputs.Add(up); }
  foreach (char c in text) { var down=new INPUT {type=1}; down.union.ki.wScan=c; down.union.ki.dwFlags=4; inputs.Add(down); var up=down; up.union.ki.dwFlags=6; inputs.Add(up); }
  foreach (ushort vk in modifiers) { var down=new INPUT { type=1 }; down.union.ki.wVk=vk; down.union.ki.dwFlags=(uint)(vk==0xA3 || vk==0xA5 ? 1 : 0); inputs.Add(down); }
  return SendInput((uint)inputs.Count,inputs.ToArray(),Marshal.SizeOf<INPUT>())==inputs.Count;
 }

 private static bool Down(int v)=>(GetAsyncKeyState(v)&0x8000)!=0;
 private delegate IntPtr HookProc(int n,IntPtr w,IntPtr l);
 [StructLayout(LayoutKind.Sequential)] private struct Kbd { public uint vk,scan,flags,time; public UIntPtr extra; }
 [StructLayout(LayoutKind.Sequential)] private struct INPUT { public uint type; public InputUnion union; }
 [StructLayout(LayoutKind.Explicit)] private struct InputUnion { [FieldOffset(0)] public KEYBDINPUT ki; [FieldOffset(0)] public MOUSEINPUT mi; }
 [StructLayout(LayoutKind.Sequential)] private struct KEYBDINPUT { public ushort wVk,wScan; public uint dwFlags,time; public UIntPtr extra; }
 [StructLayout(LayoutKind.Sequential)] private struct MOUSEINPUT { public int dx,dy; public uint mouseData,dwFlags,time; public UIntPtr extra; }
 [DllImport("user32.dll",SetLastError=true)] private static extern IntPtr SetWindowsHookEx(int id,HookProc callback,IntPtr module,uint thread);
 [DllImport("user32.dll")] private static extern bool UnhookWindowsHookEx(IntPtr h);
 [DllImport("user32.dll")] private static extern IntPtr CallNextHookEx(IntPtr h,int code,IntPtr w,IntPtr l);
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode)] private static extern IntPtr GetModuleHandle(string? name);
 [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
 [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hwnd,out uint pid);
 [DllImport("user32.dll")] private static extern IntPtr GetKeyboardLayout(uint thread);
 [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int key);
 [DllImport("user32.dll")] private static extern short GetKeyState(int key);
 [DllImport("user32.dll",SetLastError=true)] private static extern uint SendInput(uint count,INPUT[] inputs,int size);
}

