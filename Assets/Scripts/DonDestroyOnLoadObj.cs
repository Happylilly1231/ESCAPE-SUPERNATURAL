using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DonDestroyOnLoadObj : MonoBehaviour
{
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
}
