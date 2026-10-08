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
    public class CmdLogin : IExternalCommand
    {
        private UIDocument _uiDoc;
        private Document _doc;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            _uiDoc = uiapp.ActiveUIDocument;
            _doc = _uiDoc.Document;

            if (!LicenseGate.HasValidLicense)
            {
                LoginForm loginForm = new LoginForm();
                loginForm.ShowDialog();
            }
            else
            {
                InformationForm informationForm = new InformationForm();
                informationForm.ShowDialog();
            }

            return Result.Succeeded;
        }
    }
}