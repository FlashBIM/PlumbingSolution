using Autodesk.Revit.UI;
using PlumbingSolution.FireProtection.Command.Modify;

namespace PlumbingSolution.FireProtection.RequestForm
{
    /// <summary>
    /// ExternalEvent của các form đầu phun (ghép từ RequestHandler của Dirit, chỉ giữ các
    /// nhánh đầu phun). Ánh xạ Type2/Type3 bị đảo y như bản gốc Dirit — form gửi
    /// Type3_RUN cho radio "Phương án 2" và ngược lại.
    /// </summary>
    public class RequestHandler : IExternalEventHandler
    {
        public Request Request { get; } = new Request();

        public void Execute(UIApplication app)
        {
            switch (Request.Take())
            {
                case RequestId.SprinklerUp_Aplly:
                    CmdSprinklerUpright.Process();
                    break;

                case RequestId.SprinklerDownType1_RUN:
                    CmdSprinklerDownright.ProcessType1();
                    break;

                case RequestId.SprinklerDownType2_RUN:
                    CmdSprinklerDownright.ProcessType3();
                    break;

                case RequestId.SprinklerDownType3_RUN:
                    CmdSprinklerDownright.ProcessType2();
                    break;

                case RequestId.SprinklerDownType4_RUN:
                    CmdSprinklerDownright.ProcessType4();
                    break;

                case RequestId.SprinklerDownType5_RUN:
                    CmdSprinklerDownright.ProcessType5();
                    break;

                case RequestId.FlexSprinker_RUN:
                    CmdFlexSprinkler.Process();
                    break;

                case RequestId.TwinSprinker_RUN:
                    CmdTwinSprinkler.Process();
                    break;
            }
        }

        public string GetName()
        {
            return "PlumbingSolution Sprinkler";
        }
    }
}
