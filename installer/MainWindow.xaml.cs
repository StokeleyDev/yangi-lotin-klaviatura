using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Windows;
using Microsoft.Win32;
namespace YangiInstaller;
public partial class MainWindow : Window {
 private static readonly string InstallDir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"YangiLotinKlaviatura");
 private static readonly string AppPath=Path.Combine(InstallDir,"Yangi lotin klaviatura.exe");
 private bool completed;
 public MainWindow() => InitializeComponent();
 private void Install_Click(object sender,RoutedEventArgs e) {
  if (completed) { Close(); return; }
  try {
   Directory.CreateDirectory(InstallDir);
   using Stream source=Assembly.GetExecutingAssembly().GetManifestResourceStream("Payload.zip") ?? throw new InvalidOperationException("Файлы приложения не найдены.");
   using var zip=new ZipArchive(source,ZipArchiveMode.Read);
   foreach(var item in zip.Entries) {
    if (string.IsNullOrEmpty(item.Name)) continue;
    string dest=Path.GetFullPath(Path.Combine(InstallDir,item.FullName));
    if(!dest.StartsWith(InstallDir+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Недопустимый путь в архиве.");
    Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
    item.ExtractToFile(dest,true);
   }
   using var run=Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run") ?? throw new IOException("Не удалось открыть настройки автозапуска.");
   const string name="YangiLotinKlaviatura";
   if(AutoStart.IsChecked == true) run.SetValue(name,"\""+AppPath+"\"");
   else if (string.Equals(run.GetValue(name) as string,"\""+AppPath+"\"",StringComparison.OrdinalIgnoreCase)) run.DeleteValue(name,false);
   string shortcut=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),"Yangi lotin klaviatura.lnk");
   Type shellType=Type.GetTypeFromProgID("WScript.Shell") ?? throw new InvalidOperationException("Не удалось создать ярлык рабочего стола.");
   dynamic shell=Activator.CreateInstance(shellType)!;
   if(File.Exists(shortcut)) { dynamic old=shell.CreateShortcut(shortcut); string target=old.TargetPath; if(!string.Equals(target,AppPath,StringComparison.OrdinalIgnoreCase)) throw new IOException("На рабочем столе уже есть другой ярлык Yangi lotin klaviatura. Переименуйте его и повторите установку."); }
   dynamic link=shell.CreateShortcut(shortcut); link.TargetPath=AppPath; link.WorkingDirectory=InstallDir; link.Description="Yangi lotin klaviatura"; link.IconLocation=AppPath+",0"; link.Save();
   completed=true;
   OptionsCard.Height=OptionsCard.ActualHeight;
   CardTitle.Text="Установка завершена";
   CardDescription.Text="Программа установлена, ярлык добавлен на рабочий стол.";
   AutoStart.Visibility=Visibility.Hidden;
   OptionDescription.Visibility=Visibility.Hidden;
   Status.Text=string.Empty;
   InstallButton.Content="Завершить";
  } catch(Exception ex) { Status.Text="Установка не завершена: " + ex.Message; }
 }
}

