using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class AxisModifyBeacon : MonoBehaviour
{
    public int Id { get; set; } = -1;
    [field:SerializeField] public Vector3 Axis { get; private set; }
}
