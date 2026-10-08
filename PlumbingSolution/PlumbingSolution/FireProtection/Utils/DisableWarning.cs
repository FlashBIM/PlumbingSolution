using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Linq;

namespace PlumbingSolution.FireProtection.Ultis
{
    public class DisableWarning : IFailuresPreprocessor
    {
        public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
        {
            var messages = failuresAccessor.GetFailureMessages();
            if (messages.Count() > 0)
            {
                foreach (FailureMessageAccessor message in messages)
                {
                    //var lstId = message.GetFailingElementIds();
                    failuresAccessor.DeleteWarning(message);
                }
            }

            return FailureProcessingResult.Continue;
        }
    }
}
