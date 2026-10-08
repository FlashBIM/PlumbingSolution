using PlumbingSolution.LoginLicense.Gate;
using PlumbingSolution.FireProtection.ItemData;
using PlumbingSolution.FireProtection.Services;
using PlumbingSolution.FireProtection.UI.Service_J;
using PlumbingSolution.FireProtection.Ultis;
using PlumbingSolution.FireProtection.Utils;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using PlumbingSolution.FireProtection.UI.Service_J;

namespace PlumbingSolution.FireProtection.Command.Fire.DiritCAD.Service_J
{
    [Transaction(TransactionMode.Manual)]
    public class CmdCreateFmlFromBasePoint : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;

            if (!LicenseGate.IsToolAllowed(this))
                return Result.Cancelled;

            using (TransactionGroup transG = new TransactionGroup(uiDoc.Document, "CreateFamilyFromCAD"))
            {
                try
                {
                    transG.Start();

                    string linkName = string.Empty;
                    ImportInstance importInstance = null;
                    try
                    {
                        var linkRef = uiDoc.Selection.PickObject(ObjectType.Element, new LinkCADSelectionFilter());
                        importInstance = uiDoc.Document.GetElement(linkRef) as ImportInstance;

                        linkName = importInstance.Category.Name;
                    }
                    catch (Exception)
                    {
                        linkName = string.Empty;
                    }

                    CreateFamilyFromCADServices services = new CreateFamilyFromCADServices(uiDoc);

                    if (importInstance != null)
                        services.LinkCADDatas.Add(new LinkCADData(importInstance));

                    if (services.LinkCADDatas.Count > 0)
                    {
                        DialogResult dialogResult;
                        IntPtr revitHandle = commandData.Application.MainWindowHandle;
                        var revitWindow = new WindowWrapper(revitHandle);

                        // keep UI show dialog when done process
                        do
                        {
                            string blockName = string.Empty;
                            bool isAfterPick = false;
                            PickOptions pickOptions = PickOptions.None;

                            // keep UI can show and hide to pick element in Revit
                            do
                            {
                                DirectoryUtils.ReadCreateFamilyFromCADData(out CreateFamilyFromCADSettingData settingData);
                                FrmCreateFmlFromBasePoint UI = new FrmCreateFmlFromBasePoint(settingData, services, new LinkCADData(importInstance), blockName, linkName, pickOptions, isAfterPick);
                                dialogResult = UI.ShowDialog(revitWindow);

                                if (dialogResult == DialogResult.Abort)
                                {
                                    pickOptions = UI.PickOptions;

                                    if (pickOptions == PickOptions.PickBlock)
                                    {
                                        var linkCADData = UI.SelectedLink;
                                        try
                                        {
                                            var groupRef = uiDoc.Selection.PickObject(ObjectType.Element, new GroupBlockSelectionFilter(linkCADData.Blocks));
                                            Group group = uiDoc.Document.GetElement(groupRef) as Group;

                                            blockName = group.GroupType.Name;
                                        }
                                        catch (Exception)
                                        {
                                            blockName = string.Empty;
                                        }
                                    }
                                    else
                                    {
                                    }

                                    isAfterPick = true;
                                }
                            } while (dialogResult == DialogResult.Abort);

                            if (dialogResult == DialogResult.OK)
                            {
                                DirectoryUtils.ReadCreateFamilyFromCADData(out CreateFamilyFromCADSettingData settingData);
                                LinkCADData linkCADData = services.LinkCADDatas.FirstOrDefault(x => x.Name.Equals(settingData.LinkName));

                                try
                                {
                                    if (settingData.IsCreateBlockInOrigin)
                                    {
                                        // set block property IsBlockInOrigin state
                                        // group block by block name, get state by first block and set to other blocks
                                        var blocks = linkCADData.Blocks.Where(x => x.Name == settingData.BlockName);
                                        BlockData baseBlock = blocks.FirstOrDefault();
                                        //if (baseBlock.RevitGroup == null)
                                        //{
                                        //    using (Transaction trans = new Transaction(uiDoc.Document, "Check block origin"))
                                        //    {
                                        //        trans.Start();
                                        //        Group group = uiDoc.Document.Create.NewGroup(baseBlock.LineIds);
                                        //        group.GroupType.Name = baseBlock.Name;
                                        //        baseBlock.RevitGroup = group;

                                        //        trans.Commit();
                                        //        baseBlock.Location = (group.Location as LocationPoint).Point;
                                        //    }
                                        //}

                                        //double tolerance = 1 / 304.8;

                                        foreach (var item in blocks)
                                        {
                                            item.Location = item.BlockOrigin;
                                        }
                                    }
                                    else
                                    {
                                        var groupRefs = uiDoc.Selection.PickObjects(ObjectType.Element, new GroupBlockToCreateSelectionFilter(linkCADData.Blocks, settingData.BlockName));
                                        List<Group> groups = groupRefs.Select(x => uiDoc.Document.GetElement(x)).Cast<Group>().ToList();

                                        linkCADData.PickedBlocks.Clear();
                                        foreach (var group in groups)
                                        {
                                            BlockData blockData = new BlockData(settingData.BlockName, group);
                                            linkCADData.PickedBlocks.Add(blockData);
                                        }
                                    }
                                }
                                catch (Exception)
                                {
                                    dialogResult = DialogResult.Cancel;
                                }
                            }

                            // run command process
                            if (dialogResult == DialogResult.OK)
                                services.CreateFamily();

                            // break when all setting is OK
                        } while (dialogResult == DialogResult.OK);

                        using (Transaction trans = new Transaction(uiDoc.Document, "Delete"))
                        {
                            FailureHandlingOptions options = trans.GetFailureHandlingOptions();
                            DisableWarning preproccessor = new DisableWarning();
                            options.SetClearAfterRollback(true);
                            options.SetFailuresPreprocessor(preproccessor);
                            trans.SetFailureHandlingOptions(options);

                            trans.Start();
                            foreach (var linkCAD in services.LinkCADDatas)
                            {
                                var idGroups = linkCAD.Blocks.Where(x => x.RevitGroup != null).Select(x => x.RevitGroup.Id).ToList();
                                var groupTypes = linkCAD.Blocks.Where(x => x.RevitGroup != null).Select(x => x.RevitGroup).Distinct(new DistinctGroupType()).Select(x => x.GroupType).ToList();
                                var idLines = linkCAD.Blocks.SelectMany(x => x.LineIds).ToList();

                                if (idGroups.Count > 0)
                                    uiDoc.Document.Delete(idGroups);

                                if (groupTypes.Count > 0)
                                {
                                    var idGroupTypes = new List<ElementId>();
                                    foreach (var groupType in groupTypes)
                                    {
                                        if (groupType != null && groupType.IsValidObject)
                                            idGroupTypes.Add(groupType.Id);
                                    }

                                    uiDoc.Document.Delete(idGroupTypes);
                                }

                                if (idLines.Count > 0)
                                {
                                    var idLinesValid = new FilteredElementCollector(uiDoc.Document, idLines).WhereElementIsNotElementType()
                                                                                                            .Where(x => x != null && x.IsValidObject)
                                                                                                            .Select(x => x.Id)
                                                                                                            .ToList();
                                    uiDoc.Document.Delete(idLinesValid);
                                }
                            }
                            trans.Commit();
                        }
                    }
                    else
                    {
                        string mess = Common.GetTextLanguage("NoCADLinkInThisProject");
                        IO.ShowWarning(mess);
                    }

                    transG.Assimilate();
                }
                catch (Exception)
                {
                }
            }

            return Result.Succeeded;
        }
    }

    public class WindowWrapper : IWin32Window
    {
        private IntPtr _handle;

        public WindowWrapper(IntPtr handle)
        {
            _handle = handle;
        }

        public IntPtr Handle => _handle;
    }
}
