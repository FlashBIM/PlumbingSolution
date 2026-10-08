using PlumbingSolution.FireProtection.Ultis;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PlumbingSolution.FireProtection.ItemData
{
    public class CreateBranchPipeFireConfigData
    {
        /// <summary>
        /// 1: Lệch tâm trái | 2: Lệch tâm phải | 3: Đồng tâm
        /// </summary>
        public int TypeLocation { get; set; }

        public double OffsetDauPhun { get; set; }

        /// <summary>
        /// 1: Cùng cao độ | 2: Khác cao độ
        /// </summary>
        public int Elevation { get; set; }

        public double OffsetElevation { get; set; } // Giả sử cột bên cạnh Khác cao độ là nhập offset

        // ==========================================
        // 2. NHÓM: KẾT NỐI SPRINKLER
        // ==========================================

        // -- Kết nối đầu phun --
        public bool IsConnectSprinkler { get; set; }

        public string TypeConnection { get; set; }
        public double Diameter { get; set; }
        public double DiameterBranch { get; set; }

        public bool IsElbow90 { get; set; }
        public bool IsElbow90Branch2T { get; set; }

        // -- Kết nối ống nhánh --
        public bool IsConnectBranch { get; set; }

        /// <summary>
        /// 1:1 Tê  | 2:  1 Tê E 45| 3: 1 Tê E 90 |4: 2 Tê
        /// </summary>
        public int TypeConnectBranch { get; set; }

        public double OffsetCon { get; set; }

        public List<PipeSizingRule> LeftRules { get; set; }
        public List<PipeSizingRule> RightRules { get; set; }

        public CreateBranchPipeFireConfigData()
        {
            LeftRules = new List<PipeSizingRule>();
            RightRules = new List<PipeSizingRule>();
            OffsetCon = 200;
            TypeConnection = string.Empty;
            TypeLocation = 1;
            Elevation = 1;
            TypeConnectBranch = 1;
            IsElbow90 = false;
            IsElbow90Branch2T = false;
            OffsetDauPhun = 200;
            OffsetElevation = 0;
        }

        /// <summary>
        /// Reead data from config file
        /// </summary>
        public static void ReadConfigData(out CreateBranchPipeFireConfigData configData)
        {
            string configDir = DirectoryUtils.GetConfigFolder();
            string configPath = Path.Combine(configDir, "CreateBranchPipeData" + ".txt");

            var fileStream = new FileStream(configPath, FileMode.OpenOrCreate);

            using (StreamReader sr = new StreamReader(fileStream))
            {
                JsonReader jsonReader = new JsonTextReader(sr);
                JsonSerializer jsonSerializer = new JsonSerializer();
                configData = jsonSerializer.Deserialize<CreateBranchPipeFireConfigData>(jsonReader);

                fileStream.Dispose();
            }

            if (configData == null)
                configData = new CreateBranchPipeFireConfigData();
        }

        /// <summary>
        /// Write data to config file
        /// </summary>
        public static void WriteConfigData(ref CreateBranchPipeFireConfigData configData)
        {
            string configDir = DirectoryUtils.GetConfigFolder();
            string configPath = Path.Combine(configDir, "CreateBranchPipeData" + ".txt");

            string jsonData = JsonConvert.SerializeObject(configData);

            FileStream fileStream = new FileStream(configPath, FileMode.OpenOrCreate);
            using (StreamWriter sw = new StreamWriter(fileStream))
            {
                sw.Write(jsonData);
            }
            ;
        }
    }

    public class PipeSizingRule
    {
        // Kích thước ống (VD: 80, 65, 50, 40, 32, 25)
        public double PipeSize { get; set; }

        // Số đầu phun cần đi qua trước khi thu cấp (VD: 1 hoặc 2)
        public int SprinklerCount { get; set; }
    }
}
