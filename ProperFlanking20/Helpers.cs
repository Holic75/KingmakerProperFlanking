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
        public static bool isCircleIntersectedByLine(Vector2 o, float r2, Vector2 a, Vector2 b)
        {
            var ao = (o - a);
            var ab = (b - a);
            float norm_ab = (float)Math.Sqrt(Vector2.Dot(ab, ab));
            float proj = Vector2.Dot(ao, ab) / norm_ab;
            if (proj > norm_ab || proj < 0.0f)
            {
                return false;
            }
            float dist2 = Vector2.Dot(ao, ao) - proj * proj;
            return dist2 < r2;
        }


        public static bool checkGeometricFlanking(Vector2 o, Vector2 a, Vector2 b, float max_angle_rad)
        {
            //o - target, a, b - attackers
            var oa = (o - a).normalized;
            var ob = (o - b).normalized;
            var ab = (b - a).normalized;


            return Vector2.Dot(oa, ob) <= max_angle_rad
                   && Vector2.Dot(oa, ab) < Math.PI / 2
                   && Vector2.Dot(ob, ab) < Math.PI / 2;
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
