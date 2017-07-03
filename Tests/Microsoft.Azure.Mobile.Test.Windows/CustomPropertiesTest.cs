﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Mobile.Test.Windows
{
    [TestClass]
    public class CustomPropertiesTest
    {
        /// <summary>
        /// Verify that key validated correct.
        /// </summary>
        [TestMethod]
        public void TestKeyValidate()
        {
            var value1 = "test";
            var value2 = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            var value3 = 1;
            var value4 = 100L;
            var value5 = 0.1f;
            var value6 = 0.1m;
            var value7 = 0.1;
            var value8 = false;
            CustomProperties properties = new CustomProperties();
            Assert.AreEqual(0, properties.Properties.Count);

            /* Null key. */
            string nullKey = null;
            properties.Set(nullKey, value1);
            properties.Set(nullKey, value2);
            properties.Set(nullKey, value3);
            properties.Set(nullKey, value4);
            properties.Set(nullKey, value5);
            properties.Set(nullKey, value6);
            properties.Set(nullKey, value7);
            properties.Set(nullKey, value8);
            properties.Clear(nullKey);
            Assert.AreEqual(0, properties.Properties.Count);

            /* Invalid key. */
            var invalidKey = "!";
            properties.Set(invalidKey, value1);
            properties.Set(invalidKey, value2);
            properties.Set(invalidKey, value3);
            properties.Set(invalidKey, value4);
            properties.Set(invalidKey, value5);
            properties.Set(invalidKey, value6);
            properties.Set(invalidKey, value7);
            properties.Set(invalidKey, value8);
            properties.Clear(invalidKey);
            Assert.AreEqual(0, properties.Properties.Count);

            /* Long key. */
            var longKey = new string('a', 129);
            properties.Set(longKey, value1);
            properties.Set(longKey, value2);
            properties.Set(longKey, value3);
            properties.Set(longKey, value4);
            properties.Set(longKey, value5);
            properties.Set(longKey, value6);
            properties.Set(longKey, value7);
            properties.Set(longKey, value8);
            properties.Clear(longKey);
            Assert.AreEqual(0, properties.Properties.Count);

            /* Empty keys. */
            properties.Set(string.Empty, value1);
            properties.Set(string.Empty, value2);
            properties.Set(string.Empty, value3);
            properties.Set(string.Empty, value4);
            properties.Set(string.Empty, value5);
            properties.Set(string.Empty, value6);
            properties.Set(string.Empty, value7);
            properties.Set(string.Empty, value8);
            properties.Clear(string.Empty);
            Assert.AreEqual(0, properties.Properties.Count);

            /* Normal keys. */
            properties.Set("t1", value1);
            properties.Set("t2", value2);
            properties.Set("t3", value3);
            properties.Set("t4", value4);
            properties.Set("t5", value5);
            properties.Set("t6", value6);
            properties.Set("t7", value7);
            properties.Set("t8", value8);
            properties.Clear("t9");
            Assert.AreEqual(9, properties.Properties.Count);

            /* Already contains keys. */
            properties.Set("t1", value1);
            properties.Set("t2", value2);
            properties.Set("t3", value3);
            properties.Set("t4", value4);
            properties.Set("t5", value5);
            properties.Set("t6", value6);
            properties.Set("t7", value7);
            properties.Set("t8", value8);
            properties.Clear("t9");
            Assert.AreEqual(9, properties.Properties.Count);
        }


        /// <summary>
        /// Verify that properties count validated correct.
        /// </summary>
        [TestMethod]
        public void TestPropertiesCountValidate()
        {
            const int MaxPropertiesCount = 60;
            CustomProperties properties = new CustomProperties();
            Assert.AreEqual(0, properties.Properties.Count);
            for (int i = 0; i < MaxPropertiesCount; i++)
            {
                properties.Set("t" + i, "test");
                Assert.AreEqual(i + 1, properties.Properties.Count);
            }
            properties.Set("over1", "test");
            Assert.AreEqual(MaxPropertiesCount, properties.Properties.Count);
            properties.Set("over2", "test");
            Assert.AreEqual(MaxPropertiesCount, properties.Properties.Count);
        }

        /// <summary>
        /// Verify that string setting correct.
        /// </summary>
        [TestMethod]
        public void TestSetString()
        {
            var key = "test";
            CustomProperties properties = new CustomProperties();
            Assert.AreEqual(0, properties.Properties.Count);

            /* Null value. */
            string nullValue = null;
            properties.Set(key, nullValue);
            Assert.AreEqual(0, properties.Properties.Count);

            /* Long value. */
            string longValue = new string('?', 129);
            properties.Set(key, longValue);
            Assert.AreEqual(0, properties.Properties.Count);

            /* Normal value. */
            var normalValue = "test";
            properties.Set(key, normalValue);
            Assert.AreEqual(1, properties.Properties.Count);
            Assert.AreEqual(normalValue, properties.Properties[key]);
        }

        /// <summary>
        /// Verify that date setting correct.
        /// </summary>
        [TestMethod]
        public void TestSetDate()
        {
            var key = "test";
            CustomProperties properties = new CustomProperties();
            Assert.AreEqual(0, properties.Properties.Count);

            /* Normal value. */
            var normalValue = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            properties.Set(key, normalValue);
            Assert.AreEqual(1, properties.Properties.Count);
            Assert.AreEqual(normalValue, properties.Properties[key]);
        }

        /// <summary>
        /// Verify that number setting correct.
        /// </summary>
        [TestMethod]
        public void TestSetNumber()
        {
            CustomProperties properties = new CustomProperties();
            Assert.AreEqual(0, properties.Properties.Count);

            /* Normal value. */
            var value1 = 1;
            var value2 = 100L;
            var value3 = 0.1f;
            var value4 = 0.1m;
            var value5 = 0.1;
            properties.Set("t1", value1);
            properties.Set("t2", value2);
            properties.Set("t3", value3);
            properties.Set("t4", value4);
            properties.Set("t5", value5);
            Assert.AreEqual(5, properties.Properties.Count);
            Assert.AreEqual(value1, properties.Properties["t1"]);
            Assert.AreEqual(value2, properties.Properties["t2"]);
            Assert.AreEqual(value3, properties.Properties["t3"]);
            Assert.AreEqual(value4, properties.Properties["t4"]);
            Assert.AreEqual(value5, properties.Properties["t5"]);
        }

        /// <summary>
        /// Verify that bool setting correct.
        /// </summary>
        [TestMethod]
        public void TestSetBool()
        {
            var key = "test";
            CustomProperties properties = new CustomProperties();
            Assert.AreEqual(0, properties.Properties.Count);

            /* Normal value. */
            var normalValue = false;
            properties.Set(key, normalValue);
            Assert.AreEqual(1, properties.Properties.Count);
            Assert.AreEqual(normalValue, properties.Properties[key]);
        }

        /// <summary>
        /// Verify that clear correct.
        /// </summary>
        [TestMethod]
        public void TestClear()
        {
            var key = "test";
            CustomProperties properties = new CustomProperties();
            Assert.AreEqual(0, properties.Properties.Count);
            properties.Clear(key);
            Assert.AreEqual(1, properties.Properties.Count);
            Assert.IsNull(properties.Properties[key]);
        }
    }
}
