using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class BoardModifyer : MonoBehaviour
{
    [SerializeField] PlayerInput playerInput;
    [SerializeField] AxisModifyBeacon axisXprefab;
    [SerializeField] AxisModifyBeacon axisYprefab;
    [SerializeField] AxisModifyBeacon axisZprefab;

    private LayerMask worldUILayerMask;
    private InputAction clickAction;
    private InputAction pointAction;

    private enum Beacons { Left, Right, Top, Bottom, UpDown, COUNT }
    private readonly AxisModifyBeacon[] beacons = new AxisModifyBeacon[(int)Beacons.COUNT];
    private Beacons selectedBeaconIndex = Beacons.COUNT;
    private Beacons selectedPairIndex => pairBeaconMapping[(int)selectedBeaconIndex];
    private AxisModifyBeacon selectedBeacon => beacons[(int)selectedBeaconIndex];

    private ARBoard board;
    private Plane horizontalPlane;

    private static readonly IList<Vector3> beaconPositions = new List<Vector3>
    {
        new(-5, 0, 0),
        new(+5, 0, 0),
        new(0, 0, +5),
        new(0, 0, -5),
        new(0, 0, 0)
    }.AsReadOnly(); // 원본 Plane Mesh상에서 각 비콘이 놓일 위치
    private static readonly IList<Beacons> pairBeaconMapping = new List<Beacons>
    {
        Beacons.Right,
        Beacons.Left,
        Beacons.Bottom,
        Beacons.Top,
        Beacons.COUNT,
        Beacons.COUNT,
    }.AsReadOnly(); // 반대쪽 비콘

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

        worldUILayerMask = LayerMask.GetMask("World UI");
    }

    private void Start()
    {
        clickAction = playerInput.actions["Click"];
        pointAction = playerInput.actions["Point"];
    }

    public void EnterModifyMode(ARBoard board)
    {
        Debug.Log("편집 모드 진입");
        this.board = board;

        foreach (var beacon in beacons)
        {
            beacon.gameObject.SetActive(true);
            beacon.transform.rotation = board.transform.rotation;
            beacon.transform.position = board.transform.TransformPoint(beaconPositions[beacon.Id]);
            Debug.Log($"{beacon.Id}번 비콘 위치: {beacon.transform.position}");
        }

        clickAction.started += TrySelectBeacon;
        clickAction.canceled += ReleaseBeacon;

        // 수평 방향으로 드래그할 평면
        horizontalPlane = new Plane(board.transform.up, board.transform.position);
    }

    public void ExitModifyMode()
    {
        Debug.Log("편집 모드 해제");
        this.board = null;

        foreach (var beacon in beacons)
        {
            beacon.gameObject.SetActive(false);
        }

        clickAction.started -= TrySelectBeacon;
        clickAction.canceled -= ReleaseBeacon;
        ReleaseBeacon(); // 비콘이 선택된 채로 편집 모드를 나가는 경우의 처리
    }

    private void TrySelectBeacon(InputAction.CallbackContext context)
    {
        if (selectedBeaconIndex != Beacons.COUNT)
        {
            Debug.LogWarning("[BoardModifyer] 이미 비콘이 선택된 상태에서 다시 선택 시도됨");
            return;
        }

        // 비콘이 터치로 선택되었는지 확인
        Ray cursorRay = Camera.main.ScreenPointToRay(pointAction.ReadValue<Vector2>());

        if (! Physics.Raycast(cursorRay, out RaycastHit hitinfo, 1f, worldUILayerMask))
            return;

        if (! hitinfo.collider.TryGetComponent(out AxisModifyBeacon clickedBeacon))
            return;

        selectedBeaconIndex = (Beacons)clickedBeacon.Id;

        Debug.Log($"{selectedBeaconIndex} 방향 비콘 선택됨");

        if (selectedBeaconIndex == Beacons.UpDown)
        {
            pointAction.performed += DragVerticalBeacon;
        }
        else
        {
            pointAction.performed += DragHorizontalBeacon;
        }
    }

    private void ReleaseBeacon(InputAction.CallbackContext _) => ReleaseBeacon();

    private void ReleaseBeacon()
    {
        // 선택된 비콘이 있다면
        if (selectedBeaconIndex == Beacons.UpDown)
        {
            pointAction.performed -= DragVerticalBeacon;
            horizontalPlane = new Plane(board.transform.up, board.transform.position); // 높이가 변경되었으므로 수평 평면 갱신
        }
        if (selectedBeaconIndex != Beacons.COUNT)
        {
            pointAction.performed -= DragHorizontalBeacon;
        }

        selectedBeaconIndex = Beacons.COUNT;
    }

    private void DragHorizontalBeacon(InputAction.CallbackContext context)
    {
        Ray cursorRay = Camera.main.ScreenPointToRay(context.ReadValue<Vector2>());
        if (!horizontalPlane.Raycast(cursorRay, out float enter))
            return;

        Vector3 pointOnPlane = cursorRay.origin + enter * cursorRay.direction;
        Vector3 pointBoardLoacl = board.transform.InverseTransformPoint(pointOnPlane); // 보드 로컬 공간에서의 클릭 좌표
        Vector3 positionA;
        Vector3 positionB = beacons[(int)selectedPairIndex].transform.position;

        switch (selectedBeaconIndex)
        {
            case Beacons.Left:  //
            case Beacons.Right: // (보드 로컬 기준) x축 편집
                pointBoardLoacl.z = 0f;
                selectedBeacon.transform.position = board.transform.TransformPoint(pointBoardLoacl);
                positionA = selectedBeacon.transform.position;
                board.transform.position = 0.5f * (positionA + positionB);
                board.transform.localScale = new Vector3((positionA - positionB).magnitude * 0.1f, board.transform.localScale.y, board.transform.localScale.z);
                break;

            case Beacons.Top:    //
            case Beacons.Bottom: // (보드 로컬 기준) z축 편집
                pointBoardLoacl.x = 0f;
                selectedBeacon.transform.position = board.transform.TransformPoint(pointBoardLoacl);
                positionA = selectedBeacon.transform.position;
                board.transform.position = 0.5f * (positionA + positionB);
                board.transform.localScale = new Vector3(board.transform.localScale.x, board.transform.localScale.y, (positionA - positionB).magnitude * 0.1f);
                break;

            default:
                Debug.LogWarning("[BoardModifyer] 비콘 선택 상태가 잘못됨");
                return;
        }

        // 보드에 맞춰 나머지 비콘 위치 갱신
        for (int i = 0; i < beacons.Length; i++)
        {
            if (i == (int)selectedBeaconIndex)
                continue;
            if (i == (int)selectedPairIndex)
                continue;

            beacons[i].transform.position = board.transform.TransformPoint(beaconPositions[i]);
        }
    }

    private void DragVerticalBeacon(InputAction.CallbackContext context)
    {
        // 수직 방향 드래그는 카메라 방향에 의존하므로 매 프레임 평면을 갱신해야 한다
        Vector3 cameraFowardXZ = Camera.main.transform.forward;
        cameraFowardXZ.y = 0f;
        Plane VerticalPlane = new(cameraFowardXZ, selectedBeacon.transform.position); // 생성자에서 정규화되므로 두번 하지 않기

        Ray cursorRay = Camera.main.ScreenPointToRay(context.ReadValue<Vector2>());
        if (! VerticalPlane.Raycast(cursorRay, out float enter))
            return;

        Vector3 pointOnPlane = cursorRay.origin + enter * cursorRay.direction;

        // 드래그 지점 높이에 맞춰 모든 비콘과 평면 높이 조정
        foreach (var beacon in beacons)
        {
            beacon.transform.position = new Vector3(beacon.transform.position.x, pointOnPlane.y, beacon.transform.position.z);
        }

        board.transform.position = new Vector3(board.transform.position.x, pointOnPlane.y, board.transform.position.z);
    }
}
