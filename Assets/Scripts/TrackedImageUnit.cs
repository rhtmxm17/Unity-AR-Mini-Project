using EPOOutline;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[RequireComponent(typeof(ARTrackedImage))]
public class TrackedImageUnit : MonoBehaviour
{
    [SerializeField] Outlinable outlineQuad;
    [SerializeField] UnitDatatable unitDatatable;
    [SerializeField] float AppearanceLerpCoefficient = 2f;

    private ARBoard board;
    private ARTrackedImage trackedImage;
    private GameObject unit;
    private Coroutine unitAppearanceRoutine;

    private void Awake()
    {
        trackedImage = GetComponent<ARTrackedImage>();
    }

    private void Start()
    {
        outlineQuad.transform.localScale = trackedImage.size;
        outlineQuad.BackParameters.Color = Color.gray;

        GameObject prefab = unitDatatable.GetItem(trackedImage.referenceImage.name).unitPrefab;
        unit = Instantiate(prefab, this.transform);
        unit.transform.localPosition = Vector3.up * -0.1f;
        unit.SetActive(false);

        StartCoroutine(UpdateOutlineColor());
    }

    public void SetBoard(ARBoard board)
    {
        this.board = board;
        if (unitAppearanceRoutine == null)
        {
            unitAppearanceRoutine = StartCoroutine(CheckOnBoard());
        }
    }

    private IEnumerator UpdateOutlineColor()
    {
        YieldInstruction period = new WaitForSeconds(1f);
        while (true)
        {
            switch (trackedImage.trackingState)
            {
                case TrackingState.None:
                    outlineQuad.FrontParameters.Color = Color.red;
                    break;
                case TrackingState.Limited:
                    outlineQuad.FrontParameters.Color = Color.yellow;
                    break;
                case TrackingState.Tracking:
                    outlineQuad.FrontParameters.Color = Color.blue;
                    break;
            }
            yield return period;
        }
    }

    private IEnumerator CheckOnBoard()
    {
        YieldInstruction period = new WaitForSeconds(1f);

        // 트래킹 상태가 양호하며 보드 위에 있을 경우에만 탈출
        while (! (trackedImage.trackingState == TrackingState.Tracking
                && board.ImageIsOnBoard(trackedImage)))
        {
            yield return period;
        }

        Debug.Log($"[TrackedImageUnit]보드 위에 놓임:{trackedImage.referenceImage.name}");

        unit.SetActive(true);
        float timer = 2.5f;

        // 이미지 위로 튀어나오는 연출
        while (timer > 0f)
        {
            timer -= Time.deltaTime;
            unit.transform.localPosition = Vector3.Lerp(unit.transform.localPosition, Vector3.zero, Time.deltaTime * AppearanceLerpCoefficient);
            Debug.Log($"{unit.transform.localPosition}");
            yield return null;
        }
        unit.transform.localPosition = Vector3.zero;
    }
}
