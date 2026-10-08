using PlumbingSolution.FireProtection.ItemData;
using PlumbingSolution.FireProtection.Ultis;
using PlumbingSolution.FireProtection.Extensions;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using PlumbingSolution.FireProtection.SpeedHanger.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlumbingSolution.FireProtection.Utils
{

    public class GroupBlockSelectionFilter : ISelectionFilter
    {
        public HashSet<int> BlockIds { get; set; }

        public GroupBlockSelectionFilter(List<BlockData> blocks)
        {
            BlockIds = blocks.Select(x => x.RevitGroup.Id.ToInt()).ToHashSet();
        }

        public bool AllowElement(Element elem)
        {
            if (elem is Group group)
                return BlockIds.Contains(elem.Id.ToInt());

            return false;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return true;
        }
    }

    public class GroupBlockToCreateSelectionFilter : ISelectionFilter
    {
        public List<BlockData> Blocks { get; set; }

        public GroupBlockToCreateSelectionFilter(List<BlockData> blocks, string blockName)
        {
            Blocks = blocks.Where(x => x.Name == blockName).ToList();
        }

        public bool AllowElement(Element elem)
        {
            if (elem is Group group)
                return Blocks.Any(x => x.RevitGroup.Id.ToInt() == elem.Id.ToInt());

            return false;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return true;
        }
    }

    public class LinkCADSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            if (elem is ImportInstance importIns && importIns.Category != null && importIns.Category.Name.Contains(".dwg") && importIns.IsLinked)
                return true;

            return false;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return true;
        }
    }

    public class DistinctGroupType : IEqualityComparer<Group>
    {
        public bool Equals(Group x, Group y)
        {
            return x.Id.ToInt() == y.Id.ToInt();
        }

        public int GetHashCode(Group obj)
        {
            return base.GetHashCode();
        }
    }
}
