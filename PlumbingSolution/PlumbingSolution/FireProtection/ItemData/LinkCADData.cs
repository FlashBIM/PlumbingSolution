using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlumbingSolution.FireProtection.ItemData
{
    public class LinkCADData
    {
        public string Name { get; set; }
        public ImportInstance LinkCAD { get; set; }
        public List<BlockData> Blocks { get; set; }
        public bool IsExplodedLine { get; set; }
        public bool IsExplodedGroup => Blocks != null && Blocks.All(x => x.RevitGroup != null);
        public bool IsDisplayInRevit { get; set; }

        public List<BlockData> PickedBlocks { get; set; }

        public LinkCADData(ImportInstance linkCAD)
        {
            LinkCAD = linkCAD;
            Name = linkCAD.Category.Name;
            Blocks = new List<BlockData>();
            PickedBlocks = new List<BlockData>();
        }
    }
}
