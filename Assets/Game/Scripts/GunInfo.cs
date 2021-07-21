using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunInfo : MonoBehaviour
{
    public Camera aimCamera = null;
    public Camera weaponCamera = null;
    public bool isHitting = false;
    public RaycastHit raycastHit = new RaycastHit();
    public Vector3 hitPosition = Vector3.zero;
}
