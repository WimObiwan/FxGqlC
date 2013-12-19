using System;
using System.Data.Common;

namespace FxGqlLib
{
	public class DataProvider : IProvider
	{
		readonly FileOptionsFromClause fileOptions;
		DbConnection connection;
		DbCommand command;
		DbDataReader dataReader;
		ColumnName[] columnNames;
		Type[] columnTypes;
		ProviderRecord record;

		public DataProvider (FileOptionsFromClause fileOptions)
		{
			this.fileOptions = fileOptions;
		}

		#region IDisposable implementation

		public void Dispose ()
		{
			Uninitialize ();
		}

		#endregion

		#region IProvider implementation

		public string[] GetAliases ()
		{
			return null;
		}

		public ColumnName[] GetColumnNames ()
		{
			return columnNames;
		}

		public int GetColumnOrdinal (ColumnName columnName)
		{
			return Array.FindIndex (columnNames, a => a.CompareTo (columnName) == 0);
		}

		public Type[] GetColumnTypes ()
		{
			return columnTypes;
		}

		public void Initialize (GqlQueryState gqlQueryState)
		{
			string client = fileOptions.Client ?? "System.Data.SqlClient";
			DbProviderFactory factory = DbProviderFactories.GetFactory (client);
			DbConnection connection = factory.CreateConnection ();
			connection.ConnectionString = fileOptions.ConnectionString;
			connection.Open ();
			command = connection.CreateCommand ();
			command.CommandText = fileOptions.FileName.Evaluate (gqlQueryState);
			dataReader = command.ExecuteReader (System.Data.CommandBehavior.SingleResult | System.Data.CommandBehavior.CloseConnection);
			columnNames = new ColumnName[dataReader.FieldCount];
			columnTypes = new Type[dataReader.FieldCount];
			for (int i = 0; i < dataReader.FieldCount; i++) {
				columnNames [i] = new ColumnName (dataReader.GetName (i));
				/*Type type = dataReader.GetFieldType (i);
				if (type == typeof(Boolean)) {
					columnTypes [i] = typeof(DataBoolean);
				} else if (type == typeof(Byte) || type == typeof(Int16) || type == typeof(Int32) || type == typeof(Int64)) {
					columnTypes [i] = typeof(DataInteger);
				} else if (type == typeof(DateTime) || type == typeof(TimeSpan)) {
					columnTypes [i] = typeof(DataDateTime);
				} else*/
				{
					columnTypes [i] = typeof(DataString);
				}
			}
			record = new ProviderRecord (this, true);
			record.LineNo = 0;
			record.Source = client;
		}

		public bool GetNextRecord ()
		{
			if (!dataReader.Read ())
				return false;

			record.LineNo++;
			record.TotalLineNo = record.LineNo;
			for (int i = 0; i < record.Columns.Length; i++) {
				/*try {
					IData data;
					if (columnTypes [i] == typeof(DataBoolean)) {
						if (!dataReader.IsDBNull (i))							data = new DataBoolean (Convert.ToBoolean (dataReader.GetValue (i)));
						else
							data = new DataBoolean ();
					} else if (columnTypes [i] == typeof(DataInteger)) {
						if (!dataReader.IsDBNull (i))
							data = new DataInteger (Convert.ToInt64 (dataReader.GetValue (i)));
						else
							data = new DataInteger ();
					} else if (columnTypes [i] == typeof(DataDateTime)) {
						if (!dataReader.IsDBNull (i))
							data = new DataDateTime (Convert.ToDateTime (dataReader.GetValue (i)));
						else
							data = new DataDateTime ();
					} else {
						if (!dataReader.IsDBNull (i))
							data = new DataString (dataReader.GetValue (i).ToString ());
						else
							data = new DataString ();
					}
					record.Columns [i] = data;
				} catch (InvalidCastException) {
					throw new ConversionException (dataReader.GetFieldType (i), columnTypes [i]);
				}*/
				record.Columns [i] = new DataString (dataReader.GetValue (i).ToString ());
			}

			return true;
		}

		public void Uninitialize ()
		{
			record = null;
			if (dataReader != null) {
				dataReader.Close ();
				dataReader.Dispose ();
				dataReader = null;
			}
			if (command != null) {
				command.Dispose ();
				command = null;
			}
			if (connection != null) {
				connection.Close ();
				connection.Dispose ();
				connection = null;
			}
		}

		public ProviderRecord Record {
			get {
				return record;
			}
		}

		#endregion

	}
}

