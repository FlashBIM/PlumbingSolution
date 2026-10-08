using PlumbingSolution.FireProtection.ItemData;
using PlumbingSolution.FireProtection.UI.ProcessBarForm;
using PlumbingSolution.FireProtection.Ultis;
using PlumbingSolution.FireProtection.Utils;
using PlumbingSolution.FireProtection.Extensions;
using Autodesk.Revit.Creation;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using PlumbingSolution.FireProtection.SpeedHanger.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using Category = Autodesk.Revit.DB.Category;
using Curve = Autodesk.Revit.DB.Curve;
using Document = Autodesk.Revit.DB.Document;
using Ellipse = Autodesk.Revit.DB.Ellipse;
using Group = Autodesk.Revit.DB.Group;
using Level = Autodesk.Revit.DB.Level;
using Line = Autodesk.Revit.DB.Line;
using Outline = Autodesk.Revit.DB.Outline;
using PolyLine = Autodesk.Revit.DB.PolyLine;
using Reference = Autodesk.Revit.DB.Reference;
using Transform = Autodesk.Revit.DB.Transform;
using View = Autodesk.Revit.DB.View;

namespace PlumbingSolution.FireProtection.Services
{
    public class CreateFamilyFromCADServices
    {
        private UIDocument _uiDoc;
        private Document _doc;

        public List<Level> Levels;
        public List<LinkCADData> LinkCADDatas;
        public List<RevitLinkInstance> RevitLinks;
        public List<Family> Families;
        public Dictionary<string, int> CategoryMapping;

        public CreateFamilyFromCADServices(UIDocument uiDoc)
        {
            _uiDoc = uiDoc;
            _doc = uiDoc.Document;
            LinkCADDatas = new List<LinkCADData>();
            Levels = GetAllLevel();
            Families = GetFamilies();
            CategoryMapping = GetCategoryMappingName();
            RevitLinks = GetAllRevitLink();
        }

        public bool ExploreDWGImport(LinkCADData linkCADData)
        {
            using (Transaction trans = new Transaction(_doc, "Create Block Group"))
            {
                try
                {
                    FailureHandlingOptions options = trans.GetFailureHandlingOptions();
                    IgnoreWarning preproccessor = new IgnoreWarning();
                    options.SetFailuresPreprocessor(preproccessor);
                    trans.SetFailureHandlingOptions(options);

                    trans.Start();
                    EnableCategoryLineOnView();
                    ExtractBlockLines(linkCADData, out int blockErrorCount);
                    trans.Commit();

                    foreach (var block in linkCADData.Blocks)
                        block.UpdateListLine(_doc);

                    linkCADData.IsExplodedLine = true;

                    return true;
                }
                catch (Exception ex)
                {
                    IO.ShowWarning("Failed to explode the DWG file.");
                    return false;
                }
            }
        }

        public bool ExploreDWGImportBasePoint(LinkCADData linkCADData)
        {
            using (Transaction trans = new Transaction(_doc, "Create Block Group"))
            {
                try
                {
                    FailureHandlingOptions options = trans.GetFailureHandlingOptions();
                    IgnoreWarning preproccessor = new IgnoreWarning();
                    options.SetFailuresPreprocessor(preproccessor);
                    trans.SetFailureHandlingOptions(options);

                    trans.Start();
                    ExtractBlockLinesBaseP(linkCADData, out int blockErrorCount);
                    trans.Commit();

                    linkCADData.IsExplodedLine = true;

                    return true;
                }
                catch (Exception ex)
                {
                    IO.ShowWarning("Failed to explode the DWG file.");
                    return false;
                }
            }
        }

