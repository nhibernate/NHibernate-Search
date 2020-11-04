using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using Iesi.Collections;
using NHibernate.Connection;

namespace NHibernate.Test
{
	/// <summary>
	/// This connection provider keeps a list of all open connections,
	/// it is used when testing to check that tests clean up after themselves.
	/// </summary>
	public class DebugConnectionProvider : DriverConnectionProvider
	{
		private ISet<DbConnection> connections = new HashSet<DbConnection>();

		public override DbConnection GetConnection()
		{
			DbConnection connection = base.GetConnection();
			connections.Add(connection);
			return connection;
		}

		public override void CloseConnection(DbConnection conn)
		{
			base.CloseConnection(conn);
			connections.Remove(conn);
		}

		public bool HasOpenConnections
		{
			get
			{
				// check to see if all connections that were at one point opened
				// have been closed through the CloseConnection
				// method
				if (!connections.Any())
				{
					// there are no connections, either none were opened or
					// all of the closings went through CloseConnection.
					return false;
				}
				else
				{
					// Disposing of an ISession does not call CloseConnection (should it???)
					// so a Diposed of ISession will leave an IDbConnection in the list but
					// the IDbConnection will be closed (atleast with MsSql it works this way).
					foreach (IDbConnection conn in connections)
					{
						if (conn.State != ConnectionState.Closed)
						{
							return true;
						}
					}

					// all of the connections have been Disposed and were closed that way
					// or they were Closed through the CloseConnection method.
					return false;
				}
			}
		}

		public void CloseAllConnections()
		{
			while (connections.Any())
			{
                IEnumerator en = connections.GetEnumerator();
                if (en.MoveNext())
                {
                    DbConnection conn = en.Current as DbConnection;
                    en.Reset();
                    CloseConnection(conn);
                }
			}
		}
	}
}