using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;
using System.Threading.Tasks;
using System.Linq;

namespace GoogleDrive2.MyControls.BarsListPanel
{
    public class DataBindedLabel<DataType> : MyLabel, IDataBindedView<DataType> where DataType : MyDisposable
    {
        public event Libraries.Events.EmptyEventHandler Appeared;
        public Func<Task> Disappearing { get; set; }
        public void Reset(DataType source)
        {
            if (this.BindingContext != null) (this.BindingContext as MyControls.BarsListPanel.MyDisposable).UnregisterDisposingEvents();
            this.BindingContext = source;
            if (source != null) source.Disposing = new Func<Task>(async () => { await Disappearing?.Invoke(); }); //MyDispossable will automatically unregister all Disposing events after disposed
            Appeared?.Invoke();
        }
        public DataBindedLabel()
        {
            System.Threading.SemaphoreSlim semaphoreSlim = new System.Threading.SemaphoreSlim(1, 1);
            this.Appeared += async () =>
            {
                this.Opacity = 0;
                await semaphoreSlim.WaitAsync();
                await this.FadeTo(1, 500);
                lock (semaphoreSlim) semaphoreSlim.Release();
            };
            this.Disappearing = new Func<Task>(async () =>
            {
                await semaphoreSlim.WaitAsync();
                await this.FadeTo(0, 500);
                lock (semaphoreSlim) semaphoreSlim.Release();
            });
        }
    }
    public class DataBindedGrid<DataType> :MyGrid, IDataBindedView<DataType> where DataType : MyDisposable
    {
        public event Libraries.Events.EmptyEventHandler Appeared;
        public Func<Task> Disappearing { get; set; }
        public void Reset(DataType source)
        {
            if (this.BindingContext != null) (this.BindingContext as MyControls.BarsListPanel.MyDisposable).UnregisterDisposingEvents();
            this.BindingContext = source;
            if (source != null) source.Disposing = new Func<Task>(async () => { await Disappearing?.Invoke(); }); //MyDispossable will automatically unregister all Disposing events after disposed
            Appeared?.Invoke();
        }
        public DataBindedGrid()
        {
            System.Threading.SemaphoreSlim semaphoreSlim = new System.Threading.SemaphoreSlim(1, 1);
            this.Appeared += async () =>
            {
                this.Opacity = 0;
                await semaphoreSlim.WaitAsync();
                await this.FadeTo(1, 500);
                lock (semaphoreSlim) semaphoreSlim.Release();
            };
            this.Disappearing = new Func<Task>(async () =>
            {
                await semaphoreSlim.WaitAsync();
                await this.FadeTo(0, 500);
                lock (semaphoreSlim) semaphoreSlim.Release();
            });
        }
    }
    public interface IDataBindedView<DataType> where DataType: MyDisposable
    {
        event Libraries.Events.EmptyEventHandler Appeared;
        Func<Task> Disappearing { get; set; }
        void Reset(DataType data);
    }
    class BarsListPanel<GenericView,DataType>:MyContentView where DataType:MyDisposable where GenericView : Xamarin.Forms.View, IDataBindedView<DataType>,new()
    {
        public event Libraries.Events.MyEventHandler<DataType> DataInserted, DataRemoved;
        public event Libraries.Events.MyEventHandler<Treap<DataType>.TreapNodePrototype> TreapNodeAdded, TreapNodeRemoved;
        private void OnDataInserted(DataType data) { DataInserted?.Invoke(data); }
        private void OnDataRemoved(DataType data) { DataRemoved?.Invoke(data); }
        public double AnimationDuration { get { return Treap<DataType>.animationDuration; } }
        public double ItemHeight
        {
            get { return Treap.itemHeight; }
            set { Treap.itemHeight = value; }
        }
        public List<DataType>ToList()
        {
            return Treap.ToList();
        }
        protected Treap<DataType> Treap { get; private set; } = new Treap<DataType>();
        MyAbsoluteLayout ALmain;
        MyScrollView SVmain;
        MyLabel LBend;
        private delegate void TreapLayoutChangedEventHandler();
        private event TreapLayoutChangedEventHandler TreapLayoutChanged;
        private void OnTreapLayoutChanged() { TreapLayoutChanged?.Invoke(); }
        protected void ChangeHeight(Treap<DataType>.TreapNodePrototype node, double difference)
        {
            Treap.ChangeHeight(node, difference);
        }
        public void Sort(Comparison<DataType>comparer)
        {
            Treap.Sort(comparer);
            OnTreapLayoutChanged();
        }
        public async Task ClearAsync()
        {
            if (Treap.Count > 0)
            {
                int l = UponIndex(), r = DownIndex();
                var count = Treap.Count;
                List<DataType> visible = new List<DataType>(), invisible = new List<DataType>();
                for (int i = count - 1; i >= 0; i--)
                {
                    Treap.Query(i, (o) =>
                    {
                        if (l <= i && i <= r) visible.Add(o.data);
                        else invisible.Add(o.data);
                        //if (i < l || r < i) treap.Delete(o);
                    });
                }
                Libraries.MySemaphore semaphore = new Libraries.MySemaphore(-count + 1);
                foreach (var o in invisible)
                {
                    await (o as MyDisposable).OnDisposedAsync(false);
                    semaphore.Release();
                }
                var t= Task.WhenAll(visible.Select(async (o) =>
                {
                    await (o as MyDisposable).OnDisposedAsync(false);
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
        private void RegisterData(Treap<DataType>.TreapNodePrototype o, DataType data)
        {
            Libraries.Events.MyEventHandler<object> disposedEventHandler = null;
            Libraries.Events.MyEventHandler<double> heightChangedEventHandler = null;
            disposedEventHandler = new Libraries.Events.MyEventHandler<object>(delegate
            {
                data.Disposed -= disposedEventHandler;
                data.HeightChanged -= heightChangedEventHandler;
                Treap.Delete(o);
                OnTreapLayoutChanged();
                OnDataRemoved(data);
            });
            heightChangedEventHandler = new Libraries.Events.MyEventHandler<double>((difference) =>
              {
                  Treap.ChangeHeight(o, difference);
                  OnTreapLayoutChanged();
              });
            data.Disposed += disposedEventHandler;
            data.HeightChanged += heightChangedEventHandler;
            OnDataInserted(data);
        }
        public void MoveItem(int from,int to)
        {
            Treap.MoveItem(from, to);
            OnTreapLayoutChanged();
        }
        protected void DoAtomic(Action action) { Treap.DoAtomic(action); }
        public DataType Query(int idx)
        {
            DataType answer = default(DataType);
            Treap.Query(idx, (o) => answer = o.data);
            return answer;
        }
        //public Tuple<Treap<DataType>.TreapNode, double> Remove(int idx)
        //{
        //    var o = Treap.Cut(idx);
        //    OnTreapLayoutChanged();
        //    return o;
        //}
        public Treap<DataType>.TreapNodePrototype Insert(DataType data, int idx)
        {
            var o = Treap.Insert(data, idx);
            RegisterData(o, data);
            OnTreapLayoutChanged();
            return o;
        }
        public Treap<DataType>.TreapNodePrototype PushFront(DataType data)
        {
            return Insert(data, 0);
        }
        public Treap<DataType>.TreapNodePrototype PushBack(DataType data)
        {
            return Insert(data, Treap.Count);
        }
        public async Task ScrollToEnd()
        {
            await SVmain.ScrollToAsync(0, double.MaxValue, true);
            await SVmain.ScrollToAsync(0, double.MaxValue, false);
        }
        Stack<GenericView> AvaiableChildrenPool=new Stack<GenericView>();
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
            return Math.Max(0, Math.Min(Treap.Count - 1, Treap.QueryLowerBound(SVmain.ScrollY) - 1));
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
            return Math.Max(0, Math.Min(Treap.Count - 1, Treap.QueryLowerBound(SVmain.ScrollY + SVmain.Height) - 1));
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
        protected bool IsBarVisible(int idx) { return UponIndex() <= idx && idx <= DownIndex(); }
        volatile bool isLayoutRunning = false, needRunAgain = false;
        private bool UpdateLayout()
        {
            HashSet<DataType> remain = new HashSet<DataType>();
            foreach (var p in ChildrenInUse) remain.Add(p.Key);
            bool answer = false;
            int controlsAdded = 0;
            const int maxControlsToAdd = 2;
            SVmain.BatchBegin();
            Treap.DoAtomic(() =>
            {
                MyAbsoluteLayout.SetLayoutBounds(LBend, BarsLayoutMethod(Treap.QueryY(Treap.Count)).Item1);
                ALmain.HeightRequest = Treap.QueryY(Treap.Count) + Treap.itemHeight;
            });
            if (Treap.Count > 0)
            {
                double difference = 0;
                Treap.DoAtomic(() =>
                {
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
                        var targetBound = BarsLayoutMethod(Treap.QueryY(i));
                        var data = Treap.Query(i, (o) => { return o.data; });
                        GenericView view = null;
                        if (ChildrenInUse.ContainsKey(data))
                        {
                            view = ChildrenInUse[data];
                            remain.Remove(data);
                            if (i == (l + r) / 2 && view.Bounds != null) difference = targetBound.Item1.Y - view.Bounds.Y;
                        }
                        else if (controlsAdded < maxControlsToAdd)
                        {
                            controlsAdded++;
                            answer = true;
                            view = GetGenericView();
                            view.Reset(data);
                            ChildrenInUse[data] = view;
                        }
                        if (view != null)
                        {
                            MyAbsoluteLayout.SetLayoutBounds(view, targetBound.Item1);
                        }
                    }
                });
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
            double WidthRequestFrom = (ALmain.WidthRequest==-1?ALmain.Width: ALmain.WidthRequest);
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
                })), 16, (uint)(Treap<DataType>.animationDuration * (immediatelyChange?0.5:1.0)), null, new Action<double, bool>((dv, bv) =>
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
            })), 16, (uint)Treap<DataType>.animationDuration,null,new Action<double, bool>((dv,bv)=>
            {
                adding |= UpdateLayout();
                isLayoutRunning = false;
                if (adding||needRunAgain)
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
            Treap.TreapNodeAdded += (o) => { TreapNodeAdded?.Invoke(o); };
            Treap.TreapNodeRemoved += (o) => { TreapNodeRemoved?.Invoke(o); };
            this.TreapLayoutChanged += () => { AnimateLayout(); };
            SVmain.Scrolled += (sender,args) => { AnimateLayout(); };
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
                        HeightRequest = Treap.itemHeight,
                        BackgroundColor = Xamarin.Forms.Color.LightYellow
                    };
                    {
                        LBend = new MyLabel
                        {
                            Text = "End of Results",
                            IsEnabled = false
                        };
                        var layoutMethod = BarsLayoutMethod(0);
                        ALmain.Children.Add(LBend,layoutMethod.Item1,layoutMethod.Item2);
                    }
                    SVmain.Content = ALmain;
                }
                this.Content = SVmain;
            }
        }
        public BarsListPanel(double itemHeight=50)
        {
            Treap.itemHeight = itemHeight;
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
}