        public bool CreateBlockGroup(LinkCADData linkCADData)
        {
            using (Transaction trans = new Transaction(_doc, "Create Group"))
            {
                string title = Common.GetTextLanguage("CADLinkProcessing");
                FrmProcessbar progressBar = new FrmProcessbar(title, Define.MessageFinish, DiritIconTool.Mep);
                Process process = Process.GetCurrentProcess();
                IntPtr intPtr = process.MainWindowHandle;
                progressBar.prgSingle.Minimum = 1;
                progressBar.prgSingle.Maximum = linkCADData.Blocks.Count;
                progressBar.prgSingle.Value = 1;
                WindowInteropHelper helper = new WindowInteropHelper(progressBar);
                helper.Owner = intPtr;
                progressBar.Show();

                int nCount = 0;

                trans.Start();

                foreach (BlockData block in linkCADData.Blocks)
                {
                    Group group = _doc.Create.NewGroup(block.LineIds);
                    group.GroupType.Name = block.Name;

                    block.RevitGroup = group;
                    block.Location = (group.Location as LocationPoint).Point;

                    nCount++;
                    string dPercent = nCount.ToString() + "/" + linkCADData.Blocks.Count.ToString();
                    progressBar.tbxMessage.Text = dPercent.ToString();
                    progressBar.IncrementProgressBar();

                    if (progressBar.IsCancel)
                        break;
                }

                if (progressBar.IsCancel)
                {
                    linkCADData.Blocks.Select(x => x.RevitGroup).Where(x => x != null && x.IsValidObject).ToList().ForEach(x => x.UngroupMembers());
                    linkCADData.Blocks.ForEach(x => x.RevitGroup = null);
                }

                trans.Commit();

                progressBar.Dispose();

                return !progressBar.IsCancel;
            }
        }

        public void CreateFamily()
        {
            using (Transaction trans = new Transaction(_doc, "Create Family)"))
            {
                // number of success element created
                int insCount = 0;

                DirectoryUtils.ReadCreateFamilyFromCADData(out CreateFamilyFromCADSettingData settingData);
                Level level = Levels.FirstOrDefault(x => x.Name == settingData.Level);
                LinkCADData linkCADData = LinkCADDatas.FirstOrDefault(x => x.Name == settingData.LinkName);
                List<RevitLinkInstance> revitLinks = RevitLinks.Where(x => settingData.SelectedLinks.Contains(x.GetLinkDocument().Title)).ToList();

                View view3D = null;
                List<ElementId> ids = new List<ElementId>();
                List<FamilyInstanceCreationData> datas = new List<FamilyInstanceCreationData>();

                FrmProcessbar progressBar = new FrmProcessbar("Creating Families", Define.MessageFinish, DiritIconTool.Mep);
                Process process = Process.GetCurrentProcess();

                if (TryGetFamilySymbol(settingData, out FamilySymbol symbol))
                {
                    if (TryGetLocations(settingData, linkCADData, out List<XYZ> locations))
                    {
                        if (settingData.ElevationType != ElevationType.ManualElevation)
                        {
                            view3D = CreateIsolateView3D(settingData, level, locations, out ids);
                            if (view3D == null)
                            {
                                IO.ShowWarning("Cannot create a 3D view to scan ceilings/floors.");
                                return;
                            }
                        }

                        IntPtr intPtr = process.MainWindowHandle;
                        progressBar.prgSingle.Minimum = 1;
                        progressBar.prgSingle.Maximum = locations.Count;
                        progressBar.prgSingle.Value = 1;
                        WindowInteropHelper helper = new WindowInteropHelper(progressBar);
                        helper.Owner = intPtr;
                        progressBar.Show();

                        int nCount = 0;

                        foreach (XYZ location in locations)
                        {
                            List<FamilyInstanceCreationData> newDatas = new List<FamilyInstanceCreationData>();

                            if (TryGetFamilyElevation(settingData, location, level, revitLinks, view3D, out double elevation, out PlanarFace face))
                            {
                                {
                                    XYZ insLocation = new XYZ(location.X, location.Y, elevation);

                                    if (settingData.ElevationType == ElevationType.ByFace)
                                    {
                                        var refDirection = GetReferenceDirection(face, location);
                                        if (refDirection != null)
                                        {
                                            FamilyInstanceCreationData data = new FamilyInstanceCreationData(face, location, refDirection, symbol);
                                            datas.Add(data);
                                            newDatas.Add(data);
                                        }
                                    }
                                    else
                                    {
                                        FamilyInstanceCreationData data = new FamilyInstanceCreationData(insLocation, symbol, level, StructuralType.Column);
                                        datas.Add(data);
                                        newDatas.Add(data);
                                    }
                                }
                            }

                            FailureHandlingOptions options = trans.GetFailureHandlingOptions();
                            IgnoreWarning preproccessor = new IgnoreWarning();
                            options.SetFailuresPreprocessor(preproccessor);
                            trans.SetFailureHandlingOptions(options);

                            // need to pick face in TryGetFamilyElevation so transaction must be start after that
                            trans.Start();

                            var newIds = _doc.Create.NewFamilyInstances2(newDatas);
                            insCount = newIds.Count;

                            trans.Commit();

                            nCount++;
                            string dPercent = nCount.ToString() + "/" + linkCADData.Blocks.Count.ToString();
                            progressBar.tbxMessage.Text = dPercent.ToString();
                            progressBar.IncrementProgressBar();

                            if (progressBar.IsCancel)
                                break;
                        }
                    }

                    // delete view3D when done process
                    if (view3D != null)
                    {
                        trans.Start();
                        _doc.Delete(view3D.Id);
                        _doc.Delete(ids);
                        trans.Commit();
                    }

                    if (insCount > 0)
                    {
                        string mess = Common.GetTextLanguage("FamilyCreationSuccessfully");
                        IO.ShowInfo(mess);
                    }

                    progressBar.Dispose();
                }
            }
        }

