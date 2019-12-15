using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SpriteRenderer))]
public class Fragment : MonoBehaviour {

    public const float MAX_VERTICAL_VELOCITY = 30.0f;
    public const float GRAVITY = -5.0f;

    private Bounds _aliveBounds;
    private Vector2 _velocity;
    private SpriteRenderer _spriteRenderer;

    public float ExplosionForce = 5.0f;

    public Sprite Sprite
    {
        get
        {
            return this._spriteRenderer.sprite;
        }

        set
        {
            this._spriteRenderer.sprite = value;
        }
    }

    public void Awake()
    {
        this._spriteRenderer = this.GetComponent<SpriteRenderer>();

        var cameraAspect = (float)Screen.width / (float)Screen.height;
        var cameraHeight = Camera.main.orthographicSize * 2;
        this._aliveBounds = new Bounds((Vector2)Camera.main.transform.position, new Vector2(cameraHeight * cameraAspect, cameraHeight));
    }

	// Use this for initialization
	void Start () {
	
        this.AddForce(Random.insideUnitCircle * this.ExplosionForce);
	}
	
	// Update is called once per frame
	void Update () {
	
        this.transform.Translate(this._velocity * Time.deltaTime);
        this._velocity.y = Mathf.Min(MAX_VERTICAL_VELOCITY, this._velocity.y + GRAVITY * Time.deltaTime);

        if(this._aliveBounds.Intersects(this._spriteRenderer.bounds) == false)
            Destroy(this.gameObject);
	}

    public void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(this._spriteRenderer.bounds.center, this._spriteRenderer.bounds.size);
    }

    public void AddForce(float x, float y)
    {
        this._velocity.x += x;
        this._velocity.y += y;
    }

    public void AddForce(Vector2 force)
    {
        this.AddForce(force.x, force.y);
    }
}