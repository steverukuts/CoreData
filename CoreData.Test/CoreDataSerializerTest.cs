using System;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoreData.Test
{
    [TestClass]
    public class CoreDataSerializerTest
    {
        [TestMethod]
        public void TestCollapseNodeGraphCount()
        {
            TestGraph.Factory graph = TestGraph.GetSimpleGraph();
            CoreDataSerializer serializer = new CoreDataSerializer(graph);
            Assert.AreEqual(4, serializer.GraphNodes.Count());
        }

        [TestMethod]
        public void TestNodeGraphCollapseContents()
        {
            TestGraph.Factory graph = TestGraph.GetSimpleGraph();
            CoreDataSerializer serializer = new CoreDataSerializer(graph);
            Assert.IsTrue(serializer.GraphNodes.Contains(graph));
            Assert.IsTrue(serializer.GraphNodes.Contains(graph.Workers.First()));
        }
        
        [TestMethod]
        public void TestCollapseNodeGraphDuplicateReferences()
        {
            TestGraph.Factory graph = TestGraph.GetDuplicateReferenceGraph();
            CoreDataSerializer serializer = new CoreDataSerializer(graph);
            Assert.AreEqual(6, serializer.GraphNodes.Count());
        }

        [TestMethod]
        public void TestCollapseNodeGraphRecursion()
        {
            TestGraph.Factory graph = TestGraph.GetRecursiveGraph();
            CoreDataSerializer serializer = new CoreDataSerializer(graph);
            Assert.AreEqual(3, serializer.GraphNodes.Count());
        }

        [TestMethod]
        public void TestIgnoreTypes()
        {
            TestGraph.Factory graph = TestGraph.GetRecursiveGraph();
            CoreDataSerializer serializer = new CoreDataSerializer(graph);
            serializer.IgnoredTypes.Add(typeof (TestGraph.Worker));
            serializer.Refresh();
            Assert.AreEqual(1, serializer.GraphNodes.Count());
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestNullConstructor()
        {
            new CoreDataSerializer(null);
        }

        [TestMethod]
        public void TestGetFactoryCommand()
        {
            TestGraph.Factory emptyFactory = new TestGraph.Factory {Name = "Magrathea"};
            CoreDataSerializer serializer = new CoreDataSerializer(emptyFactory);
            IEnumerable<CoreDataCommand> commands = serializer.Commands;
            CoreDataCommand command = commands.First();

            Assert.AreEqual("Factory", command.ObjectName);
            Assert.AreEqual("Magrathea", command.Parameters["Name"]);
        }

        [TestMethod]
        public void TestBackwardRelation()
        {
            TestGraph.Worker arthur = new TestGraph.Worker {Name = "Arthur"};
            TestGraph.Department sales = new TestGraph.Department {Name = "sales", Workers = new[] {arthur}};
            arthur.CurrentDepartment = sales;

            CoreDataSerializer serializer = new CoreDataSerializer(sales);
            IEnumerable<CoreDataCommand> commands = serializer.Commands.ToList();
            Assert.AreEqual(2, commands.Count());

            CoreDataCommand workerCommand = commands.First(command => command.ObjectName == "Worker");
            Assert.AreEqual("1", workerCommand.Parameters["CurrentDepartment"]);
        }

        [TestMethod]
        public void TestTypeList()
        {
            TestGraph.Factory graph = TestGraph.GetSimpleGraph();
            CoreDataSerializer serializer = new CoreDataSerializer(graph);
            Assert.AreEqual(2, serializer.Types.Count());
            Assert.IsTrue(serializer.Types.Contains(typeof(TestGraph.Worker)));
            Assert.IsTrue(serializer.Types.Contains(typeof(TestGraph.Factory)));
        }

        [TestMethod]
        public void TestIgnoreRoot()
        {
            TestGraph.Factory graph = TestGraph.GetSimpleGraph();
            CoreDataSerializer serializer = new CoreDataSerializer(graph) {IgnoreRoot = true};
            serializer.Refresh();

            Assert.AreEqual(3, serializer.GraphNodes.Count());
            Assert.IsTrue(serializer.Types.Contains(typeof(TestGraph.Worker)));
            Assert.IsFalse(serializer.Types.Contains(typeof(TestGraph.Factory)));
        }

        [TestMethod]
        public void TestIgnoreDataMember()
        {
            TestGraph.Worker arthur = new TestGraph.Worker {Name = "Arthur", Password = "12345"};
            CoreDataSerializer serializer = new CoreDataSerializer(arthur);

            IEnumerable<CoreDataCommand> commands = serializer.Commands.ToList();
            CoreDataCommand command = commands.First();

            Assert.IsFalse(command.Parameters.ContainsKey("Password"));
        }

        /// <summary>
        /// Regression test for an issue where the default value converter dictionary itself was
        /// copied to ValueConverters instead of a copy of the dictionary.
        /// </summary>
        [TestMethod]
        public void TestDefaultsAreNotModified()
        {
            TestGraph.Worker arthur = new TestGraph.Worker { Name = "Arthur" };

            int defaultConverters = CoreDataSerializer.DefaultValueConverters.Count;

            CoreDataSerializer serializer = new CoreDataSerializer(arthur);
            serializer.ValueConverters[typeof(string)] = value => Convert.ToString(value) + " 1234";

            Assert.AreEqual(defaultConverters, CoreDataSerializer.DefaultValueConverters.Count);
        }
    }
}
