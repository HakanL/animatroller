﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.Framework
{
    public interface IScene
    {
        [Obsolete]
        void Start();

        void Run();

        void Stop();
    }
}
