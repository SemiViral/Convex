﻿#region

using System;
using System.Threading.Tasks;

#endregion

namespace Convex.Event
{
    public delegate Task AsyncEventHandler<in TEventArgs>(object source, TEventArgs args) where TEventArgs : EventArgs;
}
