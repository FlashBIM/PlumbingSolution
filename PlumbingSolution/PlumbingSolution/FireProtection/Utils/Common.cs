using PlumbingSolution.FireProtection.Utils;
using PlumbingSolution.FireProtection.Extensions;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.ExtensibleStorage;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Windows.Shapes;
using Form = System.Windows.Forms.Form;
using Line = Autodesk.Revit.DB.Line;
using Path = System.IO.Path;
using TextBox = System.Windows.Forms.TextBox;

namespace PlumbingSolution.FireProtection.Ultis
{
    public static partial class Common
    {
        public static double mmToFT = 0.0032808399;
        private const double _eps = 1.0e-9;

        public static List<string> GetTextLanguage(List<string> keys)
        {
            List<(int, string)> values = new List<(int, string)>();
            try
            {
                string path = System.IO.Path.Combine(IO.GetAssemlyFolder(), Define.FolderLanguage, Define.FileLanguage);
                //string path = @"E:\CTN Project\Source Code\PlumbingSolution.FireProtection\PlumbingSolution.FireProtection\Languages\Language.csv";

                var lines = System.IO.File.ReadAllLines(path);

                if (lines.Length < 2 || lines.Length < 4)
                    return new List<string>();

                int langIndex = App.LanguageIndex;

                for (int i = 1; i < lines.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(lines[i])) continue;

                    var parts = lines[i].Split(',').ToList();

                    if (parts.Count <= langIndex) continue;

                    string key = parts[0].Trim();
                    if (keys.Contains(key))
                    {
                        ValidateCSVCell(ref parts);
                        string value = parts[langIndex].Trim();
                        int index = keys.IndexOf(key);
                        values.Add((index, value));
                    }
                }
            }
            catch (Exception ex)
            {
                IO.LogException(ex);
                return new List<string>();
            }
            return values.OrderBy(x => x.Item1).Select(x => x.Item2).ToList();
        }

