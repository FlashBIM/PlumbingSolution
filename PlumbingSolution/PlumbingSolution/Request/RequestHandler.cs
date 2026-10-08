using Autodesk.Revit.UI;
using PlumbingSolution.Ultis;

namespace PlumbingSolution.Requests
{
    /// <summary>
    /// ExternalEvent handler tối thiểu, điều phối các command modeless của PlumbingSolution.
    /// </summary>
    public sealed class RequestHandler : IExternalEventHandler
    {
        public Request Request { get; } = new Request();

        public void Execute(UIApplication application)
        {
            Global.Update(application);

            switch (Request.Take())
            {
                // case RequestId.Xxx:
                //     CmdXxx.Process(application.ActiveUIDocument);
                //     break;
                default:
                    break;
            }
        }

        public string GetName()
        {
            return "PlumbingSolution External Event";
        }
    }
}
