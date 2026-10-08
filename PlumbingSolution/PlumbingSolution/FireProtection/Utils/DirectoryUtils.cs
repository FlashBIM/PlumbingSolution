using PlumbingSolution.FireProtection.ItemData;
using PlumbingSolution.FireProtection.ItemData;
using Autodesk.Revit.DB;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Controls;

namespace PlumbingSolution.FireProtection.Ultis
{
    public class DirectoryUtils
    {

        public static string GetAppFolder()
        {
            string location = Assembly.GetExecutingAssembly().Location;
            string dir = Path.GetDirectoryName(location);

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            return dir;
        }

        public static string GetFamilyFolder()
        {
#if Debug_2020 || Debug_2021 || Debug_2022 || Debug_2023 || Debug_2024 || Debug_2025 || Debug_2026 || Debug_2024
            //string familyDir = @"E:\SourceForDev\MEP\AddinDiritTool-\PlumbingSolution.FireProtection\Resource\Family";
            //string familyDir = @"E:\SourceForDev\MrC\AddinDiritTool-\PlumbingSolution.FireProtection\Resource\Family";
            string familyDir = @"C:\Users\anlin\AppData\Roaming\Autodesk\ApplicationPlugins\AddinDirit.bundle\Contents\2024\PlumbingSolution.FireProtection\Family";
#else
            string appDir = GetAppFolder();
            string familyDir = Path.Combine(appDir, "Family");
#endif

            if (!Directory.Exists(familyDir))
                Directory.CreateDirectory(familyDir);

            return familyDir;
        }

        public static string GetFamilyCategoryFolder(string categoryName)
        {
            string folderPath = Path.Combine(GetFamilyFolder(), categoryName);

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            return folderPath;
        }
    }
}
