using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;

[RequireComponent(typeof(LineRenderer))]
public class BoardPolygonSelector : MonoBehaviour
{
    [SerializeField] PlayerInput playerInput;
    [SerializeField] GameObject beaconPrefab; // 평면 위에 선택한 지점을 보여줄 프리펩
    [SerializeField] ARBoard boardPrefab; // 선택이 완료되었을 때 생성할 보드 프리펩

    public UnityEvent<ARBoard> OnBoardCreated;

    private LineRenderer lineRenderer;

    private InputAction clickAction;
    private InputAction pointAction;
    private ARBoard boardInstance;
    private List<GameObject> beacons = new();
    private int currentBeaconIndex;
    private Plane basePlane;
    private bool holdingBeacon = false;

    private struct BeaconInfo
    {
        public int index;
        public Vector3 position;
    }

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    private void Start()
    {
        lineRenderer.positionCount = 0;

        clickAction = playerInput.actions["Click"];
        pointAction = playerInput.actions["Point"];
    }

    public void EnterSelectMode(ARPlane targetPlane)
    {
        lineRenderer.enabled = true;
        lineRenderer.positionCount = 0;
        basePlane = targetPlane.infinitePlane;
        currentBeaconIndex = 0;

        clickAction.started += CreateBeacon;
        clickAction.canceled += ReleaseBeacon;
    }

    private bool TryGetCursorPoint(out Vector3 point)
    {
        // 커서 위치가 가리키는 평면상의 지점을 반환

        Ray cursorRay = Camera.main.ScreenPointToRay(pointAction.ReadValue<Vector2>());
        bool result = basePlane.Raycast(cursorRay, out float enter);
        point = cursorRay.origin + enter * cursorRay.direction;

        return result;
    }

    private void CreateBeacon(InputAction.CallbackContext _)
    {
        Debug.Log($"CreateBeacon index:{currentBeaconIndex}");

        if (holdingBeacon)
        {
            Debug.LogWarning("[BoardPolygonSelector] 클릭이 해제되지 않은 채로 다시 한 번 클릭됨");
            return;
        }

        if (TryGetCursorPoint(out Vector3 postion))
        {
            holdingBeacon = true;
            
            if (beacons.Count == currentBeaconIndex)
            {
                beacons.Add(Instantiate(beaconPrefab, postion, Quaternion.identity));
            }
            else
            {
                beacons[currentBeaconIndex].SetActive(true);
                beacons[currentBeaconIndex].transform.position = postion;
            }
            beacons[currentBeaconIndex].transform.up = basePlane.normal; // 평면의 법선 방향이 위가 되도록 조정

            pointAction.performed += UpdateBeacon;

            lineRenderer.positionCount = 1 + currentBeaconIndex;
        }
    }

    private void UpdateBeacon(InputAction.CallbackContext _)
    {
        // 선택된 비콘과 이전 비콘은 두 비콘이 마주보듯이 회전
        if (TryGetCursorPoint(out Vector3 postion))
        {
            GameObject currentBeacon = beacons[currentBeaconIndex];
            currentBeacon.transform.position = postion;
            if (currentBeaconIndex > 0)
            {
                GameObject lastBeacon = beacons[currentBeaconIndex - 1];

                Vector3 lookDir = (currentBeacon.transform.position - lastBeacon.transform.position).normalized;
                currentBeacon.transform.rotation = Quaternion.LookRotation(lookDir, basePlane.normal);
                lastBeacon.transform.rotation = Quaternion.LookRotation(lookDir, basePlane.normal);

            }
            lineRenderer.SetPosition(currentBeaconIndex, postion);
        }
    }

    private void ReleaseBeacon(InputAction.CallbackContext _)
    {
        // 클릭을 한 채로 진입할 경우를 위한 예외처리
        if (!holdingBeacon)
            return;

        Debug.Log($"PutBeacon index:{currentBeaconIndex}");

        holdingBeacon = false;

        pointAction.performed -= UpdateBeacon;

        for (int i = 0; i < currentBeaconIndex; i++)
        {
            // 비콘 간격이 0.1m 이상이라면 진행
            if (0.01f < (beacons[currentBeaconIndex].transform.position - beacons[i].transform.position).sqrMagnitude)
                continue;

            // 첫 번째 비콘 이외의 비콘과 너무 가깝다면 취소 (아직 삼각형이 안되는 경우에도)
            if (i != 0 || currentBeaconIndex < 3)
            {
                Debug.Log($"{i}번 비콘과 너무 가까움");
                beacons[currentBeaconIndex].SetActive(false);
                return;
            }

            // 그 외의 경우(==첫 번째 비콘과 가깝다면) 완료
            BuildBoardMesh();

            lineRenderer.enabled = false;
            clickAction.started -= CreateBeacon;
            clickAction.canceled -= ReleaseBeacon;
            foreach (GameObject beacon in beacons)
            {
                beacon.SetActive(false);
            }

            OnBoardCreated?.Invoke(boardInstance);
            return;
        }

        currentBeaconIndex++;
    }

