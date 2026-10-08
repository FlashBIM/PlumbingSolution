using PlumbingSolution.FireProtection.Ultis;
using PlumbingSolution.FireProtection.Utils;
using Autodesk.Revit.DB;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlumbingSolution.FireProtection.ItemData
{

    public class FamilyData
    {
        public string CategoryName { get; set; }
        public string FamilyPath { get; set; }

        public FamilyData(string familyPath, string categoryName)
        {
            CategoryName = categoryName;
            FamilyPath = familyPath;
        }
    }
}
