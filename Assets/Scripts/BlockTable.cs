using UnityEngine;
using System.Collections.Generic;

public class Point
{
    public int X { get; set; }
    public int Y { get; set; }

    public Point(int x, int y)
    {
        this.X = x;
        this.Y = y;
    }

    public static implicit operator Vector2 (Point point)
    {
        return new Vector2(point.X, point.Y);
    }

    public static explicit operator Point (Vector2 vector2)
    {
        return new Point((int)vector2.x, (int)vector2.y);
    }

    public static Point operator + (Point p, int val)
    {
        return new Point(p.X + val, p.Y + val);
    }

    public override bool Equals(object right)
    {
        var p = right as Point;
        if(p == null)
            return base.Equals(right);
        return (this.X == p.X && this.Y == p.Y);
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    public override string ToString()
    {
        return string.Format("{0}, {1}", this.X, this.Y);
    }
}

class Block
{
    public enum Direction { None, Left, Top, Right, Bottom }

    private int _changeDirectionChance = 3;
    private Point _position;
    private Direction _direction;
    public Block(Point position)
    {
        this._position = position;
        this._direction = Direction.None;
    }

    public Block(Point position, int changeDirectionChance) : this(position)
    {
        this._changeDirectionChance = changeDirectionChance;
    }

    public Block(Block parent, Direction direction, bool[,] dontMoveTable) : this(parent._position)
    {
        this._changeDirectionChance = parent._changeDirectionChance;
        this._direction = parent._direction;
        this.Move(direction, dontMoveTable);
    }

    public Point GetNextPosition(Direction direction)
    {
        var ret = new Point(this._position.X, this._position.Y);

        switch (direction)
        {
            case Direction.Left:
                ret.X--;
                break;

            case Direction.Right:
                ret.X++;
                break;

            case Direction.Top:
                ret.Y--;
                break;

            case Direction.Bottom:
                ret.Y++;
                break;
        }

        return ret;
    }

    public bool Movable(Direction direction, bool[,] dontMoveTable)
    {
        var nextPosition = this.GetNextPosition(direction);

        if (this._direction != direction && this._changeDirectionChance == 0)
            return false;

        if (nextPosition.X < 0)
            return false;

        if (nextPosition.X > dontMoveTable.GetLength(1) - 1)
            return false;

        if (nextPosition.Y < 0)
            return false;

        if (nextPosition.Y > dontMoveTable.GetLength(0) - 1)
            return false;

        if (dontMoveTable[nextPosition.Y, nextPosition.X] == true)
            return false;

        return true;
    }

    public List<Direction> MovableDirections(Point dest, bool[,] dontMoveTable)
    {
        var ret = new List<Direction>();

        if (this.Movable(Direction.Left, dontMoveTable))
            ret.Add(Direction.Left);

        if (this.Movable(Direction.Right, dontMoveTable))
            ret.Add(Direction.Right);

        if (this.Movable(Direction.Top, dontMoveTable))
            ret.Add(Direction.Top);

        if (this.Movable(Direction.Bottom, dontMoveTable))
            ret.Add(Direction.Bottom);

        ret.Sort(delegate (Direction val1, Direction val2)
        {
            var position1 = (Vector2)this.GetNextPosition(val1);
            var position2 = (Vector2)this.GetNextPosition(val2);

            var sqrDistance1 = (dest - position1).sqrMagnitude;
            var sqrDistance2 = (dest - position2).sqrMagnitude;

            return sqrDistance1.CompareTo(sqrDistance2);
        });

        return ret;
    }

    public bool Move(Direction direction, bool[,] dontMoveTable)
    {
        if (this.Movable(direction, dontMoveTable) == false)
            return false;

        dontMoveTable[this._position.Y, this._position.X] = true;
        this._position = this.GetNextPosition(direction);

        if (this._direction != direction)
        {
            this._direction = direction;
            this._changeDirectionChance--;
        }

        return true;
    }

