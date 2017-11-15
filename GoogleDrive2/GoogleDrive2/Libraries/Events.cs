using System;
using System.Collections.Generic;
using System.Text;

namespace GoogleDrive2.Libraries.Events
{
    public delegate void EmptyEventHandler();
    public delegate void MyEventHandler<T>(T data);
    public delegate void MyEventHandler<T1,T2>(T1 d1,T2 d2);
}
