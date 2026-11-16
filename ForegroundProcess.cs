using System.IO;
using System.Runtime.InteropServices;
using System.Text;
namespace UzbekKeyboard;
internal static class ForegroundProcess {
 private const uint PROCESS_QUERY_LIMITED_INFORMATION=0x1000, TOKEN_QUERY=0x8;
 private const int ERROR_ACCESS_DENIED=5, TokenElevation=20;
 internal static readonly bool SelfElevated=IsElevated((uint)Environment.ProcessId);
 /// <summary>True when the process runs elevated. Access denied is treated as elevated (UIPI would block our input anyway).</summary>
 internal static bool IsElevated(uint pid) {
  IntPtr process=OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION,false,pid);
  if (process==IntPtr.Zero) return Marshal.GetLastWin32Error()==ERROR_ACCESS_DENIED;
  try {
   if (!OpenProcessToken(process,TOKEN_QUERY,out IntPtr token)) return Marshal.GetLastWin32Error()==ERROR_ACCESS_DENIED;
   try { return GetTokenInformation(token,TokenElevation,out int elevated,sizeof(int),out _) && elevated!=0; }
   finally { CloseHandle(token); }
  } finally { CloseHandle(process); }
 }
 internal static string? ExeName(uint pid) {
  IntPtr process=OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION,false,pid);
  if (process==IntPtr.Zero) return null;
  try {
   var buffer=new StringBuilder(1024); int size=buffer.Capacity;
   return QueryFullProcessImageName(process,0,buffer,ref size) ? Path.GetFileName(buffer.ToString(0,size)) : null;
  } finally { CloseHandle(process); }
 }
 [DllImport("kernel32.dll",SetLastError=true)] private static extern IntPtr OpenProcess(uint access,bool inherit,uint pid);
 [DllImport("advapi32.dll",SetLastError=true)] private static extern bool OpenProcessToken(IntPtr process,uint access,out IntPtr token);
 [DllImport("advapi32.dll",SetLastError=true)] private static extern bool GetTokenInformation(IntPtr token,int cls,out int info,int length,out int returned);
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)] private static extern bool QueryFullProcessImageName(IntPtr process,int flags,StringBuilder name,ref int size);
 [DllImport("kernel32.dll")] private static extern bool CloseHandle(IntPtr handle);
}
