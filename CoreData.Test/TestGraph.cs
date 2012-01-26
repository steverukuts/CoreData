using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace CoreData.Test
{
    /// <summary>
    /// Provides the classes that we use to test the application. Also provides factory methods
    /// to return common configurations of the test graph (to test recursion, etc).
    /// </summary>
    public static class TestGraph
    {
        public enum WorkerStatus
        {
            Manager,
            Supervisor,
            Director,
            Normal
        }

        public class Factory
        {
            public IEnumerable<Worker> Workers { get; set; }
            public IEnumerable<Department> Departments { get; set; } 
            public string Name { get; set; }
        }

        public class Worker
        {
            public string Name { get; set; }
            public float Salary { get; set; }
            public Department CurrentDepartment { get; set; }
            public IEnumerable<Worker> Friends { get; set; }
            public DateTime HireDate { get; set; }
            public WorkerStatus Status { get; set; }

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

        public class Department
        {
            public string Name { get; set; }
            public IEnumerable<Worker> Workers { get; set; } 
        }

        public static Factory GetSimpleGraph()
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

        public static Factory GetDuplicateReferenceGraph()
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

        public static Factory GetRecursiveGraph()
        {
            Worker arthur = new Worker {Name = "Arthur"};
            arthur.Friends = new[] {arthur};

            Department sales = new Department {Name = "Sales", Workers = new[] {arthur}};
            arthur.CurrentDepartment = sales;
            
            return new Factory
                       {
                           Name = "Magrathea",
                           Workers = new[] {arthur}
                       };
        }
    }
}
