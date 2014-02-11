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
            if (File.Exists(file))
                return File.Open(file, FileMode.Open, FileAccess.Read);

            return null;
        }

        public static Stream GetConfigFileWriteStream()
        {
            return File.Create(GetWritePath(ConfigFile));
        }

        private static readonly string _PluginsFile = "Plugins\\Plugins.xml";
        private static readonly string _PluginsPath= "Plugins\\";


        private static readonly string ConfigFile = "Data\\config.xml";

        private static readonly string _PedalFile = "Pedals.exe";
        private static readonly string _FFmpegFile = "Prevod\\ffmpeg.exe";

        private static string _programDirectory;
        private static bool _writeToAppData;



        private static string _AppDataPath;

        public static string AppDataDirectory
        {
            get { return FilePaths._AppDataPath; }
        }

        static FilePaths()
        {
            _programDirectory = new FileInfo(Application.ResourceAssembly.Location).DirectoryName;
            _writeToAppData = !CheckWritePermissions(Path.Combine(_programDirectory,"writecheck.txt"));
            _AppDataPath = System.IO.Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\NanoTrans");

            CreateTemp();
        }


        private static Mutex TempCheckMutex;
        private static string _TempPath;
        private static void CreateTemp()
        {

            string foldername = System.IO.Path.GetRandomFileName();
            string temppath = System.IO.Path.GetTempPath() + "NanoTrans\\";

            Directory.CreateDirectory(temppath);
            DeleteUnusedTempFolders(temppath);
            temppath = temppath + foldername;
            Directory.CreateDirectory(temppath);
            TempCheckMutex = new Mutex(true, "NanoTransMutex_" + foldername);

            _TempPath = temppath + "\\";
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

            foreach (string f in Directory.GetFiles(_TempPath))
                File.Delete(f);

            Directory.Delete(_TempPath);
            TempCheckMutex.Close();
            TempCheckMutex = null;
        
        }

        public static string TempDirectory
        {
            get { return _TempPath; }
        }


        public static bool WriteToAppData
        {
            get { return _writeToAppData; }
        }

        public static string ProgramDirectory
        {
            get 
            { 
                return _programDirectory; 
            }
        }

        public static string DefaultDirectory
        {
            get
            {
                if (_writeToAppData)
                    return _AppDataPath;
                else
                    return _programDirectory;
            }
        
        }


        public static string PedalPath
        {
            get
            {
                return Path.Combine(_programDirectory, _PedalFile);
            }
        }

        public  static string FFmpegPath
        {
            get
            {
                return Path.Combine(_programDirectory, _FFmpegFile);
            }
        }

        public static string PluginsFile
        {
            get { return FilePaths._PluginsFile; }
        }

        public static string PluginsPath
        {
            get { return FilePaths._PluginsPath; }
        } 


        #region tool functions

        public static string GetReadPath(string relativePath)
        {
            if (_writeToAppData)
            {
                string apppath = Path.Combine(_AppDataPath, relativePath);
                if (File.Exists(apppath))
                    return apppath;
                else
                {
                    string nearexepath = Path.Combine(_programDirectory, relativePath);
                    if (File.Exists(nearexepath))
                        return nearexepath;
                    else
                        return apppath;
                
                }
            }
            else
            {
                return Path.Combine(_programDirectory, relativePath);
            }
        }


        public static string GetWritePath(string relativePath)
        {
            if (_writeToAppData)
                return Path.Combine(_AppDataPath, relativePath);
            else
                return Path.Combine(_programDirectory, relativePath);
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
