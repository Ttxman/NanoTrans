using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows;
using System.Threading;
using Microsoft.Win32;

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
            return File.Create(EnsureDirectoryExists(GetWritePath(ConfigFile)));
        }


        private static readonly string _PluginsFile = "Plugins\\Plugins.xml";
        private static readonly string _PluginsPath= "Plugins\\";


        private static readonly string ConfigFile = "Data\\config.xml";

        private static readonly string _PedalFile = "Pedals.exe";
        private static readonly string _FFmpegFile = "ffmpeg.exe";

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


            string pfiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            _writeToAppData = _programDirectory.StartsWith(pfiles) || !CheckWritePermissions(Path.Combine(_programDirectory,"writecheck.txt"));

            


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
                    //demove temp folders from unexpectedly terminated instances
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


        /// <summary>
        /// if nanotrans don't have write permissons to application folder of if it is installed in program files, all configs should be stored in appdata
        /// </summary>
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
                return EnsureDirectoryExists(Path.Combine(_AppDataPath, relativePath));
            else
                return EnsureDirectoryExists(Path.Combine(_programDirectory, relativePath));
        }


        /// <summary>
        /// creates all necessary directories on specified path
        /// </summary>
        /// <param name="path"></param>
        /// <returns>null if exeption is raised during creation, else simple fall through of path parameter</returns>
        public static string EnsureDirectoryExists(string path)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
            catch
            {
                return null;
            }
            return path;
        }

        public static bool CheckWritePermissions(string path)
        {
            /* not working properly
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

        public static string GetDefaultSpeakersPath()
        {
            return GetWritePath("Data\\SpeakersDatabase.xml");
        }



        static string _SaveDirectory = null;
        public static string QuickSaveDirectory
        {
            get { return _SaveDirectory; }
        }


        public static string SelectFolderDialog(bool ovewriteprompt = true)
        {
            SaveFileDialog sd = new SaveFileDialog();
            sd.FileName = "dummy name";
            sd.OverwritePrompt = ovewriteprompt;
            if (sd.ShowDialog() == true)
            {
                _SaveDirectory = Path.GetDirectoryName(sd.FileName);
            }

            return _SaveDirectory;
        }

    }
}
