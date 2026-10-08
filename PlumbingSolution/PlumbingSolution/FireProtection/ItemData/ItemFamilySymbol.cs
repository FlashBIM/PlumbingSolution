using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlumbingSolution.FireProtection.ItemData
{
    internal class ItemFamilySymbol
    {
        private string _name;

        private FamilySymbol _familySymbol;

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public FamilySymbol Symbol
        {
            get { return _familySymbol; }
            set { _familySymbol = value; }
        }

        public ItemFamilySymbol(FamilySymbol symbol)
        {
            Symbol = symbol;
            Name = symbol.FamilyName + " : " + symbol.Name;
        }

        public override string ToString()
        {
            return Name.ToString();
        }
    }
}
