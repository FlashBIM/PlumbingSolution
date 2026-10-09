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
    /// Tích Preview trên form: A chỉnh bằng con trỏ trong view (UprightType4Preview) thay vì nhập.
    /// Dùng chung các hàm nối với Pendent Type 5/6 (CmdSprinklerDownrightType56.cs).
    /// </summary>
    public partial class CmdSprinklerUpright
    {
        /// <summary>Hình học của một đầu phun không phụ thuộc A — tính một lần, dùng cho preview và khi nối.</summary>
        internal class Type4Plan
        {
            public FamilyInstance Sprinkler;
            public XYZ Head;       // connector đầu phun (đáy, quay xuống)
            public XYZ Tee;        // chân vuông góc trên tim ống chính
            public double Radius;  // bán kính ngoài ống nhánh
            public string Reason;  // null nếu nối được (với một A nào đó)

            /// <summary>Khoảng A hợp lệ: đủ chỗ đặt co trên ống chính và dưới đầu phun.</summary>
            public double MinA => Radius * 4;
            public double MaxA => Head.Z - Tee.Z - Radius * 4;

            public List<XYZ> Points(double aFt)
            {
                double zRun = Tee.Z + aFt;
                return new List<XYZ>
                {
                    Tee,
                    new XYZ(Tee.X, Tee.Y, zRun),
                    new XYZ(Head.X, Head.Y, zRun),
                    Head,
                };
            }
        }

        public static Result ProcessType4()
        {
            var form = App.m_ConnectSprinkleForm;
            bool reshowForm = true;

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

                Document doc = Global.UIDoc.Document;
                ElementId pipeTypeId = form.FamilyTypeC2;
                double sizeFt = Common.mmToFT * form.PipeSizeC2;
                List<ElementId> mainIds = mainPipes.Select(p => p.Id).ToList();

                if (form.IsPreview)
                {
                    double startA = form.HeightA_ > 0 ? Common.mmToFT * form.HeightA_ : Common.mmToFT * 200;
                    if (UprightType4Preview.Start(Global.UIApp, sprinklers, mainIds, pipeTypeId, sizeFt, startA))
                    {
                        // Phiên preview sống qua các lần Idling; nó tự hiện lại form khi click tạo hoặc Esc.
                        reshowForm = false;
                        return Result.Succeeded;
                    }
                    return Result.Cancelled;
                }

                CreateType4(doc, sprinklers, mainIds, pipeTypeId, sizeFt, Common.mmToFT * form.HeightA_);
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
                if (reshowForm)
                    ShowType4Form();
            }
        }

        internal static void ShowType4Form()
        {
            var form = App.m_ConnectSprinkleForm;
            if (form != null && form.IsDisposed == false)
            {
                form.Show(App.hWndRevit);
                DisplayService.SetFocus(new HandleRef(null, form.Handle));
            }
        }

        /// <summary>Nối tất cả đầu phun với cùng A; báo các đầu bị bỏ qua.</summary>
        internal static void CreateType4(Document doc, List<FamilyInstance> sprinklers, List<ElementId> mainIds,
                                         ElementId pipeTypeId, double sizeFt, double aFt)
        {
            int done = 0;
            var skipped = new List<string>();

            // Elbow Connection: đầu phun gần đầu ống chính còn hở thì nối bằng co, cắt bỏ đoạn thừa (như Type 1-3).
            // Điểm nối dự kiến của mọi đầu phun để không cắt mất đoạn ống mà đầu phun khác còn cần nối vào.
            bool elbowAtFreeEnd = App.m_ConnectSprinkleForm?.isElbow == true;
            var tees = new List<XYZ>();
            if (elbowAtFreeEnd)
            {
                foreach (FamilyInstance s in sprinklers)
                {
                    Type4Plan plan;
                    Pipe m;
                    PlanType4(doc, s, mainIds, pipeTypeId, sizeFt, out plan, out m);
                    if (plan.Tee != null)
                        tees.Add(plan.Tee);
                }
            }

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
                            reason = ConnectType4(doc, sprinkler, mainIds, pipeTypeId, sizeFt, aFt, elbowAtFreeEnd, tees, created);
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
        }

        /// <summary>Tính phần hình học không phụ thuộc A. Trả về null nếu nối được, ngược lại là lý do.</summary>
        internal static string PlanType4(Document doc, FamilyInstance sprinkler, List<ElementId> mainIds,
                                         ElementId pipeTypeId, double sizeFt, out Type4Plan plan, out Pipe main)
        {
            plan = new Type4Plan { Sprinkler = sprinkler };
            main = null;

            // Đầu phun hướng lên: connector nằm ở đáy, quay xuống.
            Connector head = sprinkler.MEPModel?.ConnectorManager?.Connectors.Cast<Connector>()
                                      .OrderBy(c => c.Origin.Z).FirstOrDefault();
            if (head == null)
                return plan.Reason = "sprinkler has no connector";

            plan.Head = head.Origin;
            if (head.IsConnected)
                return plan.Reason = "sprinkler is already connected";

            main = CmdSprinklerDownright.NearestMainPipe(doc, mainIds, plan.Head);
            if (main == null)
                return plan.Reason = "no horizontal main pipe";

            Line mainLine = (Line)((LocationCurve)main.Location).Curve;
            plan.Tee = mainLine.Project(plan.Head).XYZPoint;
            plan.Radius = CmdSprinklerDownright.OuterRadius(doc, pipeTypeId, sizeFt);

            XYZ toHead = new XYZ(plan.Head.X - plan.Tee.X, plan.Head.Y - plan.Tee.Y, 0);
            if (toHead.GetLength() < plan.Radius * 6)
                return plan.Reason = "sprinkler is too close to the main pipe in plan (use Type 1-3)";
            if (plan.MinA > plan.MaxA)
                return plan.Reason = "sprinkler is too low above the main pipe";

            return null;
        }

        internal static string CheckA(Type4Plan plan, double aFt)
        {
            if (plan.Reason != null)
                return plan.Reason;
            if (aFt < plan.MinA)
                return "A is too small for an elbow above the main pipe";
            if (aFt > plan.MaxA)
                return "A puts the horizontal run too close to the sprinkler";
            return null;
        }

        /// <summary>Nối một đầu phun. Trả về null nếu thành công, ngược lại là lý do bỏ qua.</summary>
        private static string ConnectType4(Document doc, FamilyInstance sprinkler, List<ElementId> mainIds,
                                           ElementId pipeTypeId, double sizeFt, double aFt, bool elbowAtFreeEnd,
                                           IList<XYZ> otherTees, List<ElementId> created)
        {
            Type4Plan plan;
            Pipe main;
            string reason = PlanType4(doc, sprinkler, mainIds, pipeTypeId, sizeFt, out plan, out main) ?? CheckA(plan, aFt);
            if (reason != null)
                return reason;

            List<XYZ> pts = plan.Points(aFt);
            Connector head = sprinkler.MEPModel.ConnectorManager.Connectors.Cast<Connector>()
                                      .OrderBy(c => c.Origin.Z).First();

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

            CmdSprinklerDownright.ConnectToMain(doc, main, plan.Tee, pipes[0], mainIds, created, elbowAtFreeEnd, otherTees);

            Connector last = CmdSprinklerDownright.ConnectorAt(pipes[pipes.Count - 1], plan.Head);
            if (Math.Abs(last.Radius - head.Radius) < 1e-6)
                last.ConnectTo(head);
            else
                created.Add(doc.Create.NewTransitionFitting(last, head).Id);

            return null;
        }
    }
}