        public static string GetTextLanguage(string inputKey)
        {
            string value = string.Empty;
            try
            {
                string path = System.IO.Path.Combine(IO.GetAssemlyFolder(), Define.FolderLanguage, Define.FileLanguage);
                //string path = @"E:\CTN Project\Source Code\PlumbingSolution.FireProtection\PlumbingSolution.FireProtection\Languages\Language.csv";

                var lines = System.IO.File.ReadAllLines(path);
                int langIndex = App.LanguageIndex;

                for (int i = 1; i < lines.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(lines[i])) continue;

                    var parts = lines[i].Split(',').ToList();

                    if (parts.Count <= langIndex) continue;

                    string key = parts[0].Trim();
                    if (key.Equals(inputKey))
                    {
                        ValidateCSVCell(ref parts);
                        value = parts[langIndex].Trim();
                    }
                }
            }
            catch (Exception ex)
            {
                IO.LogException(ex);
            }
            return value;
        }

        public static void SettingLanguage(Dictionary<string, System.Windows.Forms.Control> key_control)
        {
            try
            {
                string path = System.IO.Path.Combine(IO.GetAssemlyFolder(), Define.FolderLanguage, Define.FileLanguage);
                //string path = @"C:\Users\anlin\AppData\Roaming\Autodesk\ApplicationPlugins\AddinDirit.bundle\Contents\2024\PlumbingSolution.FireProtection\Language\Language.csv";

                var lines = System.IO.File.ReadAllLines(path);

                if (lines.Length < 2 || lines.Length < 4)
                    return;

                int langIndex = App.LanguageIndex;

                int count = 0;
                for (int i = 1; i < lines.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(lines[i]))
                        continue;

                    var parts = lines[i].Split(',').ToList();

                    if (parts.Count <= langIndex)
                        continue;

                    string key = parts[0].Trim();
                    if (key_control.ContainsKey(key))
                    {
                        ValidateCSVCell(ref parts);
                        string value = parts[langIndex].Trim();
                        key_control[key].Text = value;
                        count++;

                        if (count == key_control.Count)
                            continue; ;
                    }
                }
            }
            catch (Exception ex)
            {
                IO.LogException(ex);
            }
        }

        private static void ValidateCSVCell(ref List<string> parts)
        {
            string join = "";
            List<int> removes = new List<int>();
            for (int j = parts.Count - 1; j > -1; j--)
            {
                if (parts[j].LastOrDefault().ToString() == "\"" && parts[j].FirstOrDefault().ToString() != "\"")
                {
                    join = parts[j] + (join.Length > 0 ? "," : "") + join;
                    removes.Add(j);
                }
                if (parts[j].FirstOrDefault().ToString() == "\"" && parts[j].LastOrDefault().ToString() != "\"")
                {
                    parts[j] = parts[j] + "," + join;
                    join = "";
                    foreach (var ind in removes)
                        parts.RemoveAt(ind);
                    removes.Clear();
                }
            }
        }

        /// <summary>Giao diện chung của form (nền trắng, nút teal...): xem UI/FormStyle.cs.</summary>
        public static void SettingTemplate(System.Windows.Forms.Form form)
        {
            PlumbingSolution.FireProtection.UI.FormStyle.Apply(form);
        }


        public static string GetMEPPreviewFolder()
        {
            string appDir = GetPreviewFolder();
            //string appDir = "D:\\SourceCode\\PlumbingSolution.FireProtection\\Resource\\Icon\\Preview";
            string imageDir = System.IO.Path.Combine(appDir, "DIRIT MECH 2");
            return imageDir;
        }

        public static string GetFirePreviewFolder()
        {
            string appDir = GetPreviewFolder();
            //string appDir = "D:\\SourceCode\\PlumbingSolution.FireProtection\\Resource\\Icon\\Preview";
            string imageDir = System.IO.Path.Combine(appDir, "DIRIT FIRE");
            return imageDir;
        }

        public static string GetPreviewFolder()
        {
            string imageDir = System.IO.Path.Combine(IO.GetAssemlyFolder(), "Icon\\Preview");
            return imageDir;
        }

        public static string GetIconFolder()
        {
            //string imageDir = "D:\\SourceCode\\PlumbingSolution.FireProtection\\Resource\\Icon\\IconProcess";
            string imageDir = System.IO.Path.Combine(IO.GetAssemlyFolder(), "Icon\\IconProcess");
            return imageDir;
        }

        public static bool IsFormSameOpen(string nameForm)
        {
            bool checkIsOpen = false;

            foreach (System.Windows.Forms.Form openedForm in Application.OpenForms)
            {
                if (openedForm.GetType().Name == nameForm)
                {
                    openedForm.WindowState = FormWindowState.Normal;
                    checkIsOpen = true;
                    break;
                }
            }

            return checkIsOpen;
        }

        public static void SetRadiusConnector(Element element, Connector connector, double value)
        {
            if (connector == null)
            {
                IO.LogException("method SetRadiusConnector: Connector is null");
                return;
            }
            try
            {
                var param = ParameterUtils.GetAssociatedParameter(element, connector, BuiltInParameter.CONNECTOR_DIAMETER);

                if (param != null &&
                    !param.IsReadOnly &&
                    param.StorageType == StorageType.Double)
                {
                    param.Set(value * 2);
                }
                else
                {
                    param = ParameterUtils.GetAssociatedParameter(element, connector, BuiltInParameter.CONNECTOR_RADIUS);

                    if (param != null &&
                        !param.IsReadOnly &&
                        param.StorageType == StorageType.Double)
                    {
                        param.Set(value);
                    }
                    else
                    {
                        connector.Radius = value;
                    }
                }
            }
            catch (Exception ex)
            {
                IO.LogException(ex);
            }
        }

        public static void NumberCheck(object sender, KeyPressEventArgs e, bool allowNegativeValue = false) // < 0
        {
            if (allowNegativeValue == false)
            {
                if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && (e.KeyChar != '.'))
                {
                    e.Handled = true;
                }

                // only allow one decimal point
                if ((e.KeyChar == '.') && ((sender as System.Windows.Forms.TextBox).Text.IndexOf('.') > -1))
                {
                    e.Handled = true;
                }
            }
            else
            {
                if (!char.IsControl(e.KeyChar) && (!char.IsDigit(e.KeyChar)) && (e.KeyChar != '.') && (e.KeyChar != '-'))
                    e.Handled = true;

                // only allow one decimal point
                if (e.KeyChar == '.' && (sender as System.Windows.Forms.TextBox).Text.IndexOf('.') > -1)
                    e.Handled = true;

                // only allow minus sign at the beginning
                if (e.KeyChar == '-' && (sender as System.Windows.Forms.TextBox).Text.IndexOf('-') > -1)
                    e.Handled = true;
            }
        }

        /// <summary>
        /// project a point onto plane
        /// </summary>
        public static XYZ ProjectPointOnPlane(Plane plane, XYZ point)
        {
            plane.Project(point, out UV uv1, out double d);
            XYZ projectedPoint = plane.Origin + (uv1.U * plane.XVec) + (uv1.V * plane.YVec);
            return projectedPoint;
        }

        public static XYZ To2D(XYZ point, double z = 0.0)
        {
            if (point == null)
                return null;

            return new XYZ(point.X, point.Y, z);
        }

        public static Line To2D(Line line)
        {
            if (line == null)
                return null;

            XYZ p1 = To2D(line.GetEndPoint(0));

            XYZ p2 = To2D(line.GetEndPoint(1));

            if (p1.IsAlmostEqualTo(p2))
                return null;

            return Line.CreateBound(p1, p2);
        }

        public static bool IsBetween2Point(XYZ st, XYZ end, XYZ pointCheck)
        {
            if (st != null && end != null && pointCheck != null)
            {
                if (st.IsAlmostEqualTo(pointCheck)
               || end.IsAlmostEqualTo(pointCheck))
                    return false;

                XYZ vec1 = (pointCheck - st).Normalize();

                XYZ vec2 = (pointCheck - end).Normalize();

                if (vec1.DotProduct(vec2) < 0)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Get information of connector tee
        /// </summary>
        /// <param name="fitting"></param>
        /// <param name="vector"></param>
        /// <param name="main1"></param>
        /// <param name="main2"></param>
        /// <param name="tee"></param>
        public static void GetInformationConectorWye(FamilyInstance fitting, XYZ vector, out Connector main1, out Connector main2, out Connector tee)
        {
            main1 = null;
            main2 = null;
            tee = null;
            if (fitting != null)
            {
                //Get fitting info

                GetConnectorMain(fitting, vector, out main1, out main2);

                foreach (Connector c in fitting.MEPModel.ConnectorManager.Connectors)
                {
                    if (c.Id != main1.Id && c.Id != main2.Id)
                    {
                        tee = c;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Get connector main
        /// </summary>
        /// <param name="fitting"></param>
        /// <param name="vector"></param>
        /// <param name="mainConnect1"></param>
        /// <param name="mainConnect2"></param>
        public static void GetConnectorMain(FamilyInstance fitting, XYZ vector, out Connector mainConnect1, out Connector mainConnect2)
        {
            mainConnect1 = null;
            mainConnect2 = null;

            if (vector == null && fitting.MEPModel.ConnectorManager.Connectors.Size == 3)
            {
                //Main : hướng connector của 2 connector fai song song voi nhau (nguoc chieu nhau)

                foreach (Connector c1 in fitting.MEPModel.ConnectorManager.Connectors)
                {
                    foreach (Connector c2 in fitting.MEPModel.ConnectorManager.Connectors)
                    {
                        if (c1.Id == c2.Id)
                        {
                            continue;
                        }
                        else
                        {
                            var z1 = c1.CoordinateSystem.BasisZ;
                            var z2 = c2.CoordinateSystem.BasisZ;

                            if (IsParallel(z1, z2, 0.0001) == true)
                            {
                                mainConnect1 = c1;
                                mainConnect2 = c2;
                                break;
                            }
                        }
                    }

                    if (mainConnect1 != null && mainConnect2 != null)
                        break;
                }
            }
            else
            {
                foreach (Connector con in fitting.MEPModel.ConnectorManager.Connectors)
                {
                    if (vector != null)
                    {
                        if (IsParallel(vector, con.CoordinateSystem.BasisZ, 0.0001) == false)
                        {
                            continue;
                        }
                    }

                    if (mainConnect1 == null)
                        mainConnect1 = con;
                    else
                    {
                        mainConnect2 = con;
                        break;
                    }
                }
            }

            if (mainConnect1 != null && mainConnect2 != null)
            {
                //Connect nao gan location of fitting thi do la 1

                var p = (fitting.Location as LocationPoint).Point;
                if (mainConnect1.Origin.DistanceTo(p) > mainConnect2.Origin.DistanceTo(p))
                {
                    Connector temp = mainConnect1;
                    mainConnect1 = mainConnect2;

                    mainConnect2 = temp;
                }
            }
        }

        /// <summary>
        /// Lấy ra mepcurve đang kết nối
        /// </summary>
        /// <param name="connectorManager"></param>
        /// <returns></returns>
        public static Pipe GetPipeConnected(ConnectorManager connectorManager)
        {
            if (connectorManager != null)
            {
                foreach (Connector item in connectorManager.Connectors)
                {
                    foreach (Connector con in item.AllRefs)
                    {
                        Pipe pipe = con.Owner as Pipe;
                        if (null != pipe)
                            return pipe;
                    }
                }
            }

            return null;
        }

        public static List<(Pipe, Connector)> GetAllPipesConnected(ConnectorManager connectorManager)
        {
            List<(Pipe, Connector)> dic = new System.Collections.Generic.List<(Pipe, Connector)>();
            if (connectorManager != null)
            {
                foreach (Connector item in connectorManager.Connectors)
                {
                    foreach (Connector con in item.AllRefs)
                    {
                        Pipe pipe = con.Owner as Pipe;
                        if (null != pipe)
                        {
                            dic.Add((pipe, con));
                            break;
                        }
                    }
                }
            }

            return dic;
        }

        public static Pipe GetPipeConnected(Connector connector)
        {
            if (connector != null)
            {
                foreach (Connector con in connector.AllRefs)
                {
                    Pipe pipe = con.Owner as Pipe;
                    if (null != pipe)
                        return pipe;
                }
            }

            return null;
        }

        /// <summary>
        /// Lấy ra fitting đang kết nối với ống
        /// </summary>
        /// <param name="conectorIsConnecting"></param>
        /// <returns></returns>
        public static FamilyInstance GetFittingConnected(Connector conectorIsConnecting)
        {
            if (conectorIsConnecting == null || !conectorIsConnecting.IsConnected)
                return null;

            foreach (Connector con in conectorIsConnecting.AllRefs)
            {
                FamilyInstance eleCheck = con.Owner as FamilyInstance;
                if (null != eleCheck)
                    return eleCheck;
            }

            return null;
        }

        public static bool IsEqual(double first, double second, double tolerance = 10e-4)
        {
            double result = Math.Abs(first - second);
            return result < tolerance;
        }

        public static bool IsEqual(XYZ first, XYZ second)
        {
            return IsEqual(first.X, second.X)
                && IsEqual(first.Y, second.Y)
                && IsEqual(first.Z, second.Z);
        }

        public static bool IsParallel(XYZ p, XYZ q, double tolerance = 10e-4)
        {
            if (p.CrossProduct(q).IsZeroLength() == true)
                return true;

            var l = p.CrossProduct(q).GetLength();
            if (IsZero(l, tolerance))
                return true;

            return false;
        }

        public static bool IsZero(double a, double tolerance)
        {
            return tolerance > Math.Abs(a);
        }

        public static bool IsEqual(double first, double second)
        {
            double result = Math.Abs(first - second);
            return result < 10e-5;
        }

        public static bool IsPerpendicular(XYZ first, XYZ second, double tolerance = 1e-6)
        {
            double length = first.DotProduct(second);

            return IsEqual(length, 0, tolerance);
        }

        public static void RotateLineC2(Document doc, FamilyInstance wye, Line axisLine)
        {
            var lst = Common.ToList(wye.MEPModel.ConnectorManager.Connectors);

            Connector connector2 = lst[0];
            Connector connector3 = lst[1];

            Line rotateLine = Line.CreateBound(connector2.Origin, connector3.Origin);

            if (IsParallel(axisLine.Direction, rotateLine.Direction))
                return;

            XYZ vector = rotateLine.Direction.CrossProduct(axisLine.Direction);
            XYZ intersection = GetUnBoundIntersection(rotateLine, axisLine);

            double angle = rotateLine.Direction.AngleTo(axisLine.Direction);

            Line line = Line.CreateUnbound(intersection, vector);

            ElementTransformUtils.RotateElement(doc, wye.Id, line, angle);
            doc.Regenerate();
        }

        public static XYZ PointTo2D(XYZ point3d, double z = 0)
        {
            return new XYZ(point3d.X, point3d.Y, z);
        }

        public static List<Connector> ToList(ConnectorSet connectors)
        {
            List<Connector> connects = new List<Connector>();
            foreach (Connector c in connectors)
            {
                connects.Add(c);
            }
            return connects;
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

        public static string FeetToMmString(double a)
        {
            return (a / Common.mmToFT).ToString("0.##");
        }

        public static bool IsParallel(MEPCurve p1, MEPCurve p2)
        {
            Line c1 = p1.GetCurve() as Line;
            Line c2 = p2.GetCurve() as Line;
            return Math.Sin(c1.Direction.AngleTo(
              c2.Direction)) < 0.01;
        }

        /// <summary>
        /// Lấy ra fitting đang kết nối với ống
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="pipe"></param>
        /// <param name="conPipeNotConnect"></param>
        /// <param name="fitingCreating"></param>
        /// <returns></returns>
        public static FamilyInstance GetFittingConnected(Document doc, MEPCurve pipe, Connector conPipeNotConnect)
        {
            if (doc == null || pipe == null)
                return null;

            List<FamilyInstance> allFittings = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .OfCategory(BuiltInCategory.OST_PipeFitting)
                .OfClass(typeof(FamilyInstance))
                .Cast<FamilyInstance>()
                .ToList();
            if (allFittings == null || allFittings.Count == 0)
                return null;

            List<ElementId> allElementIds = allFittings.Select(e => e.Id).ToList();

            // Create a Outline, uses a minimum and maximum XYZ point to initialize the outline.
            Outline myOutLn = CreateOutLineFromBoundingBox(pipe);
            if (myOutLn == null || myOutLn.IsEmpty)
                return null;

            // Create a BoundingBoxIntersects filter with this Outline
            BoundingBoxIntersectsFilter filter = new BoundingBoxIntersectsFilter(myOutLn);

            FilteredElementCollector collector = new FilteredElementCollector(doc, allElementIds);

            List<FamilyInstance> allFittingConnected = collector.WherePasses(filter).Cast<FamilyInstance>().ToList();

            foreach (var ele in allFittingConnected)
            {
                if (ele != null)
                {
                    if (ele.MEPModel != null)
                    {
                        foreach (Connector conNector in ele.MEPModel.ConnectorManager.Connectors)
                        {
                            if (conNector != null)
                            {
                                if (IsEqual(conNector.Origin.X, conPipeNotConnect.Origin.X)
                                    && IsEqual(conNector.Origin.Y, conPipeNotConnect.Origin.Y)
                                    && IsEqual(conNector.Origin.Z, conPipeNotConnect.Origin.Z))
                                {
                                    return ele;
                                }
                            }
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// GetPointOnVector
        /// </summary>
        /// <param name="pointInsert"></param>
        /// <param name="vectorDir"></param>
        /// <param name="dDistance"></param>
        /// <returns></returns>
        public static XYZ GetPointOnVector(XYZ pointInsert, XYZ vectorDir, double dDistance)
        {
            return (pointInsert + (vectorDir.Normalize()) * dDistance);
        }

        /// <summary>
        /// Lấy ra connector gần nhất và xa nhất với 1 điểm cho trước
        /// </summary>
        /// <param name="point"></param>
        /// <param name="pipe"></param>
        /// <param name="outFarest"></param>
        /// <returns></returns>

        public static XYZ GetUnBoundIntersection(Line Line1, Line Line2)
        {
            if (Line1 != null && Line2 != null)
            {
                Curve ExtendedLine1 = Line.CreateUnbound(Line1.Origin, Line1.Direction);
                Curve ExtendedLine2 = Line.CreateUnbound(Line2.Origin, Line2.Direction);
                SetComparisonResult setComparisonResult;

#if Debug_2027 || Release_2027 || Release_2026
    // --- CẤU HÌNH CHO REVIT 2026+ ---
    var intersectResult = ExtendedLine1.Intersect(ExtendedLine2, CurveIntersectResultOption.Detailed);
    setComparisonResult = intersectResult.Result;

    if (setComparisonResult != SetComparisonResult.Disjoint)
    {
        var overlaps = intersectResult.GetOverlaps();
        if (overlaps != null && overlaps.Count > 0)
        {
            foreach (var result in overlaps)
            {
                if (result != null)
                    return result.Point;
            }
        }
    }
#else
                // --- CẤU HÌNH CHO CÁC BẢN CŨ HƠN ---
                IntersectionResultArray resultArray;
                setComparisonResult = ExtendedLine1.Intersect(ExtendedLine2, out resultArray);

                if (resultArray != null && resultArray.Size > 0)
                {
                    foreach (IntersectionResult result in resultArray)
                    {
                        if (result != null)
                            return result.XYZPoint;
                    }
                }
#endif
                else
                {
                    if (Line1.IsBound && Line2.IsBound)
                    {
                        if (Line1.GetEndPoint(0).IsAlmostEqualTo(Line2.GetEndPoint(0)) ||
                  Line1.GetEndPoint(0).IsAlmostEqualTo(Line2.GetEndPoint(1)))
                            return Line1.GetEndPoint(0);
                        else
                            if (Line1.GetEndPoint(1).IsAlmostEqualTo(Line2.GetEndPoint(0)) ||
                           Line1.GetEndPoint(1).IsAlmostEqualTo(Line2.GetEndPoint(1)))
                                return Line1.GetEndPoint(1);
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Lấy ra fitting đang kết nối với ống
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="pipe"></param>
        /// <param name="conPipeNotConnect"></param>
        /// <param name="fitingCreating"></param>
        /// <returns></returns>
        public static FamilyInstance GetFittingConnected(Document doc, MEPCurve pipe, Connector conPipeNotConnect, ElementId fitingCreating)
        {
            if (doc == null || pipe == null || fitingCreating == ElementId.InvalidElementId)
                return null;

            List<FamilyInstance> allFittings = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .OfCategory(BuiltInCategory.OST_PipeFitting)
                .OfClass(typeof(FamilyInstance))
                .Cast<FamilyInstance>()
                .ToList();
            if (allFittings == null || allFittings.Count == 0)
                return null;

            List<ElementId> allElementIds = allFittings.Select(e => e.Id).ToList();

            // Create a Outline, uses a minimum and maximum XYZ point to initialize the outline.
            Outline myOutLn = CreateOutLineFromBoundingBox(pipe);
            if (myOutLn == null || myOutLn.IsEmpty)
                return null;

            // Create a BoundingBoxIntersects filter with this Outline
            BoundingBoxIntersectsFilter filter = new BoundingBoxIntersectsFilter(myOutLn);

            FilteredElementCollector collector = new FilteredElementCollector(doc, allElementIds);

            List<FamilyInstance> allFittingConnected = collector.WherePasses(filter).Cast<FamilyInstance>().ToList();

            foreach (var ele in allFittingConnected)
            {
                if (ele != null && ele.Id != fitingCreating)
                {
                    if (ele.MEPModel != null)
                    {
                        foreach (Connector conNector in ele.MEPModel.ConnectorManager.Connectors)
                        {
                            if (conNector != null)
                            {
                                if (IsEqual(conNector.Origin.X, conPipeNotConnect.Origin.X)
                                    && IsEqual(conNector.Origin.Y, conPipeNotConnect.Origin.Y)
                                    && IsEqual(conNector.Origin.Z, conPipeNotConnect.Origin.Z))
                                {
                                    return ele;
                                }
                            }
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// CreateOutLineFromBoundingBox
        /// </summary>
        /// <param name="ele"></param>
        /// <returns></returns>

        public static Outline CreateOutLineFromBoundingBox(Element ele)
        {
            Outline retVal = null;
            if (ele == null)
                return retVal;
            BoundingBoxXYZ boundingBox = ele.get_BoundingBox(null);
            if (boundingBox == null)
                return retVal;
            XYZ min = new XYZ(boundingBox.Min.X, boundingBox.Min.Y, boundingBox.Min.Z);
            XYZ max = new XYZ(boundingBox.Max.X, boundingBox.Max.Y, boundingBox.Max.Z);
            retVal = new Outline(min, max);
            return retVal;
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

        /// <summary>
        /// Lấy connector chưa được kết nối
        /// </summary>
        /// <param name="connectorManager"></param>
        /// <returns></returns>
        public static List<Connector> GetListConnectorNotConnnected(ConnectorManager connectorManager)
        {
            List<Connector> val = new List<Connector>();
            if (connectorManager != null)
            {
                foreach (Connector con in connectorManager.Connectors)
                {
                    if (!con.IsConnected)
                        val.Add(con);
                }
            }
            if (val.Count != 0)
                return val;

            return null;
        }

        public static Solid CreateCylindricalVolume(XYZ point, double height, double radius, bool bUp)
        {
            // build cylindrical shape around endpoint
            List<CurveLoop> curveloops = new List<CurveLoop>();
            CurveLoop circle = new CurveLoop();

            // For solid geometry creation, two curves are necessary, even for closed
            // cyclic shapes like circles

            Arc arc1 = Arc.Create(point, radius, 0, Math.PI, XYZ.BasisX, XYZ.BasisY);
            Arc arc2 = Arc.Create(point, radius, Math.PI, 2 * Math.PI, XYZ.BasisX, XYZ.BasisY);

            circle.Append(arc1);
            circle.Append(arc2);
            curveloops.Add(circle);

            Solid createdCylinder = GeometryCreationUtilities.CreateExtrusionGeometry(curveloops, XYZ.BasisZ * (bUp ? 1 : -1), height);

            return createdCylinder;
        }

        public static Curve GetCurve(this Element e)
        {
            Debug.Assert(null != e.Location,
              "expected an element with a valid Location");

            LocationCurve lc = e.Location as LocationCurve;

            Debug.Assert(null != lc,
              "expected an element with a valid LocationCurve");

            return lc.Curve;
        }

        public static Connector GetConnectorClosestTo(
        Element e,
        XYZ p)
        {
            ConnectorManager cm = GetConnectorManager(e);

            return null == cm
              ? null
              : GetConnectorClosestTo(cm.Connectors, p);
        }

        public static Connector GetConnectorClosestTo1(
        Element e,
        XYZ p)
        {
            ConnectorManager cm = GetConnectorManager(e);

            return null == cm
              ? null
              : GetConnectorClosestTo1(cm.Connectors, p);
        }

        private static Connector GetConnectorClosestTo1(
        ConnectorSet connectors,
        XYZ p)
        {
            Connector targetConnector = null;
            double minDist = double.MaxValue;

            foreach (Connector c in connectors)
            {
                if (c.IsConnected)
                    continue;
                double d = c.Origin.DistanceTo(p);

                if (d < minDist)
                {
                    targetConnector = c;
                    minDist = d;
                }
            }
            return targetConnector;
        }

        private static Connector GetConnectorClosestTo(
        ConnectorSet connectors,
        XYZ p)
        {
            Connector targetConnector = null;
            double minDist = double.MaxValue;

            foreach (Connector c in connectors)
            {
                double d = c.Origin.DistanceTo(p);

                if (d < minDist)
                {
                    targetConnector = c;
                    minDist = d;
                }
            }
            return targetConnector;
        }

        public static bool IsZero(double a)
        {
            return IsZero(a, _eps);
        }

        /// <summary>
        /// Return the given element's connector manager,
        /// using either the family instance MEPModel or
        /// directly from the MEPCurve connector manager
        /// for ducts and pipes.
        /// </summary>
        private static ConnectorManager GetConnectorManager(
          Element e)
        {
            MEPCurve mc = e as MEPCurve;
            FamilyInstance fi = e as FamilyInstance;

            if (null == mc && null == fi)
            {
                throw new ArgumentException(
                  "Element is neither an MEP curve nor a fitting.");
            }

            return null == mc
              ? fi.MEPModel.ConnectorManager
              : mc.ConnectorManager;
        }
    

        public static XYZ GetVector45Degree(Line line, XYZ point)
        {
            if (line == null || point == null)
                return null;

            Line lineCopy = line.Clone() as Line;

            XYZ pointProject = Common.GetPointProjectOnLine(lineCopy.Clone() as Line, point);

            XYZ vector = (point - pointProject).Normalize();

            XYZ point1 = Common.GetPointOnVector(line.Origin, line.Direction.Negate(), 100 / 304.8);

            XYZ point2 = Common.GetPointOnVector(point1, vector, 100 / 304.8);

            return (point2 - line.Origin).Normalize();
        }

        public static XYZ GetVector90Degree(Line line, XYZ point)
        {
            if (line == null || point == null)
                return null;

            Line lineCopy = line.Clone() as Line;

            XYZ pointProject = Common.GetPointProjectOnLine(lineCopy, point);

            return (point - pointProject).Normalize();
        }

        /// <summary>
        /// project a point onto plane
        /// </summary>
        public static XYZ GetPointProjectOnPlane(Plane plane, XYZ point)
        {
            plane.Project(point, out UV uv1, out double d);
            XYZ projectedPoint = plane.Origin + (uv1.U * plane.XVec) + (uv1.V * plane.YVec);
            return projectedPoint;
        }

        public static bool IsBetweenLine(Line line, XYZ pointCheck)
        {
            if (line != null && line.IsBound == true && pointCheck != null)
            {
                XYZ st = line.GetEndPoint(0);
                XYZ end = line.GetEndPoint(1);

                if (st.IsAlmostEqualTo(pointCheck)
               || end.IsAlmostEqualTo(pointCheck))
                    return false;

                XYZ vec1 = (pointCheck - st).Normalize();

                XYZ vec2 = (pointCheck - end).Normalize();

                if (vec1.DotProduct(vec2) < 0)
                    return true;
            }

            return false;
        }

        public static Line GetLineExtend(Line line, double valueExtend)
        {
            XYZ p1 = line.GetEndPoint(0);
            p1 = GetPointOnVector(p1, line.Direction.Negate(), valueExtend);

            XYZ p2 = line.GetEndPoint(1);
            p2 = GetPointOnVector(p2, line.Direction, valueExtend);

            if (p1.IsAlmostEqualTo(p2))
                return null;

            return Line.CreateBound(p1, p2);
        }

        /// <summary>
        /// Get line project on plane
        /// </summary>
        /// <param name="plane"></param>
        /// <param name="line"></param>
        /// <returns></returns>
        public static Line GetLineProjectOnPlane(Plane plane, Line line)
        {
            Line lineProject = null;
            XYZ p1 = GetPointProjectOnPlane(plane, line.GetEndPoint(0));
            XYZ p2 = GetPointProjectOnPlane(plane, line.GetEndPoint(1));
            lineProject = Line.CreateBound(p1, p2);
            return lineProject;
        }

        public static XYZ GetPointProjectOnLine(Line line, XYZ point, bool isMakeUnbound = true)
        {
            if (line != null && point != null)
            {
                Line lineCopy = line.Clone() as Line;

                if (isMakeUnbound)
                    lineCopy.MakeUnbound();

                IntersectionResult intersectionResult = lineCopy.Project(point);
                if (intersectionResult != null)
                {
                    return intersectionResult.XYZPoint;
                }
            }

            return null;
        }

        public static XYZ Intersection(Plane plane, Line line)
        {
            UV uv1, uv2;
            plane.Project(line.Origin, out uv1, out double d);
            plane.Project(line.Origin + line.Direction, out uv2, out double b);

            XYZ xyz1 = plane.Origin + (uv1.U * plane.XVec) + (uv1.V * plane.YVec);
            XYZ xyz2 = plane.Origin + (uv2.U * plane.XVec) + (uv2.V * plane.YVec);

            Line projectedLine = Line.CreateUnbound(xyz1, xyz2 - xyz1);

#if Debug_2027 || Release_2027 || Release_2026
    // --- CẤU HÌNH CHO REVIT 2026+ ---
    var intersectResult = line.Intersect(projectedLine, CurveIntersectResultOption.Detailed);
    if (intersectResult.Result != SetComparisonResult.Disjoint)
        return intersectResult.GetOverlaps()[0].Point;
#else
            // --- CẤU HÌNH CHO CÁC BẢN CŨ HƠN ---
            IntersectionResultArray iResult = new IntersectionResultArray();
            SetComparisonResult setComparisonResult = line.Intersect(projectedLine, out iResult);

            if (setComparisonResult != SetComparisonResult.Disjoint)
                return iResult.get_Item(0).XYZPoint;
#endif
            return null;
        }

        public static XYZ GetCenterElement(Element ele)
        {
            if (ele == null)
                return null;

            // Get bounding box
            var bb = ele.get_BoundingBox(null);
            if (bb == null)
            {
                // Get location
                Location lc = ele.Location;
                if (lc == null)
                    return null;

                if (lc is LocationPoint)
                {
                    // Get location point
                    LocationPoint lcP = lc as LocationPoint;
                    if (lc == null)
                        return null;

                    // Point center
                    var centerP = lcP.Point;
                    return new XYZ(centerP.X, centerP.Y, centerP.Z);
                }
                else if (lc is LocationCurve)
                {
                    // Get location curve
                    LocationCurve lcCurve = lc as LocationCurve;
                    if (lcCurve == null)
                        return null;

                    // Point center
                    var centerP = (lcCurve.Curve.GetEndPoint(1) + lcCurve.Curve.GetEndPoint(0)) / 2;
                    return new XYZ(centerP.X, centerP.Y, centerP.Z);
                }
            }
            else
            {
                XYZ max = bb.Transform.OfPoint(bb.Max);
                XYZ min = bb.Transform.OfPoint(bb.Min);

                // Point center
                var centerP = (max + min) / 2;
                return centerP;
            }

            return null;
        }

        public static XYZ LineIntersection(Curve line1, Curve line2, bool isUnbound = false)
        {
            if (line1 != null && line2 != null)
            {
                Line lineCopy1 = line1.Clone() as Line;
                Line lineCopy2 = line2.Clone() as Line;

                if (isUnbound)
                {
                    lineCopy1.MakeUnbound();
                    lineCopy1.MakeUnbound();
                }

#if Debug_2027 || Release_2027 || Release_2026
    // --- CẤU HÌNH CHO REVIT 2026+ ---
    var intersectResult = lineCopy1.Intersect(lineCopy2, CurveIntersectResultOption.Detailed);
    if (intersectResult.Result != SetComparisonResult.Disjoint)
        return intersectResult.GetOverlaps()[0].Point;
#else
                // --- CẤU HÌNH CHO CÁC BẢN CŨ HƠN ---
                SetComparisonResult setComparisonResult = lineCopy1.Intersect(lineCopy2, out IntersectionResultArray iResult);

                if (setComparisonResult != SetComparisonResult.Disjoint)
                    return iResult.get_Item(0).XYZPoint;
#endif
            }

            return null;
        }

        public static List<string> GetSizes(Document doc, PipeType pipeType)
        {
            if (pipeType != null)
            {
                List<string> allSizes = new List<string>();

                try
                {
                    if (pipeType.RoutingPreferenceManager != null)
                    {
                        Segment CurrentSegment = null;
                        int count = pipeType.RoutingPreferenceManager.GetNumberOfRules(RoutingPreferenceRuleGroupType.Segments);

                        for (int i = 0; i < count; i++)
                        {
                            var rule = pipeType.RoutingPreferenceManager.GetRule(RoutingPreferenceRuleGroupType.Segments, i);

                            CurrentSegment = doc.GetElement(rule.MEPPartId) as PipeSegment;
                        }

                        if (CurrentSegment != null)
                        {
                            foreach (var mepsize in CurrentSegment.GetSizes())
                            {
                                if (mepsize == null)
                                    continue;
#if Debug_2020 || Debug_2021  || Release_2020 || Release_2021 || Bundle_2020 || Bundle_2021
                                double size = UnitUtils.ConvertFromInternalUnits(mepsize.NominalDiameter, DisplayUnitType.DUT_MILLIMETERS);
#else
                                double size = UnitUtils.ConvertFromInternalUnits(mepsize.NominalDiameter, UnitTypeId.Millimeters);
#endif
                                size = Math.Round(size, 7);

                                allSizes.Add(size.ToString());
                            }
                        }
                    }
                    return allSizes;
                }
                catch (Exception ex)
                {
                    IO.LogException(ex);
                }
            }

            return new List<string>();
        }

        public static List<double> GetPipeSizes(Document doc, PipeType pipeType)
        {
            if (pipeType != null)
            {
                List<double> allSizes = new List<double>();

                try
                {
                    RoutingPreferenceManager routingPreferenceManager = null;

                    routingPreferenceManager = pipeType.RoutingPreferenceManager;

                    if (routingPreferenceManager != null)
                    {
                        Segment CurrentSegment = null;
                        int count = routingPreferenceManager.GetNumberOfRules(RoutingPreferenceRuleGroupType.Segments);

                        for (int i = 0; i < count; i++)
                        {
                            var rule = routingPreferenceManager.GetRule(RoutingPreferenceRuleGroupType.Segments, i);

                            CurrentSegment = doc.GetElement(rule.MEPPartId) as PipeSegment;
                        }

                        if (CurrentSegment != null)
                        {
                            foreach (var mepsize in CurrentSegment.GetSizes())
                            {
                                if (mepsize == null)
                                    continue;

                                //double size = UnitUtils.ConvertFromInternalUnits(, DisplayUnitType.DUT_MILLIMETERS);

                                //size = Math.Round(size, 7);

                                allSizes.Add(mepsize.NominalDiameter);
                            }
                        }
                    }
                    return allSizes;
                }
                catch (Exception ex)
                {
                    IO.LogException(ex);
                }
            }

            return new List<double>();
        }

        public static List<Element> AllComponentsOnPipeTruss(Document document, ElementId elementMainId, bool addFirst = false, List<Element> rmvElement = null)
        {
            List<Element> components = new List<Element>();
            Element eleMain = document.GetElement(elementMainId);
            List<Element> rmvElement_1 = null;
            if (addFirst == true)
            {
                rmvElement_1 = new List<Element> { eleMain };
                components.Add(eleMain);
            }
            else
            {
                if (rmvElement == null)
                    rmvElement = new List<Element>();

                rmvElement_1 = new List<Element>(rmvElement);
            }
            if (eleMain != null)
            {
                List<Connector> connectors = new List<Connector>();
                if (eleMain is MEPCurve processMEPCurve && processMEPCurve.ConnectorManager != null)
                {
                    foreach (Connector connector in processMEPCurve.ConnectorManager.Connectors)
                    {
                        //if (connector.ConnectorType != ConnectorType.End)
                        //    continue;
                        connectors.Add(connector);
                    }
                }
                else if (eleMain is FamilyInstance fmlIns && fmlIns.MEPModel.ConnectorManager != null)
                {
                    foreach (Connector connector in fmlIns.MEPModel.ConnectorManager.Connectors)
                    {
                        //if (connector.ConnectorType != ConnectorType.End && connector.IsConnected == true)
                        //    continue;
                        connectors.Add(connector);
                    }
                }

                if (connectors.Count > 0)
                {
                    foreach (Connector cnt1 in connectors)
                    {
                        foreach (Connector cnt2 in cnt1.AllRefs)
                        {
                            Element eleCheck = cnt2.Owner;
                            if (null != eleCheck && (eleCheck is MEPCurve || eleCheck is FamilyInstance))
                            {
                                if (rmvElement_1 != null && rmvElement_1.Any(item => item.Id == eleCheck.Id))
                                    continue;
                                else
                                {
                                    components.Add(eleCheck);
                                    List<Element> rmvElement_2 = new List<Element>(rmvElement_1);
                                    rmvElement_2.Add(eleCheck);

                                    components.AddRange(AllComponentsOnPipeTruss(document, eleCheck.Id, false, rmvElement_2));
                                }
                            }
                        }
                    }
                }
            }

            return components;
        }

        public static void RotateLine(Document doc, FamilyInstance wye, Line axisLine)
        {
            GetInformationConectorWye(wye, null, out Connector connector2, out Connector connector3, out Connector conTee);

            Line rotateLine = Line.CreateBound(connector2.Origin, connector3.Origin);

            if (GeometryUtils.IsParallel(axisLine.Direction, rotateLine.Direction))
                return;

            XYZ vector = rotateLine.Direction.CrossProduct(axisLine.Direction);
            XYZ intersection = GeometryUtils.GetUnBoundIntersection(rotateLine, axisLine);

            if (intersection != null)
            {
                double angle = rotateLine.Direction.AngleTo(axisLine.Direction);

                Line line = Line.CreateUnbound(intersection, vector);

                ElementTransformUtils.RotateElement(doc, wye.Id, line, angle);
                doc.Regenerate();
            }
            else
            {
                intersection = (connector2.Origin + connector3.Origin) / 2;
                double angle = rotateLine.Direction.AngleTo(axisLine.Direction);

                Line line = Line.CreateUnbound(intersection, vector);

                ElementTransformUtils.RotateElement(doc, wye.Id, line, angle);
                doc.Regenerate();
            }
        }

        public static ConnectorProfileType GetShape(MEPCurve mep)
        {
            ConnectorProfileType ductShape
              = ConnectorProfileType.Invalid;

            foreach (Connector c
              in mep.ConnectorManager.Connectors)
            {
                if (c.ConnectorType == ConnectorType.End)
                {
                    ductShape = c.Shape;
                    break;
                }
            }
            return ductShape;
        }

        public static void GetInfo(FamilyInstance fitting, XYZ vector, out Connector main1, out Connector main2, out Connector tee)
        {
            //Get fitting info
            main1 = null;
            main2 = null;
            tee = null;
            Rotate45Utils.mc(fitting, vector, out main1, out main2);

            foreach (Connector c in fitting.MEPModel.ConnectorManager.Connectors)
            {
                if (c.Id != main1.Id && c.Id != main2.Id)
                {
                    tee = c;
                    break;
                }
            }
        }

        public static Element Clone(Element element)
        {
            //Create new pipe
            var newPlace = new XYZ(0, 0, 0);
            var elemIds = ElementTransformUtils.CopyElement(
              Global.UIDoc.Document, element.Id, newPlace);

            var clone = Global.UIDoc.Document.GetElement(elemIds.ToList()[0]);

            return clone;
        }

        public static void DisconnectFrom(Pipe pipe)
        {
            if (pipe != null)
            {
                Connector con1 = pipe.ConnectorManager.Lookup(0);
                Connector con2 = pipe.ConnectorManager.Lookup(1);

                if (con1 != null && con1.IsConnected)
                {
                    foreach (Connector item in con1.AllRefs)
                    {
                        if (item != null && item.IsConnectedTo(con1))
                        {
                            con1.DisconnectFrom(item);
                        }
                    }
                }

                if (con2 != null && con2.IsConnected)
                {
                    foreach (Connector item in con2.AllRefs)
                    {
                        if (item != null && item.IsConnectedTo(con2))
                        {
                            con2.DisconnectFrom(item);
                        }
                    }
                }
            }
        }

        public static List<Connector> GetConnectionNearest(Element mep1, Element mep2)
        {
            try
            {
                Curve curve1 = GetCurve(mep1);
                Curve curve2 = GetCurve(mep2);

                XYZ start1 = curve1.GetEndPoint(0);
                XYZ end1 = curve1.GetEndPoint(1);

                XYZ start2 = curve2.GetEndPoint(0);
                XYZ end2 = curve2.GetEndPoint(1);

                Connector c1 = null;
                Connector c2 = null;

                if (start1.DistanceTo(start2) < end1.DistanceTo(start2))
                {
                    c1 = GetConnectorClosestTo(mep1, start1);
                }
                else
                {
                    c1 = GetConnectorClosestTo(mep1, end1);
                }

                if (start2.DistanceTo(start1) < end2.DistanceTo(start1))
                {
                    c2 = GetConnectorClosestTo(mep2, start2);
                }
                else
                {
                    c2 = GetConnectorClosestTo(mep2, end2);
                }

                if (c1 == null || c2 == null)
                    return null;

                List<Connector> connectors = new List<Connector>() { c1, c2 };

                return connectors;
            }
            catch (System.Exception ex)
            {
                return null;
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
                GetInformationConectorWye(fittingWye, null, out Connector conSt, out Connector conEnd, out Connector conNhanhWye);

                if (conSt != null && conSt.IsConnected)
                {
                    foreach (Connector item in conSt.AllRefs)
                    {
                        if (item != null && item.IsConnectedTo(conSt))
                        {
                            conSt.DisconnectFrom(item);

                            if (item != null && item.Owner != null)
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

                            if (item != null && item.Owner != null)
                            {
                                connectedEnd = item;
                                eleEnd = item.Owner;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// project a point onto plane
        /// </summary>
        public static XYZ GetPointProjectOnPlane(PlanarFace planarFace, XYZ point)
        {
            Plane plane = Plane.CreateByNormalAndOrigin(planarFace.FaceNormal, planarFace.Origin);

            plane.Project(point, out UV uv1, out double d);
            XYZ projectedPoint = plane.Origin + (uv1.U * plane.XVec) + (uv1.V * plane.YVec);
            return projectedPoint;
        }

        public static XYZ GetPointProjectOnPlane(Plane plane, XYZ point, XYZ vectorAlong, double Tolerance = 1.0e-5)
        {
            if (vectorAlong != null)
            {
                // Calculate t parameter for intersection
                double numerator = plane.Normal.DotProduct(plane.Origin - point);
                double denominator = plane.Normal.DotProduct(vectorAlong);

                if (Math.Abs(denominator) < Tolerance)
                {
                    return null; // The vector is parallel to the plane
                }

                double t = numerator / denominator;

                // Calculate projected point
                return point + vectorAlong * t;
            }
            else
            {
                plane.Project(point, out UV uv1, out double d);
                XYZ projectedPoint = plane.Origin + (uv1.U * plane.XVec) + (uv1.V * plane.YVec);
                return projectedPoint;
            }
        }

        /// Tìm giao điểm của 2 curve
        /// Chú ý line unbound không sử dụng được
        /// </summary>
        /// <param name="c1"></param>
        /// <param name="c2"></param>
        /// <returns></returns>
        public static XYZ Intersection(Curve c1, Curve c2)
        {
            if (!c1.IsBound || !c2.IsBound)
                return null;

            XYZ p1 = c1.GetEndPoint(0);
            XYZ q1 = c1.GetEndPoint(1);
            XYZ p2 = c2.GetEndPoint(0);
            XYZ q2 = c2.GetEndPoint(1);
            XYZ v1 = q1 - p1;
            XYZ v2 = q2 - p2;
            XYZ w = p2 - p1;
            XYZ p5 = null;
            double c = (v2.X * w.Y - v2.Y * w.X)
                       / (v2.X * v1.Y - v2.Y * v1.X);
            if (!double.IsInfinity(c) && !double.IsNaN(c))
            {
                double x = p1.X + c * v1.X;
                double y = p1.Y + c * v1.Y;
                p5 = new XYZ(x, y, 0);
            }
            return p5;
        }

        public static void RotateLine(Document doc, FamilyInstance fitting, Line axisSource, Line axisDestination)
        {
            if (doc == null || fitting == null || axisDestination == null || axisSource == null)
                return;

            if (GeometryUtils.IsParallel(axisSource.Direction, axisDestination.Direction))
                return;

            XYZ vector = axisDestination.Direction.CrossProduct(axisSource.Direction);
            XYZ intersection = GeometryUtils.GetUnBoundIntersection(axisDestination, axisSource);

            if (intersection != null)
            {
                double angle = axisDestination.Direction.AngleTo(axisSource.Direction);

                Line line = Line.CreateUnbound(intersection, vector);

                ElementTransformUtils.RotateElement(doc, fitting.Id, line, angle);
                doc.Regenerate();
            }
            else
            {
                intersection = (axisDestination.GetEndPoint(0) + axisDestination.GetEndPoint(1)) / 2;
                double angle = axisDestination.Direction.AngleTo(axisSource.Direction);

                Line line = Line.CreateUnbound(intersection, vector);

                ElementTransformUtils.RotateElement(doc, fitting.Id, line, angle);
                doc.Regenerate();
            }
        }
    }

    public class ObjectItem
    {
        private string _name;
        private ElementId _objectId = ElementId.InvalidElementId;

        private string Guid = null;

        public ObjectItem(string name, ElementId objectId)
        {
            _name = name;
            _objectId = objectId;
        }

        public ObjectItem(string name, string guid)
        {
            _name = name;
            Guid = guid;
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public ElementId ObjectId
        {
            get { return _objectId; }
            set { _objectId = value; }
        }

        public override string ToString()
        {
            return _name;
        }
    }
}
