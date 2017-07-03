﻿using System;
using Microsoft.Azure.Mobile.Analytics.Ingestion.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Microsoft.Azure.Mobile.Test.Windows.Ingestion.Models
{
    using Device = Microsoft.Azure.Mobile.Ingestion.Models.Device;

    [TestClass]
    public class StartSessionLogTest
    {
        private readonly DateTime? Timestamp = null;

        /// <summary>
        /// Verify that instance is constructed properly.
        /// </summary>
        [TestMethod]
        public void TestInstanceConstruction()
        {
            var mockDevice = new Mock<Device>();

            StartSessionLog emptyLog = new StartSessionLog();
            StartSessionLog log = new StartSessionLog(Timestamp, mockDevice.Object);

            Assert.IsNotNull(emptyLog);
            Assert.IsNotNull(log);
        }
    }
}
