using Autodesk.Revit.DB;

namespace PlumbingSolution.FireProtection.SpeedHanger.Data
{
    public class DocumentData
    {
        public RevitLinkInstance RevitLinkInstance { get; set; }
        public Document Document { get; set; }

        public DocumentData(RevitLinkInstance ins, Document doc)
        {
            RevitLinkInstance = ins;
            Document = doc;
        }
    }
}
