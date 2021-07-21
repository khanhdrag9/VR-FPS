using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent()]
[RequireComponent(typeof(RectTransform))]
public class AutoAnchorUI : MonoBehaviour
{
    [SerializeField] bool enableFromStart = false;
    void Awake()
    {
        GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        gameObject.SetActive(enableFromStart);
    }
}
