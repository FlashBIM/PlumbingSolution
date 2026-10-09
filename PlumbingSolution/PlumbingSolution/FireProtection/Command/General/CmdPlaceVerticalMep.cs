using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using PlumbingSolution.LoginLicense.Gate;
using PlumbingSolution.FireProtection.Services;
using PlumbingSolution.FireProtection.UI.GeneralUI;
using PlumbingSolution.FireProtection.Ultis;
using PlumbingSolution.FireProtection.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PlumbingSolution.FireProtection.Command.General
{
    [Transaction(TransactionMode.Manual)]
    public class CmdPlaceVerticalMep : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Global.UIApp = commandData.Application;
            Global.RVTApp = commandData.Application.Application;
            Global.UIDoc = commandData.Application.ActiveUIDocument;
            Global.AppCreation = commandData.Application.Application.Create;

            if (!LicenseGate.IsToolAllowed(this))
                return Result.Cancelled;

            if (App.ShowVerticalMEPForm() == false)
            {
                return Result.Cancelled;
            }

            return Result.Succeeded;
        }

        public static Result Process()
        {
            var verticalMEPForm = App.m_VerticalMEPForm;

            if (verticalMEPForm != null && verticalMEPForm.IsDisposed == false)
                verticalMEPForm.Hide();

            Transaction tran = new Transaction(Global.UIDoc.Document, "Vertical Pipe");

            try
            {
                while (true)
                {
                    try
                    {
                        ObjectSnapTypes snapTypes = ObjectSnapTypes.Nearest |
                            ObjectSnapTypes.Midpoints |
                            ObjectSnapTypes.Endpoints |
                            ObjectSnapTypes.Intersections |
                            ObjectSnapTypes.Centers |
                            ObjectSnapTypes.Perpendicular |
                            ObjectSnapTypes.Points;

                        XYZ point = Global.UIDoc.Selection.PickPoint(snapTypes, "Select an point: ");

                        tran.Start();

                        double startZ = 0;
                        double endZ = 0;

                        ce(verticalMEPForm, out startZ, out endZ);

                        XYZ start = new XYZ(point.X, point.Y, startZ);

                        XYZ end = new XYZ(point.X, point.Y, endZ);

                        var mepNew = err(start, end, verticalMEPForm.FamilyType, verticalMEPForm.SystemType, verticalMEPForm.LevelBottomId);

                        if (mepNew != null)
                            mepNew.LookupParameter("Diameter").Set((verticalMEPForm.MEPSize_ as MEPSize).NominalDiameter);
                        tran.Commit();
                    }
                    catch (System.Exception ex)
                    {
                        string mess = ex.Message;

                        if (tran.HasStarted())
                            tran.RollBack();

                        break;
                    }
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                if (verticalMEPForm != null && verticalMEPForm.IsDisposed == false)
                    verticalMEPForm.Show(App.hWndRevit);

                DisplayService.SetFocus(new HandleRef(null, verticalMEPForm.Handle));
            }

            return Result.Succeeded;
        }

        private static void ce(VerticalMEPForm form, out double startZ, out double endZ)
        {
            var top = Global.UIDoc.Document.GetElement(form.LevelTopId) as Level;
            var bottom = Global.UIDoc.Document.GetElement(form.LevelBottomId) as Level;

            startZ = bottom.Elevation + form.OffsetBottom * Common.mmToFT;
            endZ = top.Elevation + form.OffsetTop * Common.mmToFT;
        }

        public static MEPCurve err(XYZ start, XYZ end, ElementId elementTypeId, ElementId systemTypeId, ElementId levelId)
        {
            return Pipe.Create(Global.UIDoc.Document, systemTypeId, elementTypeId, levelId, start, end);
        }
    }
}