using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class ARSceneManager : MonoBehaviour
{
    [SerializeField] ARSessionOrigin sessionOrigin;
    [SerializeField] AROcclusionManager occlusionManager;
    [SerializeField] ARPlaneSelector planeSelector;
    [SerializeField] BoardRectSelector boardRectSelector;
    [SerializeField] BoardManager boardManager;

    [SerializeField] UnitDatatable unitDatatable;

    public bool ActiveOcclusion
    {
        set
        {
            occlusionManager.enabled = value;
        }
    }

    private ARPlaneManager planeManager;
    private ARTrackedImageManager trackedImageManager;

    private void Start()
    {
        planeManager = sessionOrigin.GetComponent<ARPlaneManager>();
        trackedImageManager = sessionOrigin.GetComponent<ARTrackedImageManager>();

        // 감지된 평면중 하나를 선택 완료시 보드 영역 선택 진입
        planeSelector.OnPlaneSelected.AddListener(boardRectSelector.EnterSelectMode);

        // 보드 영역 선택 완료시 보드 관리자에 등록 및 오클루전 컬링 종료
        boardRectSelector.OnBoardCreated.AddListener(boardManager.SetBoard);
        boardRectSelector.OnBoardCreated.AddListener(_ => { ActiveOcclusion = false; });

        unitDatatable.Initialize();

        StartCoroutine(StartScene());

        trackedImageManager.trackedImagesChanged += SetBoardToImageUnit;
    }

    private IEnumerator StartScene()
    {
        yield return null;

        planeSelector.EnterSelectMode();
    }

    private void SetBoardToImageUnit(ARTrackedImagesChangedEventArgs args)
    {
        foreach (var image in args.added)
        {
            Debug.Log($"[ARSceneManager]이미지:{image.referenceImage.name} 감지됨");
            image.GetComponent<TrackedImageUnit>().SetBoardManger(boardManager);
        }
    }
}
