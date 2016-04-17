﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bonsai.Shaders
{
    [Description("Produces a sequence of events whenever the shader window is closed.")]
    [Editor("Bonsai.Shaders.Configuration.Design.ShaderConfigurationComponentEditor, Bonsai.Shaders.Design", typeof(ComponentEditor))]
    public class WindowClosed : Source<EventPattern<EventArgs>>
    {
        public override IObservable<EventPattern<EventArgs>> Generate()
        {
            return ShaderManager.WindowSource.SelectMany(window => window.WindowClosed().Take(1));
        }
    }
}