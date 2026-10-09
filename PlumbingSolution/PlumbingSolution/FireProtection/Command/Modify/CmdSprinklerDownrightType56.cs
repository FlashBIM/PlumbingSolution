using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using PlumbingSolution.FireProtection.Extensions;
using PlumbingSolution.FireProtection.Services;
using PlumbingSolution.FireProtection.Ultis;
using PlumbingSolution.FireProtection.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace PlumbingSolution.FireProtection.Command.Modify
{
    /// <summary>
    /// Pendent Sprinkler Type 5 và Type 6 (mới, theo sheet Fire Protection).
    ///
    /// Type 5: tee bên hông ống chính → ống ngang L1 → co xuống L2 → ống ngang tới trên đầu phun → co xuống đầu phun.
    /// Type 6: như Type 5 nhưng tee trên đỉnh ống chính, có thêm đoạn đứng A trước khi đi ngang.
    ///
    /// By Distance: L1 = khoảng ngang từ tâm ống chính tới tâm ống đứng, L2 = khoảng đứng giữa hai ống ngang.
    /// By Pick MEP: chọn thêm duct / cable tray... L1 = khe hở từ mép ống đứng tới mép đối tượng,
    /// đỉnh ống ngang dưới (kể cả bảo ôn của đối tượng) cách đáy đối tượng cố định 20 mm.
    ///
    /// Thao tác: chọn đầu phun → Finish, chọn ống chính → Finish, (By MEP) chọn đối tượng MEP → Finish.
    /// Co và tee sinh ra theo Routing Preferences của Pipe Type chọn trên form.
    /// </summary>
    public partial class CmdSprinklerDownright
    {
        private const double ClearanceUnderMepMm = 20;

        public static Result ProcessType5()
        {
            return ProcessType56(false);
        }

        public static Result ProcessType6()
        {
            return ProcessType56(true);
        }

        private static Result ProcessType56(bool isType6)
        {
            var form = App.m_SprinklerDownForm;
            Document doc = Global.UIDoc.Document;
            int done = 0;
            var skipped = new List<string>();

            try
            {
                if (form != null && form.IsDisposed == false)
                    form.Hide();

                List<FamilyInstance> sprinklers = sr.SelectSprinklers();
                if (sprinklers == null || sprinklers.Count == 0)
                    return Result.Cancelled;

                List<Pipe> mainPipes = PickPipes();
                if (mainPipes == null || mainPipes.Count == 0)
                    return Result.Cancelled;

                List<Element> obstacles = new List<Element>();
                if (form.IsByMep)
                {
                    obstacles = PickObstacles();
                    if (obstacles.Count == 0)
                        return Result.Cancelled;
                }

                ElementId pipeTypeId = form.PipeTypeIdC3;
                double sizeFt = Common.mmToFT * form.PipeSizeC3;
                double l1Ft = Common.mmToFT * form.L1_;
                double l2Ft = Common.mmToFT * form.L2_;
                double aFt = Common.mmToFT * form.Height_;

                // Đoạn ống chính mới sinh ra khi cắt để đặt tee cũng phải được đầu phun sau tìm thấy.
                List<ElementId> mainIds = mainPipes.Select(p => p.Id).ToList();

                using (TransactionGroup group = new TransactionGroup(doc, isType6 ? "Pendent Sprinkler Type 6" : "Pendent Sprinkler Type 5"))
                {
                    group.Start();

                    foreach (FamilyInstance sprinkler in sprinklers)
                    {
                        string reason;
                        using (Transaction t = new Transaction(doc, "Pendent Sprinkler"))
                        {
                            t.Start();
                            try
                            {
                                var created = new List<ElementId>();
                                reason = ConnectOne(doc, sprinkler, mainIds, obstacles, isType6, form.IsByMep,
                                                    pipeTypeId, sizeFt, l1Ft, l2Ft, aFt, created);
                                if (reason == null)
                                {
                                    CmdDeleteSprinker.CreateSchema(sprinkler, created.Select(x => x.ToInt().ToString()).ToList());
                                    t.Commit();
                                    done++;
                                    continue;
                                }
                            }
                            catch (Exception ex)
                            {
                                reason = ex.Message;
                            }

                            t.RollBack();
                        }

                        skipped.Add(sprinkler.Id.ToInt() + ": " + reason);
                    }

                    group.Assimilate();
                }

                if (skipped.Count > 0)
                {
                    TaskDialog.Show("Pendent Sprinkler",
                        string.Format("Connected {0}/{1} sprinklers.\nSkipped:\n{2}", done, sprinklers.Count,
                                      string.Join("\n", skipped.Take(15))));
                }

                return Result.Succeeded;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                IO.LogException(ex);
                return Result.Failed;
            }
            finally
            {
                if (form != null && form.IsDisposed == false)
                {
                    form.Show(App.hWndRevit);
                    DisplayService.SetFocus(new HandleRef(null, form.Handle));
                }
            }
        }

        /// <summary>Nối một đầu phun. Trả về null nếu thành công, ngược lại là lý do bỏ qua.</summary>
        private static string ConnectOne(Document doc, FamilyInstance sprinkler, List<ElementId> mainIds,
                                         List<Element> obstacles, bool isType6, bool byMep,
                                         ElementId pipeTypeId, double sizeFt, double l1Ft, double l2Ft, double aFt,
                                         List<ElementId> created)
        {
            Connector head = sprinkler.MEPModel?.ConnectorManager?.Connectors.Cast<Connector>()
                                      .OrderByDescending(c => c.Origin.Z).FirstOrDefault();
            if (head == null)
                return "sprinkler has no connector";
            if (head.IsConnected)
                return "sprinkler is already connected";

            XYZ headPt = head.Origin;

            Pipe main = NearestMainPipe(doc, mainIds, headPt);
            if (main == null)
                return "no horizontal main pipe";

            Line mainLine = (Line)((LocationCurve)main.Location).Curve;
            XYZ tee = mainLine.Project(headPt).XYZPoint;            // trên trục ống chính, có kẹp trong đoạn ống
            double zMain = tee.Z;

            XYZ toHead = new XYZ(headPt.X - tee.X, headPt.Y - tee.Y, 0);
            double planDist = toHead.GetLength();
            if (planDist < sizeFt * 3)
                return "sprinkler is too close to the main pipe in plan";
            XYZ dir = toHead.Normalize();

            double radius = OuterRadius(doc, pipeTypeId, sizeFt);
            double zTop = isType6 ? zMain + aFt : zMain;
            double dropDist;   // khoảng ngang từ điểm tee tới tâm ống đứng
            double zLow;

            if (byMep)
            {
                Obstacle ob = FirstObstacle(doc, obstacles, tee, dir, planDist);
                if (ob == null)
                    return "no picked MEP element between the main pipe and the sprinkler";

                dropDist = ob.Enter - l1Ft - radius;
                zLow = ob.BottomZ - Common.mmToFT * ClearanceUnderMepMm - radius;
            }
            else
            {
                dropDist = l1Ft;
                zLow = zTop - l2Ft;
            }

            if (dropDist < radius * 4)
                return "L1 leaves no room after the main pipe";
            if (dropDist > planDist - radius * 4)
                return "L1 is longer than the distance to the sprinkler";
            if (zLow > zTop - radius * 4)
                return "the lower run is not below the upper run";
            if (zLow < headPt.Z + radius * 4)
                return "the lower run is too close to the sprinkler";

            XYZ drop = tee + dir * dropDist;
            var pts = new List<XYZ>();
            pts.Add(tee);
            if (isType6)
                pts.Add(new XYZ(tee.X, tee.Y, zTop));
            pts.Add(new XYZ(drop.X, drop.Y, zTop));
            pts.Add(new XYZ(drop.X, drop.Y, zLow));
            pts.Add(new XYZ(headPt.X, headPt.Y, zLow));
            pts.Add(headPt);

            ElementId systemTypeId = main.get_Parameter(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM)?.AsElementId();
            if (systemTypeId == null || systemTypeId == ElementId.InvalidElementId)
                systemTypeId = new FilteredElementCollector(doc).OfClass(typeof(PipingSystemType)).FirstElementId();
            ElementId levelId = main.ReferenceLevel.Id;

            var pipes = new List<Pipe>();
            for (int i = 0; i < pts.Count - 1; i++)
            {
                Pipe p = Pipe.Create(doc, systemTypeId, pipeTypeId, levelId, pts[i], pts[i + 1]);
                p.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).Set(sizeFt);
                pipes.Add(p);
                created.Add(p.Id);
            }
            doc.Regenerate();

            // Co giữa các đoạn: Revit chọn family theo Routing Preferences của Pipe Type.
            for (int i = 0; i < pipes.Count - 1; i++)
            {
                FamilyInstance elbow = doc.Create.NewElbowFitting(ConnectorAt(pipes[i], pts[i + 1]), ConnectorAt(pipes[i + 1], pts[i + 1]));
                created.Add(elbow.Id);
            }

            ConnectToMain(doc, main, tee, pipes[0], mainIds, created);

            Connector last = ConnectorAt(pipes[pipes.Count - 1], headPt);
            if (Math.Abs(last.Radius - head.Radius) < 1e-6)
                last.ConnectTo(head);
            else
                created.Add(doc.Create.NewTransitionFitting(last, head).Id);

            return null;
        }

        /// <summary>Tee (hoặc Tap theo Routing Preferences) giữa ống chính; co nếu điểm nối ở đầu ống chính.</summary>
        internal static void ConnectToMain(Document doc, Pipe main, XYZ at, Pipe branch, List<ElementId> mainIds, List<ElementId> created)
        {
            Connector branchCon = ConnectorAt(branch, at);
            Line line = (Line)((LocationCurve)main.Location).Curve;
            double tol = Common.mmToFT * 1;

            Connector endCon = main.ConnectorManager.Connectors.Cast<Connector>()
                                   .FirstOrDefault(c => c.ConnectorType == ConnectorType.End && c.Origin.DistanceTo(at) < tol);
            if (endCon != null)
            {
                created.Add(doc.Create.NewElbowFitting(endCon, branchCon).Id);
                return;
            }

            if (GetPreferredJunctionType(main) == PreferredJunctionType.Tap)
            {
                created.Add(doc.Create.NewTakeoffFitting(branchCon, main).Id);
                return;
            }

            ElementId secondId = PlumbingUtils.BreakCurve(doc, main.Id, at);
            Pipe second = (Pipe)doc.GetElement(secondId);
            mainIds.Add(secondId);
            doc.Regenerate();

            created.Add(doc.Create.NewTeeFitting(ConnectorAt(main, at), ConnectorAt(second, at), branchCon).Id);
        }

        internal static Connector ConnectorAt(MEPCurve curve, XYZ pt)
        {
            return curve.ConnectorManager.Connectors.Cast<Connector>()
                        .Where(c => c.ConnectorType == ConnectorType.End)
                        .OrderBy(c => c.Origin.DistanceTo(pt)).First();
        }

        internal static Pipe NearestMainPipe(Document doc, List<ElementId> ids, XYZ pt)
        {
            Pipe best = null;
            double bestD = double.MaxValue;
            foreach (ElementId id in ids)
            {
                Pipe p = doc.GetElement(id) as Pipe;
                Line l = (p?.Location as LocationCurve)?.Curve as Line;
                if (l == null || Math.Abs(l.Direction.Z) > 0.05)
                    continue;

                XYZ q = l.Project(pt).XYZPoint;
                double d = new XYZ(q.X - pt.X, q.Y - pt.Y, 0).GetLength();
                if (d < bestD)
                {
                    bestD = d;
                    best = p;
                }
            }
            return best;
        }

        /// <summary>Đường kính ngoài/2 của ống theo segment trong Routing Preferences (không có thì lấy danh nghĩa).</summary>
        internal static double OuterRadius(Document doc, ElementId pipeTypeId, double nominalFt)
        {
            var type = doc.GetElement(pipeTypeId) as PipeType;
            var rpm = type?.RoutingPreferenceManager;
            if (rpm != null && rpm.GetNumberOfRules(RoutingPreferenceRuleGroupType.Segments) > 0)
            {
                var seg = doc.GetElement(rpm.GetRule(RoutingPreferenceRuleGroupType.Segments, 0).MEPPartId) as PipeSegment;
                MEPSize size = seg?.GetSizes().FirstOrDefault(s => Math.Abs(s.NominalDiameter - nominalFt) < 1e-6);
                if (size != null)
                    return size.OuterDiameter / 2;
            }
            return nominalFt / 2;
        }

        private class Obstacle
        {
            public double Enter;     // khoảng ngang từ điểm tee tới mép đối tượng theo hướng tới đầu phun
            public double BottomZ;   // đáy đối tượng, đã trừ bảo ôn
        }

        /// <summary>Đối tượng MEP đầu tiên mà tuyến (mặt bằng) từ tee tới đầu phun cắt qua.</summary>
        private static Obstacle FirstObstacle(Document doc, List<Element> obstacles, XYZ start, XYZ dir, double length)
        {
            Obstacle best = null;
            foreach (Element e in obstacles)
            {
                Obstacle ob = Intersect(doc, e, start, dir, length);
                if (ob != null && (best == null || ob.Enter < best.Enter))
                    best = ob;
            }
            return best;
        }

        private static Obstacle Intersect(Document doc, Element e, XYZ start, XYZ dir, double length)
        {
            double insulation = InsulationLiningBase.GetInsulationIds(doc, e.Id)
                                    .Select(id => doc.GetElement(id) as InsulationLiningBase)
                                    .Where(x => x != null).Select(x => x.Thickness).DefaultIfEmpty(0).Max();

            Line axis = (e.Location as LocationCurve)?.Curve as Line;
            if (axis != null && Math.Abs(axis.Direction.Z) < 0.05)
            {
                double width = Dimension(e, BuiltInParameter.RBS_CURVE_WIDTH_PARAM, BuiltInParameter.RBS_CURVE_DIAMETER_PARAM,
                                         BuiltInParameter.RBS_PIPE_OUTER_DIAMETER, BuiltInParameter.RBS_CONDUIT_OUTER_DIAM_PARAM);
                double height = Dimension(e, BuiltInParameter.RBS_CURVE_HEIGHT_PARAM, BuiltInParameter.RBS_CURVE_DIAMETER_PARAM,
                                          BuiltInParameter.RBS_PIPE_OUTER_DIAMETER, BuiltInParameter.RBS_CONDUIT_OUTER_DIAM_PARAM);
                if (width > 0)
                {
                    XYZ u = new XYZ(axis.Direction.X, axis.Direction.Y, 0).Normalize();
                    XYZ v = new XYZ(-u.Y, u.X, 0);
                    XYZ o = axis.GetEndPoint(0);
                    XYZ rel = new XYZ(start.X - o.X, start.Y - o.Y, 0);
                    double half = width / 2 + insulation;

                    if (Slab(rel.DotProduct(u), dir.DotProduct(u), -insulation, axis.Length + insulation, length, out double u0, out double u1)
                        && Slab(rel.DotProduct(v), dir.DotProduct(v), -half, half, length, out double v0, out double v1))
                    {
                        double enter = Math.Max(u0, v0);
                        if (enter <= Math.Min(u1, v1))
                        {
                            XYZ at = axis.Project(start + dir * enter).XYZPoint;
                            return new Obstacle { Enter = enter, BottomZ = at.Z - height / 2 - insulation };
                        }
                    }
                    return null;
                }
            }

            BoundingBoxXYZ bb = e.get_BoundingBox(null);
            if (bb == null)
                return null;
            if (Slab(start.X, dir.X, bb.Min.X - insulation, bb.Max.X + insulation, length, out double x0, out double x1)
                && Slab(start.Y, dir.Y, bb.Min.Y - insulation, bb.Max.Y + insulation, length, out double y0, out double y1))
            {
                double enter = Math.Max(x0, y0);
                if (enter <= Math.Min(x1, y1))
                    return new Obstacle { Enter = enter, BottomZ = bb.Min.Z - insulation };
            }
            return null;
        }

        /// <summary>Đoạn tham số [t0, t1] ⊂ [0, length] mà p + t*d nằm trong [min, max].</summary>
        private static bool Slab(double p, double d, double min, double max, double length, out double t0, out double t1)
        {
            t0 = 0;
            t1 = length;
            if (Math.Abs(d) < 1e-9)
                return p >= min && p <= max;

            double a = (min - p) / d, b = (max - p) / d;
            t0 = Math.Max(0, Math.Min(a, b));
            t1 = Math.Min(length, Math.Max(a, b));
            return t0 <= t1;
        }

        private static double Dimension(Element e, params BuiltInParameter[] candidates)
        {
            foreach (BuiltInParameter bip in candidates)
            {
                Parameter p = e.get_Parameter(bip);
                if (p != null && p.HasValue && p.AsDouble() > 0)
                    return p.AsDouble();
            }
            return 0;
        }

        private static List<Element> PickObstacles()
        {
            try
            {
                return Global.UIDoc.Selection
                    .PickObjects(ObjectType.Element, new ObstacleFilter(), "Pick ducts / cable trays / pipes (MEP elements), then Finish")
                    .Select(r => Global.UIDoc.Document.GetElement(r)).Where(e => e != null).ToList();
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return new List<Element>();
            }
        }

        private class ObstacleFilter : ISelectionFilter
        {
            private static readonly BuiltInCategory[] Categories =
            {
                BuiltInCategory.OST_DuctCurves, BuiltInCategory.OST_CableTray, BuiltInCategory.OST_PipeCurves,
                BuiltInCategory.OST_Conduit, BuiltInCategory.OST_StructuralFraming,
            };

            public bool AllowElement(Element elem)
            {
                return elem.Category != null && Categories.Any(c => elem.Category.Id.ToInt() == (int)c);
            }

            public bool AllowReference(Reference reference, XYZ position)
            {
                return false;
            }
        }
    }
}
