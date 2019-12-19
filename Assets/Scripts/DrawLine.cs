using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class DrawLineEvent : UnityEvent<DrawLine> { }

/// <summary>
/// DrawLine
/// 두 장의 카드를 맞추었을 때 나타낼 경로를 그리는 클래스입니다.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class DrawLine : MonoBehaviour {

    /// <summary>
    /// 라인 그리는 객체
    /// </summary>
    private LineRenderer _lineRenderer;

    /// <summary>
    /// 현재 도달한 인덱스
    /// </summary>
    private int _currentIndex;

    /// <summary>
    /// 하나의 라인이 그려지는 시간 (전체 그려지는 시간 / 코너 갯수)
    /// </summary>
    private float _explodeTime;

    /// <summary>
    /// 도달 경로
    /// </summary>
    public Vector3[] Positions;

    /// <summary>
    /// 전체 라인이 그려지는 시간
    /// </summary>
    public float ElapsedTime = 1.0f;

    /// <summary>
    /// 전체 라인이 그려졌을 때 호출될 이벤트핸들러
    /// </summary>
    public DrawLineEvent OnComplete;

    public void Awake()
    {
        this._lineRenderer = this.GetComponent<LineRenderer>();
    }

    // Use this for initialization
    void Start () {
		
        this._lineRenderer.numPositions = 1;
        this._lineRenderer.SetPosition(0, Positions[0]);

        var count = this.Positions.Length - 1;

        // 한 구획을 그리는데 걸리는 시간을 구한다.
        this._explodeTime = ElapsedTime / count;

        // 전체 라인을 구성하는 라인을 그리는 코루틴 호출
        StartCoroutine(this.IncrementLineCoroutine());
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    private IEnumerator IncrementLineCoroutine()
    {
        // 전체 라인을 그린 경우 이벤트 핸들러를 호출하고 끝낸다.
        if(this._currentIndex >= this.Positions.Length - 1)
        {
            if(this.OnComplete != null)
                this.OnComplete.Invoke(this);
            yield break;
        }

        // 현재 시점에서 그려야 할 위치를 얻는다.
        this._lineRenderer.numPositions++;
        var begin = this.Positions[this._currentIndex];
        var end = this.Positions[this._currentIndex + 1];

        // 애니메이션 효과를 주면서(0.01초 단위로) 라인을 그려준다.
        var percent = 0.0f;
        while(true)
        {
            percent = Mathf.Min(1.0f, percent + 0.01f / this._explodeTime);
            this._lineRenderer.SetPosition(this._currentIndex + 1, (end - begin) * percent + begin);
            if(percent >= 1.0f)
                break;

            yield return new WaitForSeconds(0.01f);
        }

        // 다음 코너를 위한 인덱스 증가
        this._currentIndex++;

        // 코루틴을 다시 호출
        StartCoroutine(this.IncrementLineCoroutine());
    }
}