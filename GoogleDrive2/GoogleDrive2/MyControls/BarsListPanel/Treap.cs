using System;
using System.Collections.Generic;

namespace GoogleDrive2.MyControls.BarsListPanel
{
    public partial class Treap<DataType>
    {
        //public Treap()
        //{
        //    Insert(, 0);
        //}
        public event Libraries.Events.MyEventHandler<TreapNode> TreapNodeAdded, TreapNodeRemoved;
        TreapNode root = new TreapNode(default(DataType), 0);
        public static double animationDuration = 500;
        public double itemHeight = 50;
        private static double AnimationOffsetRatio(double timeRatio)
        {
            MyLogger.Assert(timeRatio >= 0);
            return Math.Min(1.0, timeRatio);
        }
        public int Count { get { return TreapNode.GetSize(root) - 1; } }
        //public void DisposeAll(bool animated=true)
        //{
        //    for (int i = Count - 1; i >= 0; i--)
        //    {
        //        Query(i, new Action<TreapNode>(async (o) =>
        //         {
        //             await (o.data as MyDisposable).OnDisposed(animated);
        //         }));
        //    }
        //}
        public List<DataType> ToList()
        {
            var list = new List<DataType>();
            lock (root)
            {
                this.root?.AddToListRecursively(ref list);
            }
            MyLogger.Assert(list.Count >= 1);
            list.RemoveAt(list.Count - 1);
            return list;
        }
        public void AppendAnimation(int l,int r,double offset)
        {
            lock (root)
            {
                TreapNode.Split(root, out TreapNode b, out TreapNode c, r + 1);
                TreapNode.Split(b, out TreapNode a, out b, l);
                b.AppendAnimation(DateTime.Now, offset);
                root = TreapNode.Merge(a, TreapNode.Merge(b, c));
            }
        }
        public void MoveItem(int from,int to)
        {
            MyLogger.Assert(0 <= from && 0 <= to);
            MyLogger.Assert(from < Count && to < Count);
            if (from == to) return;
            var height = QueryItemHeight(from);
            if (from<to)
            {
                var blockH = QueryFinal(to + 1) - QueryFinal(from + 1);
                AppendAnimation(from + 1, to, -height);
                AppendAnimation(from, from, blockH);
            }
            else
            {
                var blockH = QueryFinal(from) - QueryFinal(to);
                AppendAnimation(to, from - 1, height);
                AppendAnimation(from, from, -blockH);
            }
            MoveTreapNode(from, to);
        }
        public void MoveTreapNode(int from,int to)
        {
            lock (root)
            {
                if (from == to) return;
                if (from < to)
                {
                    TreapNode.Split(root, out TreapNode c, out TreapNode d, to+1);
                    TreapNode.Split(c, out TreapNode b, out c, from+1);
                    TreapNode.Split(b, out TreapNode a, out b, from);
                    root = TreapNode.Merge(TreapNode.Merge(a,c), TreapNode.Merge(b, d));
                }
                else
                {
                    TreapNode.Split(root, out TreapNode c, out TreapNode d, from + 1);
                    TreapNode.Split(c, out TreapNode b, out c, from);
                    TreapNode.Split(b, out TreapNode a, out b, to);
                    root = TreapNode.Merge(TreapNode.Merge(a, c), TreapNode.Merge(b, d));
                }
            }
        }
        public double QueryItemHeight(int position)
        {
            MyLogger.Assert(0<=position);
            MyLogger.Assert(position < Count);
            var ans= QueryFinal(position + 1) - QueryFinal(position);
            return ans;
        }
        public TreapNode Insert(DataType data, int position)
        {
            var height = QueryFinal(position);// (position <= Count ?  : position * itemHeight);
            lock (root)
            {
                TreapNode.Split(root, out TreapNode a, out TreapNode b, position);
                if (b != null) b.AppendAnimation(DateTime.Now, itemHeight);
                var o = new TreapNode(data, height);
                root = TreapNode.Merge(a, TreapNode.Merge(o, b));
                TreapNodeAdded?.Invoke(o);
                return o;
            }
        }
        public int QueryLowerBound(double targetY)
        {
            lock (root)
            {
                return (root == null ? 0 : root.QueryLowerBound(targetY));
            }
        }
        private double QueryFinal(int position)
        {
            double ans;
            lock (root)
            {
                TreapNode.Split(root, out TreapNode b, out TreapNode c, position + 1);
                TreapNode.Split(b, out TreapNode a, out b, position);
                ans = b.QueryFinalYOffset();
                root = TreapNode.Merge(a, TreapNode.Merge(b, c));
            }
            return ans;
        }
        public TreapNode Delete(int position)
        {
            lock (root)
            {
                TreapNode.Split(root, out TreapNode b, out TreapNode c, position + 1);
                TreapNode.Split(b, out TreapNode a, out b, position);
                if (c != null) c.AppendAnimation(DateTime.Now, -itemHeight);
                root = TreapNode.Merge(a, c);
                TreapNodeRemoved?.Invoke(b);
                return b;
            }
        }
        public void Delete(TreapNode o)
        {
            Delete(o.GetPosition());
        }
        public double Query(int position, Action<TreapNode> callBack = null)
        {
            double ans;
            TreapNode b;
            lock (root)
            {
                TreapNode.Split(root, out b, out TreapNode c, position + 1);
                TreapNode.Split(b, out TreapNode a, out b, position);
                ans = b.QueryYOffset();
                root = TreapNode.Merge(a, TreapNode.Merge(b, c));
            }
            callBack?.Invoke(b);
            return ans;
        }
        public void ChangeHeight(TreapNode o,double difference)
        {
            lock(root)
            {
                TreapNode.Split(root, out TreapNode a, out TreapNode b, o.GetPosition() + 1);
                if (b != null) b.AppendAnimation(DateTime.Now, difference);
                root = TreapNode.Merge(a, b);
            }
        }
    }
}
