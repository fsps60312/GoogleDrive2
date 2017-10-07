using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using Xamarin.Forms;

namespace GoogleDrive2.MyControls.NewBarsListPanel
{
    public abstract class MyDisposable
    {
        public delegate void MyDisposableEventHandler();
        public event MyDisposableEventHandler Disposed;
        public Func<System.Threading.Tasks.Task> Disposing = null;
        public void UnregisterDisposingEvents() { Disposing = null; }
        public delegate void HeightChangedEventHandler(double difference);
        public event HeightChangedEventHandler HeightChanged;
        public void OnHeightChanged(double difference)
        {
            HeightChanged?.Invoke(difference);
        }
        public async System.Threading.Tasks.Task OnDisposed(bool animated = true)
        {
            if (animated && Disposing != null) await Disposing();
            Disposed?.Invoke();
            Disposing = null; Disposed = null;
        }
    }
    public interface IDataBindedView<DataType> where DataType : MyDisposable
    {
        event GoogleDrive2.Libraries.Events.MyEventHandler<IDataBindedView<DataType>> Appeared;
        Func<Task> Disappearing { get; set; }
        void Reset(DataType data);
    }
    class BarsListPanel<GenericView, DataType> : MyContentView where DataType : MyDisposable where GenericView :View, IDataBindedView<DataType>, new()
    {
        public delegate void DataEventHandler(DataType data);
        public event DataEventHandler DataInserted, DataRemoved;
        private void OnDataInserted(DataType data) { DataInserted?.Invoke(data); }
        private void OnDataRemoved(DataType data) { DataRemoved?.Invoke(data); }
        protected double AnimationDuration { get { return Treap<DataType>.animationDuration; } }
        protected double ItemHeight
        {
            get { return treap.itemHeight; }
            set { treap.itemHeight = value; }
        }
        public Treap<DataType> Treap
        {
            get { return treap; }
        }
        Treap<DataType> treap = new Treap<DataType>();
        MyAbsoluteLayout ALmain;
        protected MyScrollView SVmain;
        MyLabel LBend;
        private delegate void TreapLayoutChangedEventHandler();
        private event TreapLayoutChangedEventHandler TreapLayoutChanged;
        private void OnTreapLayoutChanged() { TreapLayoutChanged?.Invoke(); }
        protected void ChangeHeight(Treap<DataType>.TreapNode node, double difference)
        {
            treap.ChangeHeight(node, difference);
        }
        public async Task ClearAsync()
        {
            if (treap.Count > 0)
            {
                int l = UponIndex(), r = DownIndex();
                var count = treap.Count;
                List<DataType> visible = new List<DataType>(), invisible = new List<DataType>();
                for (int i = count - 1; i >= 0; i--)
                {
                    treap.Query(i, new Action<Treap<DataType>.TreapNode>((o) =>
                    {
                        if (l <= i && i <= r) visible.Add(o.data);
                        else invisible.Add(o.data);
                        //if (i < l || r < i) treap.Delete(o);
                    }));
                }
                Libraries.MySemaphore semaphore = new Libraries.MySemaphore(-count + 1);
                foreach (var o in invisible)
                {
                    await (o as MyDisposable).OnDisposed(false);
                    semaphore.Release();
                }
                var t = Task.WhenAll(visible.Select(async (o) =>
                {
                    await (o as MyDisposable).OnDisposed(false);
                    semaphore.Release();
                }));

                await t;
                //foreach (var o in visible)
                //{
                //    await (o as MyDisposable).OnDisposed(true);
                //    semaphore.Release();
                //}
                await semaphore.WaitAsync();
            }
        }
        public void Clear()
        {
            if (treap.Count > 0)
            {
                int l = UponIndex(), r = DownIndex();
                for (int i = treap.Count - 1; i >= 0; i--)
                {
                    treap.Query(i, new Action<Treap<DataType>.TreapNode>(async (o) =>
                    {
                        await (o.data as MyDisposable).OnDisposed(false && l <= i && i <= r);
                    }));
                }
            }
        }
        private void RegisterData(Treap<DataType>.TreapNode o, DataType data)
        {
            MyDisposable.MyDisposableEventHandler disposedEventHandler = null;
            MyDisposable.HeightChangedEventHandler heightChangedEventHandler = null;
            disposedEventHandler = new MyDisposable.MyDisposableEventHandler(() =>
            {
                data.Disposed -= disposedEventHandler;
                data.HeightChanged -= heightChangedEventHandler;
                treap.Delete(o);
                OnTreapLayoutChanged();
                OnDataRemoved(data);
            });
            heightChangedEventHandler = new MyDisposable.HeightChangedEventHandler((difference) =>
            {
                treap.ChangeHeight(o, difference);
                OnTreapLayoutChanged();
            });
            data.Disposed += disposedEventHandler;
            data.HeightChanged += heightChangedEventHandler;
            OnDataInserted(data);
        }
        public void PushFront(DataType data)
        {
            var o = treap.Insert(data, 0);
            RegisterData(o, data);
            OnTreapLayoutChanged();
        }
        public void PushBack(DataType data)
        {
            var o = treap.Insert(data, treap.Count);
            RegisterData(o, data);
            OnTreapLayoutChanged();
        }
        public async Task ScrollToEnd()
        {
            await SVmain.ScrollToAsync(0, double.MaxValue, true);
            await SVmain.ScrollToAsync(0, double.MaxValue, false);
        }
        Stack<GenericView> AvaiableChildrenPool = new Stack<GenericView>();
        Dictionary<DataType, GenericView> ChildrenInUse = new Dictionary<DataType, GenericView>();
        public Func<double, Tuple<Rectangle, AbsoluteLayoutFlags>> BarsLayoutMethod = null;
        GenericView GetGenericView()
        {
            if (AvaiableChildrenPool.Count == 0)
            {
                var c = new GenericView() { IsVisible = false, HorizontalOptions = Xamarin.Forms.LayoutOptions.Fill };
                AvaiableChildrenPool.Push(c);
                var layoutMethod = BarsLayoutMethod(0);
                ALmain.Children.Add(c, layoutMethod.Item1, layoutMethod.Item2);
            }
            var ans = AvaiableChildrenPool.Pop();
            ans.IsVisible = true;
            return ans;
        }
        private int UponIndex()
        {
            return Math.Max(0, Math.Min(treap.Count - 1, treap.QueryLowerBound(SVmain.ScrollY) - 1));
            //int l = 0, r = treap.Count - 1;
            //while (l < r)
            //{
            //    int mid = (l + r + 1) / 2;
            //    if (treap.Query(mid) > SVmain.ScrollY) r = mid-1;
            //    else l = mid;
            //}
            //MyLogger.Assert(l == r);
            //return r;
        }
        private int DownIndex()
        {
            return Math.Max(0, Math.Min(treap.Count - 1, treap.QueryLowerBound(SVmain.ScrollY + SVmain.Height) - 1));
            //int l = 0, r = treap.Count - 1;
            //while (l < r)
            //{
            //    int mid = (l + r + 1) / 2;
            //    if (treap.Query(mid) >= SVmain.ScrollY + SVmain.Height) r = mid - 1;
            //    else l = mid;
            //}
            //MyLogger.Assert(l == r);
            //return r;
        }
        volatile bool isLayoutRunning = false, needRunAgain = false;
        private bool UpdateLayout()
        {
            HashSet<DataType> remain = new HashSet<DataType>();
            foreach (var p in ChildrenInUse) remain.Add(p.Key);
            bool answer = false;
            int controlsAdded = 0;
            const int maxControlsToAdd = 2;
            SVmain.BatchBegin();
            treap.Query(treap.Count, new Action<Treap<DataType>.TreapNode>((o) =>
            {
                MyAbsoluteLayout.SetLayoutBounds(LBend, BarsLayoutMethod(o.QueryYOffset()).Item1);
                ALmain.HeightRequest = o.QueryYOffset() + treap.itemHeight;
            }));
            if (treap.Count > 0)
            {
                double difference = 0;
                int l = UponIndex(), r = DownIndex();
                int mid = (l + r) / 2;
                List<int> order = new List<int>();
                for (int i1 = l, i2 = r; i1 <= i2; i1++, i2--)
                {
                    if (i1 == i2) order.Add(i1);
                    else
                    {
                        order.Add(i1);
                        order.Add(i2);
                    }
                }
                foreach (int i in order)
                {
                    treap.Query(i, new Action<Treap<DataType>.TreapNode>((o) =>
                    {
                        var targetBound = BarsLayoutMethod(o.QueryYOffset());
                        GenericView view = null;
                        if (ChildrenInUse.ContainsKey(o.data))
                        {
                            view = ChildrenInUse[o.data];
                            remain.Remove(o.data);
                            if (i == (l + r) / 2 && view.Bounds != null) difference = targetBound.Item1.Y - view.Bounds.Y;
                        }
                        else if (controlsAdded < maxControlsToAdd)
                        {
                            controlsAdded++;
                            answer = true;
                            view = GetGenericView();
                            view.Reset(o.data);
                            ChildrenInUse[o.data] = view;
                        }
                        if (view != null)
                        {
                            MyAbsoluteLayout.SetLayoutBounds(view, targetBound.Item1);
                        }
                    }));
                }
                if (difference != 0)
                {
                    SVmain.MyScrollY += difference;
                    //SVmain.ScrollToAsync(SVmain.ScrollX, SVmain.ScrollY + difference /** 1.05*/, false);
                }
            }
            foreach (var d in remain)
            {
                var v = ChildrenInUse[d];
                ChildrenInUse.Remove(d);
                v.Reset(null);
                v.IsVisible = false;
                AvaiableChildrenPool.Push(v);
            }
            SVmain.BatchCommit();
            ChangeWidth();
            return answer;
        }
        private double DesiredWidth()
        {
            double width = 0;
            foreach (var c in ALmain.Children)
            {
                var cwidth = c.Measure(double.PositiveInfinity, double.PositiveInfinity, MeasureFlags.IncludeMargins).Request.Width;
                if (width < cwidth) width = cwidth;
            }
            return width;
        }
        double WidthRequestTo;
        private void ChangeWidth()
        {
            //MyLogger.LogError($"{DesiredWidth()} {ALmain.WidthRequest}");
            //ALmain.WidthRequest = DesiredWidth();
            //return;
            var width = DesiredWidth();
            if (width == WidthRequestTo) return;
            double WidthRequestFrom = (ALmain.WidthRequest == -1 ? ALmain.Width : ALmain.WidthRequest);
            WidthRequestTo = width;
            ALmain.AbortAnimation("width");
            ALmain.WidthRequest = WidthRequestFrom;
            bool immediatelyChange = WidthRequestTo > WidthRequestFrom;
            ALmain.Animate("width", new Animation(new Action<double>((ratio) =>
            {
                if (!immediatelyChange)
                {
                    if (ratio <= 0.5) return;
                    ratio = (ratio - 0.5) * 2;
                }
                ALmain.WidthRequest = ratio * WidthRequestTo + (1 - ratio) * WidthRequestFrom;
            })), 16, (uint)(Treap<DataType>.animationDuration * (immediatelyChange ? 0.5 : 1.0)), null, new Action<double, bool>((dv, bv) =>
            {
                ALmain.WidthRequest = WidthRequestTo;
            }));
        }

