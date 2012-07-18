
using System;
using System.Linq;
using System.Collections.Generic;

namespace FxGqlLib
{
	public class ParameterizedProvider : IProvider
	{
		readonly IProvider provider;
		readonly string[] parameterNames;
		readonly IExpression[] parameters;

		GqlQueryState gqlQueryState;

		public ParameterizedProvider (ViewDefinition viewDefinition, IExpression[] parameters)
		{
			this.provider = viewDefinition.Provider;
			this.parameterNames = viewDefinition.Parameters.Select (p => p.Item1).ToArray ();
			this.parameters = new IExpression[parameters.Length];

			if (parameters.Length != viewDefinition.Parameters.Count)
				throw new InvalidProgramException ();
			for (int i = 0; i < parameters.Length; i++) {
				this.parameters [i] = ConvertExpression.Create (viewDefinition.Parameters [i].Item2, parameters [i]);
			}
		}

		#region IDisposable implementation
		public void Dispose ()
		{
			provider.Dispose ();
		}
		#endregion

		#region IProvider implementation
		public string[] GetAliases ()
		{
			return provider.GetAliases ();
		}

		public ColumnName[] GetColumnNames ()
		{
			return provider.GetColumnNames ();
		}

		public int GetColumnOrdinal (ColumnName columnName)
		{
			return provider.GetColumnOrdinal (columnName);
		}

		public Type[] GetColumnTypes ()
		{
			return provider.GetColumnTypes ();
		}

		public void Initialize (GqlQueryState gqlQueryState)
		{
			this.gqlQueryState = new GqlQueryState (gqlQueryState, true);

			for (int i = 0; i < parameters.Length; i++) {
				IData result = parameters [i].EvaluateAsData (gqlQueryState);
				Variable variable = new Variable ();
				variable.Name = parameterNames [i];
				variable.Type = result.GetType ();
				variable.Value = result;

				this.gqlQueryState.Variables [variable.Name] = variable;
			}

			provider.Initialize (this.gqlQueryState);
		}

		public bool GetNextRecord ()
		{
			return provider.GetNextRecord ();
		}

		public void Uninitialize ()
		{
			provider.Uninitialize ();
		}

		public ProviderRecord Record {
			get {
				return provider.Record;
			}
		}
		#endregion

	}
}

