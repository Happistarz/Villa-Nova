using System.Collections.Generic;
using UnityEngine;

namespace Core.Extensions
{
    public static class Extensions
    {
        #region TRANSFORM & GAMEOBJECT

        /// <summary>
        /// Destroys all children of the transform.
        /// </summary>
        public static void DestroyChildren(this Transform t)
        {
            foreach (Transform child in t)
            {
                Object.Destroy(child.gameObject);
            }
        }
        
        /// <summary>
        /// Gets the component or adds it if it doesn't exist.
        /// </summary>
        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            var component = gameObject.GetComponent<T>();
            if (component == null)
            {
                component = gameObject.AddComponent<T>();
            }
            return component;
        }

        public static void ResetTransformation(this Transform trans)
        {
            trans.position = Vector3.zero;
            trans.localRotation = Quaternion.identity;
            trans.localScale = new Vector3(1, 1, 1);
        }

        #endregion

        #region COLLECTIONS

        /// <summary>
        /// Returns a random element from the list.
        /// </summary>
        public static T RandomItem<T>(this IList<T> list)
        {
            if (list.Count == 0) throw new System.IndexOutOfRangeException("Cannot select a random item from an empty list");
            return list[Random.Range(0, list.Count)];
        }

        /// <summary>
        /// Shuffles the list in place using Fisher-Yates algorithm.
        /// </summary>
        public static void Shuffle<T>(this IList<T> list)
        {
            var n = list.Count;
            while (n > 1)
            {
                n--;
                var k = Random.Range(0, n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }
        }

        #endregion

        #region VECTOR MATH

        public static Vector2 ToVector2(this Vector3 v) => new Vector2(v.x, v.y);
        
        public static Vector3 WithX(this Vector3 v, float x) => new Vector3(x, v.y, v.z);
        public static Vector3 WithY(this Vector3 v, float y) => new Vector3(v.x, y, v.z);
        public static Vector3 WithZ(this Vector3 v, float z) => new Vector3(v.x, v.y, z);

        public static Vector3 Flat(this Vector3 v) => new Vector3(v.x, 0, v.z);
        
        public static Quaternion WithXRotation(this Quaternion q, float x) => Quaternion.Euler(q.eulerAngles.WithX(x));
        public static Quaternion WithYRotation(this Quaternion q, float y) => Quaternion.Euler(q.eulerAngles.WithY(y));
        public static Quaternion WithZRotation(this Quaternion q, float z) => Quaternion.Euler(q.eulerAngles.WithZ(z));

        #endregion
    }
}

