using PlumbingSolution.FireProtection.Extensions;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace PlumbingSolution.FireProtection.Ultis
{
    public static class ConnectorUtils
    {

        public static void GetConnectorOppositeNearestClosedTo(ConnectorManager connectorManager1, List<Connector> connectors2, out Connector con1, out Connector con2)
        {
            con1 = null;
            con2 = null;

            if (connectorManager1 != null && connectors2 != null && connectors2.Count >= 1)

            {
                double distanceMin = double.MaxValue;

                foreach (Connector item1 in connectorManager1.Connectors)
                {
                    foreach (Connector item2 in connectors2)
                    {
                        if (item1.CoordinateSystem.BasisZ.DotProduct(item2.CoordinateSystem.BasisZ) < 0)
                        {
                            double distance = item1.Origin.DistanceTo(item2.Origin);
                            if (distance < distanceMin)
                            {
                                con1 = item1;
                                con2 = item2;
                                distanceMin = distance;
                            }
                        }
                    }
                }
            }
        }

        public static void GetConnectorOppositeNearestClosedTo(List<Connector> connectors1, List<Connector> connectors2, out Connector con1, out Connector con2)
        {
            con1 = null;
            con2 = null;

            if (connectors1 != null && connectors2 != null && connectors1.Count >= 1 && connectors2.Count >= 1)

            {
                double distanceMin = double.MaxValue;

                foreach (Connector item1 in connectors1)
                {
                    foreach (Connector item2 in connectors2)
                    {
                        if (item1.CoordinateSystem.BasisZ.DotProduct(item2.CoordinateSystem.BasisZ) < 0)
                        {
                            double distance = item1.Origin.DistanceTo(item2.Origin);
                            if (distance < distanceMin)
                            {
                                con1 = item1;
                                con2 = item2;
                                distanceMin = distance;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Lấy connector chưa được kết nối
        /// </summary>
        /// <param name="connectorManager"></param>
        /// <returns></returns>
        public static Connector GetConnectorNotConnnected(ConnectorManager connectorManager)
        {
            if (connectorManager != null)
            {
                foreach (Connector con in connectorManager.Connectors)
                {
                    if (!con.IsConnected)
                        return con;
                }
            }

            return null;
        }

        public static Connector GetConnectorNotConnnected2(ConnectorManager connectorManager)
        {
            if (connectorManager != null)
            {
                foreach (Connector con in connectorManager.Connectors)
                {
                    if (Common.GetFittingConnected(con) == null)
                        return con;
                }
            }

            return null;
        }

        /// <summary>
        /// Lấy connector  đã kết nối
        /// </summary>
        /// <param name="connectorManager"></param>
        /// <returns></returns>
        public static Connector GetConnectorConnnected(ConnectorManager connectorManager)
        {
            if (connectorManager != null)
            {
                foreach (Connector con in connectorManager.Connectors)
                {
                    if (con.IsConnected)
                        return con;
                }
            }

            return null;
        }

        /// <summary>
        /// Lấy 2 connector thuộc 2 ống khác nhau và gần nhau nhất
        /// </summary>
        /// <param name="connectorManager1"></param>
        /// <param name="connectorManager2"></param>
        /// <param name="con1"></param>
        /// <param name="con2"></param>
        public static void GetConnectorClosedTo(ConnectorManager connectorManager1, ConnectorManager connectorManager2, out Connector con1, out Connector con2)
        {
            con1 = null;
            con2 = null;

            if (connectorManager1 != null && connectorManager2 != null)

            {
                double distanceMin = double.MaxValue;

                foreach (Connector item1 in connectorManager1.Connectors)
                {
                    foreach (Connector item2 in connectorManager2.Connectors)
                    {
                        double distance = item1.Origin.DistanceTo(item2.Origin);
                        if (distance < distanceMin)
                        {
                            con1 = item1;
                            con2 = item2;
                            distanceMin = distance;
                        }
                    }
                }
            }
        }

        public static void GetConnectorOppositeNearestClosedTo(ConnectorManager connectorManager1, ConnectorManager connectorManager2, out Connector con1, out Connector con2)
        {
            con1 = null;
            con2 = null;

            if (connectorManager1 != null && connectorManager2 != null)

            {
                double distanceMin = double.MaxValue;

                foreach (Connector item1 in connectorManager1.Connectors)
                {
                    if (item1.ConnectorType != ConnectorType.End)
                        continue;

                    foreach (Connector item2 in connectorManager2.Connectors)
                    {
                        if (item2.ConnectorType != ConnectorType.End)
                            continue;

                        if (item1.CoordinateSystem.BasisZ.DotProduct(item2.CoordinateSystem.BasisZ) < 0)
                        {
                            double distance = item1.Origin.DistanceTo(item2.Origin);
                            if (distance < distanceMin)
                            {
                                con1 = item1;
                                con2 = item2;
                                distanceMin = distance;
                            }
                        }
                    }
                }
            }
        }
    }
}
