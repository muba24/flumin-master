using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Flumin;
using NodeSystemLib;

namespace SimpleADCTest {
    [TestClass]
    public class Buffer {

        [TestMethod]
        public void enqueue_exact() {
            var rate = 1000;

            var bufferWrite = new TimeLocatedBuffer(1000, rate);

            var queue = new SignalRingBuffer(1000, rate) {
                SizeFixed = true
            };

            var samples = bufferWrite.GetSamples();
            for (int i = 0; i < samples.Length; i++) {
                samples[i] = i;
            }
            bufferWrite.SetWritten(samples.Length);

            queue.Enqueue(bufferWrite);

            Assert.AreEqual(bufferWrite.WrittenSamples, queue.Length);
            Assert.AreEqual(queue.Length, queue.Capacity);
        }

        [TestMethod]
        public void enqueue_overflow_skip_data2() {
            var rate = 1000000;

            var bufferWrite = new TimeLocatedBuffer(rate / 10, rate);
            var bufferRead  = new TimeLocatedBuffer(rate / 10, rate);

            var queue = new SignalRingBuffer(rate, rate) {
                SizeFixed = true,
                IgnoreOverflow = true
            };

            var samples = bufferWrite.GetSamples();
            for (int i = 0; i < samples.Length; i++) {
                samples[i] = i;
            }

            for (int i = 0; i < 12; i++) {
                bufferWrite.SetWritten(samples.Length);
                queue.Enqueue(bufferWrite);
            }

            Assert.AreEqual(queue.Length, queue.Capacity);
        }

        [TestMethod]
        public void enqueue_overflow_skip_data() {
            var rate = 1000;

            var bufferWrite = new TimeLocatedBuffer(300, rate);
            var bufferRead  = new TimeLocatedBuffer(300, rate);

            var queue = new SignalRingBuffer(1000, rate) {
                SizeFixed = true
            };

            var samples = bufferWrite.GetSamples();
            for (int i = 0; i < samples.Length; i++) {
                samples[i] = i;
            }
            bufferWrite.SetWritten(samples.Length);

            queue.Enqueue(bufferWrite);
            queue.Enqueue(bufferWrite);
            queue.Enqueue(bufferWrite);
            queue.Enqueue(bufferWrite);

            var queueSizeShouldBe = queue.Capacity;
            Assert.AreEqual(queueSizeShouldBe, queue.Length);

            queue.Dequeue(bufferRead);
            samples = bufferRead.GetSamples();
            for (int i = 0; i < samples.Length; i++) {
                Assert.AreEqual(samples[i], (200 + i) % 300);
            }

            queue.Dequeue(bufferRead);
            samples = bufferRead.GetSamples();
            for (int i = 0; i < samples.Length; i++) {
                Assert.AreEqual(samples[i], (500 + i) % 300);
            }
        }

        [TestMethod]
        public void enqueue_dequeue() {
            var rate = 1000;

            var bufferWrite = new TimeLocatedBuffer(300, rate);
            var bufferRead  = new TimeLocatedBuffer(300, rate);

            var queue = new SignalRingBuffer(1000, rate) {
                SizeFixed = false
            };

            var samples = bufferWrite.GetSamples();
            for (int i = 0; i < samples.Length; i++) {
                samples[i] = i;
            }
            bufferWrite.SetWritten(samples.Length);
            queue.Enqueue(bufferWrite);
            Assert.AreEqual(bufferWrite.WrittenSamples, bufferWrite.Length);

            Assert.AreEqual(queue.Length, bufferWrite.WrittenSamples);

            queue.Dequeue(bufferRead);
            Assert.AreEqual(bufferRead.WrittenSamples, bufferWrite.WrittenSamples);
        }

    }
}
