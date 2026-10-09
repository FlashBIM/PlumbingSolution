using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
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

                        ElementId systemTypeId = ElementId.InvalidElementId;
                        if (verticalMEPForm.MEPType_ == MEPType.Pipe)
                        {
                            systemTypeId = verticalMEPForm.SystemType;
                        }
                        else if (verticalMEPForm.MEPType_ == MEPType.Round_Duct)
                        {
                            systemTypeId = verticalMEPForm.SystemType;
                        }
                        else if (verticalMEPForm.MEPType_ == MEPType.Oval_Duct || verticalMEPForm.MEPType_ == MEPType.Rectangular_Duct)
                        {
                            systemTypeId = verticalMEPForm.SystemType;
                        }

                        var mepNew = err(start, end, verticalMEPForm.FamilyType, systemTypeId, verticalMEPForm.LevelBottomId);

                        if (mepNew != null)
                        {
                            //Set parameter
                            double height = 0; //inch
                            double width = 0;
                            if (verticalMEPForm.MEPType_ == MEPType.Pipe || verticalMEPForm.MEPType_ == MEPType.Round_Duct)
                            {
                                width = (verticalMEPForm.MEPSize_ as MEPSize).NominalDiameter;
                                mepNew.LookupParameter("Diameter").Set(width);

                                height = (verticalMEPForm.MEPSize_ as MEPSize).NominalDiameter;
                            }
                            else if (verticalMEPForm.MEPType_ == MEPType.Conduit)
                            {
                                width = (verticalMEPForm.MEPSize_ as ConduitSize).NominalDiameter;
                                mepNew.LookupParameter("Diameter(Trade Size)").Set(width);

                                height = (verticalMEPForm.MEPSize_ as ConduitSize).NominalDiameter;
                            }
                            else
                            {
                                width = verticalMEPForm.MEP_Width * Common.mmToFT;
                                mepNew.LookupParameter("Width").Set(width);
                                mepNew.LookupParameter("Height").Set(verticalMEPForm.MEP_Height * Common.mmToFT);

                                height = verticalMEPForm.MEP_Height * Common.mmToFT;
                            }

                            if (verticalMEPForm.MEPType_ == MEPType.CableTray || verticalMEPForm.MEPType_ == MEPType.Conduit && verticalMEPForm.ServiceType != string.Empty)
                            {
                                mepNew.LookupParameter("Service Type").Set(verticalMEPForm.ServiceType);
                            }
                        }
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
            ElementType elementType = Global.UIDoc.Document.GetElement(elementTypeId) as ElementType;

            MEPCurve mepCurve = null;

            if (elementType is Autodesk.Revit.DB.Mechanical.DuctType)
            {
                mepCurve = Autodesk.Revit.DB.Mechanical.Duct.Create(Global.UIDoc.Document, systemTypeId, elementTypeId, levelId, start, end);
            }
            else if (elementType is PipeType)
            {
                mepCurve = Pipe.Create(Global.UIDoc.Document, systemTypeId, elementTypeId, levelId, start, end);
            }
            else if (elementType is CableTrayType)
            {
                mepCurve = CableTray.Create(Global.UIDoc.Document, elementTypeId, start, end, levelId);
            }
            else if (elementType is ConduitType)
            {
                mepCurve = Conduit.Create(Global.UIDoc.Document, elementTypeId, start, end, levelId);
            }
            return mepCurve;
        }
    }
}