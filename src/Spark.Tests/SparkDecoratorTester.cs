﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Spark.Spool;

namespace Spark.Tests
{
    [TestFixture]
    public class SparkDecoratorTester
    {
        [Test]
        public void OutputContentCollectionWorksBetweenLayers()
        {
            var layer0 = new TestLayer0();
            var layer1 = new TestLayer1(layer0);
            var content = layer1.RenderView();
            Assert.AreEqual("[layer1top][layer0head][layer1head][layer0][layer1bottom]", content);
        }

        public class TestLayer0 : SparkViewDecorator
        {
            public TestLayer0()
                : base(null)
            {
            }

            public override Guid GeneratedViewId
            {
                get { throw new System.NotImplementedException(); }
            }

            public override void RenderView(TextWriter writer)
            {
                base.RenderView(writer);

                using (OutputScope(writer))
                {
                    Output.Write("[layer0]");

                    using (OutputScope("head"))
                    {
                        Output.Write("[layer0head]");
                    }
                }
            }
        }

        public class TestLayer1 : SparkViewDecorator
        {
            public TestLayer1(SparkViewBase decorated)
                : base(decorated)
            {
            }

            public override Guid GeneratedViewId
            {
                get { throw new System.NotImplementedException(); }
            }

            public override void RenderView(TextWriter writer)
            {
                base.RenderView(writer);

                using (OutputScope(writer))
                {
                    using (OutputScope("head"))
                    {
                        Output.Write("[layer1head]");
                    }

                    Output.Write("[layer1top]");
                    Output.Write(Content["head"]);
                    Output.Write(Content["view"]);
                    Output.Write("[layer1bottom]");
                }
            }
        }
    }
}
