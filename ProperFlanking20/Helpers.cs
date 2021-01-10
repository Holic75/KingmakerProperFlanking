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


        public static bool checkGeometricFlanking(Vector2 o, Vector2 a, Vector2 b, float min_angle_rad)
        {
            //consider triangle oab
            //o - target, a, b - attackers
            //for flanking we want:
            //angle aob <= min_angle_rad
            var ao = (o - a).normalized;
            var bo = (o - b).normalized;

            float cos_a_o_b = Vector2.Dot(bo, ao);//angle between flankers
            return cos_a_o_b <= Mathf.Cos(Math.Max(Math.Min(min_angle_rad, (float)Math.PI), 0.0f));
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
