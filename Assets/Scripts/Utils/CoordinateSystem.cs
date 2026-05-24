namespace Babazhou
{
    public static class CoordinateSystem
    {
        public const int GRID_WIDTH = 7;
        public const int GRID_HEIGHT = 5;

        public static bool IsValid(Vector2Int pos)
        {
            return pos.x >= 1 && pos.x <= GRID_WIDTH
                && pos.y >= 1 && pos.y <= GRID_HEIGHT;
        }

        public static int ManhattanDist(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        /// <summary>周围4格（十字）</summary>
        public static List<Vector2Int> GetCross(Vector2Int center)
        {
            var cells = new List<Vector2Int>
            {
                new(center.x,     center.y + 1),
                new(center.x,     center.y - 1),
                new(center.x + 1, center.y),
                new(center.x - 1, center.y),
            };
            cells.RemoveAll(c => !IsValid(c));
            return cells;
        }

        /// <summary>周围8格（九宫格）</summary>
        public static List<Vector2Int> GetSquare(Vector2Int center)
        {
            var cells = GetCross(center);
            cells.Add(new Vector2Int(center.x + 1, center.y + 1));
            cells.Add(new Vector2Int(center.x + 1, center.y - 1));
            cells.Add(new Vector2Int(center.x - 1, center.y + 1));
            cells.Add(new Vector2Int(center.x - 1, center.y - 1));
            cells.RemoveAll(c => !IsValid(c));
            return cells;
        }

        /// <summary>获取攻击方向上的贯穿路径（从攻击者指向目标的方向，由近至远）</summary>
        public static List<Vector2Int> GetPenetrationPath(Vector2Int from, Vector2Int to)
        {
            var path = new List<Vector2Int>();
            int dx = to.x - from.x;
            int dy = to.y - from.y;

            // 仅支持直线方向
            if (dx != 0 && dy != 0) return path;

            int stepX = dx > 0 ? 1 : (dx < 0 ? -1 : 0);
            int stepY = dy > 0 ? 1 : (dy < 0 ? -1 : 0);

            Vector2Int cur = from;
            cur.x += stepX;
            cur.y += stepY;

            while (IsValid(cur))
            {
                path.Add(cur);
                if (cur == to) break;
                cur.x += stepX;
                cur.y += stepY;
            }
            return path;
        }
    }
}