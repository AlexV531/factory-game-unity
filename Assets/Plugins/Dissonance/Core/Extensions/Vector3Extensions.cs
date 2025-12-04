using UnityEngine;

namespace Dissonance.Extensions
{
    internal static class Vector3Extensions
    {
        public static Vector3Int Quantise(this Vector3 position, float size)
        {
            return new Vector3Int(
               (int)Mathf.Floor(position.x / size),
               (int)Mathf.Floor(position.y / size),
               (int)Mathf.Floor(position.z / size)
           );
        }
    }
}
