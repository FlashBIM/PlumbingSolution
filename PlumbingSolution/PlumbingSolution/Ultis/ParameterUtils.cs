using Autodesk.Revit.DB;

namespace PlumbingSolution.Ultis
{
    /// <summary>
    /// Đọc và ghi parameter theo tên, giữ đúng StorageType của Revit.
    /// </summary>
    internal static class ParameterUtils
    {
        public static object GetValueParameterByName(Element element, string parameterName)
        {
            Parameter parameter = element?.LookupParameter(parameterName);
            if (parameter == null)
                return null;

            switch (parameter.StorageType)
            {
                case StorageType.Double:
                    return parameter.AsDouble();
                case StorageType.Integer:
                    return parameter.AsInteger();
                case StorageType.String:
                    return parameter.AsString();
                case StorageType.ElementId:
                    return parameter.AsElementId();
                default:
                    return null;
            }
        }

        public static bool SetValueParameterByName(Element element, string parameterName, object value)
        {
            Parameter parameter = element?.LookupParameter(parameterName);
            if (parameter == null || parameter.IsReadOnly || value == null)
                return false;

            switch (parameter.StorageType)
            {
                case StorageType.Double:
                    return parameter.Set(System.Convert.ToDouble(value));
                case StorageType.Integer:
                    return parameter.Set(System.Convert.ToInt32(value));
                case StorageType.String:
                    return parameter.Set(System.Convert.ToString(value));
                case StorageType.ElementId when value is ElementId elementId:
                    return parameter.Set(elementId);
                default:
                    return false;
            }
        }
    }
}
