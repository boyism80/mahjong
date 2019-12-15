using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class DrawLineEvent : UnityEvent<DrawLine> { }

[RequireComponent(typeof(LineRenderer))]
public class DrawLine : MonoBehaviour {

    private LineRenderer _lineRenderer;
    private int _currentIndex;
    private float _explodeTime;

    public Vector3[] Positions;
    public float ElapsedTime = 1.0f;
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
        this._explodeTime = ElapsedTime / count;
        StartCoroutine(this.IncrementLineCoroutine());
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    private IEnumerator IncrementLineCoroutine()
    {
        if(this._currentIndex >= this.Positions.Length - 1)
        {
            if(this.OnComplete != null)
                this.OnComplete.Invoke(this);
            yield break;
        }

        this._lineRenderer.numPositions++;
        var begin = this.Positions[this._currentIndex];
        var end = this.Positions[this._currentIndex + 1];

        var percent = 0.0f;
        while(true)
        {
            percent = Mathf.Min(1.0f, percent + 0.01f / this._explodeTime);
            this._lineRenderer.SetPosition(this._currentIndex + 1, (end - begin) * percent + begin);
            if(percent >= 1.0f)
                break;

            yield return new WaitForSeconds(0.01f);
        }

        this._currentIndex++;
        StartCoroutine(this.IncrementLineCoroutine());
    }
}