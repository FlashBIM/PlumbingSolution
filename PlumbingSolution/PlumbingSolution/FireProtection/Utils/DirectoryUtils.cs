using PlumbingSolution.FireProtection.SpeedHanger.Data;
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
    

        /// <summary>
        /// Get config folder
        /// </summary>
        /// <returns></returns>
        public static string GetConfigFolder(string type = "")
        {
#if Bundle_2020 || Bundle_2021 || Bundle_2022 || Bundle_2023 || Bundle_2024 || Bundle_2025 || Bundle_2026 || Debug_2025 || Debug_2024
            string appDir = "C:\\Users\\anlin\\AppData\\Roaming\\Autodesk\\ApplicationPlugins\\AddinDirit.bundle\\Contents\\2024\\PlumbingSolution.FireProtection";

#else
            string appDir = GetAppFolder();
#endif
            string configDir = type == "" ? Path.Combine(appDir, "Config") : Path.Combine(appDir, "Config", type);
            if (!Directory.Exists(configDir))
                Directory.CreateDirectory(configDir);

            return configDir;
        }



        /// <summary>
        /// Reead data from config file
        /// </summary>
        public static void ReadCreateFamilyFromCADData(out CreateFamilyFromCADSettingData data)
        {
#if Debug_2019 || Debug_2020 || Debug_2021 || Debug_2022 || Debug_2024
            if (!Directory.Exists(@"C:\Users\Admin\AppData\Roaming\Autodesk\ApplicationPlugins\AddinDirit.bundle\Contents\2025\PlumbingSolution.FireProtection\Config\SingleHanger"))
                Directory.CreateDirectory(@"C:\Users\Admin\AppData\Roaming\Autodesk\ApplicationPlugins\AddinDirit.bundle\Contents\2025\PlumbingSolution.FireProtection\Config\SingleHanger");
            string configPath = @"C:\Users\Admin\AppData\Roaming\Autodesk\ApplicationPlugins\AddinDirit.bundle\Contents\2025\PlumbingSolution.FireProtection\Config\SingleHanger\CreateFamilyFromCADSettingData.txt";
#else
            string configDir = GetConfigFolder("SingleHanger");
            string configPath = Path.Combine(configDir, "CreateFamilyFromCADSettingData" + ".txt");
#endif

            var fileStream = new FileStream(configPath, FileMode.OpenOrCreate);

            using (StreamReader sr = new StreamReader(fileStream))
            {
                JsonReader jsonReader = new JsonTextReader(sr);
                JsonSerializer jsonSerializer = new JsonSerializer();
                data = jsonSerializer.Deserialize<CreateFamilyFromCADSettingData>(jsonReader);

                fileStream.Dispose();
            }

            if (data == null)
                data = new CreateFamilyFromCADSettingData();
        }

        /// <summary>
        /// Write data to config file
        /// </summary>
        public static void WriteCreateFamilyFromCADData(CreateFamilyFromCADSettingData data)
        {
#if Debug_2019 || Debug_2020 || Debug_2021 || Debug_2022 || Debug_2024
            string configPath = @"C:\Users\Admin\AppData\Roaming\Autodesk\ApplicationPlugins\AddinDirit.bundle\Contents\2025\PlumbingSolution.FireProtection\Config\SingleHanger\CreateFamilyFromCADSettingData.txt";
#else
            string configDir = DirectoryUtils.GetConfigFolder("SingleHanger");
            string configPath = Path.Combine(configDir, "CreateFamilyFromCADSettingData" + ".txt");
#endif

            string jsonData = JsonConvert.SerializeObject(data);

            FileStream fileStream = new FileStream(configPath, FileMode.OpenOrCreate);
            using (StreamWriter sw = new StreamWriter(fileStream))
            {
                sw.Write(jsonData);
            }
            ;
        }
    }
}
