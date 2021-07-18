using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    public bool Active {get; set; } = true;

    public bool IsFire {get; set;} = false;
    public bool IsJump {get; set;} = false;
    public float MouseX {get; set;} = 0f;
    public float MouseY {get; set;} = 0f;
    public Vector3 Move {get; set;} = Vector3.zero;

    void Update()
    {
        IsFire = GetButtonDown("Fire1");
        IsJump = GetButtonDown("Jump");
        MouseX = GetAxis("Mouse X");
        MouseY = GetAxis("Mouse Y");

        Move = new Vector3(GetAxis("Horizontal"), 0, GetAxis("Vertical"));
    }

    bool GetButtonDown(string name) => Active && Input.GetButtonDown(name);
    float GetAxis(string name) => Active ? Input.GetAxis(name) : 0;
}
