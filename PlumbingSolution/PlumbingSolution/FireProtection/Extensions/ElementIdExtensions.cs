using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Text;

namespace PlumbingSolution.FireProtection.Extensions
{
    public static class ElementIdExtensions
    {
        public static int ToInt(this ElementId elementId)
        {
#if Release_2026 || Debug_2026 || Bundle_2026 || Release_2027 || Debug_2027 || Bundle_2027
            return (int)elementId.Value;
#else
            return elementId.IntegerValue;
#endif
        }
    }



    public static class GeometryInstanceExtensions
    {
        public static string GetNameSymbol(this GeometryInstance geomInstance, Document doc)
        {
            if (geomInstance == null || doc == null)
                return "";

#if Release_2026 || Debug_2026 || Release_2025 || Debug_2025 || Release_2024 || Debug_2024 || Bundle_2024 || Bundle_2025|| Bundle_2026 || Release_2027 || Debug_2027 || Bundle_2027
            Element element = doc.GetElement(geomInstance.GetSymbolGeometryId().SymbolId);
            return element?.Name;

#else
            return geomInstance.Symbol.Name;

#endif
        }
    }
}
