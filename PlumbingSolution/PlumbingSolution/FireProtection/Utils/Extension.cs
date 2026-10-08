using PlumbingSolution.FireProtection.Ultis;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlumbingSolution.FireProtection.Extensions;

namespace PlumbingSolution.FireProtection.Utils
{
    public static class Extension
    {

        public static XYZ To2D(this XYZ point, double z = 0.0)
        {
            if (point == null)
                return null;
            return new XYZ(point.X, point.Y, z);
        }

        public static XYZ ProjectPointToPlane(this XYZ point, double elevation = 0)
        {
            return new XYZ(point.X, point.Y, elevation);
        }

        public static bool IsEqual(this Element elem1, Element elem2)
        {
            return elem1.Id.ToInt() == elem2.Id.ToInt();
        }

        public static bool IsEqual(this double x, double y)
        {
            return Common.IsEqual(x, y);
        }

        public static Line ProjectLineToPlane(this Line line, double elevation = 0)
        {
            XYZ startOnPlane = line.GetEndPoint(0).ProjectPointToPlane();
            XYZ endOnPlane = line.GetEndPoint(1).ProjectPointToPlane();

            return Line.CreateBound(startOnPlane, endOnPlane);
        }

        public static bool IsEqual(this XYZ check, XYZ other)
        {
            return Common.IsEqual(check.X, other.X, 10e-3) &&
                   Common.IsEqual(check.Y, other.Y, 10e-3) &&
                   Common.IsEqual(check.Z, other.Z, 10e-3);
        }
    }
}
