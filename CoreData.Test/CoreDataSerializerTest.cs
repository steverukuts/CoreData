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
        private class Factory
        {
            public IEnumerable<Worker> Workers { get; set; }
            public IEnumerable<Department> Departments { get; set; } 
            public string Name { get; set; }
        }

        private class Worker
        {
            public string Name { get; set; }
            public float Salary { get; set; }
            public Department Department { get; set; }
            public IEnumerable<Worker> Friends { get; set; }
            public DateTime HireDate { get; set; }

            [IgnoreDataMember]
            public string Password { get; set; }

            public byte[] Image { get; set; }

            public bool IsEnabled { get; set; }

            /// <summary>
            /// We should be checking for duplicates using reference value, not GetHashCode.
            /// </summary>
            /// <returns></returns>
            public override int GetHashCode()
            {
                return new Random().Next();
            }
        }

        private class Department
        {
            public string Name { get; set; }
            public IEnumerable<Worker> Workers { get; set; } 
        }

        private static Factory GetSimpleGraph()
        {
            return new Factory
                       {
                           Name = "Magrathea",
                           Workers = new[]
                               {
                                   new Worker {Name = "Arthur"},
                                   new Worker {Name = "Marvin"},
                                   new Worker {Name = "Zaphod"}
                               }
                       };
        }

        private static Factory GetDuplicateReferenceGraph()
        {
            Worker arthur = new Worker {Name = "Arthur"};
            Worker marvin = new Worker {Name = "Marvin"};
            Worker zaphod = new Worker {Name = "Zaphod"};

            Department sales = new Department {Name = "Sales", Workers = new[] {arthur, marvin}};
            Department engineering = new Department {Name = "Engineering", Workers = new[] {marvin, zaphod}};

            return new Factory
                       {
                           Name = "Magrathea",
                           Workers = new[] {arthur, marvin, zaphod},
                           Departments = new[] {sales, engineering}
                       };
        }

        private static Factory GetRecursiveGraph()
        {
            Worker arthur = new Worker {Name = "Arthur"};
            arthur.Friends = new[] {arthur};

            Department sales = new Department {Name = "Sales", Workers = new[] {arthur}};
            arthur.Department = sales;
            
            return new Factory
                       {
                           Name = "Magrathea",
                           Workers = new[] {arthur}
                       };
        }

        [TestMethod]
        public void TestCollapseNodeGraphCount()
        {
            Factory graph = GetSimpleGraph();
            CoreDataSerializer serializer = new CoreDataSerializer(graph);
            Assert.AreEqual(4, serializer.GraphNodes.Count());
        }

        [TestMethod]
        public void TestNodeGraphCollapseContents()
        {
            Factory graph = GetSimpleGraph();
            CoreDataSerializer serializer = new CoreDataSerializer(graph);
            Assert.IsTrue(serializer.GraphNodes.Contains(graph));
            Assert.IsTrue(serializer.GraphNodes.Contains(graph.Workers.First()));
        }
        
        [TestMethod]
        public void TestCollapseNodeGraphDuplicateReferences()
        {
            Factory graph = GetDuplicateReferenceGraph();
            CoreDataSerializer serializer = new CoreDataSerializer(graph);
            Assert.AreEqual(6, serializer.GraphNodes.Count());
        }

        [TestMethod]
        public void TestCollapseNodeGraphRecursion()
        {
            Factory graph = GetRecursiveGraph();
            CoreDataSerializer serializer = new CoreDataSerializer(graph);
            Assert.AreEqual(3, serializer.GraphNodes.Count());
        }

        [TestMethod]
        public void TestIgnoreTypes()
        {
            Factory graph = GetRecursiveGraph();
            CoreDataSerializer serializer = new CoreDataSerializer(graph);
            serializer.IgnoredTypes.Add(typeof (Worker));
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
            Factory emptyFactory = new Factory {Name = "Magrathea"};
            CoreDataSerializer serializer = new CoreDataSerializer(emptyFactory);
            IEnumerable<CoreDataCommand> commands = serializer.Commands;
            CoreDataCommand command = commands.First();

            Assert.AreEqual("Factory", command.ObjectName);
            Assert.AreEqual("Magrathea", command.Parameters["Name"]);
        }

        [TestMethod]
        public void TestBackwardRelation()
        {
            Worker arthur = new Worker {Name = "Arthur"};
            Department sales = new Department {Name = "sales", Workers = new[] {arthur}};
            arthur.Department = sales;

            CoreDataSerializer serializer = new CoreDataSerializer(sales);
            IEnumerable<CoreDataCommand> commands = serializer.Commands.ToList();
            Assert.AreEqual(2, commands.Count());

            CoreDataCommand workerCommand = commands.First(command => command.ObjectName == "Worker");
            Assert.AreEqual("1", workerCommand.Parameters["Department"]);
        }

        [TestMethod]
        public void TestSerializeNullValue()
        {
            Worker arthur = new Worker { Name = null };
            CoreDataSerializer serializer = new CoreDataSerializer(arthur);

            IEnumerable<CoreDataCommand> commands = serializer.Commands.ToList();
            CoreDataCommand command = commands.First();
            Assert.AreEqual("", command.Parameters["Name"]);
        }

        [TestMethod]
        public void TestTypeList()
        {
            Factory graph = GetSimpleGraph();
            CoreDataSerializer serializer = new CoreDataSerializer(graph);
            Assert.AreEqual(2, serializer.Types.Count());
            Assert.IsTrue(serializer.Types.Contains(typeof(Worker)));
            Assert.IsTrue(serializer.Types.Contains(typeof(Factory)));
        }

        [TestMethod]
        public void TestIgnoreRoot()
        {
            Factory graph = GetSimpleGraph();
            CoreDataSerializer serializer = new CoreDataSerializer(graph) {IgnoreRoot = true};
            serializer.Refresh();

            Assert.AreEqual(3, serializer.GraphNodes.Count());
            Assert.IsTrue(serializer.Types.Contains(typeof(Worker)));
            Assert.IsFalse(serializer.Types.Contains(typeof(Factory)));
        }

        [TestMethod]
        public void TestIgnoreDataMember()
        {
            Worker arthur = new Worker {Name = "Arthur", Password = "12345"};
            CoreDataSerializer serializer = new CoreDataSerializer(arthur);

            IEnumerable<CoreDataCommand> commands = serializer.Commands.ToList();
            CoreDataCommand command = commands.First();

            Assert.IsFalse(command.Parameters.ContainsKey("Password"));
        }

        /// <summary>
        /// Regression test for a crash that would occur when attempting to walk an array of structs.
        /// </summary>
        [TestMethod]
        public void TestArrayOfStructsException()
        {
            Worker arthur = new Worker { Name = "Arthur", Image = new byte[] {0, 1, 2, 3, 4, 5}};
            ObjectGraph graph = new ObjectGraph(arthur) {IncludeReferenceTypes = true};
            IEnumerable<object> objects = graph.Collapse();

            Assert.AreEqual(6, objects.Count(item => item is byte));
        }

        [TestMethod]
        public void TestDateTimeOutput()
        {
            Worker arthur = new Worker {Name = "Arthur", HireDate = new DateTime(2001, 01, 01, 08, 0, 0)};
            CoreDataSerializer serializer = new CoreDataSerializer(arthur);

            CoreDataCommand command = serializer.Commands.First();
            Assert.AreEqual("28800", command.Parameters["HireDate"]);
        }

        [TestMethod]
        public void TestBooleanOutput()
        {
            Worker arthur = new Worker {Name = "Arthur", IsEnabled = true};
            CoreDataSerializer serializer = new CoreDataSerializer(arthur);

            CoreDataCommand command = serializer.Commands.First();
            Assert.AreEqual("1", command.Parameters["IsEnabled"]);
        }

        [TestMethod]
        public void TestCustomConverter()
        {
            Worker arthur = new Worker {Name = "Arthur"};
            CoreDataSerializer serializer = new CoreDataSerializer(arthur);
            serializer.ValueConverters[typeof (string)] = value => Convert.ToString(value) + " 1234";
            
            CoreDataCommand command = serializer.Commands.First();
            Assert.AreEqual("Arthur 1234", command.Parameters["Name"]);
        }

        /// <summary>
        /// Regression test for an issue where the default value converter dictionary itself was
        /// copied to ValueConverters instead of a copy of the dictionary.
        /// </summary>
        [TestMethod]
        public void TestDefaultsAreNotModified()
        {
            Worker arthur = new Worker { Name = "Arthur" };

            int defaultConverters = CoreDataSerializer.DefaultValueConverters.Count;

            CoreDataSerializer serializer = new CoreDataSerializer(arthur);
            serializer.ValueConverters[typeof(string)] = value => Convert.ToString(value) + " 1234";

            Assert.AreEqual(defaultConverters, CoreDataSerializer.DefaultValueConverters.Count);
        }
    }
}
