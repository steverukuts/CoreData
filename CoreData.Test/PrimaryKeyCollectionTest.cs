using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CoreData;

namespace SpaceAgeTests
{
    [TestClass]
    public class PrimaryKeyCollectionTest
    {
        [TestMethod]
        public void TestAddItem()
        {
            List<string> strings = new List<string>();

            PrimaryKeyCollection collection = new PrimaryKeyCollection();
            Assert.AreEqual(1, collection.Add(strings));
        }

        [TestMethod]
        public void TestContainsItem()
        {
            List<string> strings = new List<string>();
            PrimaryKeyCollection collection = new PrimaryKeyCollection();
            collection.Add(strings);
            Assert.IsTrue(collection.Contains(strings));
        }

        [TestMethod]
        public void TestDoesNotContain()
        {
            List<string> strings = new List<string>();
            PrimaryKeyCollection collection = new PrimaryKeyCollection();
            Assert.IsFalse(collection.Contains(strings));
        }

        /// <summary>
        /// Duplicate items should have the same primary key.
        /// </summary>
        [TestMethod]
        public void TestAddDuplicateItems()
        {
            List<string> strings = new List<string>();

            PrimaryKeyCollection collection = new PrimaryKeyCollection();
            Assert.AreEqual(1, collection.Add(strings));
            Assert.AreEqual(1, collection.Add(strings));
            Assert.AreEqual(1, collection.Add(strings));
        }

        [TestMethod]
        public void TestAddUniqueItems()
        {
            List<string> item1 = new List<string>();
            List<string> item2 = new List<string>();

            PrimaryKeyCollection collection = new PrimaryKeyCollection();
            collection.Add(item1);
            collection.Add(item2);

            Assert.AreEqual(1, collection.GetKeyFor(item1));
            Assert.AreEqual(2, collection.GetKeyFor(item2));
        }

        [TestMethod]
        public void TestAddItemsOfDifferentTypes()
        {
            List<string> strings1 = new List<string>();
            List<string> strings2 = new List<string>();
            Exception exception1 = new Exception();
            Exception exception2 = new Exception();

            PrimaryKeyCollection collection = new PrimaryKeyCollection();
            collection.Add(strings1);
            collection.Add(strings2);
            collection.Add(exception1);
            collection.Add(exception2);

            Assert.AreEqual(1, collection.GetKeyFor(strings1));
            Assert.AreEqual(2, collection.GetKeyFor(strings2));
            Assert.AreEqual(1, collection.GetKeyFor(exception1));
            Assert.AreEqual(2, collection.GetKeyFor(exception2));
        }

        [TestMethod]
        [ExpectedException(typeof(KeyNotFoundException))]
        public void TestGetKeyForNoItemsDefined()
        {
            PrimaryKeyCollection collection = new PrimaryKeyCollection();
            collection.GetKeyFor(new Exception());
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestGetKeyForItemNotFound()
        {
            List<string> strings1 = new List<string>();
            List<string> strings2 = new List<string>();

            PrimaryKeyCollection collection = new PrimaryKeyCollection();
            collection.Add(strings1);
            collection.GetKeyFor(strings2);
        }

        [TestMethod]
        public void TestClear()
        {
            List<string> strings1 = new List<string>();

            PrimaryKeyCollection collection = new PrimaryKeyCollection();
            collection.Add(strings1);
            collection.Clear();

            Assert.IsFalse(collection.Contains(strings1));
        }
    }
}
