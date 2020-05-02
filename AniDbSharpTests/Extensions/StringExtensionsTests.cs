using Microsoft.VisualStudio.TestTools.UnitTesting;
using AniDbSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace AniDbSharp.Extensions.Tests
{
    [TestClass()]
    public class StringExtensionsTests
    {
        [TestMethod()]
        public void HexStringToBytesTest()
        {
            byte[] bytes = "FF".HexStringToBytes();

            Assert.AreEqual(1, bytes.Length);
            Assert.AreEqual(255, bytes[0]);

            bytes = "faef".HexStringToBytes();

            Assert.AreEqual(2, bytes.Length);
            Assert.AreEqual(250, bytes[0]);
            Assert.AreEqual(239, bytes[1]);

            try
            {
                "asdlkjlk;j".HexStringToBytes();
                Assert.Fail("Invalid Characters Accepted");
            }
            catch (Exception)
            {
            }

            try
            {
                "asd".HexStringToBytes();
                Assert.Fail("Invalid Length Accepted");
            }
            catch (Exception)
            {
            }
        }
    }
}