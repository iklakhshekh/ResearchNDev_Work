using System;
using System.Collections;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace DAL
{
	public sealed class ConnectionClass
	{
		private SqlCommand cmd;

		private SqlConnection Con;

		private SqlDataAdapter adp;

		private string strConnection = string.Empty;

		private string strErr = string.Empty;

		private DataSet ds;

		public SqlConnection SetConnectionString()
		{
			this.strConnection = ConfigurationManager.AppSettings["ServerCon"];
			this.Con = new SqlConnection(this.strConnection);
			return this.Con;
		}

		public SqlConnection getConnection()
		{
			this.Con = this.SetConnectionString();
			return this.Con;
		}

		public void disconnect()
		{
			if (this.Con.State == ConnectionState.Open)
			{
				this.Con.Close();
			}
		}

		public void Connect()
		{
			if (this.Con.State == ConnectionState.Closed)
			{
				this.Con.Open();
			}
		}

		public DataSet FillDataSet(string txt)
		{
			this.cmd = new SqlCommand(txt, this.SetConnectionString());
			this.Connect();
			this.adp = new SqlDataAdapter(this.cmd);
			this.ds = new DataSet();
			this.adp.Fill(this.ds);
			this.disconnect();
			this.Con.Close();
			return this.ds;
		}

		public string GetUrl(string Url, int LID)
		{
			string result = "";
			string cmdText = string.Concat(new object[]
			{
				"Select  dbo.fn_GetUrl('",
				Url,
				"',",
				LID,
				") as Url"
			});
			this.cmd = new SqlCommand(cmdText, this.SetConnectionString());
			this.Connect();
			this.adp = new SqlDataAdapter(this.cmd);
			this.ds = new DataSet();
			this.adp.Fill(this.ds);
			this.disconnect();
			this.Con.Close();
			if (this.ds.Tables[0].Rows.Count > 0)
			{
				result = this.ds.Tables[0].Rows[0]["Url"].ToString();
			}
			return result;
		}

		public int ExecuteSqlHelper(string spName, SqlParameter[] param)
		{
			this.getConnection();
			this.Connect();
			this.cmd = new SqlCommand();
			this.cmd.Connection = this.Con;
			this.cmd.CommandType = CommandType.StoredProcedure;
			this.cmd.CommandText = spName;
			this.cmd.Parameters.AddRange(param);
			int result = this.cmd.ExecuteNonQuery();
			this.disconnect();
			return result;
		}

		public int ExecuteSQLQuery(string query)
		{
			this.getConnection();
			this.Connect();
			this.cmd = new SqlCommand();
			this.cmd.Connection = this.Con;
			this.cmd.CommandType = CommandType.Text;
			this.cmd.CommandText = query;
			int result = this.cmd.ExecuteNonQuery();
			this.disconnect();
			this.Con.Close();
			return result;
		}

		public int ExecuteSqlHelper(string spName)
		{
			this.getConnection();
			this.Connect();
			this.cmd = new SqlCommand();
			this.cmd.Connection = this.Con;
			this.cmd.CommandType = CommandType.StoredProcedure;
			this.cmd.CommandText = spName;
			int result = this.cmd.ExecuteNonQuery();
			this.disconnect();
			this.Con.Close();
			return result;
		}

		public DataSet ExeSqlHelperDS(string spName, SqlParameter[] param)
		{
			this.getConnection();
			this.Connect();
			this.cmd = new SqlCommand();
			this.cmd.Connection = this.Con;
			this.cmd.CommandType = CommandType.StoredProcedure;
			this.cmd.CommandText = spName;
			this.cmd.Parameters.AddRange(param);
			this.ds = new DataSet();
			this.adp = new SqlDataAdapter();
			this.adp.SelectCommand = this.cmd;
			this.adp.Fill(this.ds);
			this.disconnect();
			this.Con.Close();
			return this.ds;
		}

		public SqlDataReader ExeSqlProc(string spName, ArrayList values, ArrayList names, ArrayList types)
		{
			this.getConnection();
			this.Connect();
			SqlCommand sqlCommand = new SqlCommand();
			sqlCommand.Connection = this.Con;
			for (int i = 0; i < values.Count; i++)
			{
				SqlParameter sqlParameter = sqlCommand.Parameters.Add(names[i].ToString(), types[i]);
				sqlParameter.Direction = ParameterDirection.Input;
				sqlParameter.Value = values[i];
			}
			sqlCommand.CommandType = CommandType.StoredProcedure;
			sqlCommand.CommandText = spName;
			if (this.Con.State == ConnectionState.Closed)
			{
				this.Con.Open();
			}
			SqlDataReader result = sqlCommand.ExecuteReader();
			this.disconnect();
			this.Con.Close();
			return result;
		}

		public DataSet ExeSqlHelperDS(string spName)
		{
			this.getConnection();
			this.Connect();
			this.cmd = new SqlCommand();
			this.cmd.Connection = this.Con;
			this.cmd.CommandType = CommandType.StoredProcedure;
			this.cmd.CommandText = spName;
			this.ds = new DataSet();
			this.adp = new SqlDataAdapter();
			this.adp.SelectCommand = this.cmd;
			this.adp.Fill(this.ds);
			this.disconnect();
			this.Con.Close();
			return this.ds;
		}

		public SqlDataReader ExecuteSqlHelperDR(string spName)
		{
			this.getConnection();
			this.Connect();
			this.cmd = new SqlCommand();
			this.cmd.Connection = this.Con;
			this.cmd.CommandType = CommandType.StoredProcedure;
			this.cmd.CommandText = spName;
			return this.cmd.ExecuteReader();
		}

		public SqlDataReader ExecuteSqlHelperDR(string spName, SqlParameter[] param)
		{
			this.getConnection();
			this.Connect();
			this.cmd = new SqlCommand();
			this.cmd.Connection = this.Con;
			this.cmd.CommandType = CommandType.StoredProcedure;
			this.cmd.CommandText = spName;
			this.cmd.Parameters.AddRange(param);
			return this.cmd.ExecuteReader();
		}

		public static string Encript(string value)
		{
			byte[] bytes = Encoding.UTF8.GetBytes(value);
			value = Convert.ToBase64String(bytes);
			return value;
		}

		public static string Decript(string value)
		{
			byte[] bytes = Convert.FromBase64String(value);
			return Encoding.UTF8.GetString(bytes);
		}
	}
}
