using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using Microsoft.Win32;
namespace YangiUninstall;
public static class Program {
 [STAThread] public static int Main() {
  var app=new Application();
  string root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"YangiLotinKlaviatura");
  if (!string.Equals(Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory),Path.TrimEndingDirectorySeparator(root),StringComparison.OrdinalIgnoreCase)) { MessageBox.Show("Запустите удаление из папки установленной программы.","Yangi lotin klaviatura"); return 1; }
  if (MessageBox.Show("Удалить Yangi lotin klaviatura?", "Удаление", MessageBoxButton.YesNo, MessageBoxImage.Question)!=MessageBoxResult.Yes) return 0;
  try {
   string exe=Path.Combine(root,"Yangi lotin klaviatura.exe");
   using(var run=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run",true)) { if(string.Equals(run?.GetValue("YangiLotinKlaviatura") as string,"\""+exe+"\"",StringComparison.OrdinalIgnoreCase)) run?.DeleteValue("YangiLotinKlaviatura",false); }
   string shortcut=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),"Yangi lotin klaviatura.lnk");
   if(File.Exists(shortcut)) { var t=Type.GetTypeFromProgID("WScript.Shell"); if(t!=null) { dynamic shell=Activator.CreateInstance(t)!; dynamic link=shell.CreateShortcut(shortcut); string target=link.TargetPath; if(string.Equals(target,exe,StringComparison.OrdinalIgnoreCase)) File.Delete(shortcut); } }
   string[] files={"Yangi lotin klaviatura.exe","Yangi lotin klaviatura.dll","Yangi lotin klaviatura.deps.json","Yangi lotin klaviatura.runtimeconfig.json","Yangi lotin klaviatura.pdb"};
   foreach(string name in files) { string path=Path.Combine(root,name); if(File.Exists(path)) File.Delete(path); }
   string current=Environment.ProcessPath ?? throw new InvalidOperationException("Не найден файл удаления.");
   string quotedCurrent=current.Replace("'","''"); string quotedRoot=root.Replace("'","''"); int pid=Environment.ProcessId;
   string script=$"Wait-Process -Id {pid} -ErrorAction SilentlyContinue; Remove-Item -LiteralPath '{quotedCurrent}' -Force -ErrorAction SilentlyContinue; Remove-Item -LiteralPath '{quotedRoot}' -ErrorAction SilentlyContinue";
   string encoded=Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
   Process.Start(new ProcessStartInfo(Path.Combine(Environment.SystemDirectory,"WindowsPowerShell","v1.0","powershell.exe"),"-NoProfile -NonInteractive -EncodedCommand "+encoded){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden});
   MessageBox.Show("Программа удалена.","Yangi lotin klaviatura"); return 0;
  } catch(Exception ex) { MessageBox.Show("Не удалось завершить удаление: "+ex.Message,"Yangi lotin klaviatura",MessageBoxButton.OK,MessageBoxImage.Error); return 1; }
 }
}

