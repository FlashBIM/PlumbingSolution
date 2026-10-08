using PlumbingSolution.FireProtection.SpeedHanger.Data;
using PlumbingSolution.FireProtection.Ultis;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using PlumbingSolution.FireProtection.Extensions;
using View = Autodesk.Revit.DB.View;

namespace PlumbingSolution.FireProtection.Utils
{
    public class RevitUtils
    {

        public static Line ProjectLineToPlane(Line line, double elevation = 0)
        {
            XYZ startPoint = line.GetEndPoint(0) + XYZ.BasisZ * (elevation - line.GetEndPoint(0).Z);
            XYZ endPoint = line.GetEndPoint(1) + XYZ.BasisZ * (elevation - line.GetEndPoint(1).Z);

            return Line.CreateBound(startPoint, endPoint);
        }
    

        public static List<int> GetMEPCategories()
        {
            List<int> lstCategory = new List<int>()
            {
                (int)BuiltInCategory.OST_DuctTerminal,            // Air Terminal
                (int)BuiltInCategory.OST_CableTrayFitting,        // Cable Tray Fitting
                (int)BuiltInCategory.OST_CommunicationDevices,    // Communication Device
                (int)BuiltInCategory.OST_ConduitFitting,          // Conduit Fittings
                (int)BuiltInCategory.OST_DataDevices,             // Data Device
                (int)BuiltInCategory.OST_DetailComponents,        // Detail Item
                (int)BuiltInCategory.OST_DuctFitting,             // Duct Fittings
                (int)BuiltInCategory.OST_ElectricalEquipment,     // Electrical Equipments
                (int)BuiltInCategory.OST_ElectricalFixtures,      // Electrical Fixtures
                (int)BuiltInCategory.OST_FireAlarmDevices,        // Fire Alarm Devices
                (int)BuiltInCategory.OST_GenericModel,            // Generic Model
                (int)BuiltInCategory.OST_LightingDevices,         // Lighting Devices
                (int)BuiltInCategory.OST_LightingFixtures,        // Lighting Fixture
                (int)BuiltInCategory.OST_MechanicalEquipment,     // Mechanical Equipments
                (int)BuiltInCategory.OST_NurseCallDevices,        // Nurse Call Device
                (int)BuiltInCategory.OST_PipeAccessory,           // Pipe Accessories
                (int)BuiltInCategory.OST_PipeFitting,             // Pipe Fittings
                (int)BuiltInCategory.OST_PlumbingFixtures,        // Plumbing Fixtures
                (int)BuiltInCategory.OST_SecurityDevices,         // Security Devices
                (int)BuiltInCategory.OST_Sprinklers,              // Sprinklers
                (int)BuiltInCategory.OST_TelephoneDevices         // Telephone Devices
            };

            return lstCategory;
        }
    }
}
