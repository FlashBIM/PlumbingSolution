using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
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
    /// <summary>Cách nối vào ống chính của Twin Type 6 (sheet Fire Protection).</summary>
    public enum TwinMainFitting
    {
        Tee,        // 1 Tee/Tap: ra cạnh ống chính, ống ngang trên cùng cao độ ống chính
        TeeElbow90, // tee trên đỉnh ống chính → đứng A → co 90 → ngang
        TeeElbow45, // tee/tap ống chính → chéo 45° lên cao A → co 45 → ngang
    }

    /// <summary>
    /// Twin Sprinkler Type 6 (mới, theo sheet Fire Protection): một nhánh nối cặp đầu phun.
    /// Tee/Tap ống chính → ống ngang trên; cách tim ống chính L1 là tee có đầu phun HƯỚNG LÊN (tool tự đặt theo
    /// Family/Type trên form); đi tiếp tới trước vật cản (duct, cable tray...) thì co xuống, luồn dưới vật cản tới trên
    /// đầu phun HƯỚNG XUỐNG (có sẵn, người dùng chọn) rồi co xuống đầu phun.
    /// L2 = khe hở mép ống đứng – mép vật cản, L3 = khe hở đỉnh ống ngang dưới – đáy vật cản.
    /// Vật cản: chọn (By Pick MEP Elements) hoặc tự dò (Auto, như Pendent Type 5/6).
    /// Đầu hướng lên: Elevation = khoảng từ tim ống ngang trên tới connector đầu phun; tích "Sprinkler connect Tee
    /// directly" thì đầu phun cắm thẳng vào tee, bỏ qua Elevation.
    /// Elbow 90 (Other Settings): đầu phun gần đầu ống chính còn hở thì nối bằng co, cắt đoạn thừa.
    /// </summary>
    public static class TwinSprinklerType6
    {
        public static Result Process()
        {
            var form = App.m_TwinSprinklerForm;
            Document doc = Global.UIDoc.Document;
            int done = 0;
            var skipped = new List<string>();

            try
            {
                if (form != null && form.IsDisposed == false)
                    form.Hide();

                FamilySymbol upright = form.UprightSymbol6;
                if (upright == null)
                {
                    TaskDialog.Show("Twin Sprinkler", "Select the upright sprinkler Family and Type.");
                    return Result.Cancelled;
                }

                List<FamilyInstance> sprinklers = CmdTwinSprinkler.SelectSprinklers();
                if (sprinklers == null || sprinklers.Count == 0)
                    return Result.Cancelled;

                List<Pipe> mainPipes = CmdSprinklerDownright.PickPipes();
                if (mainPipes == null || mainPipes.Count == 0)
                    return Result.Cancelled;

                List<Element> obstacles = new List<Element>();
                if (!form.IsAuto6)
                {
                    obstacles = CmdSprinklerDownright.PickObstacles();
                    if (obstacles.Count == 0)
                        return Result.Cancelled;
                }

                var p = new Params
                {
                    PipeTypeId = form.FamilyTypeC5,
                    SizeFt = Common.mmToFT * form.PipeSizeC5,
                    L1 = Common.mmToFT * form.L1_6,
                    L2 = Common.mmToFT * form.L2_6,
                    L3 = Common.mmToFT * form.L3_6,
                    A = Common.mmToFT * form.A_6,
                    Fitting = form.MainFitting6,
                    Auto = form.IsAuto6,
                    Upright = upright,
                    Elevation = Common.mmToFT * form.UprightElevation6,
                    Direct = form.ConnectTeeDirectly6,
                    ElbowAtFreeEnd = form.ElbowAtFreeEnd6,
                };

                List<ElementId> mainIds = mainPipes.Select(x => x.Id).ToList();
                var tees = new List<XYZ>();
                if (p.ElbowAtFreeEnd)
                {
                    foreach (FamilyInstance s in sprinklers)
                    {
                        Connector c = HeadConnector(s);
                        Pipe m = c == null ? null : CmdSprinklerDownright.NearestMainPipe(doc, mainIds, c.Origin);
                        if (m != null)
                            tees.Add(((Line)((LocationCurve)m.Location).Curve).Project(c.Origin).XYZPoint);
                    }
                }

                using (TransactionGroup group = new TransactionGroup(doc, "Twin Sprinkler Type 6"))
                {
                    group.Start();

                    foreach (FamilyInstance sprinkler in sprinklers)
                    {
                        string reason;
                        using (Transaction t = new Transaction(doc, "Twin Sprinkler"))
                        {
                            t.Start();
                            try
                            {
                                if (!p.Upright.IsActive)
                                {
                                    p.Upright.Activate();
                                    doc.Regenerate();
                                }

                                var created = new List<ElementId>();
                                reason = ConnectOne(doc, sprinkler, mainIds, obstacles, p, tees, created);
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
                    TaskDialog.Show("Twin Sprinkler",
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

        private class Params
        {
            public ElementId PipeTypeId;
            public double SizeFt, L1, L2, L3, A, Elevation;
            public TwinMainFitting Fitting;
            public bool Auto, Direct, ElbowAtFreeEnd;
            public FamilySymbol Upright;
        }

        // Đầu phun hướng xuống: connector ở đỉnh.
        private static Connector HeadConnector(FamilyInstance s)
        {
            return s.MEPModel?.ConnectorManager?.Connectors.Cast<Connector>().OrderByDescending(c => c.Origin.Z).FirstOrDefault();
        }

        /// <summary>Nối một đầu phun hướng xuống và đặt đầu hướng lên. Trả về null nếu thành công, ngược lại là lý do.</summary>
        private static string ConnectOne(Document doc, FamilyInstance sprinkler, List<ElementId> mainIds, List<Element> obstacles,
                                         Params p, IList<XYZ> otherTees, List<ElementId> created)
        {
            Connector head = HeadConnector(sprinkler);
            if (head == null)
                return "sprinkler has no connector";
            if (head.IsConnected)
                return "sprinkler is already connected";
            XYZ headPt = head.Origin;

            Pipe main = CmdSprinklerDownright.NearestMainPipe(doc, mainIds, headPt);
            if (main == null)
                return "no horizontal main pipe";

            Line mainLine = (Line)((LocationCurve)main.Location).Curve;
            XYZ tee = mainLine.Project(headPt).XYZPoint;
            XYZ toHead = new XYZ(headPt.X - tee.X, headPt.Y - tee.Y, 0);
            double planDist = toHead.GetLength();
            double r = CmdSprinklerDownright.OuterRadius(doc, p.PipeTypeId, p.SizeFt);
            if (planDist < r * 6)
                return "sprinkler is too close to the main pipe in plan";
            XYZ dir = toHead.Normalize();

            double zTop = p.Fitting == TwinMainFitting.Tee ? tee.Z : tee.Z + p.A;
            if (p.Fitting != TwinMainFitting.Tee && p.A < r * 4)
                return "A is too small for the elbow above the main pipe";

            CmdSprinklerDownright.Obstacle ob = p.Auto
                ? CmdSprinklerDownright.AutoObstacle(doc, main, mainIds, tee, dir, planDist, headPt.Z + r * 4, zTop + r)
                : CmdSprinklerDownright.FirstObstacle(doc, obstacles, tee, dir, planDist);
            if (ob == null)
                return p.Auto ? "no obstacle detected between the main pipe and the sprinkler"
                              : "no picked MEP element between the main pipe and the sprinkler";
            string obNote = p.Auto ? " (obstacle " + ob.Id.ToInt() + ")" : "";

            double dropDist = ob.Enter - p.L2 - r;
            double zLow = ob.BottomZ - p.L3 - r;
            double minL1 = (p.Fitting == TwinMainFitting.TeeElbow45 ? p.A : 0) + r * 4;

            if (p.L1 < minL1)
                return "L1 is too short for the fittings at the main pipe";
            if (dropDist < p.L1 + r * 6)
                return "no room between the upright sprinkler and the drop before the obstacle" + obNote;
            if (dropDist > planDist - r * 4)
                return "the drop is beyond the sprinkler" + obNote;
            if (zLow > zTop - r * 4)
                return "the lower run is not below the upper run" + obNote;
            if (zLow < headPt.Z + r * 4)
                return "the lower run is too close to the sprinkler" + obNote;
            if (!p.Direct && p.Elevation < r * 4)
                return "Elevation is too small for the upright sprinkler riser";

            // Tuyến: điểm nối ống chính → (đứng A / chéo 45°) → tee đầu hướng lên → ống đứng xuống → ngang dưới → đầu phun.
            XYZ up = new XYZ(tee.X, tee.Y, zTop) + dir * p.L1;
            XYZ drop = tee + dir * dropDist;
            var pts = new List<XYZ> { tee };
            if (p.Fitting == TwinMainFitting.TeeElbow90)
                pts.Add(new XYZ(tee.X, tee.Y, zTop));
            else if (p.Fitting == TwinMainFitting.TeeElbow45)
                pts.Add(new XYZ(tee.X, tee.Y, zTop) + dir * p.A);
            int upIdx = pts.Count;
            pts.Add(up);
            pts.Add(new XYZ(drop.X, drop.Y, zTop));
            pts.Add(new XYZ(drop.X, drop.Y, zLow));
            pts.Add(new XYZ(headPt.X, headPt.Y, zLow));
            pts.Add(headPt);

            ElementId systemTypeId = main.get_Parameter(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM)?.AsElementId();
            if (systemTypeId == null || systemTypeId == ElementId.InvalidElementId)
                systemTypeId = new FilteredElementCollector(doc).OfClass(typeof(PipingSystemType)).FirstElementId();
            Level level = main.ReferenceLevel;

            var pipes = new List<Pipe>();
            for (int i = 0; i < pts.Count - 1; i++)
                pipes.Add(NewPipe(doc, systemTypeId, p, level.Id, pts[i], pts[i + 1], created));
            doc.Regenerate();

            for (int i = 1; i < pts.Count - 1; i++)
            {
                if (i == upIdx)
                    continue;
                created.Add(doc.Create.NewElbowFitting(CmdSprinklerDownright.ConnectorAt(pipes[i - 1], pts[i]),
                                                       CmdSprinklerDownright.ConnectorAt(pipes[i], pts[i])).Id);
            }

            CmdSprinklerDownright.ConnectToMain(doc, main, tee, pipes[0], mainIds, created, p.ElbowAtFreeEnd, otherTees);

            Connector last = CmdSprinklerDownright.ConnectorAt(pipes[pipes.Count - 1], headPt);
            Join(doc, last, head, created);

            return PlaceUpright(doc, systemTypeId, p, level, up, pipes[upIdx - 1], pipes[upIdx], created);
        }

        /// <summary>Tee trên ống ngang tại <paramref name="up"/> và đầu phun hướng lên (cắm thẳng vào tee hoặc qua ống đứng Elevation).</summary>
        private static string PlaceUpright(Document doc, ElementId systemTypeId, Params p, Level level, XYZ up,
                                           Pipe before, Pipe after, List<ElementId> created)
        {
            // Tee cần đủ 3 connector: dựng ống đứng (thật, hoặc đoạn mồi rồi xoá khi cắm thẳng).
            double riser = p.Direct ? Math.Max(OuterR(doc, p) * 6, Common.mmToFT * 100) : p.Elevation;
            XYZ top = up + XYZ.BasisZ * riser;
            Pipe vertical = NewPipe(doc, systemTypeId, p, level.Id, up, top, created);
            doc.Regenerate();

            FamilyInstance teeFit = doc.Create.NewTeeFitting(CmdSprinklerDownright.ConnectorAt(before, up),
                                                             CmdSprinklerDownright.ConnectorAt(after, up),
                                                             CmdSprinklerDownright.ConnectorAt(vertical, up));
            created.Add(teeFit.Id);

            Connector target;
            if (p.Direct)
            {
                created.Remove(vertical.Id);
                doc.Delete(vertical.Id);
                doc.Regenerate();
                target = teeFit.MEPModel.ConnectorManager.Connectors.Cast<Connector>()
                               .Where(c => !c.IsConnected).OrderByDescending(c => c.Origin.Z).FirstOrDefault();
                if (target == null)
                    return "the tee has no free outlet for the upright sprinkler";
            }
            else
            {
                target = CmdSprinklerDownright.ConnectorAt(vertical, top);
            }

            FamilyInstance upright = doc.Create.NewFamilyInstance(target.Origin, p.Upright, level, StructuralType.NonStructural);
            created.Add(upright.Id);
            doc.Regenerate();

            Connector sc = upright.MEPModel?.ConnectorManager?.Connectors.Cast<Connector>().OrderBy(c => c.Origin.Z).FirstOrDefault();
            if (sc == null)
                return "the upright sprinkler family has no pipe connector";
            if (sc.CoordinateSystem.BasisZ.Z > -0.5)
                return "the selected upright sprinkler's connector does not point down (choose an upright family)";

            ElementTransformUtils.MoveElement(doc, upright.Id, target.Origin - sc.Origin);
            doc.Regenerate();
            sc = upright.MEPModel.ConnectorManager.Connectors.Cast<Connector>().OrderBy(c => c.Origin.Z).First();
            Join(doc, target, sc, created);
            return null;
        }

        private static double OuterR(Document doc, Params p)
        {
            return CmdSprinklerDownright.OuterRadius(doc, p.PipeTypeId, p.SizeFt);
        }

        private static Pipe NewPipe(Document doc, ElementId systemTypeId, Params p, ElementId levelId, XYZ a, XYZ b, List<ElementId> created)
        {
            Pipe pipe = Pipe.Create(doc, systemTypeId, p.PipeTypeId, levelId, a, b);
            pipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).Set(p.SizeFt);
            created.Add(pipe.Id);
            return pipe;
        }

        // Cùng cỡ thì nối thẳng, khác cỡ thì chèn côn thu.
        private static void Join(Document doc, Connector pipeEnd, Connector sprinklerCon, List<ElementId> created)
        {
            if (Math.Abs(pipeEnd.Radius - sprinklerCon.Radius) < 1e-6)
                pipeEnd.ConnectTo(sprinklerCon);
            else
                created.Add(doc.Create.NewTransitionFitting(pipeEnd, sprinklerCon).Id);
        }
    }
}
