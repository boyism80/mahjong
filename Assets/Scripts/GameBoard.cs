using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class GameBoard : MonoBehaviour {

    private MoneySuitedCard[,] _moneySuitedCardTable;
    private int _row, _column;
    private BlockTable _blockTable;
    private MoneySuitedCard _selected;

    public Sprite[] Sprites;
    public Vector2 MoneySuitedCardSize;
    public MoneySuitedCard MoneySuitedCard;
    public int InitRows = 6, InitColumn = 4;
    public DrawLine DrawLine;

    public int Rows
    {
        get
        {
            return this._row;
        }
        private set
        {
            this._row = (value % 2 == 0 ? value : value + 1);
        }
    }

    public int Columns
    {
        get
        {
            return this._column;
        }

        private set
        {
            this._column = value;
        }
    }

    public MoneySuitedCard Selected
    {
        get
        {
            return this._selected;
        }

        set
        {
            if(this._selected == value)
                return;

            if(this._selected != null)
                this._selected.Selected = false;

            this._selected = value;
            if(this._selected != null)
                this._selected.Selected = true;
        }
    }

    public MoneySuitedCard[] Exists
    {
        get
        {
            var list = new List<MoneySuitedCard>();

            for(var col = 0; col < this.Columns; col++)
            {
                for(var row = 0; row < this.Rows; row++)
                {
                    if(this._moneySuitedCardTable[col, row].gameObject.activeSelf)
                        list.Add(this._moneySuitedCardTable[col, row]);
                }
            }
            return list.ToArray();
        }
    }

    private bool IsContinuable
    {
        get
        {
            foreach (var src in this.Exists)
            {
                foreach (var dest in this.GetExistsFromMatchingType(src.MatchingIndex))
                {
                    if(src.Equals(dest))
                        continue;

                    if (this._blockTable.GetRoute(src.Offset + 1, dest.Offset + 1) != null)
                        return true;
                }
            }
            return false;
        }
    }

    private bool IsClear
    {
        get
        {
            return this.Exists.Length == 0;
        }
    }

    public void Awake()
    {
        this.Rows = this.InitRows;
        this.Columns = this.InitColumn;
    }


	// Use this for initialization
	void Start () {
	
        this.Setup();
	}
	
	// Update is called once per frame
	void Update () {
	
        if(Input.GetButtonDown("Fire1") == false)
            return;

        var dest = this.Select();
        if(dest == null)
        {

        }
        else if(this.Selected == null)
        {
            this.Selected = dest;
        }
        else
        {
            var route = this.Remove(this.Selected, dest);
            if(route == null)
            {
                this.Selected = dest;
            }
            else if(this.IsClear)
            {
                this.Rows++;
                this.Columns++;
                this.Setup();
            }
            else if(this.IsContinuable == false)
            {
                this.Shuffle();
            }
            else
            {
                if(this.DrawLine != null)
                {
                    var line = Instantiate<DrawLine>(this.DrawLine);
                    line.OnComplete.AddListener(this.OnDrawLineComplete);
                    var points = new List<Vector3>();
                    foreach(var r in route.Reverse<Point>())
                        points.Add(this.GetMoneySuitedCardPosition(r.X - 1, r.Y - 1));

                    line.Positions = points.ToArray();
                }
                else
                { }
                //line.numPositions = route.Count;
                //foreach (var r in route.Reverse<Point>())
                //    line.SetPosition(index++, this.GetMoneySuitedCardPosition(r.X - 1, r.Y - 1));
            }
        }
	}

    private void OnDrawLineComplete(DrawLine drawLine)
    {
        Destroy(drawLine.gameObject);
    }

    public void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        for(var col = 0; col < this.Columns; col++)
        {
            for(var row = 0; row < this.Rows; row++)
                Gizmos.DrawWireCube(this.GetMoneySuitedCardPosition(row, col), this.MoneySuitedCardSize);
        }
    }

    private Stack<Point> Remove(MoneySuitedCard card1, MoneySuitedCard card2)
    {
        if(card1.MatchingIndex != card2.MatchingIndex)
            return null;

        var route = this._blockTable.GetRoute(card1.Offset + 1, card2.Offset + 1);
        if(route == null)
            return null;

        this._blockTable.SetDontMove(card1.Offset + 1, false);
        this._blockTable.SetDontMove(card2.Offset + 1, false);

        card1.Explode();
        card2.Explode();

        this.Selected = null;
        return route;
    }

    private MoneySuitedCard SetMoneySuitedCard(int index, int matchingIndex, int row, int col)
    {
        try
        {
            if(this._moneySuitedCardTable[col, row] == null)
                this._moneySuitedCardTable[col, row] = Instantiate<MoneySuitedCard>(this.MoneySuitedCard);

            _moneySuitedCardTable[col, row].Initialize(index, matchingIndex, row, col, this.Sprites[matchingIndex], this.MoneySuitedCardSize);
            _moneySuitedCardTable[col, row].transform.position = this.GetMoneySuitedCardPosition(row, col);
            return _moneySuitedCardTable[col, row];
        }
        catch(IndexOutOfRangeException)
        {
            return null;
        }
    }

    public Vector2 GetMoneySuitedCardPosition(int row, int col)
    {
        return new Vector2(-this.Rows / 2 + row + this.MoneySuitedCardSize.x / 2.0f,
                           this.Columns / 2 - col - this.MoneySuitedCardSize.y / 2.0f);
    }

    public void Clear()
    {
        if(this._moneySuitedCardTable == null)
            return;

        for(int col = 0; col < this._moneySuitedCardTable.GetLength(0); col++)
        {
            for(int row = 0; row < this._moneySuitedCardTable.GetLength(1); row++)
                Destroy(this._moneySuitedCardTable[col, row].gameObject);
        }

        this._moneySuitedCardTable = null;
        this.Selected = null;
    }

    public void Resize(int row, int col)
    {
        this.Clear();
        this.Rows = row;
        this.Columns = col;
        this._moneySuitedCardTable = new MoneySuitedCard[this.Columns, this.Rows];

        this._blockTable = new BlockTable(this.Rows, this.Columns);
    }

    public void Setup()
    {
        this.Resize(this.Rows, this.Columns);

        var requireCardCount = this.Rows * this.Columns;
        for(var col = 0; col < this.Columns; col++)
        {
            for(var row = 0; row < this.Rows; row += 2)
            {
                var index = (col * this.Rows) + row;
                var matchingIndex = index % (Mathf.Min(this.Sprites.Length, requireCardCount / 2));
                this.SetMoneySuitedCard(index + 0, matchingIndex, row, col);
                this.SetMoneySuitedCard(index + 1, matchingIndex, row + 1, col);
            }
        }

        this.Shuffle();
    }

    private MoneySuitedCard[] GetExistsFromMatchingType(int matchingIndex)
    {
        var list = new List<MoneySuitedCard>();
        foreach(var card in this.Exists)
        {
            if(card.MatchingIndex == matchingIndex)
                list.Add(card);
        }

        return list.ToArray();
    }

    private void Swap(ref MoneySuitedCard card1, ref MoneySuitedCard card2)
    {
        var temp = card1;
        card1 = card2;
        card2 = temp;

        card1.Swap(card2);
    }

    public void Shuffle()
    {
        this.Selected = null;

        var exists = this.Exists;
        for(var i = 0; i < exists.Length; i++)
        {
            var randomIndex = UnityEngine.Random.Range(0, exists.Length);
            var src = exists[i];
            var dest = exists[randomIndex];
            this.Swap(ref src, ref dest);
        }

        if (this.IsContinuable == false)
            this.Shuffle();
    }

    public MoneySuitedCard Select()
    {
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        var hit = Physics2D.GetRayIntersection(ray, Mathf.Infinity);
        if(hit == false)
            return null;

        var moneySuitedCard = hit.transform.GetComponent<MoneySuitedCard>();
        if(moneySuitedCard == null)
            return null;

        return moneySuitedCard;
    }
}