using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour
{
    public enum Type
    {
        Ammo,
        Document,
        Weapon,
        Healing,
    }

    public Type type;
    public int id;
    public int curBulletCnt;
}
