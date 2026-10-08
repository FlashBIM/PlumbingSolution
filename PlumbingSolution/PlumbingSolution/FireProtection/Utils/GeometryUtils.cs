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
    

        /// <summary>
        /// return intersection of the 2 lines given that are unbound
        /// </summary>
        /// <param name="Line1"></param>
        /// <param name="Line2"></param>
        /// <returns></returns>
        public static XYZ GetUnBoundIntersection(Line Line1, Line Line2)
        {
            if (Line1 != null && Line2 != null)
            {
                Curve ExtendedLine1 = Line.CreateUnbound(Line1.Origin, Line1.Direction);
                Curve ExtendedLine2 = Line.CreateUnbound(Line2.Origin, Line2.Direction);
                SetComparisonResult setComparisonResult;

#if Debug_2027 || Release_2027 || Release_2026
    // --- CẤU HÌNH CHO REVIT 2026+ ---
    var intersectResult = ExtendedLine1.Intersect(ExtendedLine2, CurveIntersectResultOption.Detailed);
    setComparisonResult = intersectResult.Result;

    if (setComparisonResult != SetComparisonResult.Disjoint)
    {
        var overlaps = intersectResult.GetOverlaps();
        if (overlaps != null && overlaps.Count > 0)
        {
            foreach (var result in overlaps)
            {
                if (result != null)
                    return result.Point;
            }
        }
    }
#else
                // --- CẤU HÌNH CHO CÁC BẢN CŨ HƠN ---
                IntersectionResultArray resultArray;
                setComparisonResult = ExtendedLine1.Intersect(ExtendedLine2, out resultArray);

                if (resultArray != null && resultArray.Size > 0)
                {
                    foreach (IntersectionResult result in resultArray)
                    {
                        if (result != null)
                            return result.XYZPoint;
                    }
                }
#endif
            }
            return null;
        }

        public static bool IsParallel(XYZ first, XYZ second, double tolerance = 10e-3)
        {
            XYZ product = first.CrossProduct(second);
            double length = product.GetLength();
            return Common.IsEqual(length, 0, tolerance);
        }
    }
}
