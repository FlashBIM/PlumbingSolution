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
    }
}