        public void LinkCadDisplayControl(LinkCADData linkCADData, bool isDisplay)
        {
            using (Transaction trans = new Transaction(_doc, "ShowHide"))
            {
                trans.Start();
                var lineIDs = linkCADData.Blocks.SelectMany(x => x.LineIds).ToList();
                if (lineIDs.Count > 0)
                {
                    if (isDisplay)
                    {
                        _uiDoc.ActiveGraphicalView.UnhideElements(lineIDs);
                        linkCADData.IsDisplayInRevit = true;
                    }
                    else
                    {
                        _uiDoc.ActiveGraphicalView.HideElements(lineIDs);
                        linkCADData.IsDisplayInRevit = false;
                    }
                }

                trans.Commit();
            }
        }

        private bool TryGetFamilyElevation(CreateFamilyFromCADSettingData settingData,
                                           XYZ location,
                                           Level level,
                                           List<RevitLinkInstance> revitLinks,
                                           View view3D,
                                           out double elevation,
                                           out PlanarFace face)

        {
            elevation = 0;
            face = null;

            switch (settingData.ElevationType)
            {
                case ElevationType.ByFloorElevation:
                    try
                    {
                        ObjectType objType = settingData.IsProjectElement ? ObjectType.Element : ObjectType.LinkedElement;

                        Element elem = FindNearestElement(_doc, revitLinks, view3D as View3D, BuiltInCategory.OST_Floors, location);

                        if (elem == null)
                            return false;

                        var bottomFace = GetBottomFace(elem);
                        if (bottomFace == null)
                            return false;

                        elevation = bottomFace.Origin.Z - level.Elevation;
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                    break;

                case ElevationType.ManualElevation:
                    elevation = double.Parse(settingData.ManualElevation) / 304.8;
                    break;

                case ElevationType.ByFace:
                case ElevationType.ByCeilingElevation:
                    try
                    {
                        ObjectType objType = settingData.IsProjectElement ? ObjectType.Element : ObjectType.LinkedElement;
                        Element elem = FindNearestElement(_doc, revitLinks, view3D as View3D, BuiltInCategory.OST_Ceilings, location);

                        if (elem == null)
                            return false;

                        face = GetBottomFace(elem);
                        if (face == null)
                            return false;

                        elevation = face.Origin.Z - level.Elevation;
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                    break;
            }

            return true;
        }

        private bool TryGetFamilySymbol(CreateFamilyFromCADSettingData settingData, out FamilySymbol symbol)
        {
            symbol = null;

            var family = Families.FirstOrDefault(x => x.Name == settingData.Family);
            if (family != null)
            {
                symbol = family.GetFamilySymbolIds()
                               .Select(x => _doc.GetElement(x))
                               .FirstOrDefault(x => x.Name == settingData.FamilyType) as FamilySymbol;

                if (symbol != null)
                {
                    if (symbol.Family.FamilyPlacementType == FamilyPlacementType.WorkPlaneBased && settingData.ElevationType != ElevationType.ByFace)
                    {
                        IO.ShowWarning("The selected family is placed by face. Please change the settings.");
                        return false;
                    }

                    return true;
                }
            }

            return false;
        }

        private bool TryGetLocations(CreateFamilyFromCADSettingData settingData, LinkCADData linkCADData, out List<XYZ> location)
        {
            if (settingData.IsCreateBlockInOrigin)
            {
                location = linkCADData.Blocks.Where(x => x.Name == settingData.BlockName)
                                             .Select(x => x.Location)
                                             .ToList();
            }
            else
            {
                location = linkCADData.PickedBlocks.Where(x => x.Name == settingData.BlockName)
                                                   .Select(x => x.Location)
                                                   .ToList();
            }

            return true;
        }

        private void EnableCategoryLineOnView()
        {
            Category cat = _doc.Settings.Categories.get_Item(BuiltInCategory.OST_Lines);
            if (cat != null)
            {
                View view;

                if (_uiDoc.ActiveGraphicalView.ViewTemplateId != ElementId.InvalidElementId)
                    view = _doc.GetElement(_uiDoc.ActiveGraphicalView.ViewTemplateId) as View;
                else
                    view = _uiDoc.ActiveView;

                if (view.GetCategoryHidden(cat.Id))
                    view.SetCategoryHidden(cat.Id, false);
            }
        }

        private void ExtractBlockLines(LinkCADData linkCADData, out int blockErrorCount)
        {
            blockErrorCount = 0;

            var cadTransf = linkCADData.LinkCAD.GetTotalTransform();

            Options options = new Options() { View = _uiDoc.ActiveView };
            GeometryElement geoEle = linkCADData.LinkCAD.get_Geometry(options);

            if (geoEle.FirstOrDefault() is GeometryInstance geoIns)
            {
                foreach (GeometryObject item in geoIns.GetSymbolGeometry())
                {
                    if (item is GeometryInstance subGeoIns)
                    {
                        List<ElementId> lstBlockLineId = new List<ElementId>();

                        var totalTransf = cadTransf * subGeoIns.Transform;
                        if (subGeoIns.SymbolGeometry.Any(x => x is GeometryInstance))
                        {
                            var subSubGeoIns = subGeoIns.SymbolGeometry.FirstOrDefault(x => x is GeometryInstance) as GeometryInstance;
                            foreach (var subSymGeo in subSubGeoIns.SymbolGeometry)
                            {
                                try
                                {
                                    ExtractSymbolGeo(subSymGeo, lstBlockLineId, totalTransf * subSubGeoIns.Transform);
                                }
                                catch (Exception)
                                {
                                    blockErrorCount++;
                                }
                            }

                            if (lstBlockLineId.Count > 0)
                            {
                                BlockData newBlock = new BlockData(subSubGeoIns.GetNameSymbol(_doc), lstBlockLineId, totalTransf.OfPoint(subSubGeoIns.Transform.Origin));
                                linkCADData.Blocks.Add(newBlock);
                            }
                        }
                        else
                        {
                            foreach (var symGeo in subGeoIns.SymbolGeometry)
                            {
                                try
                                {
                                    ExtractSymbolGeo(symGeo, lstBlockLineId, totalTransf);
                                }
                                catch (Exception)
                                {
                                    blockErrorCount++;
                                }
                            }
                            if (lstBlockLineId.Count > 0)
                            {
                                BlockData newBlock = new BlockData(subGeoIns.GetNameSymbol(_doc), lstBlockLineId, cadTransf.OfPoint(subGeoIns.Transform.Origin));
                                linkCADData.Blocks.Add(newBlock);
                            }
                        }
                    }
                }
            }
        }

        private void ExtractBlockLinesBaseP(LinkCADData linkCADData, out int blockErrorCount)
        {
            blockErrorCount = 0;

            var cadTransf = linkCADData.LinkCAD.GetTotalTransform();

            Options options = new Options() { View = _uiDoc.ActiveView };
            GeometryElement geoEle = linkCADData.LinkCAD.get_Geometry(options);

            if (geoEle.FirstOrDefault() is GeometryInstance geoIns)
            {
                foreach (GeometryObject item in geoIns.GetSymbolGeometry())
                {
                    if (item is GeometryInstance subGeoIns)
                    {
                        List<ElementId> lstBlockLineId = new List<ElementId>();

                        var totalTransf = cadTransf * subGeoIns.Transform;
                        if (subGeoIns.SymbolGeometry.Any(x => x is GeometryInstance))
                        {
                            var subSubGeoIns = subGeoIns.SymbolGeometry.FirstOrDefault(x => x is GeometryInstance) as GeometryInstance;

                            BlockData newBlock = new BlockData(subSubGeoIns.GetNameSymbol(_doc), lstBlockLineId, totalTransf.OfPoint(subSubGeoIns.Transform.Origin));
                            linkCADData.Blocks.Add(newBlock);
                        }
                        else
                        {
                            BlockData newBlock = new BlockData(subGeoIns.GetNameSymbol(_doc), lstBlockLineId, cadTransf.OfPoint(subGeoIns.Transform.Origin));
                            linkCADData.Blocks.Add(newBlock);
                        }
                    }
                }
            }
        }

        private void ExtractSymbolGeo(GeometryObject symGeo, List<ElementId> lstBlockLineId, Transform totalTransf)
        {
            if (symGeo is Curve curve)
            {
                if (curve is HermiteSpline)
                {
                }
                else
                {
                    try
                    {
                        curve = curve.CreateTransformed(totalTransf);
                        var newCurve = _doc.Create.NewDetailCurve(_doc.ActiveView, curve);
                        lstBlockLineId.Add(newCurve.Id);
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            else if (symGeo is PolyLine plLine)
            {
                for (int i = 0; i < plLine.NumberOfCoordinates - 1; i++)
                {
                    if (!plLine.GetCoordinate(i).IsAlmostEqualTo(plLine.GetCoordinate(i + 1), 10e-3))
                    {
                        try
                        {
                            Line line = Line.CreateBound(plLine.GetCoordinate(i), plLine.GetCoordinate(i + 1));
                            line = line.CreateTransformed(totalTransf) as Line;

                            var newCurve = _doc.Create.NewDetailCurve(_doc.ActiveView, line);
                            lstBlockLineId.Add(newCurve.Id);
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
            }
        }

        private List<Level> GetAllLevel()
        {
            return new FilteredElementCollector(_doc).WhereElementIsNotElementType()
                                                     .OfClass(typeof(Level))
                                                     .Cast<Level>()
                                                     .OrderBy(x => x.Elevation)
                                                     .ToList();
        }

        private List<Family> GetFamilies()
        {
            var cateogryIds = RevitUtils.GetMEPCategories();

            return new FilteredElementCollector(_doc).WhereElementIsNotElementType()
                                                     .OfClass(typeof(Family))
                                                     .Cast<Family>()
                                                     .Where(x => x.FamilyCategory != null && cateogryIds.Contains(x.FamilyCategory.Id.ToInt()))
                                                     .Where(x => x.FamilyPlacementType != FamilyPlacementType.CurveBased &&
                                                                 x.FamilyPlacementType != FamilyPlacementType.CurveBasedDetail &&
                                                                 x.FamilyPlacementType != FamilyPlacementType.CurveDrivenStructural &&
                                                                 x.FamilyPlacementType != FamilyPlacementType.TwoLevelsBased)
                                                     .ToList();
        }

        private Dictionary<string, int> GetCategoryMappingName()
        {
            return new Dictionary<string, int>()
            {
               { "Air Terminal", (int)BuiltInCategory.OST_DuctTerminal },
               { "Cable Tray Fitting", (int)BuiltInCategory.OST_CableTrayFitting },
               { "Communication Device", (int)BuiltInCategory.OST_CommunicationDevices },
               { "Conduit Fittings", (int)BuiltInCategory.OST_ConduitFitting },
               { "Data Device", (int)BuiltInCategory.OST_DataDevices },
               { "Detail Item", (int)BuiltInCategory.OST_DetailComponents },
               { "Duct Fittings", (int)BuiltInCategory.OST_DuctFitting },
               { "Electrical Equipments", (int)BuiltInCategory.OST_ElectricalEquipment },
               { "Electrical Fixtures", (int)BuiltInCategory.OST_ElectricalFixtures },
               { "Fire Alarm Devices", (int)BuiltInCategory.OST_FireAlarmDevices },
               { "Generic Model", (int)BuiltInCategory.OST_GenericModel },
               { "Lighting Devices", (int)BuiltInCategory.OST_LightingDevices },
               { "Lighting Fixtures", (int)BuiltInCategory.OST_LightingFixtures },
               { "Mechanical Equipments", (int)BuiltInCategory.OST_MechanicalEquipment },
               { "Nurse Call Devices", (int)BuiltInCategory.OST_NurseCallDevices },
               { "Pipe Accessories", (int)BuiltInCategory.OST_PipeAccessory },
               { "Pipe Fittings", (int)BuiltInCategory.OST_PipeFitting },
               { "Plumbing Fixtures", (int)BuiltInCategory.OST_PlumbingFixtures },
               { "Security Devices", (int)BuiltInCategory.OST_SecurityDevices },
               { "Sprinklers", (int)BuiltInCategory.OST_Sprinklers },
               { "Telephone Devices", (int)BuiltInCategory.OST_TelephoneDevices }
            };
        }

        private List<LinkCADData> GetAllLinkCAD()
        {
            return new FilteredElementCollector(_doc).WhereElementIsNotElementType()
                                                     .OfClass(typeof(ImportInstance))
                                                     .Cast<ImportInstance>()
                                                     .Where(x => x.Category != null && x.Category.Name.EndsWith("dwg", StringComparison.CurrentCultureIgnoreCase) && x.IsLinked)
                                                     .Select(x => new LinkCADData(x))
                                                     .ToList();
        }

        private List<RevitLinkInstance> GetAllRevitLink()
        {
            return new FilteredElementCollector(_doc).OfClass(typeof(RevitLinkInstance))
                                                     .Cast<RevitLinkInstance>()
                                                     .Where(x => x.GetLinkDocument() != null)
                                                     .ToList();
        }

        private PlanarFace GetBottomFace(Element ceilingAndFloor)
        {
            var solids = GeometryUtils.GetAllSolids(_doc, ceilingAndFloor, true, null);
            foreach (var solid in solids)
            {
                var face = solid.Faces.Cast<Face>().FirstOrDefault(x => x is PlanarFace plFace && plFace.FaceNormal.IsAlmostEqualTo(XYZ.BasisZ.Negate())) as PlanarFace;
                if (face != null)
                    return face;
            }

            return null;
        }

        private XYZ GetReferenceDirection(Face face, XYZ location)
        {
            if (face.Project(location) != null)
            {
                UV uv = face.Project(location).UVPoint;
                if (uv != null)
                {
                    XYZ tangentU = face.ComputeDerivatives(uv).BasisX.Normalize();
                    return tangentU;
                }
            }

            return null;
        }

        private View3D CreateIsolateView3D(CreateFamilyFromCADSettingData settingData, Level level, List<XYZ> locations, out List<ElementId> IdsCopy)
        {
            IdsCopy = new List<ElementId>();
            List<RevitLinkInstance> lstRevitLinkIns = RevitLinks.Where(x => settingData.SelectedLinks.Contains(x.GetLinkDocument().Title)).ToList();
            BuiltInCategory category = settingData.ElevationType == ElevationType.ByFloorElevation ? BuiltInCategory.OST_Floors : BuiltInCategory.OST_Ceilings;

            if (!settingData.IsProjectElement)
                IdsCopy = CopyElement(lstRevitLinkIns, locations, level, _doc, category);

            return CreateView3D(_doc, category, IdsCopy);
        }

        private List<ElementId> CopyElement(List<RevitLinkInstance> lstRevitLinkIns,
                                            List<XYZ> locations,
                                            Level level,
                                            Document doc,
                                            BuiltInCategory category)
        {
            List<ElementId> retVal = new List<ElementId>();
            using (Transaction trans = new Transaction(doc, "Copy Element Form Link"))
            {
                var failureOption = trans.GetFailureHandlingOptions();
                IgnoreWarning ignoreWarning = new IgnoreWarning();
                failureOption.SetFailuresPreprocessor(ignoreWarning);
                trans.SetFailureHandlingOptions(failureOption);

                trans.Start();

                ElementFilter elemFilter = new ElementCategoryFilter(new ElementId((int)category));
                CopyPasteOptions copyOptions = new CopyPasteOptions();
                copyOptions.SetDuplicateTypeNamesHandler(new CopyUseDestination());

                foreach (var revitLinkIns in lstRevitLinkIns)
                {
                    Document linkDoc = revitLinkIns.GetLinkDocument();

                    Outline outline = GetBoxLimit(locations, level, revitLinkIns.GetTotalTransform());
                    BoundingBoxIntersectsFilter filter = new BoundingBoxIntersectsFilter(outline);

                    var elemToCopy = new FilteredElementCollector(linkDoc).WhereElementIsNotElementType()
                                                                          .WherePasses(elemFilter)
                                                                          .WherePasses(filter)
                                                                          .ToElementIds();

                    if (elemToCopy.Count > 0)
                    {
                        var copyIds = ElementTransformUtils.CopyElements(linkDoc, elemToCopy, doc, revitLinkIns.GetTotalTransform(), copyOptions);
                        retVal.AddRange(copyIds);
                    }
                }

                trans.Commit();
            }
            return retVal;
        }

        private View3D CreateView3D(Document doc, BuiltInCategory category, List<ElementId> copyIDs)
        {
            View3D view3D = null;
            ViewFamilyType viewFamType = new FilteredElementCollector(doc).WhereElementIsElementType()
                                                                          .OfClass(typeof(ViewFamilyType))
                                                                          .Cast<ViewFamilyType>()
                                                                          .FirstOrDefault(x => x.ViewFamily == ViewFamily.ThreeDimensional);
            if (viewFamType != null)
            {
                using (Transaction trans = new Transaction(doc, "CreateNewDefaultView3D"))
                {
                    trans.Start();
                    view3D = View3D.CreateIsometric(doc, viewFamType.Id);
                    view3D.AreAnalyticalModelCategoriesHidden = true;

                    if (copyIDs.Count > 0)
                        view3D.IsolateElementsTemporary(copyIDs);
                    else
                        view3D.IsolateCategoryTemporary(new ElementId((int)category));

                    trans.Commit();
                }
            }

            return view3D;
        }

        private Element FindNearestElement(Document doc,
                                           List<RevitLinkInstance> revitLinks,
                                           View3D view3D,
                                           BuiltInCategory category,
                                           XYZ location)
        {
            List<DocumentData> revitLinkDocs = revitLinks.Select(x => new DocumentData(x, x.GetLinkDocument())).ToList();
            ElementFilter elemFilter = new ElementCategoryFilter(new ElementId((int)category));

            ReferenceIntersector refIntersector = new ReferenceIntersector(elemFilter, FindReferenceTarget.Face, view3D);
            if (refIntersector == null)
                return null;

            ReferenceWithContext referenceWithContext = refIntersector.FindNearest(location, XYZ.BasisZ);
            if (referenceWithContext == null)
                return null;

            Reference elementRef = referenceWithContext.GetReference();
            Element element = doc.GetElement(elementRef);

            if (element != null &&
                element is RevitLinkInstance)
            {
                foreach (var linkDoc in revitLinkDocs)
                {
                    element = linkDoc.Document.GetElement(elementRef.LinkedElementId);
                    if (element != null)
                        break;
                }
            }

            return element;
        }

        private Outline GetBoxLimit(List<XYZ> points, Level level, Transform transform)
        {
            var maxX = points.Select(x => x.X).Max();
            var maxY = points.Select(x => x.Y).Max();
            var maxZ = 1000;

            var minX = points.Select(x => x.X).Min();
            var minY = points.Select(x => x.Y).Min();
            var minZ = level.Elevation + 1 / 304.8;

            var max = new XYZ(maxX, maxY, maxZ);
            var min = new XYZ(minX, minY, minZ);

            max = transform.Inverse.OfPoint(max);
            min = transform.Inverse.OfPoint(min);

            return new Outline(min, max);
        }
    }
}
