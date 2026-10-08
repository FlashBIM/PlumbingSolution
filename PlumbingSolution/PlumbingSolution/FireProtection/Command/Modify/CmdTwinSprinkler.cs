using PlumbingSolution.LoginLicense.Gate;
using PlumbingSolution.FireProtection;
using PlumbingSolution.FireProtection.Command.Modify;
using PlumbingSolution.FireProtection.Services;
using PlumbingSolution.FireProtection.UI.ProcessBarForm;
using PlumbingSolution.FireProtection.Ultis;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Interop;

namespace PlumbingSolution.FireProtection.Command.Modify
{
    [Transaction(TransactionMode.Manual)]
    public class CmdTwinSprinkler : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            if (!LicenseGate.IsToolAllowed(this))
                return Result.Cancelled;

            Global.UIApp = commandData.Application;
            Global.RVTApp = commandData.Application.Application;
            Global.UIDoc = commandData.Application.ActiveUIDocument;
            Global.AppCreation = commandData.Application.Application.Create;

            //Show form
            if (App.ShowTwinSprinklerForm() == false)
                return Result.Cancelled;

            return Result.Succeeded;
        }

        public static Result Process()
        {
            try
            {
                if (App.m_TwinSprinklerForm != null && App.m_TwinSprinklerForm.IsDisposed == false)
                    App.m_TwinSprinklerForm.Hide();

                List<FamilyInstance> sprinklers = SelectSprinklers();
                if (sprinklers == null || sprinklers.Count == 0)
                    return Result.Cancelled;

                List<Pipe> pipes = PickPipe();
                if (pipes == null || pipes.Count == 0)
                    return Result.Cancelled;

                // Status cancel export : default = false
                bool isCancelExport = false;

                // Count type imported
                int nCount = 0;

                System.Diagnostics.Process process = System.Diagnostics.Process.GetCurrentProcess();
                IntPtr intPtr = process.MainWindowHandle;
                FrmProcessbar progressBar = new FrmProcessbar("Quá trình kết nối cặp đầu phun", Define.MessageFinish, DiritIconTool.Mep);
                progressBar.prgSingle.Minimum = 1;
                progressBar.prgSingle.Maximum = pipes.Count;
                progressBar.prgSingle.Value = 1;
                WindowInteropHelper helper = new WindowInteropHelper(progressBar);
                helper.Owner = intPtr;
                progressBar.Show();

                TransactionGroup tranGr = new TransactionGroup(Global.UIDoc.Document, "CreateConnector");
                tranGr.Start();

                //Find pipe
                try
                {
                    int count = pipes.Count;
                    foreach (Pipe pipe in pipes)
                    {
                        double dPercent = 0.0;

                        Transaction tran = new Transaction(Global.UIDoc.Document, "CreateConnector");
                        tran.Start();
                        try
                        {
                            TwinSprinklerProcess twinSprinklerProcess = new TwinSprinklerProcess(Global.UIDoc.Document, tran, App.m_TwinSprinklerForm);
                            TwinSprinklerProcess.Process(ref sprinklers, pipes, pipe);
                            tran.Commit();
                        }
                        catch (Exception)
                        {
                            tran.RollBack();
                        }

                        // If click cancel button when exporting
                        if (progressBar.IsCancel)
                        {
                            isCancelExport = true;
                            break;
                        }

                        nCount++;
                        dPercent = (double.Parse(nCount.ToString()) / double.Parse(count.ToString())) * 100;
                        progressBar.tbxMessage.Text = Math.Round(dPercent, 2).ToString() + "% ";
                        progressBar.IncrementProgressBar();
                    }
                }
                catch (Exception ex)
                { }
                finally
                {
                    progressBar.Dispose();
                }

                tranGr.Assimilate();

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                string mess = ex.Message;
                return Result.Cancelled;
            }
            finally
            {
                if (App.m_TwinSprinklerForm != null && App.m_TwinSprinklerForm.IsDisposed == false)
                {
                    App.m_TwinSprinklerForm.Show(App.hWndRevit);
                }

                DisplayService.SetFocus(new HandleRef(null, App.m_TwinSprinklerForm.Handle));
            }
            return Result.Cancelled;
        }

        public static List<FamilyInstance> SelectSprinklers()
        {
            List<FamilyInstance> list = new List<FamilyInstance>();
            try
            {
                var pickedObjs = Global.UIDoc.Selection.PickObjects(ObjectType.Element, new SprinklerFilter(), "Pick Sprinklers: ");

                foreach (Reference reference in pickedObjs)
                {
                    var familyInstance = Global.UIDoc.Document.GetElement(reference) as FamilyInstance;
                    if (familyInstance == null)
                        continue;

                    var check = Common.GetConnectorNotConnnected(familyInstance.MEPModel.ConnectorManager);
                    if (check == null)
                        continue;

                    list.Add(familyInstance);
                }
            }
            catch (System.Exception ex)
            {
            }
            return list;
        }

        public static List<Pipe> PickPipe()
        {
            //Pick pipe
            List<Pipe> pipes = new List<Pipe>();
            try
            {
                var pickedObjs = Global.UIDoc.Selection.PickObjects(ObjectType.Element, new MEPCurveFilter(), "Pick pipes: ");

                foreach (Reference pickedObj in pickedObjs)
                {
                    var pipe = Global.UIDoc.Document.GetElement(pickedObj) as Pipe;

                    if (pipe != null)
                        pipes.Add(pipe);
                }
            }
            catch (System.Exception ex)
            {
            }

            return pipes;
        }
    }
}
