using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
public class TagAttribute : PropertyAttribute
{
    // empty - marker attribute for the editor drawer
}
