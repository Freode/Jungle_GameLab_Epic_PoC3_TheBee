using UnityEngine;
using System.Collections;

public class HoneyProjectile : MonoBehaviour
{
    [Header("이동 설정 (인스펙터 조정 가능)")]
    [Tooltip("비행 시간 (초) - 낮을수록 빠름")]
    public float minDuration = 0.8f;
    [Tooltip("비행 시간 (초) - 높을수록 느림")]
    public float maxDuration = 1.2f;

    [Tooltip("포물선 높이 - 높을수록 높게 뜸")]
    public float arcHeight = 5.0f;

    [Tooltip("도착 시 크기가 작아지는 효과 사용 여부")]
    public bool useShrinkEffect = true;

    private Vector3 startPos;
    private Vector3 targetPos;

    public void Initialize(Vector3 start, Vector3 target)
    {
        startPos = start;
        targetPos = target;
        
        // 최소~최대 사이의 랜덤한 속도로 설정 (벌떼처럼 보이기 위해)
        float randomDuration = Random.Range(minDuration, maxDuration);
        
        StartCoroutine(FlyRoutine(randomDuration));
    }

    IEnumerator FlyRoutine(float duration)
    {
        float elapsed = 0f;
        Vector3 initialScale = transform.localScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration; // 0에서 1로 증가

            // 1. 이동 (선형 보간 + 포물선)
            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);
            
            // 포물선 공식 (sin 곡선 활용하거나 2차 함수 활용)
            // t * (1-t)는 0->0.5->1 과정에서 0->0.25->0 이 됨. 여기에 4를 곱하면 0->1->0
            currentPos.y += arcHeight * 4 * t * (1 - t);

            transform.position = currentPos;

            // 2. (선택) 도착할 때쯤 크기가 작아지며 빨려 들어가는 느낌
            if (useShrinkEffect && t > 0.8f)
            {
                float shrinkT = (t - 0.8f) * 5f; // 0~1
                transform.localScale = Vector3.Lerp(initialScale, Vector3.zero, shrinkT);
            }

            // 3. (선택) 회전 효과 (날아가는 방향을 보거나 뱅글뱅글 돌거나)
            // transform.Rotate(Vector3.forward * 360 * Time.deltaTime);

            yield return null;
        }

        // 도착 후 파괴
        Destroy(gameObject);
    }
}