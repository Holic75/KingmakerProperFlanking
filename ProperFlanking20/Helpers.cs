using Kingmaker.Enums;
using Kingmaker.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProperFlanking20
{
    public class Helpers
    {
        public static bool isCircleIntersectedByLine(Vector3 o, float r2, Vector3 a, Vector3 b)
        {
            var ao = (o - a).To2D();
            var ab = (b - a).To2D();
            float norm_ab = (float)Math.Sqrt(Vector2.Dot(ab, ab));
            float proj = Vector2.Dot(ao, ab) / norm_ab;
            if (proj > norm_ab || proj < 0.0f)
            {
                return false;
            }
            float dist2 = Vector2.Dot(ao, ao) - proj * proj;
            return dist2 < r2;
        }


        public static float unitSizeToDiameter(Size sz) //in feet
        {
            switch (sz)
            {
                case Size.Large:
                    return 10;
                case Size.Huge:
                    return 15;
                case Size.Gargantuan:
                    return 20;
                case Size.Colossal:
                    return 30;
                default:
                    return 5;
            }
        }
    }
}
