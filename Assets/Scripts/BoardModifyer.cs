using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BoardModifyer : MonoBehaviour
{
    [SerializeField] PlayerInput playerInput;
    [SerializeField] AxisModifyBeacon axisXprefab;
    [SerializeField] AxisModifyBeacon axisYprefab;
    [SerializeField] AxisModifyBeacon axisZprefab;

    private enum Beacons { Left, Right, Top, Bottom, UpDown, COUNT }
    private readonly AxisModifyBeacon[] beacons = new AxisModifyBeacon[(int)Beacons.COUNT];
    private static readonly IList<Vector3> beaconPositions = new List<Vector3>
    {
        new(-5, 0, 0),
        new(+5, 0, 0),
        new(0, 0, +5),
        new(0, 0, -5),
        new(0, 0, 0)
    }.AsReadOnly(); // 원본 Plane Mesh상에서 각 비콘이 놓일 위치

    private InputAction clickAction;
    private InputAction pointAction;

    private void Awake()
    {
        beacons[(int)Beacons.Left] = Instantiate(axisXprefab);
        beacons[(int)Beacons.Right] = Instantiate(axisXprefab);
        beacons[(int)Beacons.Top] = Instantiate(axisZprefab);
        beacons[(int)Beacons.Bottom] = Instantiate(axisZprefab);
        beacons[(int)Beacons.UpDown] = Instantiate(axisYprefab);

        for (int i = 0; i < beacons.Length; i++)
        {
            beacons[i].Id = i;
            beacons[i].gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        clickAction = playerInput.actions["Click"];
        pointAction = playerInput.actions["Point"];
    }

    public void EnterModifyMode(GameObject board)
    {
        Debug.Log("편집 모드 진입");
        foreach (var beacon in beacons)
        {
            beacon.gameObject.SetActive(true);
            beacon.transform.rotation = board.transform.rotation;
            beacon.transform.position = board.transform.TransformPoint(beaconPositions[beacon.Id]);
            Debug.Log($"{beacon.Id}번 비콘 위치: {beacon.transform.position}");
        }
    }

}
