using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlumbingSolution.FireProtection.ItemData
{
    public class CreateFamilyFromCADSettingData
    {
        public bool IsPickLinkFromList { get; set; }
        public string LinkName { get; set; }
        public bool IsCreateBlockInOrigin { get; set; }
        public bool IsPickBlockFromList { get; set; }
        public string BlockName { get; set; }
        public string FamilyCategory { get; set; }
        public string Family { get; set; }
        public string FamilyType { get; set; }
        public string Level { get; set; }
        public string ManualElevation { get; set; }
        public ElevationType ElevationType { get; set; }
        public bool IsProjectElement { get; set; }
        public List<string> SelectedLinks { get; set; }

        public CreateFamilyFromCADSettingData()
        {
            IsPickLinkFromList = true;
            IsPickBlockFromList = true;
            ElevationType = ElevationType.ManualElevation;
            IsProjectElement = true;
            SelectedLinks = new List<string>();
        }
    }

    public enum ElevationType
    {
        ManualElevation,
        ByFloorElevation,
        ByCeilingElevation,
        ByFace
    }
}
