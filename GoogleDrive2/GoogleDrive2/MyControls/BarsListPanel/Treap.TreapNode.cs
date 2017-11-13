﻿using System;
using System.Collections.Generic;

namespace GoogleDrive2.MyControls.BarsListPanel
{
    class TreapNodeStatistics
    {
        static volatile int InstanceCount = 0;
        public static event Libraries.Events.MyEventHandler<int> InstanceCountChanged;
        public static void AddInstanceCount(int value) { System.Threading.Interlocked.Add(ref InstanceCount, value); InstanceCountChanged?.Invoke(InstanceCount); }
    }
    public partial class Treap<DataType>
    {
        public class TreapNodePrototype
        {
            public DataType data;
            ~TreapNodePrototype() { TreapNodeStatistics.AddInstanceCount(-1); }
            public TreapNodePrototype() { TreapNodeStatistics.AddInstanceCount(1); }
        }
        private class TreapNode:TreapNodePrototype
        {
            //private TreapNode<DataType1> l
            //{
            //    get { return _l; }
            //    set
            //    {
            //        if (_l != null) _l.parent = null;
            //        _l = value;
            //        if (_l != null) _l.parent = this;
            //    }
            //}
            //private TreapNode<DataType1> r
            //{
            //    get { return _r; }
            //    set
            //    {
            //        if (_r != null) _r.parent = null;
            //        _r = value;
            //        if (_r != null) _r.parent = this;
            //    }
            //}
            private static volatile Random rand = new Random();
            private TreapNode l = null, r = null, parent = null;
            private double yOffset, yOffsetTag = 0;
            private DateTime animationStartTime = DateTime.MinValue, animationStartTimeTag = DateTime.MinValue;
            private double animationOffset, animationOffsetTag;
            private int size = 1;
            public TreapNode(DataType _data, double _yOffset)
            {
                data = _data;
                yOffset = _yOffset;
            }
            private void Maintain()
            {
                size = GetSize(l) + 1 + GetSize(r);
            }
            public void CutArmsAndLegs()
            {
                l = r = parent = null;
                size = 1;
            }
            private double AppendAnimation(ref DateTime time1, ref double offset1, DateTime time2, double offset2)
            {
                if (time1 == DateTime.MinValue)
                {
                    time1 = time2;
                    offset1 = offset2;
                    return 0;
                }
                else
                {
                    double ratio = AnimationOffsetRatio((time2 - time1).TotalMilliseconds / animationDuration);
                    double moved = offset1 * ratio;
                    time1 = time2;
                    offset1 = (offset1 - moved) + offset2;
                    return moved;
                }
            }
            private void PutDown(TreapNode child)
            {
                if (child == null) return;
                child.yOffset += this.yOffsetTag;
                child.yOffsetTag += this.yOffsetTag;
                //this.yOffsetTag = 0;
                if (animationStartTimeTag != DateTime.MinValue)
                {
                    child.AppendAnimation(animationStartTimeTag, animationOffsetTag);
                    //animationStartTimeTag = DateTime.MinValue;
                }
            }
            private void PutDown()
            {
                PutDown(l);
                PutDown(r);
                animationStartTimeTag = DateTime.MinValue;
                yOffsetTag = 0;
            }
            public TreapNode Front()
            {
                PutDown();
                return l == null ? this : l.Front();
            }
            public TreapNode Back()
            {
                PutDown();
                return r == null ? this : r.Back();
            }
            public void ForEach(Action<TreapNode> callBack)
            {
                PutDown();
                this.l?.ForEach(callBack);
                callBack(this);
                this.r?.ForEach(callBack);
            }
            public int QueryLowerBound(double targetY)
            {
                PutDown();
                if (this.QueryYOffset() >= targetY) return (l == null ? 0 : l.QueryLowerBound(targetY));
                else return GetSize(l) + (r == null ? 1 : r.QueryLowerBound(targetY) + 1);
            }
            public double QueryFinalYOffset()
            {
                if (animationStartTime == DateTime.MinValue)
                {
                    return yOffset;
                }
                else
                {
                    return yOffset + animationOffset;
                }
            }
            public double QueryYOffset()
            {
                if (animationStartTime == DateTime.MinValue)
                {
                    return yOffset;
                }
                else
                {
                    return yOffset + animationOffset * AnimationOffsetRatio((DateTime.Now - animationStartTime).TotalMilliseconds / animationDuration);
                }
            }
            public void AppendAnimation(DateTime time, double offset)
            {
                {
                    double moved = AppendAnimation(ref animationStartTime, ref animationOffset, time, offset);
                    yOffset += moved;
                }
                {
                    double moved = AppendAnimation(ref animationStartTimeTag, ref animationOffsetTag, time, offset);
                    yOffsetTag += moved;
                }
            }
            public int GetPosition()
            {
                int position = GetSize(this.l);
                var o = this;
                for (; o.parent != null; o = o.parent)
                {
                    if (o.parent.r == o) position += GetSize(o.parent.l) + 1;
                }
                return position;
            }
            public static int GetSize(TreapNode o) { return o == null ? 0 : o.size; }
            public static TreapNode Merge(TreapNode a, TreapNode b)
            {
                if (a == null || b == null) return a ?? b;
                if (rand.NextDouble() < (double)GetSize(a) / (GetSize(a) + GetSize(b)))
                {
                    a.PutDown();
                    if (a.r != null) a.r.parent = null;
                    a.r = Merge(a.r, b);
                    a.r.parent = a;
                    a.Maintain();
                    return a;
                }
                else
                {
                    b.PutDown();
                    if (b.l != null) b.l.parent = null;
                    b.l = Merge(a, b.l);
                    b.l.parent = b;
                    b.Maintain();
                    return b;
                }
            }
            public static void Split(TreapNode o, out TreapNode a, out TreapNode b, int position)
            {
                if(position<0)
                {
                    MyLogger.LogError($"TreapNode: position = {position}");
                    position = 0;
                }
                if(position>GetSize(o))
                {
                    MyLogger.LogError($"TreapNode: position = {position}");
                    position = GetSize(o);
                }
                //MyLogger.Assert(0 <= position && position <= GetSize(o));
                if (o == null) { a = b = null; return; }
                o.PutDown();
                if (position <= GetSize(o.l))
                {
                    b = o;
                    if (b.l != null) b.l.parent = null;
                    Split(b.l, out a,out b.l, position);
                    if (b.l != null) b.l.parent = b;
                    b.Maintain();
                }
                else
                {
                    a = o;
                    if (a.r != null) a.r.parent = null;
                    Split(a.r, out a.r, out b, position - GetSize(a.l) - 1);
                    if (a.r != null) a.r.parent = a;
                    a.Maintain();
                }
            }
        }
    }
}
