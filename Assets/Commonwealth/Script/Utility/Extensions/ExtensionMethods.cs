using UnityEngine;
using System.Collections;

public static class ExtensionMethods
{
    public static void Reset(this Transform transform)
    {
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;
    }

    public static void SetX(this Transform transform, float x)
    {
        transform.position = new Vector3(x, transform.position.y, transform.position.z);
    }

    public static void SetY(this Transform transform, float y)
    {
        transform.position = new Vector3(transform.position.x, y, transform.position.z);
    }

    public static void SetZ(this Transform transform, float z)
    {
        transform.position = new Vector3(transform.position.x, transform.position.y, z);
    }

    public static void SetParent(this GameObject g, GameObject parent)
    {
        g.transform.parent = parent.transform;
    }

    public static Bounds CalculateBounds(this Transform parent)
    {
        Bounds bounds = new Bounds();
        foreach (BoxCollider box in parent.GetComponentsInChildren<BoxCollider>())
        {
            bounds.Encapsulate(box.bounds);
        }

        return bounds;
    }

    public static void DestroyChildren(this GameObject parent)
    {
        Transform[] children = new Transform[parent.transform.childCount];
        for (int i = 0; i < parent.transform.childCount; i++)
            children[i] = parent.transform.GetChild(i);
        for (int i = 0; i < children.Length; i++)
            Object.Destroy(children[i].gameObject);
    }

    public static void MoveChildren(this GameObject from, GameObject to)
    {
        Transform[] children = new Transform[from.transform.childCount];
        for (int i = 0; i < from.transform.childCount; i++)
            children[i] = from.transform.GetChild(i);
        for (int i = 0; i < children.Length; i++)
            children[i].gameObject.SetParent(to);
    }

    public static Color GetColor(this Color c, int r, int g, int b)
    {
        return new Color(r / 255f, g / 255f, b / 255f, 1f);
    }

    public static Color HexToColor(this Color c, string hex)
    {
        byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
        byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
        byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
        return new Color32(r, g, b, 255);
    }

    public static Color SetAlpha(this Color c, float a)
    {
        return new Color(c.r, c.g, c.b, a);
    }
}