using PlumbingSolution.LoginLicense.Gate;
using PlumbingSolution.FireProtection.SelectionFilters;
using PlumbingSolution.FireProtection.Ultis;
using PlumbingSolution.FireProtection.Utils;
using PlumbingSolution.FireProtection.Extensions;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Forms;

namespace PlumbingSolution.FireProtection.Command.Fire
{
    [Transaction(TransactionMode.Manual)]
    public class CmdConnectParallel : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Global.UIApp = commandData.Application;
            Global.RVTApp = commandData.Application.Application;
            Global.UIDoc = commandData.Application.ActiveUIDocument;
            Global.AppCreation = commandData.Application.Application.Create;

            if (!LicenseGate.IsToolAllowed(this))
                return Result.Cancelled;

            // Theo sheet: chỉ dùng chế độ "Đầu ống", không tích Set Point -> không cần form, chạy luôn.
            return Process(commandData.Application.ActiveUIDocument);
        }

        public static Result Process(UIDocument uiDoc)
        {
            var doc = uiDoc.Document;

            using (TransactionGroup trG = new TransactionGroup(doc, "Parallel Pipe"))
            {
                trG.Start();

                while (true)
                {
                    using (Transaction tran = new Transaction(doc, "Connect"))
                    {
                        try
                        {
                            tran.Start();

                            var pipeRef1 = Global.UIDoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element, new PipeSelectionFilter(), "Select main pipe");
                            var pipeMain = doc.GetElement(pipeRef1.ElementId) as Pipe;
                            if (pipeMain == null)
                                return Result.Cancelled;

                            var pipeRef2 = Global.UIDoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element, new PipeSelectionFilter(), "Select parallel pipe");
                            var pipeBranch = doc.GetElement(pipeRef2.ElementId) as Pipe;
                            if (pipeBranch == null)
                                return Result.Cancelled;

                            Line mainLine = (pipeMain.Location as LocationCurve).Curve as Line;
                            Line branchLine = (pipeBranch.Location as LocationCurve).Curve as Line;

                            XYZ branchEndPoint = new XYZ(); // End;
                            XYZ teePoint = new XYZ();

                            AlignBranchToMain(doc, pipeBranch, mainLine);
                            branchLine = (pipeBranch.Location as LocationCurve).Curve as Line;

                            if (ProjectPointOntoLine((pipeBranch.Location as LocationCurve).Curve.GetEndPoint(0), mainLine))
                                branchEndPoint = (pipeBranch.Location as LocationCurve).Curve.GetEndPoint(0);
                            else if (ProjectPointOntoLine((pipeBranch.Location as LocationCurve).Curve.GetEndPoint(1), mainLine))
                                branchEndPoint = (pipeBranch.Location as LocationCurve).Curve.GetEndPoint(1);
                            else
                            {
                                MessageBox.Show("The branch pipe must be connected to the main pipe at one of its endpoints.", "Invalid Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return Result.Cancelled;
                            }

                            teePoint = new XYZ(branchEndPoint.X, branchEndPoint.Y, mainLine.GetEndPoint(0).Z);

                            // 5. Tính điểm giao trên ống chính (cùng XY với đầu ống nhánh, cùng Z với ống chính)

                            // 6. Kiểm tra lại teePoint nằm trong đoạn ống chính
                            ProjectPointOntoLine(teePoint, mainLine);
                            if (branchEndPoint == null)
                            {
                                tran.RollBack();
                                return Result.Failed;
                            }

                            if (GetPreferredJunctionType(doc, pipeMain) == ConnectionType.Tee)

                                ConnectWithTee(doc, pipeBranch, pipeMain, teePoint, branchEndPoint);
                            else

                                ConnectWithTap(doc, pipeBranch, pipeMain, teePoint, branchEndPoint);

                            tran.Commit();
                        }
                        catch (Exception)
                        {
                            tran.RollBack();
                            break;
                        }
                    }
                }

                trG.Assimilate();
            }

            return Result.Succeeded;
        }

        /// <summary>
        /// Kết nối bằng Tee: BreakCurve ống chính → tạo Elbow ở đầu nhánh → tạo ống đứng → tạo Tee
        /// </summary>
        private static void ConnectWithTee(Document doc, Pipe branchPipe, Pipe mainPipe, XYZ teePoint, XYZ branchEndPoint)
        {
            // -BreakCurve ống chính tại teePoint ---
            // Revit sẽ tạo ra 2 pipe segment, connector hở tại teePoint
            Pipe[] splitPipes = BreakMainPipe(doc, mainPipe, teePoint);
            Pipe mainPipe1 = splitPipes[0];
            Pipe mainPipe2 = splitPipes[1];

            // Tạo ống đứng từ branchEndPoint xuống teePoint ---
            // (hoặc từ teePoint lên branchEndPoint tùy cao độ)
            XYZ verticalStart = branchEndPoint;
            XYZ verticalEnd = teePoint;

            var verticalPipe = Common.Clone(mainPipe) as Pipe;
            (verticalPipe.Location as LocationCurve).Curve = Line.CreateBound(verticalStart, verticalEnd);

            // Tạo Elbow nối ống nhánh ↔ ống đứng ---
            Connector branchEnd = ConnectorUtils.GetConnectorNearest(branchEndPoint, branchPipe, out Connector outFarest);
            Connector verticalTop = ConnectorUtils.GetConnectorNearest(branchEndPoint, verticalPipe, out outFarest);
            doc.Create.NewElbowFitting(branchEnd, verticalTop);

            ////Tạo Tee nối ống đứng ↔ ống chính (2 segment) ---
            Connector verticalBottom = ConnectorUtils.GetConnectorNearest(teePoint, verticalPipe, out outFarest);
            Connector main1Connector = ConnectorUtils.GetConnectorNearest(teePoint, mainPipe1, out outFarest);
            Connector main2Connector = ConnectorUtils.GetConnectorNearest(teePoint, mainPipe2, out outFarest);

            doc.Create.NewTeeFitting(main1Connector, main2Connector, verticalBottom);
        }

        /// <summary>
        /// Kết nối bằng Tap (không BreakCurve)
        /// </summary>
        private static void ConnectWithTap(Document doc, Pipe branchPipe, Pipe mainPipe, XYZ teePoint, XYZ branchEndPoint)
        {
            XYZ verticalStart = branchEndPoint;
            XYZ verticalEnd = teePoint;

            var verticalPipe = Common.Clone(mainPipe) as Pipe;
            (verticalPipe.Location as LocationCurve).Curve = Line.CreateBound(verticalStart, verticalEnd);

            // Tạo Elbow nối ống nhánh ↔ ống đứng
            Connector branchEnd = ConnectorUtils.GetConnectorNearest(branchEndPoint, branchPipe, out Connector outFarest);
            Connector verticalTop = ConnectorUtils.GetConnectorNearest(branchEndPoint, verticalPipe, out outFarest);
            doc.Create.NewElbowFitting(branchEnd, verticalTop);

            // Tạo Tap nối ống đứng ↔ ống chính
            Connector verticalBottom = ConnectorUtils.GetConnectorNearest(teePoint, verticalPipe, out outFarest);

            // Dùng NewTakeoffFitting cho Tap
            doc.Create.NewTakeoffFitting(verticalBottom, mainPipe);
        }

        /// <summary>
        /// BreakCurve ống chính tại 1 điểm → trả về 2 pipe segment
        /// </summary>
        private static Pipe[] BreakMainPipe(Document doc, Pipe mainPipe, XYZ breakPoint)
        {
            ElementId newPipeId = PlumbingUtils.BreakCurve(doc, mainPipe.Id, breakPoint);
            Pipe newPipe = doc.GetElement(newPipeId) as Pipe;
            return new Pipe[] { mainPipe, newPipe };
        }

        private static ConnectionType GetPreferredJunctionType(Document doc, Pipe mainPipe)
        {
            PipeType pipeType = doc.GetElement(mainPipe.GetTypeId()) as PipeType;
            if (pipeType == null) return ConnectionType.Tee;

            return pipeType.RoutingPreferenceManager.PreferredJunctionType == PreferredJunctionType.Tap
                ? ConnectionType.Tap
                : ConnectionType.Tee;
        }

        /// <summary>
        /// Align ống nhánh: dịch chuyển theo XY sao cho đầu nhánh nằm trên đường tâm ống chính
        /// </summary>
        private static void AlignBranchToMain(Document doc, Pipe branchPipe, Line mainLine)
        {
            var location = branchPipe.Location as LocationCurve;

            var mainLine2d = RevitUtils.ProjectLineToPlane(mainLine);
            mainLine2d.MakeUnbound();

            var branchEndPoint2d = new XYZ(location.Curve.GetEndPoint(0).X, location.Curve.GetEndPoint(0).Y, 0);

            var result = mainLine2d.Project(branchEndPoint2d);
            if (result != null)
            {
                ElementTransformUtils.MoveElement(doc, branchPipe.Id, result.XYZPoint - branchEndPoint2d);
            }
        }

        /// <summary>
        /// Chiếu điểm lên đoạn thẳng (chỉ theo XY),
        /// trả về false nếu hình chiếu nằm ngoài đoạn
        /// </summary>
        private static bool ProjectPointOntoLine(XYZ point, Line line)
        {
            XYZ origin = line.GetEndPoint(0);
            XYZ dir = line.Direction;
            double length = line.Length;

            // Chiếu bỏ qua Z
            XYZ pointXY = new XYZ(point.X, point.Y, origin.Z);
            XYZ originXY = new XYZ(origin.X, origin.Y, origin.Z);

            double t = (pointXY - originXY).DotProduct(dir);

            if (t < 0 || t > length)
                return false;

            return true;
        }
    }

    public enum ConnectionType
    {
        Tee,
        Tap
    }
}