using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace CoreData
{
    /// <summary>
    /// Allows for one-way serialisation of an object graph to a set of SQLite queries that are compatible with
    /// Core Data, assuming the object graph on the device is set up in that way.
    /// </summary>
    public class CoreDataSerializer
    {
        /// <summary>
        /// Query to resync the Z_PRIMARYKEY table for when we change the number of records in use.
        /// </summary>
        private const string PrimaryKeySyncQuery = "UPDATE Z_PRIMARYKEY SET Z_MAX = (SELECT MAX(Z_PK) FROM Z{0}) WHERE Z_NAME='{1}';";

        /// <summary>
        /// The date and time that all CoreData timestamps start from.
        /// </summary>
        private static readonly DateTime CoreDataEpoch = new DateTime(2001, 1, 1);

        public delegate string ValueConverter(object value);

        public static Dictionary<Type, ValueConverter> DefaultValueConverters = new Dictionary<Type, ValueConverter>
        {
            {typeof (DateTime), value => Convert.ToString((((DateTime) value) - CoreDataEpoch).TotalSeconds)},
            {typeof (bool), value => (bool) value ? "1" : "0"}
        };

        /// <summary>
        /// A mapping of Type to a function that can convert instances of the given type to a String. See
        /// the contents of DefaultValueConverters first before adding your own as most are there.
        /// </summary>
        public Dictionary<Type, ValueConverter> ValueConverters { get; private set; }

        /// <summary>
        /// The graph object that this serialiser represents.
        /// </summary>
        public object Graph { get; private set; }

        private ObjectGraph _objectGraph;

        /// <summary>
        /// Keeps track of primary keys to maintain referential integrity - <see cref="PrimaryKeyCollection"/>
        /// </summary>
        private readonly PrimaryKeyCollection _primaryKeys = new PrimaryKeyCollection();

        /// <summary>
        /// The default set of types that will be ignored by the CoreDataSerializer. It is not necessary to add
        /// things like int32 here, only reference types will be walked.
        /// </summary>
        public static Type[] DefaultIgnoredTypes = new [] {typeof (String)};

        /// <summary>
        /// The collapsed object graph.
        /// </summary>
        public IEnumerable<object> GraphNodes { get; private set; }

        /// <summary>
        /// Ignore the given types when serializing. All subsequent nodes will be ignored as well. Call
        /// <see cref="Refresh"/> after setting this.
        /// </summary>
        public List<Type> IgnoredTypes { get; private set; }

        /// <summary>
        /// A list of all the unique types in use.
        /// </summary>
        public IEnumerable<Type> Types { get; private set; }

        /// <summary>
        /// When building the node graph, don't include the root node.
        /// </summary>
        public bool IgnoreRoot { get; set; }

        /// <summary>
        /// Creates a new CoreDataSerializer instance with the given object graph. The graph will be cached
        /// immediately. Subsequent changes to the object graph will require a call to <see cref="Refresh"/>.
        /// </summary>
        /// <param name="graph"></param>
        public CoreDataSerializer(object graph)
        {
            if (graph == null)
            {
                throw new ArgumentNullException("graph");
            }

            this.Graph = graph;
            this.IgnoredTypes = DefaultIgnoredTypes.ToList();
            this.ValueConverters = new Dictionary<Type, ValueConverter>(DefaultValueConverters);

            this.Refresh();
        }

        /// <summary>
        /// Re-reads the object graph with updated settings in this class.
        /// </summary>
        public void Refresh()
        {
            this._objectGraph = new ObjectGraph(this.Graph)
                                    {
                                        IgnoredTypes = this.IgnoredTypes,
                                        IgnoreRoot = this.IgnoreRoot,
                                        IncludeStrings = false
                                    };

            List<object> nodes = this._objectGraph.Collapse().ToList();
            this.GraphNodes = nodes;

            this._primaryKeys.Clear();
            nodes.ForEach(node => this._primaryKeys.Add(node));

            this.Types = nodes.Select(node => node.GetType()).Distinct();
        }

        public IEnumerable<CoreDataCommand> Commands
        {
            get
            {
                foreach (object node in this.GraphNodes)
                {
                    Type nodeType = node.GetType();

                    CoreDataCommand command = new CoreDataCommand
                                                  {
                                                      ObjectName = nodeType.Name
                                                  };

                    IEnumerable<PropertyInfo> properties = from property in nodeType.GetProperties()
                                                           where property.PropertyType == typeof (String)
                                                                 || property.PropertyType.GetInterface("IEnumerable") == null
                                                           select property;

                    foreach (PropertyInfo property in properties)
                    {
                        object value = property.GetValue(node, null);
                        bool isIgnored = property.GetCustomAttributes(typeof (IgnoreDataMemberAttribute), false).Any();
                        bool isBackReference = property.GetCustomAttributes(typeof(BackReferenceAttribute), false).Any();

                        if (isIgnored)
                        {
                            continue;
                        }

                        if (isBackReference)
                        {
                            object backReference = this._objectGraph.ResolveBackReference(node);
                            command.Parameters[property.Name] = Convert.ToString(_primaryKeys.GetKeyFor(backReference));
                        }
                        else if (value == null)
                        {
                            command.Parameters[property.Name] = "";
                        }
                        else if (_primaryKeys.Contains(value))
                        {
                            command.Parameters[property.Name] = Convert.ToString(_primaryKeys.GetKeyFor(value));
                        }
                        else
                        {
                            command.Parameters[property.Name] = ConvertValue(value);
                        }
                    }

                    yield return command;
                }
            }
        }

        /// <summary>
        /// Checks <see cref="ValueConverters"/> to see if there is a user-supplied function to 
        /// convert the value. If not, attempt to convert it to a string using the built-in methods.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private string ConvertValue(object value)
        {
            if (this.ValueConverters.ContainsKey(value.GetType()))
            {
                ValueConverter converter = this.ValueConverters[value.GetType()];
                return converter.Invoke(value);
            }

            if (value is Enum)
            {
                return Convert.ToString(Convert.ToInt32(value));
            }

            return Convert.ToString(value);
        }

        /// <summary>
        /// Retrieves the full INSERT payload for this serializer, including the resync commands.
        /// </summary>
        public string Sql
        {
            get
            {
                return "BEGIN TRANSACTION;\r\n"
                    + this.Commands.Aggregate("", (output, command) => output + command.Sql + "\r\n")
                    + "\r\n\r\n"
                    + this.Types.Aggregate("", (output, type) => output + 
                        String.Format(PrimaryKeySyncQuery, type.Name.ToUpper(), type.Name) + "\r\n")
                    + "\r\n"
                    + "COMMIT;";
            }
        }
    }
}
