﻿#region usings

using System;
using System.Threading.Tasks;

#endregion

namespace Convex.Event {
    public delegate Task AsyncEventHandler<in TEventArgs>(object sender, TEventArgs args) where TEventArgs : EventArgs;
}