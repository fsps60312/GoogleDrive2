using System;
using System.Collections.Generic;
using System.Linq;

namespace GoogleDrive2.MyControls.BarsListPanel
{
    public partial class Treap<DataType>
    {
        public event Libraries.Events.MyEventHandler<TreapNode> TreapNodeAdded, TreapNodeRemoved;
        TreapNode root = new TreapNode(default(DataType), 0);
        public static double animationDuration = 500;
        public double itemHeight = 50;
        public Func<DataType, bool> FilterCondition = null;
        public void UnfilterIfNot()
        {
            lock(root)
            {
                if (FilterCondition == null) return;
                // TODO
                FilterCondition = null;
            }
        }
        public void Filter(Func<DataType,bool>condition)
        {
            lock(root)
            {
                UnfilterIfNot();
                if (condition == null) return;
                // TODO
                FilterCondition = condition;
            }
        }
        public void DoAtomic(Action action)
        {
            lock (root) action.Invoke();
        }
        public void Sort(Comparison<DataType> comparer)
        {
            lock (root)
            {
                var filterCondition = FilterCondition;
                UnfilterIfNot();
                Dictionary<TreapNode, double> heights = new Dictionary<TreapNode, double>();
                List<TreapNode> list = new List<TreapNode>();
                root?.ForEach((o) => list.Add(o));
                for (int i = 1; i < list.Count; i++) heights[list[i - 1]] = list[i].QueryFinalYOffset() - list[i - 1].QueryFinalYOffset();
                var empty = list.Last(); list.RemoveAt(list.Count - 1);
                empty.CutArmsAndLegs();
                empty.AppendAnimation(DateTime.Now, -empty.QueryFinalYOffset());
                root = empty;
                list.Sort(new Comparison<TreapNode>((a, b) => { return comparer(a.data, b.data); }));
                for (int i = 0; i < list.Count; i++)
                {
                    var o = list[i];
                    o.CutArmsAndLegs();
                    Insert(o, heights[o], i, true);
                }
                Filter(filterCondition);
                //MyLogger.Debug("sorted");
            }
        }
        private static double AnimationOffsetRatio(double timeRatio)
        {
            if(timeRatio<0)
            {
                MyLogger.LogError($"timeRatio={timeRatio}");
                timeRatio = 0;
            }
            MyLogger.Assert(timeRatio >= 0);
            return Math.Min(1.0, timeRatio);
        }
        public int Count { get { return TreapNode.GetSize(root) - 1; } }
        public List<DataType> ToList()
        {
            lock (root)
            {
                var list = new List<DataType>();
                this.root?.ForEach((o) => list.Add(o.data));
                MyLogger.Assert(list.Count >= 1);
                list.RemoveAt(list.Count - 1);
                return list;
            }
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
            lock (root)
            {
                MyLogger.Assert(0 <= from && 0 <= to);
                MyLogger.Assert(from < Count && to < Count);
                if (from == to) return;
                var height = QueryItemHeight(from);
                if (from < to)
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
            lock (root)
            {
                MyLogger.Assert(0 <= position);
                MyLogger.Assert(position < Count);
                var ans = QueryFinal(position + 1) - QueryFinal(position);
                return ans;
            }
        }
        public void Insert(TreapNode o, double height, int position)
        {
            lock(root)
            {
                Insert(o, height, position, false);
            }
        }
        private void Insert(TreapNode o, double height, int position, bool suppressAddEvent)
        {
            lock (root)
            {
                if (o == null) return;
                MyLogger.Assert(0 <= position && position <= Count);
                var y = QueryFinal(position);
                TreapNode.Split(root, out TreapNode a, out TreapNode b, position);
                MyLogger.Assert(b != null);
                o.AppendAnimation(DateTime.Now, y - o.Front().QueryFinalYOffset());
                if (!suppressAddEvent) o.ForEach((u) => TreapNodeAdded?.Invoke(u));
                b.AppendAnimation(DateTime.Now, height);
                root = TreapNode.Merge(a, TreapNode.Merge(o, b));
            }
        }
        public TreapNode Insert(DataType data, int position)
        {
            lock (root)
            {
                var o = new TreapNode(data, QueryFinal(position));
                Insert(o, itemHeight, position);
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
            lock (root)
            {
                double ans;
                TreapNode.Split(root, out TreapNode b, out TreapNode c, position + 1);
                TreapNode.Split(b, out TreapNode a, out b, position);
                ans = b.QueryFinalYOffset();
                root = TreapNode.Merge(a, TreapNode.Merge(b, c));
                return ans;
            }
        }
        public Tuple<TreapNode,double> Cut(int l,int r)
        {
            lock (root)
            {
                MyLogger.Assert(0 <= l && l <= r && r < Count);
                var yDiff = QueryFinal(r + 1) - QueryFinal(l);
                TreapNode.Split(root, out TreapNode b, out TreapNode c, r + 1);
                TreapNode.Split(b, out TreapNode a, out b, l);
                if (c != null) c.AppendAnimation(DateTime.Now, -yDiff);
                root = TreapNode.Merge(a, c);
                b?.ForEach((o) => TreapNodeRemoved?.Invoke(o));
                return new Tuple<TreapNode, double>(b, yDiff);
            }
        }
        public Tuple<TreapNode, double> Cut(int position)
        {
            lock (root)
            {
                var ans = Cut(position, position);
                MyLogger.Assert(TreapNode.GetSize(ans.Item1) == 1);
                return ans;
            }
        }
        public void Delete(TreapNode o)
        {
            lock (root)
            {
                Cut(o.GetPosition());
            }
        }
        public double Query(int position, Action<TreapNode> callBack = null)
        {
            lock (root)
            {
                double ans;
                TreapNode b;
                TreapNode.Split(root, out b, out TreapNode c, position + 1);
                TreapNode.Split(b, out TreapNode a, out b, position);
                ans = b.QueryYOffset();
                root = TreapNode.Merge(a, TreapNode.Merge(b, c));
                callBack?.Invoke(b);
                return ans;
            }
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
