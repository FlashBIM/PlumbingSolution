using System;
using System.IO;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;
using PlumbingSolution.Commands.Plumbing;
using PlumbingSolution.FireProtection.Command.Drain;
using PlumbingSolution.FireProtection.Command.Fire;
using PlumbingSolution.FireProtection.Command.General;
using PlumbingSolution.FireProtection.Command.Fire.DiritCAD.Service_J;
using PlumbingSolution.FireProtection.Command.Fire.DiritConnectSprayHead.Service_E;
using PlumbingSolution.FireProtection.Command.Mep.MainPipe;
using PlumbingSolution.FireProtection.Command.Modify;

namespace PlumbingSolution
{
    // Ribbon PlumbingSolution theo sheet Ribbon: Fire Protection / Pipe Connect / Auto Route / Pipe Fittings / QUICK.
    // Mọi nút dùng chung icon PlumbingSolution_32x32 (nút lớn) và PlumbingSolution_16x16 (nút xếp chồng).
    // Bật / tắt theo license (RevitUtils) duyệt mọi panel của tab nên áp luôn cho các panel này.
    public partial class App
    {
        private const string PlumbingIconLarge = "PlumbingSolution_32x32.png";
        private const string PlumbingIconSmall = "PlumbingSolution_16x16.png";

        private void CreatePlumbingPanel(UIControlledApplication app,
                                         string tabName,
                                         string assemblyPath,
                                         string iconFolder)
        {
            Autodesk.Revit.UI.RibbonPanel panel0 = app.CreateRibbonPanel(tabName, "Fire Protection");
            // Nút xổ "Pendent Sprinkler" = panel "Kết nối đầu phun" của Dirit (ghép trong FireProtection/).
            PulldownButtonData sprinklerData = new PulldownButtonData("PendentSprinkler", "Pendent\nSprinkler");
            sprinklerData.ToolTip = "Pendent Sprinkler";
            sprinklerData.LargeImage = LoadIcon(iconFolder, PlumbingIconLarge);
            sprinklerData.Image = LoadIcon(iconFolder, PlumbingIconSmall);
            PulldownButton sprinkler = (PulldownButton)panel0.AddItem(sprinklerData);
            sprinkler.AddPushButton(NewPlumbingButton("UprightSprinkler", "Upright Sprinkler", typeof(ConnectSprinklerCommand), assemblyPath, iconFolder));
            sprinkler.AddPushButton(NewPlumbingButton("PendentSprinklerDown", "Pendent Sprinkler", typeof(SprinklerDownCommand), assemblyPath, iconFolder));
            sprinkler.AddPushButton(NewPlumbingButton("FlexSprinkler", "Flex Sprinkler", typeof(FlexSprinklerCommand), assemblyPath, iconFolder));
            sprinkler.AddPushButton(NewPlumbingButton("TwinSprinkler", "Twin Sprinkler", typeof(CmdTwinSprinkler), assemblyPath, iconFolder));
            sprinkler.AddPushButton(NewPlumbingButton("DeleteConnection", "Delete Connection", typeof(CmdDeleteSprinker), assemblyPath, iconFolder));
            // Nút xổ "Place Sprinkler" = "Family Tự động" / "Family theo block" (panel CAD của Dirit).
            PulldownButtonData placeData = new PulldownButtonData("PlaceSprinkler", "Place\nSprinkler");
            placeData.ToolTip = "Place Sprinkler";
            placeData.LargeImage = LoadIcon(iconFolder, PlumbingIconLarge);
            placeData.Image = LoadIcon(iconFolder, PlumbingIconSmall);
            PulldownButton place = (PulldownButton)panel0.AddItem(placeData);
            place.AddPushButton(NewPlumbingButton("PlaceSprinklerAuto", "Place Sprinkler", typeof(CmdCreateFmlFromBasePoint), assemblyPath, iconFolder));
            place.AddPushButton(NewPlumbingButton("SelectBlock", "Select Block", typeof(CmdCreateFmlFromBlockCad), assemblyPath, iconFolder));
            panel0.AddItem(NewPlumbingButton("HosereelConnect", "Hosereel\nConnect", typeof(CmdHosereelConnect), assemblyPath, iconFolder));
            // Create Branch = "Ống nhánh FP" của Dirit.
            panel0.AddItem(NewPlumbingButton("CreateBranch", "Create\nBranch", typeof(CmdCreateBranchPipeFire), assemblyPath, iconFolder));

            Autodesk.Revit.UI.RibbonPanel panel1 = app.CreateRibbonPanel(tabName, "Pipe Connect");
            // Vertical Pipe = tool "Vertical MEP" của Quick MEP.
            panel1.AddItem(NewPlumbingButton("VerticalPipe", "Vertical\nPipe", typeof(CmdPlaceVerticalMep), assemblyPath, iconFolder));
            // Connect Pipe = "Nối ngang Tê" + "Nối ngang Elbow" của Dirit (sheet Pipe Connect).
            PulldownButton connect = NewPlumbingPulldown(panel1, "ConnectPipe", "Connect\nPipe", iconFolder);
            connect.AddPushButton(NewPlumbingButton("BranchElbow45", "Branch - Elbow 45", typeof(CmdConnect1T1E45), assemblyPath, iconFolder));
            connect.AddPushButton(NewPlumbingButton("BranchElbow90", "Branch - Elbow 90", typeof(CmdConnect1T1E90), assemblyPath, iconFolder));
            connect.AddPushButton(NewPlumbingButton("BranchSameElevation", "Branch - Same Elevation", typeof(CmdConnectBranchSameElevation), assemblyPath, iconFolder));
            connect.AddSeparator();
            connect.AddPushButton(NewPlumbingButton("ElbowElbow45", "Elbow - Elbow 45", typeof(CmdConnectE45), assemblyPath, iconFolder));
            connect.AddPushButton(NewPlumbingButton("ElbowElbow90", "Elbow - Elbow 90", typeof(CmdConnectE90), assemblyPath, iconFolder));
            connect.AddPushButton(NewPlumbingButton("ElbowSameElevation", "Elbow - Same Elevation", typeof(CmdConnectElbowTypeSameElevation), assemblyPath, iconFolder));
            // Parallel Pipe = "Nối song song" của Dirit.
            PulldownButton parallel = NewPlumbingPulldown(panel1, "ParallelPipe", "Parallel\nPipe", iconFolder);
            parallel.AddPushButton(NewPlumbingButton("ParallelElbow45", "Parallel - Elbow 45", typeof(CmdConnectParallel45Deg), assemblyPath, iconFolder));
            parallel.AddPushButton(NewPlumbingButton("ParallelElbow90", "Parallel - Elbow 90", typeof(CmdConnectParallel90Deg), assemblyPath, iconFolder));
            parallel.AddPushButton(NewPlumbingButton("ParallelSameHorizontal", "Parallel - Same - Horizontal", typeof(CmdConnectParallelSameElevation), assemblyPath, iconFolder));
            parallel.AddPushButton(NewPlumbingButton("ParallelSameVertical", "Parallel - Same - Vertical", typeof(CmdConnectParallel), assemblyPath, iconFolder));
            panel1.AddItem(NewPlumbingButton("ChangeConnect", "Change\nConnect", typeof(CmdChangeConnect), assemblyPath, iconFolder));
            panel1.AddStackedItems(
                NewPlumbingButton("BranchAlign", "Branch Align", typeof(CmdBranchAlign), assemblyPath, iconFolder),
                NewPlumbingButton("ElbowAlign", "Elbow Align", typeof(CmdElbowAlign), assemblyPath, iconFolder),
                NewPlumbingButton("TrimStraight", "Trim Straight", typeof(CmdTrimStraight), assemblyPath, iconFolder));

            Autodesk.Revit.UI.RibbonPanel panel2 = app.CreateRibbonPanel(tabName, "Auto Route");
            panel2.AddItem(NewPlumbingButton("AutoChiller", "Auto\nChiller", typeof(CmdAutoChiller), assemblyPath, iconFolder));
            panel2.AddItem(NewPlumbingButton("AutoWS", "Auto\nWS", typeof(CmdAutoWS), assemblyPath, iconFolder));
            panel2.AddItem(NewPlumbingButton("PipeRoute", "Pipe\nRoute", typeof(CmdPipeRoute), assemblyPath, iconFolder));

            Autodesk.Revit.UI.RibbonPanel panel3 = app.CreateRibbonPanel(tabName, "Pipe Fittings");
            panel3.AddItem(NewPlumbingButton("ConnectPump", "Connect\nPump", typeof(CmdConnectPump), assemblyPath, iconFolder));
            panel3.AddItem(NewPlumbingButton("PGConnect", "PG\nConnect", typeof(CmdPGConnect), assemblyPath, iconFolder));
            panel3.AddItem(NewPlumbingButton("ValveAssembly", "Valve\nAssembly", typeof(CmdValveAssembly), assemblyPath, iconFolder));
            panel3.AddItem(NewPlumbingButton("PlaceValve", "Place\nValve", typeof(CmdPlaceValve), assemblyPath, iconFolder));
            panel3.AddItem(NewPlumbingButton("CreateFlange", "Create\nFlange", typeof(CmdCreateFlange), assemblyPath, iconFolder));
            panel3.AddItem(NewPlumbingButton("CreateCoupling", "Create\nCoupling", typeof(CmdCreateCoupling), assemblyPath, iconFolder));
            panel3.AddItem(NewPlumbingButton("CreateTee", "Create\nTee", typeof(CmdCreateTee), assemblyPath, iconFolder));

            Autodesk.Revit.UI.RibbonPanel panel4 = app.CreateRibbonPanel(tabName, "QUICK");
            panel4.AddItem(NewPlumbingButton("PipeUpdown", "Pipe\nUpdown", typeof(CmdPipeUpdown), assemblyPath, iconFolder));
            panel4.AddItem(NewPlumbingButton("DeleteFitting", "Delete\nFitting", typeof(CmdDeleteFitting), assemblyPath, iconFolder));
            panel4.AddStackedItems(
                NewPlumbingButton("CreatePipe", "Create Pipe", typeof(CmdCreatePipe), assemblyPath, iconFolder),
                NewPlumbingButton("QuickPipe", "Quick Pipe", typeof(CmdQuickPipe), assemblyPath, iconFolder),
                NewPlumbingButton("ConnectFittings", "Connect Fittings", typeof(CmdConnectFittings), assemblyPath, iconFolder));
            panel4.AddItem(NewPlumbingButton("HMEPDistance", "H.MEP\nDistance", typeof(CmdHMEPDistance), assemblyPath, iconFolder));
        }

        private PushButtonData NewPlumbingButton(string name, string text, Type commandType,
                                                 string assemblyPath, string iconFolder)
        {
            PushButtonData data = new PushButtonData(name, text, assemblyPath, commandType.FullName);
            data.ToolTip = text.Replace("\n", " ");
            AddImages(data, iconFolder, PlumbingIconLarge, PlumbingIconSmall);
            return data;
        }

        private PulldownButton NewPlumbingPulldown(Autodesk.Revit.UI.RibbonPanel panel, string name, string text, string iconFolder)
        {
            PulldownButtonData data = new PulldownButtonData(name, text);
            data.ToolTip = text.Replace("\n", " ");
            data.LargeImage = LoadIcon(iconFolder, PlumbingIconLarge);
            data.Image = LoadIcon(iconFolder, PlumbingIconSmall);
            return (PulldownButton)panel.AddItem(data);
        }

        private static BitmapImage LoadIcon(string iconFolder, string fileName)
        {
            string path = Path.Combine(iconFolder ?? string.Empty, fileName);
            return File.Exists(path) ? new BitmapImage(new Uri(path)) : null;
        }
    }
}
