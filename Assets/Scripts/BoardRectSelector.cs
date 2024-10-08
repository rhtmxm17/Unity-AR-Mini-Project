using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;

public class BoardRectSelector : MonoBehaviour
{
    [SerializeField] PlayerInput playerInput;
    [SerializeField] GameObject beaconPrefab; // 평면 위에 선택한 지점을 보여줄 프리펩

    enum Phase { First, Second, Last, COUNT }

    private Phase phase;
    private InputAction clickAction;
    private InputAction pointAction;
    private GameObject[] beacons = new GameObject[(int)Phase.COUNT]; // 선택한 지점을 표시할 비콘
    private Action<InputAction.CallbackContext>[] updateBeacons = new Action<InputAction.CallbackContext>[(int)Phase.COUNT]; // 드래그하는 동안 실행할 메서드
    private Plane basePlane;
    private bool holdingBeacon = false;

    private void Awake()
    {
        updateBeacons[(int)Phase.First] = UpdateBeacon_FirstPhase;
        updateBeacons[(int)Phase.Second] = UpdateBeacon_SecondPhase;
        updateBeacons[(int)Phase.Last] = UpdateBeacon_LastPhase;
    }

    private void Start()
    {
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
        }
    }

    private void UpdateBeacon_FirstPhase(InputAction.CallbackContext obj)
    {
        // 첫번째 비콘은 단순 배치
        if (TryGetCursorPoint(out Vector3 postion))
        {
            beacons[(int)phase].transform.position = postion;
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

            // 이곳에 두 비콘을 잇는 선 그리기 추가
        }
    }

    private void UpdateBeacon_LastPhase(InputAction.CallbackContext obj)
    {
        if (TryGetCursorPoint(out Vector3 postion))
        {
            GameObject currentBeacon = beacons[(int)phase];
            currentBeacon.transform.position = postion;

            // 이곳에 두 비콘을 꼭짓점으로 하고 세번째 비콘을 지나는 직사각형 선 그리기 추가
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
            // 테스트 코드
            {
                clickAction.started -= CreateBeacon;
                clickAction.canceled -= PutBeacon;
                Debug.Log("생성 완료 단계");
            }
        }
    }
}
