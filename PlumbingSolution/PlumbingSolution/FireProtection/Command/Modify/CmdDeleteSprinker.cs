using PlumbingSolution.LoginLicense.Gate;
using PlumbingSolution.FireProtection.Ultis;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PlumbingSolution.FireProtection.Command.Modify
{
    [Transaction(TransactionMode.Manual)]
    public class CmdDeleteSprinker : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            if (!LicenseGate.IsToolAllowed(this))
                return Result.Cancelled;

            Global.UIApp = commandData.Application;
            Global.RVTApp = commandData.Application.Application;
            Global.UIDoc = commandData.Application.ActiveUIDocument;
            Global.AppCreation = commandData.Application.Application.Create;

            using (Transaction trans = new Transaction(Global.UIDoc.Document, "Delete Sprinker"))
            {
                trans.Start();
                Process();
                trans.Commit();
            }

            return Result.Succeeded;
        }

        public static void Process()
        {
            List<FamilyInstance> sprinklers = SelectSprinklers();
            if (sprinklers == null || sprinklers.Count == 0)
                return;

            foreach (var item in sprinklers)
            {
                var fitting = Common.GetFittingConnected(item.MEPModel.ConnectorManager.Connectors.GetConectors().FirstOrDefault());
                if (fitting != null)
                    Global.UIDoc.Document.Delete(fitting.Id);

                DeleteSchema(item);
            }
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

                    list.Add(familyInstance);
                }
            }
            catch (System.Exception ex)
            {
            }
            return list;
        }


        public static void CreateSchema(FamilyInstance sprinker, List<string> listId, double zSprinkler = 0)
        {
            var schema = Autodesk.Revit.DB.ExtensibleStorage.Schema.Lookup(StorageUtility.m_MEP_DeleteSprinker_Guid);
            var value = StorageUtility.GetValue(sprinker, schema, StorageUtility.m_MEP_DeleteSprinker, typeof(string));
            string newstring = string.Empty;
            foreach (string id in listId)
                newstring += id + "|";

            if (zSprinkler != 0)
            {
                newstring += "locZ" + ":" + zSprinkler.ToString();
            }

            if (value != null)
            {
                StorageUtility.SetValue(sprinker, StorageUtility.m_MEP_DeleteSprinker_Guid, StorageUtility.m_MEP_DeleteSprinker, typeof(string), newstring);
            }
            else
                StorageUtility.AddEntity(sprinker, StorageUtility.m_MEP_DeleteSprinker_Guid, StorageUtility.m_MEP_DeleteSprinker, newstring);
        }

        private static void DeleteSchema(FamilyInstance sprinker)
        {
            var schema = Autodesk.Revit.DB.ExtensibleStorage.Schema.Lookup(StorageUtility.m_MEP_DeleteSprinker_Guid);
            var value = StorageUtility.GetValue(sprinker, schema, StorageUtility.m_MEP_DeleteSprinker, typeof(string));
            if (value != null)
            {
                string[] split = value.ToString().Split('|');
                bool isDown = false;
                bool isType6 = false;
                for (int i = 0; i < split.Count(); i++)
                {
                    FamilyInstance elbowFittingConnected1 = null;
                    FamilyInstance elbowFittingConnected2 = null;
                    MEPCurve pipe1 = null;
                    MEPCurve pipe2 = null;
                    FamilyInstance nipple = null;
                    List<Tuple<XYZ, XYZ>> tuples = new List<Tuple<XYZ, XYZ>>();

                    string id = split[i];
                    if (id == "IsDown")
                    {
                        isDown = true;
                        continue;
                    }

                    if (id == "Type6")
                    {
                        isType6 = true;
                        continue;
                    }

                    if (isType6)
                    {
                        try
                        {
                            var elementId = new ElementId(int.Parse(id));
                            FamilyInstance elem = Global.UIDoc.Document.GetElement(elementId) as FamilyInstance;

                            var fitting = Common.GetFittingConnected(elem.MEPModel.ConnectorManager.Connectors.GetConectors().FirstOrDefault());
                            if (fitting != null)
                                Global.UIDoc.Document.Delete(fitting.Id);

                            break;
                        }
                        catch (Exception)
                        {
                        }
                    }
                    if (isDown)
                    {
                        try
                        {
                            var elementId = new ElementId(int.Parse(id));
                            FamilyInstance elem = Global.UIDoc.Document.GetElement(elementId) as FamilyInstance;

                            var fitting = Common.GetFittingConnected(elem.MEPModel.ConnectorManager.Connectors.GetConectors().FirstOrDefault());
                            if (fitting != null)
                                Global.UIDoc.Document.Delete(fitting.Id);

                            break;
                        }
                        catch (Exception)
                        {
                        }
                    }

                    try
                    {
                        var elementId = new ElementId(int.Parse(id));
                        Element elem = Global.UIDoc.Document.GetElement(elementId);
                        if (elem != null && elem is FamilyInstance instance && instance.MEPModel is MechanicalFitting)
                        {
                            Common.GetInformationConectorWye(instance, null, out Connector conS, out Connector conE, out Connector conTee);
                            if (conTee != null)
                            {
                                try
                                {
                                    if (conS.AllRefs.GetConectors().FirstOrDefault().Owner is MEPCurve)
                                        pipe1 = conS.AllRefs.GetConectors().FirstOrDefault().Owner as MEPCurve;
                                    else if (conS.AllRefs.GetConectors().FirstOrDefault().Owner is FamilyInstance)
                                        nipple = conS.AllRefs.GetConectors().FirstOrDefault().Owner as FamilyInstance;

                                    if (conE.AllRefs.GetConectors().FirstOrDefault().Owner is MEPCurve)
                                        pipe2 = conE.AllRefs.GetConectors().FirstOrDefault().Owner as MEPCurve;
                                    else if (conE.AllRefs.GetConectors().FirstOrDefault().Owner is FamilyInstance)
                                        nipple = conE.AllRefs.GetConectors().FirstOrDefault().Owner as FamilyInstance;
                                }
                                catch (Exception)
                                {
                                }
                            }
                        }

                        Global.UIDoc.Document.Delete(elementId);
                        Global.UIDoc.Document.Regenerate();

                        if (pipe1 != null && pipe2 != null)
                        {
                            var conStPipeHor1 = (pipe1 as MEPCurve).ConnectorManager.Lookup(0);
                            var conEndPipeHor1 = (pipe1 as MEPCurve).ConnectorManager.Lookup(1);

                            var conStPipeHor2 = (pipe2 as MEPCurve).ConnectorManager.Lookup(0);
                            var conEndPipeHor2 = (pipe2 as MEPCurve).ConnectorManager.Lookup(1);
                            tuples.Add(new Tuple<XYZ, XYZ>(conStPipeHor1.Origin, conStPipeHor2.Origin));
                            tuples.Add(new Tuple<XYZ, XYZ>(conStPipeHor1.Origin, conEndPipeHor2.Origin));
                            tuples.Add(new Tuple<XYZ, XYZ>(conEndPipeHor1.Origin, conStPipeHor2.Origin));
                            tuples.Add(new Tuple<XYZ, XYZ>(conEndPipeHor1.Origin, conEndPipeHor2.Origin));

                            if (conStPipeHor1 != null && conEndPipeHor1 != null)
                            {
                                if (conStPipeHor1.IsConnected || conEndPipeHor1.IsConnected)
                                {
                                    Connector connectorIsConnecting = (conStPipeHor1.IsConnected) ? conStPipeHor1 : conEndPipeHor1;

                                    elbowFittingConnected1 = Common.GetFittingConnected(connectorIsConnecting);
                                    if (elbowFittingConnected1 != null)
                                    {
                                        ConnectorUtils.GetConnectorClosedTo((pipe1 as MEPCurve).ConnectorManager, elbowFittingConnected1.MEPModel.ConnectorManager, out Connector conPipeHor1, out Connector conFitHor);
                                        if (conPipeHor1 != null && conFitHor != null && conPipeHor1.IsConnectedTo(conFitHor))
                                            conPipeHor1.DisconnectFrom(conFitHor);
                                    }
                                }

                                if (conStPipeHor2.IsConnected || conEndPipeHor2.IsConnected)
                                {
                                    Connector connectorIsConnecting = (conStPipeHor2.IsConnected) ? conStPipeHor2 : conEndPipeHor2;

                                    elbowFittingConnected2 = Common.GetFittingConnected(connectorIsConnecting);
                                    if (elbowFittingConnected2 != null)
                                    {
                                        ConnectorUtils.GetConnectorClosedTo((pipe2 as MEPCurve).ConnectorManager, elbowFittingConnected2.MEPModel.ConnectorManager, out Connector conPipeHor1, out Connector conFitHor);
                                        if (conPipeHor1 != null && conFitHor != null && conPipeHor1.IsConnectedTo(conFitHor))
                                            conPipeHor1.DisconnectFrom(conFitHor);
                                    }
                                }
                            }

                            var tup = tuples.Where(x => x.Item1.DistanceTo(x.Item2) == tuples.Max(y => y.Item1.DistanceTo(y.Item2))).FirstOrDefault();
                            (pipe1.Location as LocationCurve).Curve = Line.CreateBound(tup.Item1, tup.Item2);

                            // Join lại fitting
                            if (elbowFittingConnected1 != null)
                            {
                                Element mepCurve = pipe1;

                                if (id != null)
                                {
                                    ConnectorUtils.GetConnectorClosedTo((mepCurve as MEPCurve).ConnectorManager, elbowFittingConnected1.MEPModel.ConnectorManager, out Connector conPipeHor1, out Connector conFitHor);

                                    if (conPipeHor1 != null && conFitHor != null && !conPipeHor1.IsConnected && !conFitHor.IsConnected)
                                    {
                                        conPipeHor1.ConnectTo(conFitHor);
                                    }
                                }
                            }

                            if (elbowFittingConnected2 != null)
                            {
                                Element mepCurve = pipe1;

                                if (id != null)
                                {
                                    ConnectorUtils.GetConnectorClosedTo((mepCurve as MEPCurve).ConnectorManager, elbowFittingConnected2.MEPModel.ConnectorManager, out Connector conPipeHor1, out Connector conFitHor);

                                    if (conPipeHor1 != null && conFitHor != null && !conPipeHor1.IsConnected && !conFitHor.IsConnected)
                                    {
                                        conPipeHor1.ConnectTo(conFitHor);
                                    }
                                }
                            }

                            Global.UIDoc.Document.Delete(pipe2.Id);
                        }
                        else if (nipple != null)
                        {
                            MEPCurve pipe = null;
                            if (pipe1 != null)
                                pipe = pipe1;
                            else
                                pipe = pipe2;

                            var conectorNipple = Common.GetConnectorNotConnnected(nipple.MEPModel.ConnectorManager);

                            var connectorPipe = Common.GetConnectorClosestTo(pipe, conectorNipple.Origin);
                            connectorPipe.Origin = conectorNipple.Origin;
                            connectorPipe.ConnectTo(conectorNipple);
                        }
                    }
                    catch (Exception ex)
                    {
                        continue;
                    }
                }

                var locP = split.FirstOrDefault(x => x.Contains("locZ"));
                if (!string.IsNullOrEmpty(locP))
                {
                    var z = locP.Split(':')[1];
                    if (double.TryParse(z.Trim(), out double locZ))
                    {
                        var locSprink = sprinker.Location as LocationPoint;
                        if (locSprink != null)
                        {
                            (sprinker.Location as LocationPoint).Point = new XYZ(locSprink.Point.X, locSprink.Point.Y, locZ);
                        }
                    }
                }
            }
        }

    }
}
