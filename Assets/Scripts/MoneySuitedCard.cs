using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(BoxCollider2D))]
public class MoneySuitedCard : MonoBehaviour {

    private SpriteRenderer _spriteRenderer;
    private Animator _animator;
    private Vector2 _size;

    public Fragment Fragment;

    public BoxCollider2D BoxCollider2D { get; private set; }
    public Sprite Sprite
    {
        get
        {
            if(this._spriteRenderer == null)
                return null;

            return this._spriteRenderer.sprite;
        }

        set
        {
            if(this._spriteRenderer == null)
                return;

            if(value == null)
                return;

            this._spriteRenderer.sprite = value;
            //this.BoxCollider2D.offset = value.bounds.center;
            //this.BoxCollider2D.size = value.bounds.size;
        }
    }

    public Vector2 Size
    {
        get
        {
            return this._size;
        }

        set
        {
            this._size = value;
            if(this._spriteRenderer != null && this.Sprite != null)
                this._spriteRenderer.gameObject.transform.localScale = new Vector2(value.x / this.Sprite.bounds.size.x, value.y / this.Sprite.bounds.size.y);

            if(this.BoxCollider2D != null)
                this.BoxCollider2D.size = value;
        }
    }

    public int Row { get; private set; }
    public int Column { get; private set; }

    public Point Offset
    {
        get
        {
            return new Point(this.Row, this.Column);
        }

        private set
        {
            this.Row = value.X;
            this.Column = value.Y;
        }
    }

    public int Index { get; private set; }
    public int MatchingIndex { get; private set; }

    public bool Selected
    {
        get
        {
            return this._animator.GetBool("Selected");
        }
        set
        {
            this._animator.SetBool("Selected", value);
            if(value)
                this._spriteRenderer.sortingLayerName = "Money-suited selected card";
            else
                this._spriteRenderer.sortingLayerName = "Money-suited card";
        }
    }

    public void Awake()
    {
        this.BoxCollider2D = this.GetComponent<BoxCollider2D>();
        this._spriteRenderer = this.GetComponentInChildren<SpriteRenderer>();
        this._animator = this.GetComponent<Animator>();
    }

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    private Fragment ExtractFragment(Rect area)
    {
        var texture = this.Sprite.texture;
        var splited = Sprite.Create(texture, area, Vector2.zero);

        var ret = Instantiate<Fragment>(this.Fragment);
        ret.Sprite = splited;
        ret.transform.position = this.transform.position;
        ret.transform.localScale = this._spriteRenderer.transform.localScale;

        return ret;
    }

    private Fragment[] ExtractExplosionFragments(int row, int col)
    {
        var list = new List<Fragment>();
        var size = new Vector2(this.Sprite.texture.width / (float)row, Sprite.texture.height / (float)col);

        for(var c = 0; c < col; c++)
        {
            for(var r = 0; r < row; r++)
            {
                var area = new Rect(r * size.x, c * size.y, size.x, size.y);
                list.Add(this.ExtractFragment(area));
            }
        }

        return list.ToArray();
    }

    public MoneySuitedCard Initialize(int index, int matchingIndex, int row, int col, Sprite sprite, Vector2 size)
    {
        this.Index = index;
        this.MatchingIndex = matchingIndex;
        this.Sprite = sprite;
        this.Size = size;
        this.Row = row;
        this.Column = col;

        return this;
    }

    public bool IsMatchable(MoneySuitedCard right)
    {
        return this.MatchingIndex == right.MatchingIndex;
    }

    public void Swap(MoneySuitedCard right)
    {
        var position = this.transform.position;
        var offset = this.Offset;

        this.transform.position = right.transform.position;
        this.Offset = right.Offset;

        right.transform.position = position;
        right.Offset = offset;
    }

    public void Explode()
    {
        if(this.gameObject.activeSelf == false)
            return;

        this.ExtractExplosionFragments(3, 3);
        this.gameObject.SetActive(false);
    }
}