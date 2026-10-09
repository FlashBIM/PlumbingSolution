using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
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
    /// <summary>
    /// Upright Sprinkler Type 4 (mới, theo sheet Fire Protection): tee trên đỉnh ống chính → ống đứng cao A →
    /// co → ống ngang tới ngay dưới đầu phun → co → ống đứng lên đầu phun.
    /// Chọn đầu phun → Finish, chọn ống chính → Finish; mỗi đầu phun nối ống chính gần nhất.
    /// Co theo Routing Preferences của Pipe Type trên form; tee/tap giữa ống chính, co ở đầu ống chính.
    /// Dùng chung các hàm nối với Pendent Type 5/6 (CmdSprinklerDownrightType56.cs).
    /// </summary>
    public partial class CmdSprinklerUpright
    {
        public static Result ProcessType4()
        {
            var form = App.m_ConnectSprinkleForm;
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

                List<Pipe> mainPipes = CmdSprinklerDownright.PickPipes();
                if (mainPipes == null || mainPipes.Count == 0)
                    return Result.Cancelled;

                ElementId pipeTypeId = form.FamilyTypeC2;
                double sizeFt = Common.mmToFT * form.PipeSizeC2;
                double aFt = Common.mmToFT * form.HeightA_;
                List<ElementId> mainIds = mainPipes.Select(p => p.Id).ToList();

                using (TransactionGroup group = new TransactionGroup(doc, "Upright Sprinkler Type 4"))
                {
                    group.Start();

                    foreach (FamilyInstance sprinkler in sprinklers)
                    {
                        string reason;
                        using (Transaction t = new Transaction(doc, "Upright Sprinkler"))
                        {
                            t.Start();
                            try
                            {
                                var created = new List<ElementId>();
                                reason = ConnectType4(doc, sprinkler, mainIds, pipeTypeId, sizeFt, aFt, created);
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
                    TaskDialog.Show("Upright Sprinkler",
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
        private static string ConnectType4(Document doc, FamilyInstance sprinkler, List<ElementId> mainIds,
                                           ElementId pipeTypeId, double sizeFt, double aFt, List<ElementId> created)
        {
            // Đầu phun hướng lên: connector nằm ở đáy, quay xuống.
            Connector head = sprinkler.MEPModel?.ConnectorManager?.Connectors.Cast<Connector>()
                                      .OrderBy(c => c.Origin.Z).FirstOrDefault();
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
            double radius = CmdSprinklerDownright.OuterRadius(doc, pipeTypeId, sizeFt);
            if (toHead.GetLength() < radius * 6)
                return "sprinkler is too close to the main pipe in plan (use Type 1-3)";

            double zRun = tee.Z + aFt;
            if (zRun < tee.Z + radius * 4)
                return "A is too small for an elbow above the main pipe";
            if (zRun > headPt.Z - radius * 4)
                return "A puts the horizontal run too close to the sprinkler";

            var pts = new List<XYZ>
            {
                tee,
                new XYZ(tee.X, tee.Y, zRun),
                new XYZ(headPt.X, headPt.Y, zRun),
                headPt,
            };

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

            for (int i = 0; i < pipes.Count - 1; i++)
            {
                FamilyInstance elbow = doc.Create.NewElbowFitting(CmdSprinklerDownright.ConnectorAt(pipes[i], pts[i + 1]),
                                                                  CmdSprinklerDownright.ConnectorAt(pipes[i + 1], pts[i + 1]));
                created.Add(elbow.Id);
            }

            CmdSprinklerDownright.ConnectToMain(doc, main, tee, pipes[0], mainIds, created);

            Connector last = CmdSprinklerDownright.ConnectorAt(pipes[pipes.Count - 1], headPt);
            if (Math.Abs(last.Radius - head.Radius) < 1e-6)
                last.ConnectTo(head);
            else
                created.Add(doc.Create.NewTransitionFitting(last, head).Id);

            return null;
        }
    }
}