    private Stack<Point> GetRouteRecursive(Point dest, bool[,] dontMoveTable, ref Stack<Point> route)
    {
        if (this._position.Equals(dest))
            return null;

        route.Push(new Point(this._position.X, this._position.Y));
        var movableDirections = this.MovableDirections(dest, dontMoveTable);
        if(movableDirections.Count == 0) // 더 이상 갈 곳이 없으면 null을 리턴
            return null;

        foreach (var direction in movableDirections)
        {
            var dontBlockTableClone = dontMoveTable.Clone() as bool[,];
            var blockClone = new Block(this, direction, dontBlockTableClone);
            if (blockClone._position.Equals(dest))
            {
                route.Push(new Point(dest.X, dest.Y));
                return route;
            }

            var ret = blockClone.GetRouteRecursive(dest, dontBlockTableClone, ref route);
            if (ret == null)
            {
                route.Pop();
                continue;
            }

            return ret;
        }

        return null;
    }

    public Stack<Point> GetRoute(Point dest, bool[,] dontMoveTable)
    {
        var route = new Stack<Point>();
        var dontMoveTableClone = dontMoveTable.Clone() as bool[,];
        dontMoveTableClone[dest.Y, dest.X] = false;
        return this.GetRouteRecursive(dest, dontMoveTableClone, ref route);
    }
}

class BlockTable
{
    private bool[,] _dontMoveTable;

    public const int MAX_CHANGE_DIRECTION_CHANCE = 3;

    public BlockTable(int row, int col)
    {
        this.Resize(row, col);
    }

    private bool IsContainRange(Point position)
    {
        if (position.X < 0)
            return false;

        if (position.X > this._dontMoveTable.GetLength(1) - 1)
            return false;

        if (position.Y < 0)
            return false;

        if (position.Y > this._dontMoveTable.GetLength(0) - 1)
            return false;

        return true;
    }

    private bool IsContainRange(int row, int col)
    {
        return this.IsContainRange(new Point(row, col));
    }

    /// <summary>
    /// 테이블의 크기를 설정한다. 실제 생성되는 테이블은 상하좌우로 1열씩 여유공간을 가진다.
    /// 여유공간이 아닌 본래의 크기는 기본적으로 이동불가 지역이다.
    /// 예) rows = 3, columns = 3으로 한다면 5x5의 테이블이 생성되고 사용되는 공간(3x3)은 이동불가지역이 됨
    /// </summary>
    /// <param name="rows"></param>
    /// <param name="columns"></param>
    public void Resize(int rows, int columns)
    {
        this._dontMoveTable = new bool[columns + 2, rows + 2];
        for(var col = 1; col < columns + 1; col++)
        {
            for(var row = 1; row < rows + 1; row++)
            {
                this._dontMoveTable[col, row] = true;
            }
        }
    }

    /// <summary>
    /// 이동할 수 없는 지역 여부를 설정한다. one-base로 설정해야 정확한 값을 얻는다.
    /// 예) 가장 왼쪽 상단의 값을 변경한다면 row = 1, col = 1
    /// </summary>
    /// <param name="row">행</param>
    /// <param name="col">열</param>
    /// <param name="value">설정할 값</param>
    public void SetDontMove(int row, int col, bool value)
    {
        if (this.IsContainRange(row, col) == false)
            return;

        this._dontMoveTable[col, row] = value;
    }

    /// <summary>
    /// 이동할 수 없는 지역 여부를 설정한다. one-base로 설정해야 정확한 값을 얻는다.
    /// 예) 가장 왼쪽 상단의 값을 변경한다면 offset = (1, 1)
    /// </summary>
    /// <param name="offset">설정할 지역</param>
    /// <param name="value">설정할 값</param>
    public void SetDontMove(Point offset, bool value)
    {
        this.SetDontMove(offset.X, offset.Y, value);
    }

    /// <summary>
    /// 시작점부터 도착점까지의 경로를 얻는다. 정확한 값을 얻기 위해서는 one-base로 계산해야 한다.
    /// 예) 가장 왼쪽 상단의 카드를 시작점으로 한다면 src는 (1, 1)이 된다.
    /// </summary>
    /// <param name="src">시작점</param>
    /// <param name="dest">도착점</param>
    /// <returns>성공시 경로를 저장한 큐, 실패시 null을 리턴.</returns>
    public Stack<Point> GetRoute(Point src, Point dest)
    {
        if (this._dontMoveTable == null)
            return null;

        if (this.IsContainRange(src) == false || this.IsContainRange(dest) == false)
            return null;

        return new Block(src).GetRoute(dest, this._dontMoveTable);
    }
}