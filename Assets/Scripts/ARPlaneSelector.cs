using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;

public class ARPlaneSelector : MonoBehaviour
{
    [SerializeField] ARPlaneManager planeManager;
    [SerializeField] PlayerInput playerInput;
    [SerializeField] Material focusedPlaneMaterial;
    [SerializeField] Material defaultPlaneMaterial;
    
    public event UnityAction<ARPlane> OnPlaneSelected;
    public ARPlane SelectedPlane { get; private set; } = null; // 선택이 확정된 ARPlane

    private InputAction clickAction;
    private InputAction pointAction;
    private ARPlane focusedPlane = null; // 임시로 선택된 ARPlane(재차 선택시 확정)
    private LayerMask arPlaneMask;

    private void Start()
    {
        clickAction = playerInput.actions["Click"];
        pointAction = playerInput.actions["Point"];
        arPlaneMask = LayerMask.GetMask("AR Plane");

        // TestCode
        {
            EnterSelectMode();
            planeManager.planesChanged += args =>
            {
                foreach (ARPlane plane in args.added)
                {
                    Debug.Log($"AR Plane 추가됨, Name:{plane.gameObject.name}, Position:{plane.transform.position}, Layer:{LayerMask.LayerToName(plane.gameObject.layer)}");
                }
            };
        }
    }

    public void EnterSelectMode()
    {
        planeManager.enabled = true;
        clickAction.started += OnClick;
    }

    public void ExitSelectMode()
    {
        // 다른 AR Plane 정리 및 planeManager 정지
        foreach(ARPlane plane in planeManager.trackables)
        {
            if (plane != SelectedPlane)
                Destroy(plane.gameObject);
        }
        planeManager.enabled = false;
        Debug.Log($"선택 완료시 planeManager.trackables.count:{planeManager.trackables.count}");

        clickAction.started -= OnClick;
        OnPlaneSelected?.Invoke(SelectedPlane);
    }

    private void OnClick(InputAction.CallbackContext context)
    {
        Ray clickRay = Camera.main.ScreenPointToRay(pointAction.ReadValue<Vector2>());

        Debug.Log($"Camera Position: {Camera.main.transform.position}");
        Debug.Log($"clickRayUnityWorld: {clickRay}");

        ARPlane clicked = null;
        
        // 화면에 보이는 ARPlane을 선택해야하므로 ARRaycast 대신 Physics.Raycast 사용
        if (Physics.Raycast(clickRay, out RaycastHit hitInfo, 5f, arPlaneMask))
        {
            clicked = hitInfo.collider.GetComponent<ARPlane>();
        }
        Debug.Log($"클릭됨: {(clicked != null ? clicked.name : "null")}");
        ClickPlane(clicked);
    }

    private void ClickPlane(ARPlane clicked)
    {
        // 사용자가 ARPlane을 클릭(터치)했을 때 포커스된 평면이면 확정, 아니라면 포커스 전환
        if (clicked != null && clicked == focusedPlane)
        {
            SelectedPlane = clicked;
            Debug.Log("선택 완료됨");
            ExitSelectMode();
            return;
        }

        if (focusedPlane != null)
        {
            focusedPlane.GetComponent<Renderer>().material = defaultPlaneMaterial;
        }

        focusedPlane = clicked;

        if (focusedPlane != null)
        {
            focusedPlane.GetComponent<Renderer>().material = focusedPlaneMaterial;
            Debug.Log("임시 선택됨");
        }
    }
}
