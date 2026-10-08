using System;
using System.IO;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;
using PlumbingSolution.Commands.Plumbing;
using PlumbingSolution.FireProtection.Command.Fire.DiritCAD.Service_J;
using PlumbingSolution.FireProtection.Command.Fire.DiritConnectSprayHead.Service_E;
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
            panel0.AddItem(NewPlumbingButton("CreateBranch", "Create\nBranch", typeof(CmdCreateBranch), assemblyPath, iconFolder));

            Autodesk.Revit.UI.RibbonPanel panel1 = app.CreateRibbonPanel(tabName, "Pipe Connect");
            panel1.AddItem(NewPlumbingButton("VerticalPipe", "Vertical\nPipe", typeof(CmdVerticalPipe), assemblyPath, iconFolder));
            panel1.AddItem(NewPlumbingButton("ConnectPipe", "Connect\nPipe", typeof(CmdConnectPipe), assemblyPath, iconFolder));
            panel1.AddItem(NewPlumbingButton("ParallelPipe", "Parallel\nPipe", typeof(CmdParallelPipe), assemblyPath, iconFolder));
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

        private static BitmapImage LoadIcon(string iconFolder, string fileName)
        {
            string path = Path.Combine(iconFolder ?? string.Empty, fileName);
            return File.Exists(path) ? new BitmapImage(new Uri(path)) : null;
        }
    }
}
