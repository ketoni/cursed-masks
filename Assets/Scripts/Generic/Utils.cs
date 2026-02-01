using UnityEngine;

namespace Assets.Scripts.Generic
{
    internal static class Utils
    {
        public static bool FiftyFifty()
        {
            return Random.Range(0, 2) == 1;
        }
    }
}
