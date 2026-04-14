using System.Collections.Generic;
using UnityEngine;

namespace Core.Extensions
{
    public static class Extensions
    {
        #region TRANSFORM & GAMEOBJECT

        /// <summary>Destroys all children of the transform</summary>
        public static void DestroyChildren(this Transform _t)
        {
            foreach (Transform child in _t)
            {
                Object.Destroy(child.gameObject);
            }
        }

        /// <summary>Gets or adds a component on the GameObject</summary>
        public static T GetOrAddComponent<T>(this GameObject _gameObject) where T : Component
        {
            var component = _gameObject.GetComponent<T>();

            if (!component) component = _gameObject.AddComponent<T>();

            return component;
        }

        public static void ResetTransformation(this Transform _trans)
        {
            _trans.position      = Vector3.zero;
            _trans.localRotation = Quaternion.identity;
            _trans.localScale    = new Vector3(1, 1, 1);
        }

        #endregion

        #region COLLECTIONS

        /// <summary>Returns a random element from the list</summary>
        public static T RandomItem<T>(this IList<T> _list, System.Random _rng = null)
        {
            var index = _rng?.Range(0, _list.Count) ?? Random.Range(0, _list.Count);
            return _list.Count == 0
                ? throw new System.IndexOutOfRangeException("Can't select a random item from an empty list")
                : _list[index];
        }

        /// <summary>Shuffles the list in place</summary>
        public static void Shuffle<T>(this IList<T> _list)
        {
            var n = _list.Count;
            while (n > 1)
            {
                n--;
                var k = Random.Range(0, n + 1);
                (_list[k], _list[n]) = (_list[n], _list[k]);
            }
        }

        #endregion

        #region VECTOR MATH

        public static Vector2 ToVector2(this Vector3    _v) => new(_v.x, _v.y);
        public static Vector3 ToVector3(this Vector2Int _v) => new(_v.x, 0f, _v.y);

        public static Vector3 WithX(this Vector3 _v, float _x) => new(_x, _v.y, _v.z);
        public static Vector3 WithY(this Vector3 _v, float _y) => new(_v.x, _y, _v.z);
        public static Vector3 WithZ(this Vector3 _v, float _z) => new(_v.x, _v.y, _z);

        public static Vector3 Flat(this Vector3 _v) => new(_v.x, 0, _v.z);

        public static Quaternion WithXRotation(this Quaternion _q, float _x) =>
            Quaternion.Euler(_q.eulerAngles.WithX(_x));

        public static Quaternion WithYRotation(this Quaternion _q, float _y) =>
            Quaternion.Euler(_q.eulerAngles.WithY(_y));

        public static Quaternion WithZRotation(this Quaternion _q, float _z) =>
            Quaternion.Euler(_q.eulerAngles.WithZ(_z));

        #endregion

        #region SYSTEM RANDOM

        /// <summary>Returns a random int in [min, max[</summary>
        public static int Range(this System.Random _rng, int _min, int _max)
        {
            return _rng.Next(_min, _max);
        }

        /// <summary>Returns a random float in [min, max[</summary>
        public static float Range(this System.Random _rng, float _min, float _max)
        {
            return _min + (float)_rng.NextDouble() * (_max - _min);
        }

        #endregion
    }
}