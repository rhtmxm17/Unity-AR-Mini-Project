using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class BoardManager : MonoBehaviour
{
    [SerializeField] float maxDistance = 0.1f;

    private Collider board;

    public void SetBoard(Collider board) => this.board = board;

    public bool ImageIsOnBoard(ARTrackedImage image)
    {
        if (board == null)
        {
            Debug.Log("보드가 아직 등록되지 않음");
            return false;
        }

        // Board에 대한 Raycast 실패시
        if (false == board.Raycast(new Ray(image.transform.position + maxDistance * board.transform.up, -board.transform.up), out _, 2f * maxDistance))
        {
            Debug.Log("보드 영역이 아님");
            return false;
        }

        // 기울기가 너무 다를 경우
        if (0.8 > Vector3.Dot(image.transform.up, board.transform.up))
        {
            Debug.Log($"보드와 기울기가 크게 다름 {image.transform.up} / {board.transform.up}");
            return false;
        }

        return true;
    }
}
