using System;
using System.Collections;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace ReflectionTest
{

    /// <summary>
    /// Summary description for SQLConnection
    /// </summary>

    public class SQLConnection
    {
        #region variable declaration
        private SqlCommand cmd;
        private SqlConnection Con;
        private SqlDataAdapter adp;
        private string strConnection = string.Empty;
        private string strErr = string.Empty;
        private DataSet ds;
        private DataTable dt;
        #endregion

        #region methods
        /// <summary>
        /// Reading the connection string from WebConfig file and Setting up the Connection
        /// </summary>
        /// <returns>SqlConnection object</returns>
        public SqlConnection SetConnectionString()
        {
            this.Con = new SqlConnection(ConfigurationManager.AppSettings["sqlConnection"]);
            return this.Con;
        }


        /// <summary>
        /// Getting the SqlConnection
        /// </summary>
        /// <returns>SqlConnection object</returns>
        public SqlConnection GetConnection()
        {
            this.Con = this.SetConnectionString();
            return this.Con;
        }

        /// <summary>
        /// Closing the active connection
        /// </summary>
        public void Disconnect()
        {
            if (this.Con.State == ConnectionState.Open)
            {
                this.Con.Close();
            }
        }

        /// <summary>
        /// Opening the connection
        /// </summary>
        public void Connect()
        {
            if (this.Con.State == ConnectionState.Closed)
            {
                this.Con.Open();
            }
        }

        /// <summary>
        /// Retrieving data and filling to dataset
        /// </summary>
        /// <param name="query">SQL query to be executed</param>
        /// <returns>Dataset object</returns>
        public DataSet FillDataSet(string query)
        {
            this.cmd = new SqlCommand(query, this.SetConnectionString());
            this.Connect();
            this.adp = new SqlDataAdapter(this.cmd);
            this.ds = new DataSet();
            this.adp.Fill(this.ds);
            this.Disconnect();
            this.Con.Close();
            return this.ds;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="storedProcedure"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public int ExecuteSqlHelper(string storedProcedure, SqlParameter[] param)
        {
            this.GetConnection();
            this.Connect();
            this.cmd = new SqlCommand();
            this.cmd.Connection = this.Con;
            this.cmd.CommandType = CommandType.StoredProcedure;
            this.cmd.CommandText = storedProcedure;
            this.cmd.Parameters.AddRange(param);
            int result = this.cmd.ExecuteNonQuery();
            this.Disconnect();
            return result;
        }

        public int ExecuteSQLQuery(string query)
        {
            this.GetConnection();
            this.Connect();
            this.cmd = new SqlCommand();
            this.cmd.Connection = this.Con;
            this.cmd.CommandType = CommandType.Text;
            this.cmd.CommandText = query;
            int result = this.cmd.ExecuteNonQuery();
            this.Disconnect();
            this.Con.Close();
            return result;
        }

        public int ExecuteSqlHelper(string storedProcedure)
        {
            this.GetConnection();
            this.Connect();
            this.cmd = new SqlCommand();
            this.cmd.Connection = this.Con;
            this.cmd.CommandType = CommandType.StoredProcedure;
            this.cmd.CommandText = storedProcedure;
            this.cmd.CommandTimeout = 3600;
            int result = this.cmd.ExecuteNonQuery();
            this.Disconnect();
            this.Con.Close();
            return result;
        }

        public DataSet ExeSqlHelperDS(string storedProcedure, SqlParameter[] param)
        {
            this.GetConnection();
            this.Connect();
            this.cmd = new SqlCommand();
            this.cmd.Connection = this.Con;
            this.cmd.CommandType = CommandType.StoredProcedure;
            this.cmd.CommandText = storedProcedure;
            this.cmd.Parameters.AddRange(param);
            this.ds = new DataSet();
            this.adp = new SqlDataAdapter();
            this.adp.SelectCommand = this.cmd;
            this.cmd.CommandTimeout = 3600;
            this.adp.Fill(this.ds);
            this.Disconnect();
            this.Con.Close();
            return this.ds;
        }

        public SqlDataReader ExeSqlProc(string storedProcedure, ArrayList values, ArrayList names, ArrayList types)
        {
            this.GetConnection();
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
            sqlCommand.CommandText = storedProcedure;
            if (this.Con.State == ConnectionState.Closed)
            {
                this.Con.Open();
            }
            SqlDataReader result = sqlCommand.ExecuteReader();
            this.Disconnect();
            this.Con.Close();
            return result;
        }

        public DataSet ExeSqlHelperDS(string storedProcedure)
        {
            this.GetConnection();
            this.Connect();
            this.cmd = new SqlCommand();
            this.cmd.Connection = this.Con;
            this.cmd.CommandType = CommandType.StoredProcedure;
            this.cmd.CommandText = storedProcedure;
            this.ds = new DataSet();
            this.adp = new SqlDataAdapter();
            this.adp.SelectCommand = this.cmd;
            this.cmd.CommandTimeout = 3600;
            this.adp.Fill(this.ds);
            this.Disconnect();
            this.Con.Close();
            return this.ds;
        }

        public SqlDataReader ExecuteSqlHelperDR(string storedProcedure)
        {
            this.GetConnection();
            this.Connect();
            this.cmd = new SqlCommand();
            this.cmd.Connection = this.Con;
            this.cmd.CommandType = CommandType.StoredProcedure;
            this.cmd.CommandText = storedProcedure;
            return this.cmd.ExecuteReader();
        }

        public SqlDataReader ExecuteSqlHelperDR(string storedProcedure, SqlParameter[] param)
        {
            this.GetConnection();
            this.Connect();
            this.cmd = new SqlCommand();
            this.cmd.Connection = this.Con;
            this.cmd.CommandType = CommandType.StoredProcedure;
            this.cmd.CommandText = storedProcedure;
            this.cmd.Parameters.AddRange(param);
            return this.cmd.ExecuteReader();
        }

        public DataSet ExecuteSQLQuery(string query, int? flag = null)
        {
            this.GetConnection();
            this.Connect();
            this.cmd = new SqlCommand();
            this.cmd.Connection = this.Con;
            this.cmd.CommandType = CommandType.Text;
            this.cmd.CommandText = query;
            this.ds = new DataSet();
            this.adp = new SqlDataAdapter();
            this.adp.SelectCommand = this.cmd;
            this.adp.Fill(this.ds);
            this.Disconnect();
            this.Con.Close();
            return this.ds;
        }
        public DataTable ExeSqlQuery(string query)
        {
            this.GetConnection();
            this.Connect();
            this.cmd = new SqlCommand();
            this.cmd.Connection = this.Con;
            this.cmd.CommandType = CommandType.Text;
            this.cmd.CommandText = query;
            this.dt = new DataTable();
            this.adp = new SqlDataAdapter();
            this.adp.SelectCommand = this.cmd;
            this.adp.Fill(this.dt);
            this.Disconnect();
            this.Con.Close();
            return this.dt;
        }
        #endregion

    }
}

