using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlumbingSolution.FireProtection.ItemData
{
    public class BlockData
    {
        public string Name { get; set; }
        public List<ElementId> LineIds { get; set; }
        public Group RevitGroup { get; set; }
        public XYZ Location { get; set; }
        public XYZ BlockOrigin { get; set; }
        public bool IsBlockInOrigin { get; set; }

        public BlockData(string name, List<ElementId> lineIds, XYZ blockOrigin)
        {
            Name = name;
            LineIds = lineIds;
            BlockOrigin = blockOrigin;
        }

        public BlockData(string name, Group group)
        {
            Name = name;
            Location = (group.Location as LocationPoint).Point;
        }

        public void UpdateListLine(Document doc)
        {
            List<ElementId> newLineIds = new List<ElementId>();
            foreach (ElementId id in LineIds)
            {
                if (doc.GetElement(id) != null && doc.GetElement(id).IsValidObject)
                    newLineIds.Add(id);
            }

            LineIds = newLineIds;
        }
    }
}
