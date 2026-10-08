using System;
using System.Collections.Generic;
using System.Text;

namespace PlumbingSolution.FireProtection.ItemData
{
    public class PipeSegmentRange
    {
        /// <summary>
        /// Đường kính ống, đơn vị mm.
        /// </summary>
        public double DiameterMm { get; }

        /// <summary>
        /// Số lượng sprinkler trên đoạn,
        /// bao gồm sprinkler đầu và sprinkler cuối.
        /// </summary>
        public int SprinklerCount { get; }
        public int StartIndex { get; }

        public int EndIndex { get; }

        public PipeSegmentRange(
            double diameterMm,
            int sprinklerCount,
            int startIndex,
            int endIndex)
        {
            DiameterMm = diameterMm;
            SprinklerCount = sprinklerCount;

            StartIndex = startIndex;
            EndIndex = endIndex;
        }
    }
}
