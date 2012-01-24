using System;
using System.Collections.Generic;
using System.Linq;

namespace CoreData
{
    /// <summary>
    /// Represents a single INSERT command within a CoreData sql command payload, with a property, <see cref="Sql"/>
    /// to retrieve it.
    /// </summary>
    public class CoreDataCommand
    {
        /// <summary>
        /// This query retrieves the Z_ENT field from Z_PRIMARYKEY to be inserted into the given table. I'm not
        /// sure why this information is duplicated, perhaps the coredata table names don't always match up to
        /// entity names.
        /// </summary>
        private const string EntityIdQuery = "SELECT `Z_ENT` FROM `Z_PRIMARYKEY` WHERE `Z_NAME` = '{0}'";

        /// <summary>
        /// This is the unformatted SQL that will be output by the <see cref="Sql"/> property.
        /// </summary>
        private const string OutputSql = "INSERT INTO `Z{0}` (`Z_ENT`, `Z_OPT`, {1}) VALUES (({2}), '1', {3});";

        /// <summary>
        /// The name of the object that we want to affect (i.e. Employee, Company, etc).
        /// </summary>
        public string ObjectName { get; set; }

        /// <summary>
        /// A dictionary of parameters that will be inserted into the given Object.
        /// </summary>
        public Dictionary<string, string> Parameters { get; private set; }

        public CoreDataCommand()
        {
            this.Parameters = new Dictionary<string, string>();
        }

        /// <summary>
        /// Utility method that will safely quote a column name for use in a CoreData query.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private static string QuoteColumnName(string token)
        {
            return String.Format("`Z{0}`", token.ToUpper());
        }

        /// <summary>
        /// Utility method that will safely quote a string so it can be inserted without an accidental SQL
        /// injection.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static string QuoteValue(string value)
        {
            return String.Format("'{0}'", value.Replace("'", "''"));
        }

        /// <summary>
        /// Retrieves the SQL statement that the CoreDataCommand will execute.
        /// </summary>
        public string Sql
        {
            get
            {
                string columnNames = String.Join(", ", (IEnumerable<string>) this.Parameters.Keys.Select(QuoteColumnName));
                string valueNames = String.Join(", ", (IEnumerable<string>) this.Parameters.Values.Select(QuoteValue));
                string idQuery = String.Format(EntityIdQuery, ObjectName);
                return String.Format(String.Format(OutputSql, ObjectName.ToUpper(), columnNames, idQuery, valueNames));
            }
        }
    }
}
