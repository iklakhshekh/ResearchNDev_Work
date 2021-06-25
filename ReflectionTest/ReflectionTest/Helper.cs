using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace ReflectionTest
{
    public static class Helper
    {

        #region "getobject filled object with property reconized"

        public static List<T> DataTableToList<T>(DataTable datatable) where T : new()
        {
            List<T> Temp = new List<T>();
            try
            {
                List<string> columnsNames = new List<string>();
                foreach (DataColumn DataColumn in datatable.Columns)
                    columnsNames.Add(DataColumn.ColumnName);
                Temp = datatable.AsEnumerable().ToList().ConvertAll<T>(row => getObject<T>(row, columnsNames));
                return Temp;
            }
            catch
            {
                return Temp;
            }

        }
        public static T getObject<T>(DataRow row, List<string> columnsName) where T : new()
        {
            T obj = new T();
            try
            {
                string columnname = "";
                string value = "";
                PropertyInfo[] Properties;
                Properties = typeof(T).GetProperties();
                foreach (PropertyInfo objProperty in Properties)
                {
                    columnname = columnsName.Find(name => name.ToLower() == objProperty.Name.ToLower());
                    if (!string.IsNullOrEmpty(columnname))
                    {
                        value = row[columnname].ToString();
                        if (!string.IsNullOrEmpty(value))
                        {
                            if (Nullable.GetUnderlyingType(objProperty.PropertyType) != null)
                            {
                                value = row[columnname].ToString().Replace("$", "").Replace(",", "");
                                objProperty.SetValue(obj, Convert.ChangeType(value, Type.GetType(Nullable.GetUnderlyingType(objProperty.PropertyType).ToString())), null);
                            }
                            else
                            {
                                value = row[columnname].ToString().Replace("%", "");
                                objProperty.SetValue(obj, Convert.ChangeType(value, Type.GetType(objProperty.PropertyType.ToString())), null);
                            }
                        }
                    }
                }
                return obj;
            }
            catch
            {
                return obj;
            }
        }

        #endregion
        /// <summary>
        /// Convert object to XML
        /// </summary>
        /// <param name="dataToSerialize">Object to be serialized (DataTable/List etc)</param>
        /// <returns>Serialized objec (XML)</returns>
        public static string ToXML(object dataToSerialize)
        {
            if (dataToSerialize == null) return null;
            var serializer = new XmlSerializer(dataToSerialize.GetType());
            var ns = new XmlSerializerNamespaces();
            ns.Add("", "");
            var sw = new StringWriter();
            var xmlWriter = XmlWriter.Create(sw, new XmlWriterSettings() { OmitXmlDeclaration = true });
            serializer.Serialize(xmlWriter, dataToSerialize, ns);
            string result = Regex.Replace(sw.ToString(), "\\s+<\\w+ xsi:nil=\"true\" \\/>", string.Empty);
            return result;
        }

        //Creates an object from an XML string.
        public static object FromXml(string Xml, System.Type ObjType)
        {

            XmlSerializer ser;
            ser = new XmlSerializer(ObjType);
            StringReader stringReader;
            stringReader = new StringReader(Xml);
            XmlTextReader xmlReader;
            xmlReader = new XmlTextReader(stringReader);
            object obj;
            obj = ser.Deserialize(xmlReader);
            xmlReader.Close();
            stringReader.Close();
            return obj;

        }
        public class PropertyComparer<T> : IEqualityComparer<T>
        {
            private PropertyInfo _PropertyInfo;

            /// <summary>
            /// Creates a new instance of PropertyComparer.
            /// </summary>
            /// <param name="propertyName">The name of the property on type T 
            /// to perform the comparison on.</param>
            public PropertyComparer(string propertyName)
            {
                //store a reference to the property info object for use during the comparison
                _PropertyInfo = typeof(T).GetProperty(propertyName,
            BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.Public);
                if (_PropertyInfo == null)
                {
                    throw new ArgumentException(string.Format("{0} is not a property of type { 1 }.", propertyName, typeof(T)));
                }
            }

            #region IEqualityComparer<T> Members

            public bool Equals(T x, T y)
            {
                //get the current value of the comparison property of x and of y
                object xValue = _PropertyInfo.GetValue(x, null);
                object yValue = _PropertyInfo.GetValue(y, null);

                //if the xValue is null then we consider them equal if and only if yValue is null
                if (xValue == null)
                    return yValue == null;

                //use the default comparer for whatever type the comparison property is.
                return xValue.Equals(yValue);
            }

            public int GetHashCode(T obj)
            {
                //get the value of the comparison property out of obj
                object propertyValue = _PropertyInfo.GetValue(obj, null);

                if (propertyValue == null)
                    return 0;

                else
                    return propertyValue.GetHashCode();
            }

            #endregion
        }
        /// <summary>
        /// Get User information by MSID
        /// </summary>
        /// <param name="MSID">MSID of user to fetch user's name</param>
        /// <returns>User's name (string)</returns>
        //public static string GetUserNameByMSID(string MSID)
        //{
        //    string result = string.Empty;
        //    using (var searcher = new CloudSdkDirectory())
        //    {
        //        searcher.UserAttributesToLoad = DirectoryUserAttributes.All;
        //        var user = searcher.GetUserByUserId(MSID);
        //        if (user != null)
        //        {
        //            result = user.DisplayName.ToString();
        //        }
        //    }
        //    return result;
        //}
        ///// <summary>
        ///// Get users by Name
        ///// </summary>
        ///// <param name="lastName">last name of user</param>
        ///// <param name="firstName">first name of user</param>
        ///// <param name="middleName">middle name of user</param>
        ///// <returns>list of users (List<DirectoryUser>)</returns>
        //public static List<DirectoryUser> GetUsersByName(string lastName, string firstName = "", string middleName = "")
        //{
        //    List<DirectoryUser> users = new List<DirectoryUser>();
        //    using (var searcher = new CloudSdkDirectory())
        //    {
        //        searcher.UserAttributesToLoad = DirectoryUserAttributes.All;
        //        users = searcher.GetUsersByName(lastName, firstName, middleName);
        //    }
        //    return users;
        //}
        ///// <summary>
        ///// Get users by email
        ///// </summary>
        ///// <param name="email">User email to retirieve user list</param>
        ///// <returns>list of users (List<DirectoryUser>)</returns>
        //public static List<DirectoryUser> GetUsersByEmail(string email)
        //{
        //    List<DirectoryUser> users = new List<DirectoryUser>();
        //    using (var searcher = new CloudSdkDirectory())
        //    {
        //        searcher.UserAttributesToLoad = DirectoryUserAttributes.All;
        //        users = searcher.GetUsersByEmail(email);

        //    }
        //    return users;
        //}
        ///// <summary>
        ///// Get User by SID
        ///// </summary>
        ///// <param name="SID">User SID to fetch information</param>
        ///// <returns>user information (DirectoryUser)</returns>
        //public static DirectoryUser GetUserBySID(string SID)
        //{
        //    DirectoryUser user = null;
        //    using (var searcher = new CloudSdkDirectory())
        //    {
        //        searcher.UserAttributesToLoad = DirectoryUserAttributes.All;
        //        user = searcher.GetUserBySid(SID);
        //    }
        //    return user;
        //}
        ///// <summary>
        ///// Split NT user id and retrive MSID
        ///// </summary>
        ///// <param name="windowID">windows id</param>
        ///// <returns>MSID of user (string)</returns>
        public static string SplitMSID(string windowID)
        {
            string[] arrMsId = windowID.Split('\\');
            string MsId = arrMsId[1];
            return MsId;
        }

        ///// <summary>  
        ///// Summary description for ExceptionLogging  
        ///// </summary>  
        //public static class ExceptionLogging
        //{

        //    private static string errorLineNo, excMsg, excType, excRuntimeType, excUrl, hostIP, errorLocation, hostAdd;


        //    /// <summary>
        //    /// Saving Error log in a text file
        //    /// </summary>
        //    /// <param name = "ex" > Exception object</param>
        //    public static void SaveErrorLog(Exception ex)
        //    {
        //        var singleline = Environment.NewLine;
        //        var doubleline = Environment.NewLine + Environment.NewLine;

        //        var lineNumber = 0;
        //        const string lineSearch = ":line ";
        //        var index = ex.StackTrace.LastIndexOf(lineSearch);
        //        if (index != -1)
        //        {
        //            var lineNumberText = ex.StackTrace.Substring(index + lineSearch.Length);
        //            if (int.TryParse(lineNumberText, out lineNumber))
        //            {
        //            }
        //        }
        //        errorLineNo = lineNumber.ToString();
        //        excType = ex.GetType().Name.ToString();
        //        excRuntimeType = ex.GetType().ToString();
        //        excUrl = context.Current.Request.Url.ToString();
        //        excMsg = ex.Message.ToString();

        //        try
        //        {
        //            //string filepath = context.Current.Server.MapPath("~/ErrorLogs/");  //Text File Path

        //            //if (!Directory.Exists(filepath))
        //            //{
        //            //    Directory.CreateDirectory(filepath);

        //            //}
        //            //filepath = filepath + DateTime.Today.ToString("MM-dd-yyyy") + ".txt";   //Text File Name
        //            //if (!File.Exists(filepath))
        //            //{
        //            //    File.Create(filepath).Dispose();
        //            //}
        //            //using (StreamWriter sw = File.AppendText(filepath))
        //            //{
        //            //string error = @" Log written date       : " + DateTime.Now.ToString("MM-dd-yyyy hh:mm:ss") + doubleline + " Error Line No          :" + " " + errorLineNo + doubleline + " Exception Type         :" + " " + excType + doubleline + " Exception Runtime Type :" + " " + excRuntimeType + doubleline + " Exception Message      :" + " " + excMsg + doubleline + " Exception Page Url     :" + " " + excUrl + doubleline + " User Host IP           :" + (string.IsNullOrEmpty(hostIP) ? " N/A" : hostIP) + doubleline;
        //            //sw.WriteLine("|---------------------- Exception details on " + DateTime.Now.ToString("MM-dd-yyyy hh:mm:ss") + " ---------------------|");
        //            //sw.WriteLine(singleline);
        //            //sw.WriteLine(error);
        //            //sw.WriteLine("|---------------------------------------- [End] ----------------------------------------|");
        //            //sw.WriteLine(singleline);
        //            //sw.Flush();
        //            //sw.Close();
        //            //}
        //            //saving error log to db

        //            SQLConnection connObj = new SQLConnection();
        //            SqlParameter[] param = new SqlParameter[5];
        //            param[0] = new SqlParameter("@lineNo", Convert.ToInt32(errorLineNo));
        //            param[0].SqlDbType = SqlDbType.Int;
        //            param[1] = new SqlParameter("@exceptionType", excType);
        //            param[1].SqlDbType = SqlDbType.VarChar;
        //            param[2] = new SqlParameter("@exceptionRuntimeType", excRuntimeType);
        //            param[2].SqlDbType = SqlDbType.VarChar;
        //            param[3] = new SqlParameter("@exceptionMessage", excMsg);
        //            param[3].SqlDbType = SqlDbType.VarChar;
        //            param[4] = new SqlParameter("@exceptionPageUrl", excUrl);
        //            param[4].SqlDbType = SqlDbType.VarChar;
        //            var ds = connObj.ExeSqlHelperDS("saveErrorLog", param);

        //        }
        //        catch (Exception e)
        //        {
        //            e.ToString();

        //        }
        //    }


        //}

        public static int GetWeekNumberOfMonth(DateTime date)
        {
            date = date.Date;
            DateTime firstMonthDay = new DateTime(date.Year, date.Month, 1);
            DateTime firstMonthMonday = firstMonthDay.AddDays((DayOfWeek.Monday + 7 - firstMonthDay.DayOfWeek) % 7);
            if (firstMonthMonday > date)
            {
                firstMonthDay = firstMonthDay.AddMonths(-1);
                firstMonthMonday = firstMonthDay.AddDays((DayOfWeek.Monday + 7 - firstMonthDay.DayOfWeek) % 7);
            }
            return (date - firstMonthMonday).Days / 7 + 1;
        }

        /// <summary>
        /// Send email from outlook
        /// </summary>
        /// <param name="to">Recipient's emails (array)</param>
        /// <param name="from">sender's email</param>
        /// <param name="subject">email subject</param>
        /// <param name="body">email body</param>
        /// <param name="file">attachment to be sent</param>
        /// <returns>result whether the email is sent or not (int)</returns>
        public static void SendEmail(string body = "", string subject = "")
        {
            try
            {
                using (MailMessage mail1 = new MailMessage())
                {
                    MailAddress fromMail1 = new MailAddress("noreply@uhc.com");
                    // MailAddress fromMail1 = new MailAddress("arif_ali@uhc.com");
                    mail1.From = fromMail1;
                    mail1.CC.Add(new MailAddress("arif_ali@uhc.com"));
                    mail1.CC.Add(new MailAddress("shekh_iklakh@uhc.com"));
                    mail1.CC.Add(new MailAddress("prashant_kumar_ara@uhc.com"));
                    // mail1.To.Add(new MailAddress("arif_ali@uhc.com"));
                    // mail1.To.Add(new MailAddress(empemail));
                    //foreach (var to in toEmails)
                    //{manageremail
                    //    mail1.To.Add(new MailAddress(to));
                    //}
                    SmtpClient client1 = new SmtpClient();
                    client1.Port = 25;
                    client1.DeliveryMethod = SmtpDeliveryMethod.Network;
                    client1.UseDefaultCredentials = true;
                    client1.Host = "mailo2.uhc.com";


                    mail1.Subject = subject;// Window Service - Salesforce API ";

                    //mail1.CC.Add(CCEmails);


                    // mail1.CC.Add(empemail);
                    //mail1.Bcc.Add("shekh_iklakh@uhc.com");
                    mail1.IsBodyHtml = true;
                    //mail.Body = String.Format("");
                    mail1.Body = body;// type + " - "+(isNewInstance ? "<h3>New" : "<h3>Current") + " Instance Data ingested successfully !</h3>";
                    client1.Send(mail1);
                }


                //// Create the Outlook application.
                //Outlook.Application oApp = new Outlook.Application();
                //// Create a new mail item.
                //Outlook.MailItem oMsg = (Outlook.MailItem)oApp.CreateItem(Outlook.OlItemType.olMailItem);
                //// Set HTMLBody. 
                ////add the body of the email
                //oMsg.HTMLBody = body;

                ////Subject line
                //oMsg.Subject = subject;
                //// Add a recipient.
                //Outlook.Recipients oRecips = (Outlook.Recipients)oMsg.Recipients;
                //// Change the recipient in the next line if necessary.
                //Outlook.Recipient oRecip = (Outlook.Recipient)oRecips.Add(to);
                //oRecip.Resolve();
                //// Send.
                //oMsg.Send();
                //// Clean up.
                //oRecip = null;
                //oRecips = null;
                //oMsg = null;
                //oApp = null;
            }//end of try block
            catch (Exception ex)
            {
                LogMessage(ex.Message, true);
            }//end of catch

        }

        public static void LogMessage(string Message, bool error = false)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + "\\Logs";
            string errorPath = AppDomain.CurrentDomain.BaseDirectory + "\\ErrorLogs";

            string filepath = string.Empty;
            if (error)
            {
                if (!Directory.Exists(errorPath))
                {
                    Directory.CreateDirectory(errorPath);
                }
                filepath = AppDomain.CurrentDomain.BaseDirectory + "\\ErrorLogs\\ErrorLog_" + DateTime.Now.Date.ToShortDateString().Replace('/', '_') + ".txt";
            }
            else
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                filepath = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\ServiceLog_" + DateTime.Now.Date.ToShortDateString().Replace('/', '_') + ".txt";
            }
            if (!File.Exists(filepath))
            {
                // Create a file to write to.   
                using (StreamWriter sw = File.CreateText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
            else
            {
                using (StreamWriter sw = File.AppendText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
        }
        public static IEnumerable<List<T>> SplitList<T>(List<T> bigList, int nSize = 3)
        {
            for (int i = 0; i < bigList.Count; i += nSize)
            {
                yield return bigList.GetRange(i, Math.Min(nSize, bigList.Count - i));
            }
        }

    }
}

