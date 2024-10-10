using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;

[RequireComponent(typeof(LineRenderer))]
public class BoardRectSelector : MonoBehaviour
{
    [SerializeField] PlayerInput playerInput;
    [SerializeField] GameObject beaconPrefab; // 평면 위에 선택한 지점을 보여줄 프리펩
    [SerializeField, Tooltip("원본 사이즈 10*10")] GameObject boardPrefab; // 선택이 완료되었을 때 생성할 보드 프리펩

    public UnityEvent<GameObject> OnBoardCreated;

    private LineRenderer lineRenderer;

    private enum Phase { First, Second, Last, COUNT }

    private Phase phase;
    private InputAction clickAction;
    private InputAction pointAction;
    private GameObject[] beacons = new GameObject[(int)Phase.COUNT]; // 선택한 지점을 표시할 비콘
    private Action<InputAction.CallbackContext>[] updateBeacons = new Action<InputAction.CallbackContext>[(int)Phase.COUNT]; // 드래그하는 동안 실행할 메서드
    private Plane basePlane;
    private bool holdingBeacon = false;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();

        updateBeacons[(int)Phase.First] = UpdateBeacon_FirstPhase;
        updateBeacons[(int)Phase.Second] = UpdateBeacon_SecondPhase;
        updateBeacons[(int)Phase.Last] = UpdateBeacon_LastPhase;
    }

    private void Start()
    {
        lineRenderer.positionCount = 0;

        clickAction = playerInput.actions["Click"];
        pointAction = playerInput.actions["Point"];
    }

    public void EnterSelectMode(ARPlane targetPlane)
    {
        basePlane = targetPlane.infinitePlane;

        phase = Phase.First;
        clickAction.started += CreateBeacon;
        clickAction.canceled += PutBeacon;
    }

    private bool TryGetCursorPoint(out Vector3 point)
    {
        // 커서 위치가 가리키는 평면상의 지점을 반환

        Ray cursorRay = Camera.main.ScreenPointToRay(pointAction.ReadValue<Vector2>());
        bool result = basePlane.Raycast(cursorRay, out float enter);
        point = cursorRay.origin + enter * cursorRay.direction;

        return result;
    }

    private void CreateBeacon(InputAction.CallbackContext obj)
    {
        Debug.Log($"CreateBeacon Phase:{phase}");

        if (holdingBeacon)
        {
            Debug.LogWarning("[BoardRectSelector] 클릭이 해제되지 않은 채로 다시 한 번 클릭됨");
            return;
        }
        
        if (TryGetCursorPoint(out Vector3 postion))
        {
            holdingBeacon = true;

            beacons[(int)phase] = Instantiate(beaconPrefab, postion, Quaternion.identity);
            beacons[(int)phase].transform.up = basePlane.normal; // 평면의 법선 방향이 위가 되도록 조정

            pointAction.performed += updateBeacons[(int)phase];

            lineRenderer.positionCount = 1 + (int)phase;
            if (phase == Phase.Last)
                lineRenderer.positionCount = 4;
        }
    }

    private void UpdateBeacon_FirstPhase(InputAction.CallbackContext obj)
    {
        // 첫번째 비콘은 단순 배치
        if (TryGetCursorPoint(out Vector3 postion))
        {
            beacons[(int)phase].transform.position = postion;
            lineRenderer.SetPosition(0, postion);
        }
    }

    private void UpdateBeacon_SecondPhase(InputAction.CallbackContext obj)
    {
        // 두번째 비콘은 두 비콘이 마주보듯이 회전
        // 아래와 같은 형태가 되도록(+: 비콘)
        // + ------ +
        if (TryGetCursorPoint(out Vector3 postion))
        {
            GameObject currentBeacon = beacons[(int)phase];
            GameObject lastBeacon = beacons[(int)phase - 1];

            currentBeacon.transform.position = postion;

            Vector3 lookDir = (currentBeacon.transform.position - lastBeacon.transform.position).normalized;
            currentBeacon.transform.rotation = Quaternion.LookRotation(lookDir, basePlane.normal);
            lastBeacon.transform.rotation = Quaternion.LookRotation(lookDir, basePlane.normal);

            lineRenderer.SetPosition(1, postion);
        }
    }

    private void UpdateBeacon_LastPhase(InputAction.CallbackContext obj)
    {
        if (TryGetCursorPoint(out Vector3 postion))
        {
            GameObject currentBeacon = beacons[(int)phase];
            currentBeacon.transform.position = postion;

            Vector3 pointA = lineRenderer.GetPosition(0);
            Vector3 pointB = lineRenderer.GetPosition(1);
            Vector3 vectorAB = pointB - pointA; // 밑변
            Vector3 pointH = pointA + vectorAB / Vector3.Dot(vectorAB, vectorAB) * Vector3.Dot(vectorAB, postion - pointA); // 수선의 발
            Vector3 vectorHC = postion - pointH; // 밑변->클릭지점 벡터

            lineRenderer.SetPosition(2, pointB + vectorHC);
            lineRenderer.SetPosition(3, pointA + vectorHC);
        }
    }

    private void PutBeacon(InputAction.CallbackContext obj)
    {
        // 클릭을 한 채로 진입할 경우를 위한 예외처리
        if (! holdingBeacon)
            return;

        Debug.Log($"PutBeacon Phase:{phase}");

        holdingBeacon = false;

        pointAction.performed -= updateBeacons[(int)phase];
        phase++;
        Debug.Log($"Phase 변경:{phase}");

        if (Phase.COUNT == phase)
        {
            clickAction.started -= CreateBeacon;
            clickAction.canceled -= PutBeacon;

            // 보드 사이즈 계산
            Vector3[] vertexes = new Vector3[4];
            lineRenderer.GetPositions(vertexes);

            Vector3 center = (vertexes[0] + vertexes[2]) * 0.5f;
            float length = (vertexes[1] - vertexes[0]).magnitude;
            float height = (vertexes[3] - vertexes[0]).magnitude;

            GameObject board = Instantiate(boardPrefab, center, beacons[0].transform.rotation);
            board.transform.localScale = new Vector3(height * 0.1f, 1f, length * 0.1f);

            //// 자유 도형으로 한다면 Mesh 생성이 필요할듯
            //Mesh boardMesh = new();
            //boardMesh.SetVertices(vertexes);
            //boardMesh.SetTriangles(new int[6] { 0, 1, 2, 0, 2, 3 }, 0);

            OnBoardCreated?.Invoke(board);
        }
    }
}
