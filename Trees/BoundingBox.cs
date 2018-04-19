using System;

namespace Trees
{
    public class BoundingBox
    {
        internal BoundingBox()
        {
        }

        public BoundingBox(int x1, int y1, int x2, int y2)
        {
            X1 = x1;
            Y1 = y1;
            X2 = x2;
            Y2 = y2;
        }

        public int X1 { get; private set; } // 0
        public int Y1 { get; private set; } // 1
        public int X2 { get; private set; } // 2
        public int Y2 { get; private set; } // 3

        internal int Area { get { return (X2 - X1) * (Y2 - Y1); } }
        internal int Margin { get { return (X2 - X1) + (Y2 - Y1); } }

        internal void Extend(BoundingBox by)
        {
            X1 = Math.Min(X1, by.X1);
            Y1 = Math.Min(Y1, by.Y1);
            X2 = Math.Max(X2, by.X2);
            Y2 = Math.Max(Y2, by.Y2);
        }

        public override string ToString()
        {
            return $"{X1},{Y1} - {X2},{Y2}";
        }

        internal bool Intersects(BoundingBox bbox)
        {
            return bbox.X1 <= X2 && bbox.Y1 <= Y2 && bbox.X2 >= X1 && bbox.Y2 >= Y1;
        }

        internal bool Contains(BoundingBox b)
        {
            return X1 <= b.X1 && Y1 <= b.Y1 && b.X2 <= X2 && b.Y2 <= Y2;
        }
    }
}