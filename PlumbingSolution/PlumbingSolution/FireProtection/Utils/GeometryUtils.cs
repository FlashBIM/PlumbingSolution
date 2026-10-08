using PlumbingSolution.FireProtection.Utils;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using System;
using System.Collections.Generic;
using System.Linq;
using Document = Autodesk.Revit.DB.Document;
using View = Autodesk.Revit.DB.View;

namespace PlumbingSolution.FireProtection.Ultis
{
    public class GeometryUtils
    {

        public static List<Solid> GetAllSolids(Document doc,
                                               Element elem,
                                               bool getInsGeo,
                                               bool computeReferences = true,
                                               bool includeNonVisibleObjects = false)
        {
            Options options = new Options
            {
                ComputeReferences = computeReferences,
                IncludeNonVisibleObjects = includeNonVisibleObjects,
            };

            GeometryElement geoElem = elem.get_Geometry(options);
            List<Solid> solids = new List<Solid>();
            GetSolidFromGeometry(doc, geoElem, getInsGeo, ref solids);
            return solids;
        }

        /// <summary>
        /// get all solids of a given element
        /// </summary>
        public static List<Solid> GetAllSolids(Document doc,
                                               Element elem,
                                               bool getInsGeo,
                                               View view = null,
                                               bool computeReferences = true,
                                               bool includeNonVisibleObjects = true)
        {
            Options options = new Options
            {
                ComputeReferences = computeReferences,
                IncludeNonVisibleObjects = includeNonVisibleObjects,
                DetailLevel = ViewDetailLevel.Fine
            };
            if (view != null)
                options.View = view;

            GeometryElement geoElem = elem.get_Geometry(options);
            List<Solid> solids = new List<Solid>();
            GetSolidFromGeometry(doc, geoElem, getInsGeo, ref solids);
            return solids;
        }

        /// <summary>
        /// recursively get solid from geometry element
        /// </summary>
        public static void GetSolidFromGeometry(Document doc, GeometryElement geoElem, bool getInstGeo, ref List<Solid> solids, Autodesk.Revit.DB.View view = null)
        {
            foreach (GeometryObject geoObj in geoElem)
            {
                if (geoObj is Solid solid
                    && solid.Volume > 0
                    && IsSolidGraphicallyVisible(doc, view, solid))
                    solids.Add(solid);
                else if (geoObj is GeometryInstance geoInst)
                {
                    GeometryElement innerGeo = getInstGeo ? geoInst.GetInstanceGeometry() : geoInst.GetSymbolGeometry();
                    GetSolidFromGeometry(doc, innerGeo, getInstGeo, ref solids, view);
                }
            }
        }

        /// <summary>
        /// Determine if soid is visivle in view
        /// </summary>
        public static bool IsSolidGraphicallyVisible(Document doc, Autodesk.Revit.DB.View view, Solid solid)
        {
            if (doc != null
                && view != null
                && solid.GraphicsStyleId != null
                && solid.GraphicsStyleId != ElementId.InvalidElementId)
            {
                if (doc.GetElement(solid.GraphicsStyleId) is GraphicsStyle graphicalStyle
                    && graphicalStyle.GraphicsStyleCategory != null)
                    return graphicalStyle.GraphicsStyleCategory.get_Visible(view);
            }
            return true;
        }
    }
}
