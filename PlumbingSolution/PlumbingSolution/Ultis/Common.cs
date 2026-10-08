using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using System;
using System.Collections.Generic;
using System.Text;

namespace PlumbingSolution.Ultis
{
    internal class Common
    {
        // NOTE: Common.Swap<T> hiện không còn nơi nào gọi tới (đã vậy từ trước, không
        // phải do thay đổi của đợt license này) — giữ nguyên, không xoá dead code không
        // liên quan tới phạm vi đang sửa.
        public static void Swap<T>(ref T obj1, ref T obj2)
        {
            T temp = obj1;
            obj1 = obj2;
            obj2 = temp;
        }
    }
}
