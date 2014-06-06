using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using System.Reflection;

namespace AutoUpdate
{  
  static class Program
  {
    private const string PERSISTENCE_KEY = "AUTOUPDATE_PERSISTENCE_DATA";
    private const string EXE_NAME = "tasks3.dll";
    //private const string SERVER_FILE_PATH = @"e:\home\tasktracker\bin\debug\tasks3.exe";
    private const string SERVER_FILE_PATH = @"\\stingray\software\tasktracker\Beta\tasks3.dll";
    private const double CHECK_INTERVAL_MINS = 0.5f;

    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main(params string[] args)
    {
      Application.EnableVisualStyles();
      Application.SetCompatibleTextRenderingDefault(false);

      CleanOldVersions();

      WatchForUpdates();
    }

    private static DateTime lastModified;
    private static AppDomain clientAppDomain;
    private static object lockable = new object();
    private static ManualResetEvent waitable = new ManualResetEvent(false);

    private static void WatchForUpdates()
    {
      Thread client = null;

      while (client == null || client.ThreadState == ThreadState.Running)
      {
        var exe = new FileInfo(SERVER_FILE_PATH);

        if (!exe.Exists)
        {
          if (client == null)
          {
            var old = GetOldExe();

            if (old != null)
            {
              client = RunApp(old, null);
            }
            else
            {
              MessageBox.Show("Unable to locate latest version at " + SERVER_FILE_PATH + " and no local version exists");
            }
          }
        }
        else if (exe.LastWriteTimeUtc != lastModified.ToUniversalTime())
        {
          var dir = Path.Combine(Application.UserAppDataPath, EXE_NAME + "." + exe.LastWriteTimeUtc.Ticks.ToString());
          Directory.CreateDirectory( dir);
          var newFile = new FileInfo(Path.Combine(dir, EXE_NAME));
          if (!newFile.Exists)
          {
            exe.CopyTo(newFile.FullName);

            var serverDir = Path.GetDirectoryName(SERVER_FILE_PATH);
            foreach (var dll in Directory.EnumerateFiles(serverDir).Where(f => f.EndsWith("dll")))
            {
              var local = Path.Combine(dir, Path.GetFileName(dll));

              // File may exist if the application binary is also a dll
              if (!File.Exists(local))
              {
                File.Copy(dll, local);
              }              
            }

            // need this assembly to be accessible to new appdomain. Must use original assembly name otherwise client appdomains can't find it
            var assembly = Assembly.GetCallingAssembly().Location;
            File.Copy(assembly, Path.Combine(dir, "AutoUpdate.exe"));

          }

          client = RunApp(newFile, client);          
        }

        waitable.WaitOne(TimeSpan.FromMinutes(CHECK_INTERVAL_MINS));
      }
    }

    

    private static FileInfo GetOldExe()
    {
      var dir = Directory.EnumerateDirectories(Application.UserAppDataPath).Where(d => d.Contains(EXE_NAME)).OrderByDescending(d => d).FirstOrDefault();

      if (dir != null)
      {
        var file = new FileInfo(Path.Combine(dir, EXE_NAME));

        if (file.Exists)
        {
          return file;
        }
      }

      return null;
    }

    private static void CleanOldVersions()
    {
      var list = Directory.EnumerateDirectories(Application.UserAppDataPath).Where(d => d.Contains(EXE_NAME)).OrderByDescending(d => d).Skip(4).ToList();
      foreach (var dir in list)
      {
        Directory.Delete(dir, true);
      }

    }

    private static Thread RunApp(FileInfo file, Thread oldthread)
    {
      string persistenceData = null;

      if (clientAppDomain != null)
      {
        clientAppDomain.DoCallBack(() => Application.Exit());
        persistenceData = clientAppDomain.GetData(PERSISTENCE_KEY) as string;
        AppDomain.Unload(clientAppDomain);
        oldthread.Join();
      }

      var ads = new AppDomainSetup();
      ads.ApplicationBase = file.DirectoryName;
      ads.ConfigurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
      clientAppDomain = AppDomain.CreateDomain(file.FullName, null, ads);
      clientAppDomain.SetData(PERSISTENCE_KEY, persistenceData);

      waitable.Reset();

      var thread = new Thread(
        () => { try { clientAppDomain.ExecuteAssembly(file.FullName); } catch (AppDomainUnloadedException) { } finally { waitable.Set(); } });
      thread.SetApartmentState(ApartmentState.STA);
      thread.Start();

      file.Refresh();
      lastModified = file.LastWriteTime;

      return thread;
    }
  }
}