        private void AnimateLayout()
        {
            if (isLayoutRunning)
            {
                needRunAgain = true;
                return;
            }
            isLayoutRunning = true;
            //MyLogger.Log($"treap.Count: {treap.Count}");
            bool adding = UpdateLayout();
            ALmain.Animate("animation", new Animation(new Action<double>((ratio) =>
            {
                adding |= UpdateLayout();
            })), 16, (uint)Treap<DataType>.animationDuration, null, new Action<double, bool>((dv, bv) =>
            {
                adding |= UpdateLayout();
                isLayoutRunning = false;
                if (adding || needRunAgain)
                {
                    needRunAgain = false;
                    AnimateLayout();
                }
            }));
            //AbsoluteLayout.AutoSize
            //double width = 0, cnt = 0;
            //foreach(var c in ALmain.Children)
            //{
            //    if (width < c.Measure(double.MaxValue,double.MaxValue,MeasureFlags.None).Request.Width) width = c.Width;
            //    cnt++;
            //}
            //width = ALmain.Measure(double.MaxValue, double.MaxValue, MeasureFlags.IncludeMargins).Request.Width;
            //MyLogger.LogError($"width={width}, cnt={cnt}, autosize={AbsoluteLayout.AutoSize}");
            //ALmain.WidthRequest = width;
        }
        private void RegisterEvents()
        {
            this.TreapLayoutChanged += () => { AnimateLayout(); };
            SVmain.Scrolled += (sender, args) => { AnimateLayout(); };
            SVmain.SizeChanged += (sender, args) => { AnimateLayout(); };
        }
        private void InitializeViews()
        {
            {
                SVmain = new MyScrollView { Orientation = Xamarin.Forms.ScrollOrientation.Vertical };
                {
                    ALmain = new MyAbsoluteLayout
                    {
                        //HorizontalOptions = Xamarin.Forms.LayoutOptions.FillAndExpand,
                        //VerticalOptions =Xamarin.Forms.LayoutOptions.Start,
                        HeightRequest = treap.itemHeight,
                        BackgroundColor = Xamarin.Forms.Color.LightYellow
                    };
                    {
                        LBend = new MyLabel
                        {
                            Text = "End of Results",
                            IsEnabled = false
                        };
                        var layoutMethod = BarsLayoutMethod(0);
                        ALmain.Children.Add(LBend, layoutMethod.Item1, layoutMethod.Item2);
                    }
                    SVmain.Content = ALmain;
                }
                this.Content = SVmain;
            }
        }
        public BarsListPanel()
        {
            BarsLayoutMethod = new Func<double, Tuple<Rectangle, AbsoluteLayoutFlags>>((y) =>
            {
                return new Tuple<Rectangle, AbsoluteLayoutFlags>(new Rectangle(0, y, /*ALmain.Width*/1, -1), AbsoluteLayoutFlags.WidthProportional);
            });
            InitializeViews();
            RegisterEvents();
            OnTreapLayoutChanged();
            //MyLogger.AddTestMethod("Show scroll info", new Func<Task>(async () =>
            //  {
            //      var l = new MyLabel("I'm here") { BackgroundColor = Color.Red };
            //      //await l.TranslateTo(0, SVmain.ScrollY);
            //      //l.Layout(new Rectangle(0, SVmain.ScrollY, -1, -1));
            //      //MyAbsoluteLayout.SetLayoutBounds(l, new Rectangle(0, SVmain.ScrollY, -1, -1));
            //      ALmain.Children.Add(l,new Rectangle(-25,0,1,-1),AbsoluteLayoutFlags.WidthProportional);
            //      await MyLogger.Alert("wait");
            //      MyAbsoluteLayout.SetLayoutBounds(l, new Rectangle(0, SVmain.ScrollY, -1, -1));
            //      int u = UponIndex(), d = DownIndex();
            //      await MyLogger.Alert($"({SVmain.ScrollY},{SVmain.ScrollY + SVmain.Height}),({u},{d}),({treap.Query(u)},{treap.Query(d)}),({l.TranslationY},{l.Y},{l.TranslationY/l.Y},{l.Bounds})");
            //  }));
            //MyLogger.AddTestMethod("AnimationIsRunning", new Func<Task>(async () =>
            //  {
            //      await MyLogger.Alert($"AnimationIsRunning: {ALmain.AnimationIsRunning("animation")}");
            //  }));
        }
    }
    public partial class Treap<DataType>
    {
        //public Treap()
        //{
        //    Insert(, 0);
        //}
        public delegate void TreapDataEventHandler(DataType data);
        public event TreapDataEventHandler DataInserted, DataRemoved;
        private void OnDataInserted(DataType data) { DataInserted?.Invoke(data); }
        private void OnDataRemoved(DataType data) { DataRemoved?.Invoke(data); }
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
        public TreapNode Insert(DataType data, int position)
        {
            var height = QueryFinal(position);// (position <= Count ?  : position * itemHeight);
            try
            {
                lock (root)
                {
                    TreapNode.Split(root, out TreapNode a, out TreapNode b, position);
                    if (b != null) b.AppendAnimation(DateTime.Now, itemHeight);
                    var o = new TreapNode(data, height);
                    root = TreapNode.Merge(a, TreapNode.Merge(o, b));
                    return o;
                }
            }
            finally
            {
                OnDataInserted(data);
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
                OnDataRemoved(b.data);
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
        public void ChangeHeight(TreapNode o, double difference)
        {
            lock (root)
            {
                TreapNode.Split(root, out TreapNode a, out TreapNode b, o.GetPosition() + 1);
                if (b != null) b.AppendAnimation(DateTime.Now, difference);
                root = TreapNode.Merge(a, b);
            }
        }
    }
    public partial class Treap<DataType>
    {
        public class TreapNode
        {
            public DataType data;
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
            public void AddToListRecursively(ref List<DataType> list)
            {
                this.l?.AddToListRecursively(ref list);
                list.Add(this.data);
                this.r?.AddToListRecursively(ref list);
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
            public static int GetSize(TreapNode o) { return o == null ? 0 : o.size; }
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
                if (position < 0)
                {
                    MyLogger.LogError($"TreapNode: position = {position}");
                    position = 0;
                }
                if (position > GetSize(o))
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
                    Split(b.l, out a, out b.l, position);
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
