using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Diagnostics;

namespace PlumbingSolution.FireProtection.Commands
{
    public static class PipeExtension
    {
        public static XYZ FlattenPoint(this XYZ point3d, double z = 0)
        {
            return new XYZ(point3d.X, point3d.Y, z);
        }

        public static List<Connector> GetConectors(this ConnectorSet connectors)
        {
            List<Connector> connects = new List<Connector>();
            foreach (Connector connector in connectors)
            {
                connects.Add(connector);
            }
            return connects;
        }

        public static Curve GetCurve(this Element element)
        {
            Debug.Assert(null != element.Location,
              "Expected an element with a valid Location");

            LocationCurve locationCurve = element.Location as LocationCurve;

            Debug.Assert(null != locationCurve,
              "Expected an element with a valid LocationCurve");

            return locationCurve.Curve;
        }

        public static Line GetFlattenCurve(this Element element)
        {
            Curve realCurve = element.GetCurve();
            Line retLine = Line.CreateBound(new XYZ(realCurve.GetEndPoint(0).X, realCurve.GetEndPoint(0).Y, 0), new XYZ(realCurve.GetEndPoint(1).X, realCurve.GetEndPoint(1).Y, 0));
            return retLine;
        }

        public static Line GetExpandFlattenCurve(this Element element, double expandVal = 0)
        {
            Curve realCurve = element.GetFlattenCurve();
            Line lineFromCurve = realCurve as Line;

            XYZ newStartPoint = lineFromCurve.Evaluate(lineFromCurve.GetEndParameter(0) - expandVal, false);
            XYZ newEndPoint = lineFromCurve.Evaluate(lineFromCurve.GetEndParameter(1) + expandVal, false);

            return Line.CreateBound(newStartPoint, newEndPoint);
        }
    }
}
