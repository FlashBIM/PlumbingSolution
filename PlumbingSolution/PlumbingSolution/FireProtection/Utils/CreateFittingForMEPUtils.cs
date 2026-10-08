using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using System.Collections.Generic;

namespace PlumbingSolution.FireProtection.Ultis
{

    public enum ft
    {
        Invalid = -1,
        Elbow = 0,
        Tee,
        Union,
        Cross
    }
}
