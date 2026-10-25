// Copyright 2026 Spellbound Studio Inc.

using System;
using NUnit.Framework;
using Spellbound.Core.Tooling;

namespace Spellbound.Core.Tests {
    public class ObservableArrayTests {
        [Test]
        public void SetFiresChangeWithValueAndIndex() {
            var array = new ObservableArray<int>(3);
            ObservableArrayChange<int> received = default;
            var fired = 0;
            array.onChanged += change => {
                received = change;
                fired++;
            };

            array[1] = 42;

            Assert.AreEqual(1, fired);
            Assert.AreEqual(ObservableArrayOperation.Set, received.Operation);
            Assert.AreEqual(42, received.NewValue);
            Assert.AreEqual(1, received.Index);
            Assert.AreEqual(42, array[1]);
        }

        [Test]
        public void SettingEqualValueDoesNotFire() {
            var array = new ObservableArray<int>(2);
            array[0] = 5;
            var fired = 0;
            array.onChanged += _ => fired++;

            array[0] = 5;

            Assert.AreEqual(0, fired);
        }

        [Test]
        public void ClearZeroesSlotsAndFiresOnce() {
            var array = new ObservableArray<int>(2);
            array[0] = 1;
            array[1] = 2;
            var fired = 0;
            array.onChanged += _ => fired++;

            array.Clear();

            Assert.AreEqual(1, fired);
            Assert.AreEqual(0, array[0]);
            Assert.AreEqual(0, array[1]);
        }

        [Test]
        public void ResizePreservesExistingElements() {
            var array = new ObservableArray<int>(2);
            array[0] = 10;
            array[1] = 20;

            array.Resize(4);

            Assert.AreEqual(4, array.Count);
            Assert.AreEqual(10, array[0]);
            Assert.AreEqual(20, array[1]);
            Assert.AreEqual(0, array[3]);
        }

        [Test]
        public void OutOfRangeAccessThrows() {
            var array = new ObservableArray<int>(2);

            Assert.Throws<IndexOutOfRangeException>(() => _ = array[2]);
            Assert.Throws<IndexOutOfRangeException>(() => array[-1] = 1);
        }

        [Test]
        public void InsertionOperationsAreNotSupported() {
            var array = new ObservableArray<int>(1);

            Assert.Throws<NotSupportedException>(() => array.Add(1));
            Assert.Throws<NotSupportedException>(() => array.Insert(0, 1));
            Assert.Throws<NotSupportedException>(() => array.Remove(1));
            Assert.Throws<NotSupportedException>(() => array.RemoveAt(0));
        }
    }
}
