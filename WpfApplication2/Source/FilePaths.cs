using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows;
using System.Threading;

namespace NanoTrans
{
    public static class FilePaths
    {
        public static Stream GetConfigFileReadStream()
        {
            string file = GetReadPath(ConfigFile);
            if(File.Exists(file))
                return File.Open(file, FileMode.Open, FileAccess.Read);
            return null;
        }

        public static Stream GetConfigFileWriteStream()
        {
            return File.Create(GetWritePath(ConfigFile));
        }

        private static readonly string m_PluginsFile = "Plugins\\Plugins.xml";
        private static readonly string m_PluginsPath= "Plugins\\";


        private static readonly string ConfigFile = "Data\\config.xml";

        private static readonly string m_PedalFile = "Pedals.exe";
        private static readonly string m_FFmpegFile = "Prevod\\ffmpeg.exe";

        private static string m_programDirectory;
        private static bool m_writeToAppData;



        private static string m_AppDataPath;

        public static string AppDataDirectory
        {
            get { return FilePaths.m_AppDataPath; }
        }

        static FilePaths()
        {
            m_programDirectory = new FileInfo(Application.ResourceAssembly.Location).DirectoryName;
            m_writeToAppData = !CheckWritePermissions(Path.Combine(m_programDirectory,"writecheck.txt"));
            m_AppDataPath = System.IO.Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\NanoTrans");

            CreateTemp();
        }


        private static Mutex TempCheckMutex;
        private static string m_TempPath;
        private static void CreateTemp()
        {

            string foldername = System.IO.Path.GetRandomFileName();
            string temppath = System.IO.Path.GetTempPath() + "NanoTrans\\";

            Directory.CreateDirectory(temppath);
            DeleteUnusedTempFolders(temppath);
            temppath = temppath + foldername;
            Directory.CreateDirectory(temppath);
            TempCheckMutex = new Mutex(true, "NanoTransMutex_" + foldername);

            m_TempPath = temppath + "\\";
        }

        private static void DeleteUnusedTempFolders(string foldername)
        {
            DirectoryInfo di = new DirectoryInfo(foldername);
            DirectoryInfo[] dirs = di.GetDirectories();

            foreach (DirectoryInfo dir in dirs)
            {
                try
                {
                    //ziskal sem nezaregistrovany pristup ke slozce.. smazat
                    bool isnew;
                    using (Mutex m = new Mutex(true, "NanoTransMutex_" + dir.Name, out isnew))
                    {
                        if (isnew)
                        {
                            foreach (var f in dir.GetFiles())
                            {
                                f.Delete();
                            }

                            dir.Delete();
                        }

                    }
                }
                catch
                { }

            }
        }

        public static void DeleteTemp()
        {

            foreach (string f in Directory.GetFiles(m_TempPath))
                File.Delete(f);

            Directory.Delete(m_TempPath);
            TempCheckMutex.Close();
            TempCheckMutex = null;
        
        }

        public static string TempDirectory
        {
            get { return m_TempPath; }
        }


        public static bool WriteToAppData
        {
            get { return m_writeToAppData; }
        }

        public static string ProgramDirectory
        {
            get 
            { 
                return m_programDirectory; 
            }
        }

        public static string DefaultDirectory
        {
            get
            {
                if (m_writeToAppData)
                    return m_AppDataPath;
                else
                    return m_programDirectory;
            }
        
        }


        public static string PedalPath
        {
            get
            {
                return Path.Combine(m_programDirectory, m_PedalFile);
            }
        }

        public  static string FFmpegPath
        {
            get
            {
                return Path.Combine(m_programDirectory, m_FFmpegFile);
            }
        }

        public static string PluginsFile
        {
            get { return FilePaths.m_PluginsFile; }
        }

        public static string PluginsPath
        {
            get { return FilePaths.m_PluginsPath; }
        } 


        #region tool functions

        public static string GetReadPath(string relativePath)
        {
            if (m_writeToAppData)
            {
                string apppath = Path.Combine(m_AppDataPath, relativePath);
                if (File.Exists(apppath))
                    return apppath;
                else
                {
                    string nearexepath = Path.Combine(m_programDirectory, relativePath);
                    if (File.Exists(nearexepath))
                        return nearexepath;
                    else
                        return apppath;
                
                }
            }
            else
            {
                return Path.Combine(m_programDirectory, relativePath);
            }
        }


        public static string GetWritePath(string relativePath)
        {
            if (m_writeToAppData)
                return Path.Combine(m_AppDataPath, relativePath);
            else
                return Path.Combine(m_programDirectory, relativePath);
        }




        public static bool CheckWritePermissions(string path)
        {
            /*
            var set = new PermissionSet(PermissionState.None);
            set.AddPermission(new FileIOPermission(FileIOPermissionAccess.AllAccess,System.IO.Path.GetDirectoryName(path)));
            set.AddPermission(new FileIOPermission(FileIOPermissionAccess.AllAccess, path));
             * 
            return set.IsSubsetOf(AppDomain.CurrentDomain.PermissionSet);*/
            try
            {

                bool delete = !File.Exists(path);

                File.AppendAllText(path, "");
                if (delete)
                    File.Delete(path);
            }
            catch
            {
                return false;
            }
            return true;
        }

        #endregion
    }
}
