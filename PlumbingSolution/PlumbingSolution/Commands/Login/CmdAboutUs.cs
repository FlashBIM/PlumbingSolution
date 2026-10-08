using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using PlumbingSolution.LoginLicense.Gate;
using PlumbingSolution.UI.BeginUI;
using System;
using System.Collections.Generic;
using System.Text;

namespace PlumbingSolution.Commands.Login
{
    [Transaction(TransactionMode.Manual)]
    public class CmdAboutUs : IExternalCommand
    {
        private UIDocument _uiDoc;
        private Document _doc;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            if (!LicenseGate.IsToolAllowed(this))
                return Result.Cancelled;

            UIApplication uiapp = commandData.Application;
            _uiDoc = uiapp.ActiveUIDocument;
            _doc = _uiDoc.Document;
#if RELEASE2019_VN || RELEASE2020_VN || RELEASE2021_VN || RELEASE2022_VN || RELEASE2023_VN || RELEASE2024_VN
            AboutUsVNForm UI_AboutUs = new AboutUsVNForm();
#else
            AboutUsForm UI_AboutUs = new AboutUsForm();
#endif
            UI_AboutUs.ShowDialog();

            return Result.Succeeded;
        }
    }
}