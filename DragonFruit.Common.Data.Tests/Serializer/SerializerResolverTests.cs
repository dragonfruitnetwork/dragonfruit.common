﻿// DragonFruit.Common Copyright 2021 DragonFruit Network
// Licensed under the MIT License. Please refer to the LICENSE file at the root of this project for details

using System.IO;
using System.Net.Http;
using DragonFruit.Common.Data.Serializers;
using NUnit.Framework;

namespace DragonFruit.Common.Data.Tests.Serializer
{
    [TestFixture]
    public class SerializerResolverTests : ApiTest
    {
        [SetUp]
        public void Setup()
        {
            // testobject will use xml
            SerializerResolver.Register<TestObject, ApiXmlSerializer>();

            // anothertestobject will use the dummyserializer
            SerializerResolver.Register<AnotherTestObject, DummySerializer>();
        }

        [Test]
        public void TestResolution()
        {
            Assert.AreEqual(typeof(ApiXmlSerializer), Client.Serializer.Resolve<TestObject>(DataDirection.In).GetType());
            Assert.AreEqual(typeof(ApiJsonSerializer), Client.Serializer.Resolve<YetAnotherTestObject>(DataDirection.Out).GetType());
        }

        [Test]
        public void TestRemovalResolution()
        {
            Assert.AreEqual(typeof(DummySerializer), Client.Serializer.Resolve<AnotherTestObject>(DataDirection.In).GetType());

            SerializerResolver.Unregister<AnotherTestObject>();
            Assert.AreEqual(typeof(ApiJsonSerializer), Client.Serializer.Resolve<AnotherTestObject>(DataDirection.In).GetType());
        }

        [Test]
        public void TestConfigure()
        {
            string a = string.Empty;
            Client.Serializer.Configure<DummySerializer>(o => a = o.ContentType);
            Assert.AreEqual("nothing", a);
        }
    }

    public class TestObject
    {
    }

    public class AnotherTestObject
    {
    }

    public class YetAnotherTestObject
    {
    }

    public class DummySerializer : ApiSerializer
    {
        public override string ContentType => "nothing";

        public override HttpContent Serialize<T>(T input) where T : class => throw new System.NotImplementedException();
        public override T Deserialize<T>(Stream input) where T : class => throw new System.NotImplementedException();
    }
}