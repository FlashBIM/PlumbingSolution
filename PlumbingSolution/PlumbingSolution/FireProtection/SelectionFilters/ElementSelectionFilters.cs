using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using PlumbingSolution.FireProtection.Ultis;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;
using PlumbingSolution.FireProtection.Extensions;
using PlumbingSolution.FireProtection.Utils;

namespace PlumbingSolution.FireProtection.SelectionFilters
{

    // Bộ lọc nâng cấp: Chỉ cho phép chọn FamilyInstance thuộc category Sprinklers
    public class SprinklerSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            // 1. Kiểm tra xem đối tượng có phải là FamilyInstance không
            if (elem is FamilyInstance)
            {
                // 2. Kiểm tra Category của đối tượng có phải là OST_Sprinklers không
                if (elem.Category != null &&
                    elem.Category.Id.ToInt() == (int)BuiltInCategory.OST_Sprinklers)
                {
                    return true; // Hợp lệ, cho phép pick
                }
            }
            return false; // Từ chối tất cả các đối tượng khác
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }

    public class PipeSelectionFilter : ISelectionFilter
    {
        private ElementId _pipeTypeId;

        public PipeSelectionFilter(ElementId pipeTypeId = null)
        {
            _pipeTypeId = pipeTypeId;
        }

        public bool AllowElement(Element elem)
        {
            if (elem is Pipe pipe)
            {
                if (_pipeTypeId == null || _pipeTypeId != null && pipe.PipeType.Id == _pipeTypeId)
                    return true;
                else
                    return false;
            }
            else
                return false;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return true;
        }
    }
}
