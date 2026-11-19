// Copyright 2026 Spellbound Studio Inc.

using System.Collections.Generic;
using NUnit.Framework;
using Spellbound.Core.Tooling;

namespace Spellbound.Core.Tests {
    public class SingletonManagerTests {
        private class Service { }

        [TearDown]
        public void TearDown() => SingletonManager.UnregisterSingleton<Service>();

        [Test]
        public void RegisterThenGetReturnsSameInstance() {
            var service = new Service();

            SingletonManager.RegisterSingleton(service);

            Assert.AreSame(service, SingletonManager.GetSingletonInstance<Service>());
            Assert.IsTrue(SingletonManager.TryGetSingletonInstance<Service>(out var resolved));
            Assert.AreSame(service, resolved);
        }

        [Test]
        public void MissingSingletonThrowsAndTryGetReturnsFalse() {
            Assert.Throws<KeyNotFoundException>(() => SingletonManager.GetSingletonInstance<Service>());
            Assert.IsFalse(SingletonManager.TryGetSingletonInstance<Service>(out _));
        }

        [Test]
        public void UnregisterRemovesInstance() {
            SingletonManager.RegisterSingleton(new Service());
            SingletonManager.UnregisterSingleton<Service>();

            Assert.IsFalse(SingletonManager.TryGetSingletonInstance<Service>(out _));
        }
    }
}
