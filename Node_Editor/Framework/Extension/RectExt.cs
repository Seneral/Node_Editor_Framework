using UnityEngine;
using System.Collections;

public static class RectExt
{
    /// <summary>
    /// Returns true if rect2 is fully inside rect1
    /// </summary>
    /// <param name="rect1"></param>
    /// <param name="rect2"></param>
    /// <returns></returns>
    public static bool Contains(this Rect rect1, Rect rect2)
    {
        return rect1.Contains(rect2.min) && rect1.Contains(rect2.max);
    }
}
