﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reactive.Linq;
using System.Xml.Serialization;
using System.ComponentModel;

namespace Bonsai.Reactive
{
    [XmlType(Namespace = Constants.XmlNamespace)]
    [Description("Bypasses the specified number of contiguous elements at the start of the sequence.")]
    public class Skip : Combinator
    {
        [Description("The number of elements to skip.")]
        public int Count { get; set; }

        public override IObservable<TSource> Process<TSource>(IObservable<TSource> source)
        {
            return source.Skip(Count);
        }
    }
}