    private void BuildBoardMesh()
    {
        int index = 0;
        LinkedList<BeaconInfo> beaconList = new LinkedList<BeaconInfo>
        (
            beacons.GetRange(0, currentBeaconIndex) // 현재 비콘 번호 (==점 개수)까지 사용
                .ConvertAll(beacon => 
                {
                    return new BeaconInfo() // 번호와 위치를 저장
                    {
                        index = index++,
                        position = beacon.transform.position
                    };
                })
        );
        List<int[]> triangleList = new(beaconList.Count - 2);

        LinkedListNode<BeaconInfo> nodeA = beaconList.First;
        LinkedListNode<BeaconInfo> nodeB = nodeA.Next;
        LinkedListNode<BeaconInfo> nodeC = nodeB.Next;

        while (beaconList.Count >=3)
        {
            Debug.Log($"남은 점:{beaconList.Count}");

            while (! ClockwiseTriangle(nodeA.Value.position, nodeB.Value.position, nodeC.Value.position))
            {
                Debug.Log($"반시계:{nodeA.Value.index}, {nodeB.Value.index}, {nodeC.Value.index}");
                // 시계방향이 아니라면 다음 삼각형
                nodeA = nodeB;
                nodeB = nodeC;
                nodeC = (nodeC.Next ?? beaconList.First);
            }

            bool result = true; // 다른 모든 점이 삼각형 밖에 있는지 검사 결과
            for (var iter = nodeC.Next ?? beaconList.First; iter != nodeA; iter = iter.Next ?? beaconList.First)
            {
                if (InTriangle(nodeA.Value.position, nodeB.Value.position, nodeC.Value.position, iter.Value.position))
                {
                    Debug.Log($"삼각형 침범:{nodeA.Value.index}, {nodeB.Value.index}, {nodeC.Value.index} / {iter.Value.index}");
                    result = false;
                    break;
                }
            }

            if (result)
            {
                Debug.Log($"삼각형 등록:{nodeA.Value.index}, {nodeB.Value.index}, {nodeC.Value.index}");
                // 검사 통과시 삼각형 목록에 등록
                triangleList.Add(new int[] { nodeA.Value.index, nodeB.Value.index, nodeC.Value.index });

                // 사이의 점을 목록에서 제거해서 해당 삼각형을 작업 목록에서 제외한 뒤 속행
                beaconList.Remove(nodeB);

                nodeB = nodeC;
                nodeC = (nodeC.Next ?? beaconList.First);
                Debug.Log($"nodeC: {(nodeC?.Value.index)}");
            }
            else
            {
                // 검사 실패시 다음 삼각형
                nodeA = nodeB;
                nodeB = nodeC;
                nodeC = (nodeC.Next ?? beaconList.First);
            }
        }
    }

    private static bool ClockwiseTriangle(Vector3 triA, Vector3 triB, Vector3 triC)
    {
        // xz 평면상에서 ABC가 시계방향인지 검사한다.

        return (0f < Vector3.Cross(triB - triA, triC - triA).y);
    }

    private static bool InTriangle(Vector3 triA, Vector3 triB, Vector3 triC, Vector3 pos)
    {
        // xz 평면상에서 pos가 ABC(시계방향)로 구성된 삼각형 안쪽에 있는지 검사한다.
        // pos가 AB, BC, CA 모두의 오른쪽에 있다면 삼각형 안쪽일 것이다.

        if (0f >= Vector3.Cross(triB - triA, pos - triA).y)
            return false;
        if (0f >= Vector3.Cross(triA - triB, pos - triB).y)
            return false;
        if (0f >= Vector3.Cross(triA - triC, pos - triC).y)
            return false;

        return true;
    }
}
