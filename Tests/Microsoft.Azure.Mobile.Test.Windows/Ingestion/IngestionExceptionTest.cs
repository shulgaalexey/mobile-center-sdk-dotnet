﻿using Microsoft.Azure.Mobile.Ingestion;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Microsoft.Azure.Mobile.Test.Windows.Ingestion
{
    [TestClass]
    public class IngestionExceptionTest
    {
        /// <summary>
        /// Validate that exception message is saving
        /// </summary>
        [TestMethod]
        public void CheckMessageError()
        {
            string exceptionMessage = "Test exception message";
            IngestionException ingException = new IngestionException(exceptionMessage);

            Assert.AreEqual(exceptionMessage, ingException.Message);
        }

        /// <summary>
        /// Validate that exception is saving as an internal exception
        /// </summary>
        [TestMethod]
        public void CheckInternalError()
        {
            string exceptionMessage = "Test exception message";
            Exception internalException = new Exception(exceptionMessage);
            IngestionException ingException = new IngestionException(internalException);

            Assert.AreSame(internalException, ingException.InnerException);
            Assert.AreEqual(exceptionMessage, ingException.InnerException.Message);
        }
    }
}
