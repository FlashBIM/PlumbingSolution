using System;
using Autodesk.Revit.UI;
using PlumbingSolution.Commands.Plumbing;

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
            panel0.AddItem(NewPlumbingButton("PendentSprinkler", "Pendent\nSprinkler", typeof(CmdPendentSprinkler), assemblyPath, iconFolder));
            panel0.AddItem(NewPlumbingButton("PlaceSprinkler", "Place\nSprinkler", typeof(CmdPlaceSprinkler), assemblyPath, iconFolder));
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
    }
}
