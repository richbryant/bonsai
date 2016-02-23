﻿using OpenTK;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bonsai.Shaders
{
    [Description("Creates a translation matrix.")]
    public class CreateTranslation : Source<Matrix4>
    {
        [Description("The translation along the x-axis.")]
        public float X { get; set; }

        [Description("The translation along the y-axis.")]
        public float Y { get; set; }

        [Description("The translation along the z-axis.")]
        public float Z { get; set; }

        public override IObservable<Matrix4> Generate()
        {
            return Observable.Defer(() => Observable.Return(Matrix4.CreateTranslation(X, Y, Z)));
        }

        public IObservable<Matrix4> Generate<TSource>(IObservable<TSource> source)
        {
            return source.Select(input => Matrix4.CreateTranslation(X, Y, Z));
        }
    }
}
