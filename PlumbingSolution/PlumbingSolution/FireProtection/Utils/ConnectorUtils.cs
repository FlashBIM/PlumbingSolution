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
    

        public static void DisconnectFrom(FamilyInstance fittingWye, out Connector connectedSt, out Connector connectedEnd, out Element eleSt, out Element eleEnd)
        {
            connectedSt = null;
            connectedEnd = null;
            eleSt = null;
            eleEnd = null;
            if (fittingWye != null)
            {
                Common.GetInformationConectorWye(fittingWye, null, out Connector conSt, out Connector conEnd, out Connector conNhanhWye);

                if (conSt != null && conSt.IsConnected)
                {
                    foreach (Connector item in conSt.AllRefs)
                    {
                        if (item != null && item.IsConnectedTo(conSt))
                        {
                            conSt.DisconnectFrom(item);

                            if (item != null && item.Owner != null && item.Owner.Id != fittingWye.Id)
                            {
                                connectedSt = item;
                                eleSt = item.Owner;
                            }
                        }
                    }
                }

                if (conEnd != null && conEnd.IsConnected)
                {
                    foreach (Connector item in conEnd.AllRefs)
                    {
                        if (item != null && item.IsConnectedTo(conEnd))
                        {
                            conEnd.DisconnectFrom(item);

                            if (item != null && item.Owner != null && item.Owner.Id != fittingWye.Id)
                            {
                                connectedEnd = item;
                                eleEnd = item.Owner;
                            }
                        }
                    }
                }
            }
        }

        public static List<Connector> ToList(ConnectorManager connectorManager)
        {
            List<Connector> retval = new List<Connector>();

            if (connectorManager != null)
            {
                foreach (Connector con in connectorManager.Connectors)
                {
                    if (con != null)
                        retval.Add(con);
                }
            }

            return retval;
        }

        public static Element GetElementConnectedWithConnector(Connector con)
        {
            if (con != null && con.IsConnected)
            {
                Element main = con.Owner as Element;

                foreach (Connector item in con.AllRefs)
                {
                    Element ele = item.Owner;
                    if (null != ele && main.Id != ele.Id && (ele is FamilyInstance || ele is MEPCurve))
                    {
                        if (ele.Category.Id.ToInt() != (int)BuiltInCategory.OST_DuctInsulations
                               && ele.Category.Id.ToInt() != (int)BuiltInCategory.OST_PipeInsulations
                               && ele.Category.Id.ToInt() != (int)BuiltInCategory.OST_DuctLinings)
                            return ele;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Lấy ra connector gần nhất và xa nhất với 1 điểm cho trước
        /// </summary>
        /// <param name="point"></param>
        /// <param name="pipe"></param>
        /// <param name="outFarest"></param>
        /// <returns></returns>
        public static Connector GetConnectorNearest(XYZ point, MEPCurve pipe, out Connector outFarest)
        {
            Connector retval = null;
            outFarest = null;

            if (point != null && pipe != null)
            {
                ConnectorManager connectorManager = pipe.ConnectorManager;

                double max = double.MaxValue;
                double min = double.MinValue;

                foreach (Connector item in connectorManager.Connectors)
                {
                    double distance = item.Origin.DistanceTo(point);

                    // lấy connector gần nhất
                    if (distance < max)
                    {
                        max = distance;
                        retval = item;
                    }
                    // lấy connector xa nhất
                    if (distance > min)
                    {
                        min = distance;
                        outFarest = item;
                    }
                }
            }

            return retval;
        }
    

        public static void DisconnectFrom(Connector conInput, out Element eleInput)
        {
            eleInput = null;

            if (conInput != null && conInput.IsConnected)
            {
                Element main = conInput.Owner as Element;

                foreach (Connector item in conInput.AllRefs)
                {
                    if (item != null && item.IsConnectedTo(conInput))
                    {
                        Element ele = item.Owner;

                        if (ele != null && ele.Id != main.Id && (ele is FamilyInstance || ele is MEPCurve))
                        {
                            if (ele.Category.Id.ToInt() != (int)BuiltInCategory.OST_DuctInsulations
                                && ele.Category.Id.ToInt() != (int)BuiltInCategory.OST_PipeInsulations
                                && ele.Category.Id.ToInt() != (int)BuiltInCategory.OST_DuctLinings)
                            {
                                eleInput = ele;
                                conInput.DisconnectFrom(item);
                                break;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Lấy ra connector gần nhất và xa nhất với 1 điểm cho trước
        /// </summary>
        /// <param name="point"></param>
        /// <param name="pipe"></param>
        /// <param name="outFarest"></param>
        /// <returns></returns>
        public static Connector GetConnectorNearest(XYZ point, List<Connector> connectors, out Connector outFarest, bool is2D = false)
        {
            Connector retval = null;
            outFarest = null;

            if (point != null && connectors != null)
            {
                double max = double.MaxValue;
                double min = double.MinValue;

                if (is2D)
                    point = Common.To2D(point);

                foreach (Connector item in connectors)
                {
                    XYZ conPoint = new XYZ(item.Origin.X, item.Origin.Y, item.Origin.Z);

                    if (is2D)
                        conPoint = Common.To2D(conPoint);

                    double distance = conPoint.DistanceTo(point);

                    // lấy connector gần nhất
                    if (distance < max)
                    {
                        max = distance;
                        retval = item;
                    }
                    // lấy connector xa nhất
                    if (distance > min)
                    {
                        min = distance;
                        outFarest = item;
                    }
                }
            }

            return retval;
        }

        public static Connector GetConnectorNearest(XYZ point, ConnectorManager connectorManager, out Connector outFarest, bool is2D = false)
        {
            Connector retval = null;
            outFarest = null;

            if (point != null && connectorManager != null)
            {
                if (is2D)
                    point = Common.To2D(point);

                double max = double.MaxValue;
                double min = double.MinValue;

                foreach (Connector item in connectorManager.Connectors)
                {
                    if (item.ConnectorType != ConnectorType.End)
                        continue;

                    XYZ conPoint = new XYZ(item.Origin.X, item.Origin.Y, item.Origin.Z);

                    if (is2D)
                        conPoint = Common.To2D(conPoint);

                    double distance = conPoint.DistanceTo(point);

                    // lấy connector gần nhất
                    if (distance < max)
                    {
                        max = distance;
                        retval = item;
                    }
                    // lấy connector xa nhất
                    if (distance > min)
                    {
                        min = distance;
                        outFarest = item;
                    }
                }
            }

            return retval;
        }
    }
}
