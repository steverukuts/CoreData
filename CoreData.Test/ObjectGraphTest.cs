using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CoreData;

namespace CoreData.Test
{
    [TestClass]
    public class ObjectGraphTest
    {
        class Product
        {
            public static IEnumerable<Product> Products
            {
                get { return new[] {new Product {Name = "Outrageously priced product"}}; }
            }

            public string Name { get; set; }

            [BackReference]
            public Shop Shop { get; set; }
        }

        class Shop
        {
            public List<Product> Products { get; set; }

            public Owner Owner { get; set; }

            public Shop()
            {
                this.Products = new List<Product>();
            }
        }

        class Owner
        {
            public string Name { get; set; }

            [BackReference]
            public Shop Shop { get; set; }
        }

        [TestMethod]
        public void TestContainsObject()
        {
            Product toaster = new Product {Name = "toaster"};
            Shop shop = new Shop {Products = {toaster}};

            ObjectGraph graph = new ObjectGraph(shop);
            Assert.IsTrue(graph.ContainsNode(toaster));
        }

        [TestMethod]
        public void TestDoesNotContainObject()
        {
            Product kettle = new Product { Name = "kettle" };
            Product toaster = new Product { Name = "toaster" };
            Shop shop = new Shop {Products = {kettle}};

            ObjectGraph graph = new ObjectGraph(shop);
            Assert.IsFalse(graph.ContainsNode(toaster));
        }

        [TestMethod]
        public void TestResolveBackReferenceInEnumerable()
        {
            Product toaster = new Product {Name = "toaster"};
            Shop shop = new Shop {Products = {toaster}};

            ObjectGraph graph = new ObjectGraph(shop);
            object backReference = graph.ResolveBackReference(toaster);
            Assert.IsTrue(Object.ReferenceEquals(backReference, shop));
        }

        [TestMethod]
        public void TestResolveBackReference()
        {
            Owner owner = new Owner {Name = "Arthur Dent"};
            Shop shop = new Shop {Owner = owner};

            ObjectGraph graph = new ObjectGraph(shop);
            object backReference = graph.ResolveBackReference(owner);
            Assert.IsTrue(Object.ReferenceEquals(backReference, shop));
        }

        /// <summary>
        /// Regression test for a crash that would occur when attempting to walk a node with static
        /// properties that returned instances of that node.
        /// </summary>
        [TestMethod]
        public void TestStaticPropertyCrash()
        {
            Product toaster = new Product { Name = "toaster" };
            Shop shop = new Shop { Products = { toaster } };
            ObjectGraph graph = new ObjectGraph(shop);
            graph.Collapse();
        }
    }
}
