using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Flumin;
using NodeSystemLib;

namespace SimpleADCTest {

    [TestClass]
    public class UnitTestTimeStamp {

        [TestMethod]
        public void TestMethod1() {
            TimeStamp stamp = new TimeStamp(1, 1000);
            TimeStamp stamp2 = new TimeStamp(2, 1000);

            Assert.AreEqual(stamp2.Value - stamp.Value, 2000);
        }

    }
}
