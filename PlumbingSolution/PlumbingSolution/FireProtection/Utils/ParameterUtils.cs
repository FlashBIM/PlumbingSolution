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
    }
}
