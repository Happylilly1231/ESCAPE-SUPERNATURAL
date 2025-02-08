using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour
{
    public enum Type
    {
        Weapon,
        TimeBomb,
        TimeBombSetter,
    }

    public Type type;
    public int id;
    public int curBulletCnt;
}
