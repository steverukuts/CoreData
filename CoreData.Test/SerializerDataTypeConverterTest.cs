using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoreData.Test
{
    [TestClass]
    public class SerializerDataTypeConverterTest
    {
        [TestMethod]
        public void TestDateTimeOutput()
        {
            TestGraph.Worker arthur = new TestGraph.Worker { Name = "Arthur", HireDate = new DateTime(2001, 01, 01, 08, 0, 0) };
            CoreDataSerializer serializer = new CoreDataSerializer(arthur);

            CoreDataCommand command = serializer.Commands.First();
            Assert.AreEqual("28800", command.Parameters["HireDate"]);
        }

        [TestMethod]
        public void TestBooleanOutput()
        {
            TestGraph.Worker arthur = new TestGraph.Worker { Name = "Arthur", IsEnabled = true };
            CoreDataSerializer serializer = new CoreDataSerializer(arthur);

            CoreDataCommand command = serializer.Commands.First();
            Assert.AreEqual("1", command.Parameters["IsEnabled"]);
        }

        [TestMethod]
        public void TestCustomConverter()
        {
            TestGraph.Worker arthur = new TestGraph.Worker { Name = "Arthur" };
            CoreDataSerializer serializer = new CoreDataSerializer(arthur);
            serializer.ValueConverters[typeof(string)] = value => Convert.ToString(value) + " 1234";

            CoreDataCommand command = serializer.Commands.First();
            Assert.AreEqual("Arthur 1234", command.Parameters["Name"]);
        }

        [TestMethod]
        public void TestSerializeNullValue()
        {
            TestGraph.Worker arthur = new TestGraph.Worker { Name = null };
            CoreDataSerializer serializer = new CoreDataSerializer(arthur);

            IEnumerable<CoreDataCommand> commands = serializer.Commands.ToList();
            CoreDataCommand command = commands.First();
            Assert.AreEqual("", command.Parameters["Name"]);
        }

        [TestMethod]
        public void TestSerializeEnum()
        {
            TestGraph.Worker arthur = new TestGraph.Worker {Name = "Arthur", Status = TestGraph.WorkerStatus.Normal};
            CoreDataSerializer serializer = new CoreDataSerializer(arthur);

            CoreDataCommand command = serializer.Commands.First();
            Assert.AreEqual("3", command.Parameters["Status"]);
        }
    }
}
