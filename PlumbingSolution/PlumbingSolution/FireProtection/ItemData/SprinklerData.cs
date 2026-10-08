using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Text;

namespace PlumbingSolution.FireProtection.ItemData
{
    public class SprinklerData
    {
        public FamilyInstance Instance { get; set; }
        public XYZ Point { get; set; }
        public XYZ Direction { get; set; } // Vector hướng chuẩn hóa từ ống ra
        public double DistanceToPipe { get; set; }
    }
}
