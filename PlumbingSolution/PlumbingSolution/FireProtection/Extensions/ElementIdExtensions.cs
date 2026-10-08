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

            // PlumbingSolution chỉ build cho Revit 2024+ (GeometryInstance.Symbol đã bỏ), nên dùng thẳng API mới;
            // ký hiệu cấu hình kiểu Dirit (Release_2024...) không có trong project này.
            Element element = doc.GetElement(geomInstance.GetSymbolGeometryId().SymbolId);
            return element?.Name;
        }
    }
}
