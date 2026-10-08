using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PlumbingSolution.FireProtection.Utils;

namespace PlumbingSolution.FireProtection.Ultis
{
    public class ParameterUtils
    {


        public static Parameter GetAssociatedParameter(Element element, Connector connector, BuiltInParameter connectorParameter)
        {
            if (connector == null)
            {
                Utils.IO.LogException("method SetRadiusConnector: Connector is null");
                return null;
            }
            var connectorInfo = connector.GetMEPConnectorInfo() as MEPFamilyConnectorInfo;

            if (connectorInfo == null)
                return null;

            var associatedFamilyParameterId = connectorInfo.GetAssociateFamilyParameterId(new ElementId(connectorParameter));

            if (associatedFamilyParameterId == ElementId.InvalidElementId)
                return null;

            var document = element.Document;

            var parameterElement = document.GetElement(associatedFamilyParameterId) as ParameterElement;

            if (parameterElement == null)
                return null;

            var paramterDefinition = parameterElement.GetDefinition();

            return element.get_Parameter(paramterDefinition);
        }
    

        /// <summary>
        /// Set giá trị cho paramter theo tên
        /// </summary>
        /// <param name="el"></param>
        /// <param name="parameterName"></param>
        /// <param name="valuePara"></param>
        /// <returns></returns>
        public static bool SetValueParameterByName(Element el, string parameterName, object valuePara)
        {
            if (el == null || string.IsNullOrEmpty(parameterName) || valuePara == null)
                return false;
            Parameter prm = el.LookupParameter(parameterName);
            if (prm != null && !prm.IsReadOnly)
            {
                if (prm.StorageType == StorageType.ElementId)
                {
                    prm.Set((ElementId)valuePara);

                    return true;
                }
                if (prm.StorageType == StorageType.Double)
                {
                    prm.Set((double)valuePara);

                    return true;
                }
                if (prm.StorageType == StorageType.Integer)
                {
                    prm.Set((int)valuePara);

                    return true;
                }
                if (prm.StorageType == StorageType.String)
                {
                    prm.Set((string)valuePara);

                    return true;
                }
            }
            return false;
        }

        /// <summary>
        ///  sét giá trị parameter theo BuiltInParameter
        /// </summary>
        /// <param name="element"></param>
        /// <param name="builtIn"></param>
        /// <param name="valuePara"></param>
        /// <returns></returns>
        public static bool SetValueParameterByBuiltIn(Element element, BuiltInParameter builtIn, object valuePara)
        {
            if (element == null || element.IsValidObject == false || valuePara == null)
                return false;
            Parameter prm = element.get_Parameter(builtIn);
            if (prm != null && !prm.IsReadOnly)
            {
                if (prm.StorageType == StorageType.ElementId)
                {
                    prm.Set((ElementId)valuePara);

                    return true;
                }
                if (prm.StorageType == StorageType.Double)
                {
                    prm.Set((double)valuePara);

                    return true;
                }
                if (prm.StorageType == StorageType.Integer)
                {
                    prm.Set((int)valuePara);

                    return true;
                }
                if (prm.StorageType == StorageType.String)
                {
                    prm.Set((string)valuePara);

                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Lấy giá trị của paramter theo built in parameter
        /// </summary>
        /// <param name="element"></param>
        /// <param name="buintinparameter"></param>
        /// <returns></returns>
        public static object GetValueParameterByBuilt(Element element, BuiltInParameter buintinparameter)
        {
            if (element == null)
                return null;
            Parameter prm = element.get_Parameter(buintinparameter);
            if (prm != null)
            {
                if (prm.StorageType == StorageType.ElementId)
                {
                    return prm.AsElementId();
                }
                if (prm.StorageType == StorageType.Double)
                {
                    return prm.AsDouble();
                }
                if (prm.StorageType == StorageType.Integer)
                {
                    return prm.AsInteger();
                }
                if (prm.StorageType == StorageType.String)
                {
                    return prm.AsString();
                }
            }
            return null;
        }
    }
}
