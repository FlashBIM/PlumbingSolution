using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace PlumbingSolution.Ultis
{
    internal class DirectoryUtils
    {
        public static string GetTempFolder()
        {
            string pathSystemFolder = Path.GetPathRoot(Environment.SystemDirectory);

            string tempDir = Path.Combine(pathSystemFolder, "Temp");

            if (!Directory.Exists(tempDir))
                Directory.CreateDirectory(tempDir);

            return tempDir;
        }

        public static string GetRevitVersionFolder()
        {
#if DEBUG2020
            string nameVersion = "2020";
#else
            string nameVersion = App.VersionRevit;
#endif
            string tempPath = GetTempFolder();

            string versionDir = Path.Combine(tempPath, nameVersion);

            if (!Directory.Exists(versionDir))
                Directory.CreateDirectory(versionDir);

            return versionDir;
        }

        public static string GetToolFolder()
        {
            string revitVersionFolder = GetRevitVersionFolder();

            string sleeveConfigDir = Path.Combine(revitVersionFolder, "PlumbingSolution");

            if (!Directory.Exists(sleeveConfigDir))
                Directory.CreateDirectory(sleeveConfigDir);

            return sleeveConfigDir;
        }

        public static string GetTempConfigFolder()
        {
            string revitVersionFolder = GetToolFolder();

            string configDir = Path.Combine(revitVersionFolder, "Config");

            if (!Directory.Exists(configDir))
                Directory.CreateDirectory(configDir);

            return configDir;
        }

        public static string GetTempConfigFamilyFolder(string type = "")
        {
#if DEBUG2020 || DEBUG2021 || DEBUG2022
            string tempPath = @"C:\Temp\2024\SleeveAddinProject\Config";
#else
            string tempPath = GetTempConfigFolder();
#endif
            string configDir = type == "" ? Path.Combine(tempPath) : Path.Combine(tempPath, type);

            if (!Directory.Exists(configDir))
                Directory.CreateDirectory(configDir);

            return configDir;
        }

        public static string GetIconFolder()
        {
            string appDir = GetAppFolder();
            string imageDir = Path.Combine(appDir, "Icon");

            if (!Directory.Exists(imageDir))
                Directory.CreateDirectory(imageDir);

            return imageDir;
        }

        public static string GetAppFolder()
        {
            string location = Assembly.GetExecutingAssembly().Location;
            string dir = Path.GetDirectoryName(location);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            return dir;
        }
    }
}