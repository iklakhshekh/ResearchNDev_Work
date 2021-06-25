using System;

using Attachmate.Reflection.Framework;
using Attachmate.Reflection.Emulation.OpenSystems;
using Attachmate.Reflection.UserInterface;
using Attachmate.Reflection;
using System.Drawing;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Data.SqlClient;
using System.Data;
using System.Reflection;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ReflectionTest
{
    class Program
    {
       
        public static void EnterSpecialKey(IScreen screen, ControlKeyCode key)
        {
            screen.SendControlKeySync(key);
        }

        public static ReturnCode GoToSpecificScreen(IScreen screen, ControlKeyCode key, string numKey = null, int wait = 0)
        {
            string screenText = "";
            ReturnCode code = screen.SendControlKeySync(key);

            if (numKey != null)
            {
                code = screen.SendKeys(numKey);
                
            }
            if (wait > 0)
            {
                screen.WaitForControlKey(wait, key, true);
                screenText = GetText(screen, 15, 1, 1000, true);
                if (screenText.Contains("Error processing request interface call"))
                {
                    code = ReturnCode.Error;
                    return code;
                }
            }
            if (screenText.Contains("Error processing request interface call"))
            {
                code = ReturnCode.Error;
            }
            //screen.WaitForIncomingData(5000, key, false);
            return code;

        }

        public static void EnterText(IScreen screen, string text, bool pressEnter = false)
        {
            int row = screen.CursorRow;
            int column = screen.CursorColumn;
            //screen.SelectAll();
            ReturnCode code = screen.SendKeys(text);
            //int row = screen.MouseRow;
            //int column = screen.MouseColumn;
            if (pressEnter)
                screen.SendControlKeySync(ControlKeyCode.Enter);
            //Console.WriteLine(code);
        }
        public static string GetText(IScreen screen, int row, int col, int length, bool allText = false)
        {
            string text = string.Empty;
            int mouseRow = screen.MouseRow;
            int mouseColumn = screen.MouseColumn;

            if (allText)
                text = screen.GetText(1, 1, screen.DisplayRows, screen.DisplayColumns);
            else
                text = screen.GetText(row, col, length);
            return text;
        }
        public static ITerminal ConnectToSession(string sessionPath)
        {
            ITerminal terminal = null;
            //Get a handle to an application that represents the first instance of started manually.
            //For production code, use a try catch block here to handle a System Application Exception thrown
            //if the app isn't running.
            Application app = MyReflection.ActiveApplication;

            //Get the terminal from the session document file path.

            object[] terminals = app.GetControlsByFilePath(sessionPath);

            //Make sure the session is running.
            if (terminals != null && terminals.Length > 0)
            {
                //Get a screen and then get some text from the screen.
                terminal = (ITerminal)terminals[0];

            }
            else
            {
                Console.WriteLine("No such control exists. Check to make sure that the session from the file is running.");
            }
            return terminal;
        }
        public static ITerminal OpenSession(string sessionPath, string user)
        {

            //Start a visible instance of Reflection or get the instance running at the given channel name
            Application app = MyReflection.CreateApplication("myWorkspace-" + user, true);
            //Create a terminal from the session document file

            ITerminal terminal = (ITerminal)app.CreateControl(sessionPath);

            //Make the session visible in the workspace
            IFrame frame = (IFrame)app.GetObject("Frame");
            frame.CreateView(terminal);
            return terminal;
        }


        public bool CheckMemberInformation(string claimNo)
        {
            bool flag = false;
            return true;
        }
        public static bool CloseSession(string sessionPath)
        {
            ITerminal terminal = null;
            //Get a handle to an application that represents the first instance of started manually.
            //For production code, use a try catch block here to handle a System Application Exception thrown
            //if the app isn't running.
            Application app = MyReflection.ActiveApplication;

            //Get the terminal from the session document file path.

            object[] terminals = app.GetControlsByFilePath(sessionPath);

            //Make sure the session is running.
            if (terminals != null && terminals.Length > 0)
            {
                //Get a screen and then get some text from the screen.
                terminal = (ITerminal)terminals[0];
                //close the active session
                terminal.CloseAllConnections();
                terminal.Close(CloseOption.CloseAlwaysSave);

            }
            return true;

        }

        public static bool LoginToHost(ITerminal terminal, string user)
        {
            bool connected = terminal.IsConnected;
            ReturnCode codeReturned = ReturnCode.Success;
            while (codeReturned == ReturnCode.Success)
            {
                string screenText = GetText(terminal.Screen, 15, 1, 1000, true);
                if (screenText.Contains("Connecting to " + user + "@nice.phs.com"))
                {
                    terminal.Screen.Wait(5000);
                    //GoToSpecificScreen(terminal.Screen, ControlKeyCode.Enter, null, 1000);
                    EnterText(terminal.Screen, System.Configuration.ConfigurationManager.AppSettings[user + "pwd"].ToString(), true);
                    codeReturned = ReturnCode.Error;
                }
            }
            //terminal.Screen.WaitForString("Press <RET> for more");
            //string test = terminal.Screen.GetText(1, 1, terminal.Screen.DisplayRows, terminal.Screen.DisplayColumns);
            terminal.Screen.Wait(3000);
            return connected;
        }

        public static void ExecuteClaimDataExtraction()
        {
            try
            {
                SQLConnection connObj = new SQLConnection();
                var claims = connObj.ExeSqlHelperDS("[dbo].[sp_getNiceClaims]").Tables[0];//getting claims data
                if (claims != null && claims.Rows.Count > 0)
                {
                    List<Claim> claimLst = ConvertDataTable<Claim>(claims).ToList();
                    Helper.LogMessage("Total "+claimLst.Count+ " claims have to processed - " + DateTime.Now.ToString("MM/dd/yyyy HH:mm"));
                    int chunksPart = (int)Math.Ceiling((double)claimLst.Count / 2); //divide total claims with no of non user ids/ total instance of reflection app we will run
                    var claimsChunks = Split(claimLst, chunksPart).ToList();
                    claimsChunks.ForEach(s => s.OrderBy(a => a.claimRecievedDate).ToList());
                    var tasks = new List<Task>();
                    tasks.Add(Task.Factory.StartNew(() => ProcessClaimDataExtraction(claimsChunks[0], "BAA")));
                    tasks.Add(Task.Factory.StartNew(() => ProcessClaimDataExtraction(claimsChunks[1], "BAB")));
                    //tasks.Add(Task.Factory.StartNew(() => ProcessClaimDataExtraction(claimsChunks[2], "BAC")));// if adding a new non user ids

                    Task.WaitAll(tasks.ToArray());

                    Process[] _proceses = null;
                    _proceses = Process.GetProcessesByName("Attachmate.Emulation.Frame");
                    foreach (Process proces in _proceses)
                    {
                        proces.Kill();
                    }

                    Helper.SendEmail("NICE DATA SAVED SUCESSFULLY", "NICE JOB - " + DateTime.Now.ToString());
                    Helper.LogMessage("Data formatting started - " + DateTime.Now.ToString("MM/dd/yyyy HH:mm"));
                    connObj.ExecuteSqlHelper("[dbo].[Pro_Call_NSD_ADD_MOD]");//data formatting
                    connObj.ExecuteSqlHelper("[dbo].[Pro_Call_NSD_COMMENTS]");
                    connObj.ExecuteSqlHelper("[dbo].[Pro_Call_NSD_COMMENTS_O]");
                    connObj.ExecuteSqlHelper("[dbo].[Pro_Call_NSD_COMMENTS_Y]");
                    connObj.ExecuteSqlHelper("[dbo].[Pro_Call_NSD_DIAGNOSIS_DATA]");
                    connObj.ExecuteSqlHelper("[dbo].[Pro_Call_NSD_MBR_ELG_PLANS]");
                    connObj.ExecuteSqlHelper("[dbo].[Pro_Call_NSD_MEMBER_ALERTS]");
                    connObj.ExecuteSqlHelper("[dbo].[Pro_Call_NSD_SERVICE_ITEMS]");
                    connObj.ExecuteSqlHelper("[dbo].[Proc_NSD_claims_Assignment_details_Data_Load]");
                    Helper.LogMessage("Data formatting ended - " + DateTime.Now.ToString("MM/dd/yyyy HH:mm"));
                }
            }
            catch (Exception ex)
            {
                // Console.WriteLine(ex.Message);
                Helper.LogMessage(ex.Message +"\n"+ ex.StackTrace+ " at ExecuteClaimDataExtraction() - " + DateTime.Now.ToString("MM/dd/yyyy HH:mm"), true);
            }
        }
        static async Task ProcessClaimDataExtraction(List<Claim> claims, string user)
        {
            string _currentClaim = string.Empty;
            try
            {
                string session = Environment.GetEnvironmentVariable("USERPROFILE") + @"\Documents\Micro Focus\Reflection\Custom" + user + ".rdox";
                //ITerminal terminal = ConnectToSession(session);  //OpenSession(session, user);
                ITerminal terminal = OpenSession(session, user);
                //LoginToHost(terminal, "BAA");
                //string session2 = Environment.GetEnvironmentVariable("USERPROFILE") + @"\Documents\Micro Focus\Reflection\CustomBAB.rdox";
                //ITerminal terminal2 = OpenSession(session2, "BAB");
                //LoginToHost(terminal2, "BAB");
                if (LoginToHost(terminal, user))
                {
                    SQLConnection connObj = new SQLConnection();
                    EnterText(terminal.Screen, "7", true);//Claim Inquiry screen

                    //string arrClms = "851234501304,252774901181,844831401409,952219501136,923798401009,919215501122,852134901036,294084101067,857765301101,849629901083,406857501015,917278901011,408639401014,369033701002,230474501449,866501801400";
                    foreach (Claim claim in claims)
                    {
                        string clmNo = claim.claimNo;
                        clmNo = clmNo.Trim();
                        if (!string.IsNullOrEmpty(clmNo))
                        {
                            _currentClaim = clmNo;
                            string claimFoundText = string.Empty;
                            string facilityOrProfessionalClaim = string.Empty;
                            string MBR = "";
                            string MEMBER = "";
                            string SUFFIX = "";
                            string CLM = "";
                            //string DEC_NUMBER = "";
                            //string CLAIM_TYPE = "";
                            string STS = "";
                            string H = "";
                            //string PROV_SPEC = "";
                            string ASSGN_PROVIDER = "";
                            //string PROV_CONTRACT = "";
                            string COB_ANESTHESIA = "";
                            string SERVICE_PAYMENT = "";
                            string FEE_SCHEDULE_NUMBER = "";
                            string DIAGNOSIS_DATA = "";
                            string ADDITIONAL_MOD = "";
                            string COMMENTS = "";
                            string COMMENTS_ZERO = "";
                            string COMMENTS_Y = "";
                            string MEMBER_ALERTS = "";
                            string SERVICE_ITEMS = "";
                            string MBR_ELG_PLANS = "";
                            string FDOS = "";
                            string LDOS = "";
                            string ERSA = "";
                            string TYP = "";
                            string EMP = "";
                            string PL = "";
                            string ELG = "";
                            string REASON_TOP = "";
                            string ADJ_ID = "";
                            string AMT = "";
                            string DOB = "";
                            string AGE = "";
                            string SEX = "";
                            string LC = "";
                            string WL = "";
                            string IO = "";
                            string SUBSCR = "";
                            string COB = "";
                            string BATCH = "";
                            string SVC_PRV = "";
                            string TID = "";
                            string NETWRK = "";
                            string ATH = "";
                            string ST = "";
                            string CL = "";
                            string NAME = "";
                            string CNTRCT = "";
                            string APP_DAYS_VST = "";
                            string TO_DATE = "";
                            string TYPE_TOP = "";
                            string TYPE_BOTTOM = "";
                            string NPI = "";
                            string CLEAN = "";
                            string DRG = "";
                            string PCN = "";
                            string RVD = "";
                            string GROUP_NAME = "";
                            string ACT_DAY_VST = "";
                            string LOC = "";
                            string SP = "";
                            string FAC_PHY_NAME = "";
                            string RISK_ARR = "";
                            string RISK_WH = "";
                            string REASON_BOTTOM = "";
                            string ATT_PH_OTH = "";
                            string LC_STS = "";
                            string ICN = "";
                            string screenText = string.Empty;
                            //if(screenText.Contains("Press <RET> for first page"))
                            //{
                            //    EnterText(terminal.Screen, "7", true);//Claim Inquiry screen
                            //}
                            ReturnCode codeReturned = GoToSpecificScreen(terminal.Screen, ControlKeyCode.PF1, "S", 250);// Clearing the Claim inquiry screen
                            EnterText(terminal.Screen, clmNo, true);
                            //terminal.Screen.WaitForIncomingData(1000);
                            terminal.Screen.Wait(1000);
                            codeReturned = ReturnCode.Success;

                            while (codeReturned == ReturnCode.Success)
                            {
                                screenText = GetText(terminal.Screen, 15, 1, 1000, true);
                                if (screenText.Contains("REQUEST IN PROGRESS"))
                                {
                                    codeReturned = ReturnCode.Success;
                                }
                                else
                                {
                                    codeReturned = ReturnCode.Error;
                                }
                            }
                            codeReturned = ReturnCode.Success;
                            while (codeReturned == ReturnCode.Success)
                            {
                                screenText = GetText(terminal.Screen, 15, 1, 1000, true);
                                if (screenText.Contains("Connecting to " + user + "@nice.phs.com"))
                                {
                                    codeReturned = ReturnCode.Success;
                                }
                                else
                                {
                                    codeReturned = ReturnCode.Error;
                                }
                            }
                            codeReturned = ReturnCode.Success;
                            while (codeReturned == ReturnCode.Success)
                            {
                                screenText = GetText(terminal.Screen, 15, 1, 1000, true);
                                if (screenText.Contains("ENTER OPTION LETTER:"))
                                {
                                    codeReturned = ReturnCode.Success;
                                }
                                else
                                {
                                    codeReturned = ReturnCode.Error;
                                }
                            }

                            // terminal.Screen.Wait(1000);
                            codeReturned = ReturnCode.Success;
                            claimFoundText = GetAttributeValue(terminal.Screen, "ClaimFound", out codeReturned);
                            if (!claimFoundText.Contains("SPECIFIED MEMBER NUMBER NOT VALID-REENTER NEW NUMBER."))
                            {
                                screenText = GetText(terminal.Screen, 15, 1, 1000, true);
                                var lines = screenText.Split('\n');
                                //Helper.LogMessage(_currentClaim + ": screenText--->" + screenText);
                                //int[] lineIndexToLook = new int[12] { 2, 3, 5, 6, 7, 9, 10, 11, 12, 13, 14, 15 };// these are line index we are fetching data fields
                                //for (var line = 0; line < lines.Length; line++)
                                //{
                                //if (lineIndexToLook.Contains(line))
                                // {
                                //  if (line == 2)
                                ICN = lines[2].Split(new char[] { 'I', 'C', 'N', ':', '<', '>' })[5];
                                // else
                                // {
                                MBR = lines[3].Substring(lines[3].IndexOf("MBR #:"), 18).Split(':')[1].Split('-')[0];
                                SUFFIX = lines[3].Substring(lines[3].IndexOf("MBR #:"), 18).Split(':')[1].Split('-')[1];
                                CLM = lines[3].Substring(lines[3].IndexOf("CLM #:"), 12).Split(':')[1];
                                TYPE_TOP = lines[3].Substring(lines[3].IndexOf("TYPE:"), 10).Split(':')[1];
                                REASON_TOP = lines[3].Substring(lines[3].IndexOf("REASON:"), 12).Split(':')[1];
                                ADJ_ID = lines[3].Substring(lines[3].IndexOf("ADJ ID:"), 12).Split(':')[1];
                                AMT = lines[3].Substring(lines[3].IndexOf("AMT:"), 14).Split(':')[1];
                                MEMBER = lines[5].Substring(lines[5].IndexOf("MEMBER:"), 47).Split(':')[1];
                                DOB = lines[5].Substring(lines[5].IndexOf("DOB:"), 12).Split(':')[1];
                                AGE = lines[5].Substring(lines[5].IndexOf("AGE:"), 8).Split(':')[1];
                                SEX = lines[5].Substring(lines[5].IndexOf("SEX:"), 6).Split(':')[1];
                                LC = lines[5].Substring(lines[5].IndexOf("LC:"), 4).Split(':')[1];
                                ELG = lines[6].Substring(lines[6].IndexOf("ELG:"), 30).Split(':')[1];
                                ERSA = lines[6].Substring(lines[6].IndexOf("ERSA:"), 7).Split(':')[1];
                                TYP = lines[6].Substring(lines[6].IndexOf("TYP:"), 12).Split(':')[1];
                                EMP = lines[6].Substring(lines[6].IndexOf("EMP:"), 11).Split(':')[1];
                                PL = lines[6].Substring(lines[6].IndexOf("PL:"), 7).Split(':')[1];
                                WL = lines[6].Substring(lines[6].IndexOf("WL:"), 7).Split(':')[1];
                                IO = lines[6].Substring(lines[6].IndexOf("IO:"), 4).Split(':')[1];
                                SUBSCR = lines[7].Substring(lines[7].IndexOf("SUBSCR:"), 47).Split(':')[1];
                                COB = lines[7].Substring(lines[7].IndexOf("COB:"), 6).Split(':')[1];
                                BATCH = lines[7].Substring(lines[7].IndexOf("BATCH:"), 25).Split(':')[1];
                                SVC_PRV = lines[9].Substring(lines[9].IndexOf("SVC PRV:"), 20).Split(':')[1];
                                TID = lines[9].Substring(lines[9].IndexOf("TID:"), 16).Split(':')[1];
                                NETWRK = lines[9].Substring(lines[9].IndexOf("NETWRK:"), 11).Split(':')[1];
                                ATH = lines[9].Substring(lines[9].IndexOf("ATH:"), 14).Split(':')[1];
                                ST = lines[9].Substring(lines[9].IndexOf("ST:"), 6).Split(':')[1];
                                CL = lines[9].Substring(lines[9].IndexOf("CL:"), 10).Split(':')[1];
                                NAME = lines[10].Substring(lines[10].IndexOf("NAME:"), 36).Split(':')[1];
                                CNTRCT = lines[10].Substring(lines[10].IndexOf("CNTRCT:"), 11).Split(':')[1];
                                APP_DAYS_VST = lines[10].Substring(lines[10].IndexOf("APP DAYS/VST:"), 20).Split(':')[1];
                                TO_DATE = lines[10].Substring(lines[10].IndexOf("TO DATE:"), 10).Split(':')[1];
                                TYPE_BOTTOM = lines[11].Substring(lines[11].IndexOf("TYPE:"), 20).Split(':')[1];
                                NPI = lines[11].Substring(lines[11].IndexOf("NPI:"), 16).Split(':')[1];
                                CLEAN = lines[11].Substring(lines[11].IndexOf("CLEAN :"), 11).Split(':')[1];
                                FDOS = lines[11].Substring(lines[11].IndexOf("FDOS:"), 16).Split(':')[1];
                                LDOS = lines[11].Substring(lines[11].IndexOf("LDOS:"), 14).Split(':')[1];
                                ASSGN_PROVIDER = lines[12].Substring(lines[12].IndexOf("ASSGN PROVIDER:"), 35).Split(':')[1];
                                DRG = lines[12].Substring(lines[12].IndexOf("DRG:"), 8).Split(':')[1];
                                PCN = lines[12].Substring(lines[12].IndexOf("PCN:"), 22).Split(':')[1];
                                RVD = lines[12].Substring(lines[12].IndexOf("RVD:"), 12).Split(':')[1];
                                GROUP_NAME = lines[13].Substring(lines[13].IndexOf("GROUP NAME:"), 43).Split(':')[1];
                                ACT_DAY_VST = lines[13].Substring(lines[13].IndexOf("ACT.DAY/VST:"), 21).Split(':')[1];
                                LOC = lines[13].Substring(lines[13].IndexOf("LOC:"), 8).Split(':')[1];
                                SP = lines[13].Substring(lines[13].IndexOf("SP:"), 5).Split(':')[1];
                                FAC_PHY_NAME = lines[14].Substring(lines[14].IndexOf("FAC/PHY NAME:"), 43).Split(':')[1];
                                RISK_ARR = lines[14].Substring(lines[14].IndexOf("RISK ARR:"), 14).Split(':')[1];
                                RISK_WH = lines[14].Substring(lines[14].IndexOf("RISK/WH:"), 10).Split(':')[1];
                                REASON_BOTTOM = lines[14].Substring(lines[14].IndexOf("REASON:"), 10).Split(':')[1];
                                ATT_PH_OTH = lines[15].Substring(lines[15].IndexOf("ATT PH/OTH:"), 31).Split(':')[1];
                                STS = lines[15].Substring(lines[15].IndexOf("STS:"), 7).Split(':')[1];
                                H = lines[15].Substring(lines[15].IndexOf(" H:"), 6).Split(':')[1];
                                LC_STS = lines[15].Substring(lines[15].IndexOf("LC/STS:"), 34).Split(':')[1];


                                
                                facilityOrProfessionalClaim = string.IsNullOrEmpty(H.Trim()) ? "Professional" : "Facility";
                               
                                //Console.Clear();
                                //Console.WriteLine("MBR				    {0}", MBR);
                                //Console.WriteLine("MEMBER 				{0}", MEMBER);
                                //Console.WriteLine("SUFFIX 				{0}", SUFFIX);
                                //Console.WriteLine("CLM 					{0}", CLM);
                                //Console.WriteLine("STS 					{0}", STS);
                                //Console.WriteLine("H 					{0}", H);
                                //Console.WriteLine("ASSGN_PROVIDER 		{0}", ASSGN_PROVIDER);
                                //Console.WriteLine("COB_ANESTHESIA 		{0}", COB_ANESTHESIA);
                                //Console.WriteLine("SERVICE_PAYMENT 		{0}", SERVICE_PAYMENT);
                                //Console.WriteLine("FEE_SCHEDULE_NUMBER  {0}", FEE_SCHEDULE_NUMBER);
                                //Console.WriteLine("DIAGNOSIS_DATA 		{0}", DIAGNOSIS_DATA);
                                //Console.WriteLine("ADDITIONAL_MOD 		{0}", ADDITIONAL_MOD);
                                //Console.WriteLine("COMMENTS 			{0}", COMMENTS);
                                //Console.WriteLine("COMMENTS_ZERO 		{0}", COMMENTS_ZERO);
                                //Console.WriteLine("COMMENTS_Y 			{0}", COMMENTS_Y);
                                //Console.WriteLine("MEMBER_ALERTS 		{0}", MEMBER_ALERTS);
                                //Console.WriteLine("SERVICE_ITEMS 		{0}", SERVICE_ITEMS);
                                //Console.WriteLine("MBR_ELG_PLANS 		{0}", MBR_ELG_PLANS);
                                //Console.WriteLine("FDOS 				{0}", FDOS);
                                //Console.WriteLine("LDOS 				{0}", LDOS);
                                //Console.WriteLine("ERSA 				{0}", ERSA);
                                //Console.WriteLine("TYP 					{0}", TYP);
                                //Console.WriteLine("EMP 					{0}", EMP);
                                //Console.WriteLine("PL 					{0}", PL);
                                //Console.WriteLine("ELG 					{0}", ELG);
                                //Console.WriteLine("REASON_TOP 			{0}", REASON_TOP);
                                //Console.WriteLine("ADJ_ID 				{0}", ADJ_ID);
                                //Console.WriteLine("AMT 					{0}", AMT);
                                //Console.WriteLine("DOB 					{0}", DOB);
                                //Console.WriteLine("AGE 					{0}", AGE);
                                //Console.WriteLine("SEX 					{0}", SEX);
                                //Console.WriteLine("LC 					{0}", LC);
                                //Console.WriteLine("WL 					{0}", WL);
                                //Console.WriteLine("IO 					{0}", IO);
                                //Console.WriteLine("SUBSCR 				{0}", SUBSCR);
                                //Console.WriteLine("COB 					{0}", COB);
                                //Console.WriteLine("BATCH 				{0}", BATCH);
                                //Console.WriteLine("SVC_PRV 				{0}", SVC_PRV);
                                //Console.WriteLine("TID 					{0}", TID);
                                //Console.WriteLine("NETWRK 				{0}", NETWRK);
                                //Console.WriteLine("ATH					{0}", ATH);
                                //Console.WriteLine("ST 					{0}", ST);
                                //Console.WriteLine("CL 					{0}", CL);
                                //Console.WriteLine("NAME 				{0}", NAME);
                                //Console.WriteLine("CNTRCT 				{0}", CNTRCT);
                                //Console.WriteLine("APP_DAYS_VST 		{0}", APP_DAYS_VST);
                                //Console.WriteLine("TO_DATE 				{0}", TO_DATE);
                                //Console.WriteLine("TYPE_TOP 			{0}", TYPE_TOP);
                                //Console.WriteLine("TYPE_BOTTOM 			{0}", TYPE_BOTTOM);
                                //Console.WriteLine("NPI 					{0}", NPI);
                                //Console.WriteLine("CLEAN 				{0}", CLEAN);
                                //Console.WriteLine("DRG 					{0}", DRG);
                                //Console.WriteLine("PCN 					{0}", PCN);
                                //Console.WriteLine("RVD 					{0}", RVD);
                                //Console.WriteLine("GROUP_NAME 			{0}", GROUP_NAME);
                                //Console.WriteLine("ACT_DAY_VST 			{0}", ACT_DAY_VST);
                                //Console.WriteLine("LOC 					{0}", LOC);
                                //Console.WriteLine("SP 					{0}", SP);
                                //Console.WriteLine("FAC_PHY_NAME 		{0}", FAC_PHY_NAME);
                                //Console.WriteLine("RISK_ARR 			{0}", RISK_ARR);
                                //Console.WriteLine("RISK_WH 				{0}", RISK_WH);
                                //Console.WriteLine("REASON_BOTTOM 		{0}", REASON_BOTTOM);
                                //Console.WriteLine("ATT_PH_OTH 			{0}", ATT_PH_OTH);
                                //Console.WriteLine("LC_STS 				{0}", LC_STS);
                                //Console.WriteLine("ICN 					{0}", ICN);


                                COB_ANESTHESIA = CNTRCT.Trim() == "Y" ? GetAttributeValue(terminal.Screen, "cobAnesthesia", out codeReturned, facilityOrProfessionalClaim) : "";
                                SERVICE_PAYMENT = CNTRCT.Trim() == "Y" && COB_ANESTHESIA != "NO CONTRACT IN EFFECT FOR SVC PROVIDER" ? GetAttributeValue(terminal.Screen, "servicePayment", out codeReturned, facilityOrProfessionalClaim) : "";
                                FEE_SCHEDULE_NUMBER = CNTRCT.Trim() == "Y" && COB_ANESTHESIA != "NO CONTRACT IN EFFECT FOR SVC PROVIDER" && facilityOrProfessionalClaim == "Professional" ? GetAttributeValue(terminal.Screen, "feeSchduled", out codeReturned, facilityOrProfessionalClaim) : "";

                                DIAGNOSIS_DATA = GetAttributeValue(terminal.Screen, "diagnosisData", out codeReturned);
                                ADDITIONAL_MOD = facilityOrProfessionalClaim == "Professional" ? GetAttributeValue(terminal.Screen, "additionalMOD", out codeReturned) : "";

                                if (codeReturned == ReturnCode.Error)
                                {
                                    EnterText(terminal.Screen, clmNo, true);
                                    terminal.Screen.Wait(1000);
                                }

                                COMMENTS = GetAttributeValue(terminal.Screen, "comments", out codeReturned);
                                if (codeReturned == ReturnCode.Timeout)
                                {
                                    EnterText(terminal.Screen, "7", true);
                                    terminal.Screen.Wait(1000);
                                    EnterText(terminal.Screen, clmNo, true);
                                    terminal.Screen.Wait(1000);
                                    codeReturned = GoToSpecificScreen(terminal.Screen, ControlKeyCode.PF1, "I", 250);// Navigating to Comments Maintenece screen
                                }
                                if (codeReturned == ReturnCode.Error)
                                {
                                    EnterText(terminal.Screen, clmNo, true);
                                    terminal.Screen.Wait(1000);
                                }
                                if (codeReturned != ReturnCode.Error && codeReturned != ReturnCode.Error)
                                {
                                    COMMENTS_ZERO = GetAttributeValue(terminal.Screen, "comments_zero", out codeReturned);
                                    COMMENTS_Y = GetAttributeValue(terminal.Screen, "commentsY", out codeReturned);
                                }
                                MEMBER_ALERTS = GetAttributeValue(terminal.Screen, "memberAlerts", out codeReturned);//1 screen
                                SERVICE_ITEMS = GetAttributeValue(terminal.Screen, "serviceItem", out codeReturned);
                                bool MEMBER_APPEARS_INELIGIBLE_AS_OF_FDOS = screenText.Contains("MEMBER APPEARS INELIGIBLE AS OF FDOS");
                                //int _dobyear = DateTime.Now.Year - Convert.ToInt32(AGE.Trim());
                                //string _dob = DOB.Trim().Split('/')[0] + "-" + DOB.Trim().Split('/')[1] + "-" + _dobyear;
                                MBR_ELG_PLANS = GetAttributeValue(terminal.Screen, "mbrElgPlans", out codeReturned, ELG + "|" + FDOS + "|" + LDOS + "|" + MBR +  "|" + MEMBER_APPEARS_INELIGIBLE_AS_OF_FDOS + "|" + MEMBER);//1 screen
                            }
                            else
                            {
                                GoToSpecificScreen(terminal.Screen, ControlKeyCode.PF1, "M");//for main screen
                                EnterText(terminal.Screen, "7", true);
                            }
                            //save information


                            SqlParameter[] param = new SqlParameter[66];
                            param[0] = new SqlParameter("@CLAIM_NUMBER", clmNo);
                            param[0].SqlDbType = SqlDbType.VarChar;
                            param[1] = new SqlParameter("@MBR", MBR.Trim());
                            param[1].SqlDbType = SqlDbType.VarChar;
                            param[2] = new SqlParameter("@MEMBER", MEMBER.Trim());
                            param[2].SqlDbType = SqlDbType.VarChar;
                            param[3] = new SqlParameter("@SUFFIX", SUFFIX.Trim());
                            param[3].SqlDbType = SqlDbType.VarChar;
                            param[4] = new SqlParameter("@TYPE_TOP", TYPE_TOP.Trim());
                            param[4].SqlDbType = SqlDbType.VarChar;
                            param[5] = new SqlParameter("@DIAGNOSIS_DATA", DIAGNOSIS_DATA.Trim());
                            param[5].SqlDbType = SqlDbType.VarChar;
                            param[6] = new SqlParameter("@STS", STS.Trim());
                            param[6].SqlDbType = SqlDbType.VarChar;
                            param[7] = new SqlParameter("@H", H.Trim());
                            param[7].SqlDbType = SqlDbType.VarChar;
                            param[8] = new SqlParameter("@TYPE_BOTTOM", TYPE_BOTTOM.Trim());
                            param[8].SqlDbType = SqlDbType.VarChar;
                            param[9] = new SqlParameter("@CNTRCT", CNTRCT.Trim());
                            param[9].SqlDbType = SqlDbType.VarChar;
                            param[10] = new SqlParameter("@ADDITIONAL_MOD", ADDITIONAL_MOD.Trim());
                            param[10].SqlDbType = SqlDbType.VarChar;
                            param[11] = new SqlParameter("@COMMENTS", COMMENTS.Trim());
                            param[11].SqlDbType = SqlDbType.VarChar;
                            param[12] = new SqlParameter("@MEMBER_ALERTS", MEMBER_ALERTS.Trim());
                            param[12].SqlDbType = SqlDbType.VarChar;
                            param[13] = new SqlParameter("@SERVICE_ITEMS", SERVICE_ITEMS.Trim());
                            param[13].SqlDbType = SqlDbType.VarChar;
                            param[14] = new SqlParameter("@CLM", CLM.Trim());
                            param[14].SqlDbType = SqlDbType.VarChar;
                            param[15] = new SqlParameter("@SVC_PRV", SVC_PRV.Trim());
                            param[15].SqlDbType = SqlDbType.VarChar;
                            param[16] = new SqlParameter("@ASSGN_PROVIDER", ASSGN_PROVIDER.Trim());
                            param[16].SqlDbType = SqlDbType.VarChar;
                            param[17] = new SqlParameter("@MBR_ELG_PLANS", MBR_ELG_PLANS.Trim());
                            param[17].SqlDbType = SqlDbType.VarChar;
                            param[18] = new SqlParameter("@COB_ANESTHESIA", COB_ANESTHESIA.Trim());
                            param[18].SqlDbType = SqlDbType.VarChar;
                            param[19] = new SqlParameter("@SERVICE_PAYMENT", SERVICE_PAYMENT.Trim());
                            param[19].SqlDbType = SqlDbType.VarChar;
                            param[20] = new SqlParameter("@FEE_SCHEDULE_NUMBER", FEE_SCHEDULE_NUMBER.Trim());
                            param[20].SqlDbType = SqlDbType.VarChar;
                            param[21] = new SqlParameter("@COMMENTS_ZERO", COMMENTS_ZERO.Trim());
                            param[21].SqlDbType = SqlDbType.VarChar;
                            param[22] = new SqlParameter("@COMMENTS_Y", COMMENTS_Y.Trim());
                            param[22].SqlDbType = SqlDbType.VarChar;
                            param[23] = new SqlParameter("@FDOS", FDOS.Trim());
                            param[23].SqlDbType = SqlDbType.VarChar;
                            param[24] = new SqlParameter("@LDOS", LDOS.Trim());
                            param[24].SqlDbType = SqlDbType.VarChar;
                            param[25] = new SqlParameter("@ELG", ELG.Trim());
                            param[25].SqlDbType = SqlDbType.VarChar;
                            param[26] = new SqlParameter("@ERSA", ERSA.Trim());
                            param[26].SqlDbType = SqlDbType.VarChar;
                            param[27] = new SqlParameter("@TYP", TYP.Trim());
                            param[27].SqlDbType = SqlDbType.VarChar;
                            param[28] = new SqlParameter("@EMP", EMP.Trim());
                            param[28].SqlDbType = SqlDbType.VarChar;
                            param[29] = new SqlParameter("@PL", PL.Trim());
                            param[29].SqlDbType = SqlDbType.VarChar;
                            param[30] = new SqlParameter("@REASON_TOP", REASON_TOP.Trim());
                            param[30].SqlDbType = SqlDbType.VarChar;
                            param[31] = new SqlParameter("@ADJ_ID", ADJ_ID.Trim());
                            param[31].SqlDbType = SqlDbType.VarChar;
                            param[32] = new SqlParameter("@AMT", AMT.Trim());
                            param[32].SqlDbType = SqlDbType.VarChar;
                            param[33] = new SqlParameter("@DOB", DOB.Trim());
                            param[33].SqlDbType = SqlDbType.VarChar;
                            param[34] = new SqlParameter("@AGE", AGE.Trim());
                            param[34].SqlDbType = SqlDbType.VarChar;
                            param[35] = new SqlParameter("@SEX", SEX.Trim());
                            param[35].SqlDbType = SqlDbType.VarChar;
                            param[36] = new SqlParameter("@LC", LC.Trim());
                            param[36].SqlDbType = SqlDbType.VarChar;
                            param[37] = new SqlParameter("@WL", WL.Trim());
                            param[37].SqlDbType = SqlDbType.VarChar;
                            param[38] = new SqlParameter("@IO", IO.Trim());
                            param[38].SqlDbType = SqlDbType.VarChar;
                            param[39] = new SqlParameter("@SUBSCR", SUBSCR.Trim());
                            param[39].SqlDbType = SqlDbType.VarChar;
                            param[40] = new SqlParameter("@COB", COB.Trim());
                            param[40].SqlDbType = SqlDbType.VarChar;
                            param[41] = new SqlParameter("@BATCH", BATCH.Trim());
                            param[41].SqlDbType = SqlDbType.VarChar;
                            param[42] = new SqlParameter("@TID", TID.Trim());
                            param[42].SqlDbType = SqlDbType.VarChar;
                            param[43] = new SqlParameter("@NETWRK", NETWRK.Trim());
                            param[43].SqlDbType = SqlDbType.VarChar;
                            param[44] = new SqlParameter("@ATH", ATH.Trim());
                            param[44].SqlDbType = SqlDbType.VarChar;
                            param[45] = new SqlParameter("@ST", ST.Trim());
                            param[45].SqlDbType = SqlDbType.VarChar;
                            param[46] = new SqlParameter("@CL", CL.Trim());
                            param[46].SqlDbType = SqlDbType.VarChar;
                            param[47] = new SqlParameter("@NAME", NAME.Trim());
                            param[47].SqlDbType = SqlDbType.VarChar;
                            param[48] = new SqlParameter("@APP_DAYS_VST", APP_DAYS_VST.Trim());
                            param[48].SqlDbType = SqlDbType.VarChar;
                            param[49] = new SqlParameter("@TO_DATE", TO_DATE.Trim());
                            param[49].SqlDbType = SqlDbType.VarChar;
                            param[50] = new SqlParameter("@NPI", NPI.Trim());
                            param[50].SqlDbType = SqlDbType.VarChar;
                            param[51] = new SqlParameter("@CLEAN", CLEAN.Trim());
                            param[51].SqlDbType = SqlDbType.VarChar;
                            param[52] = new SqlParameter("@DRG", DRG.Trim());
                            param[52].SqlDbType = SqlDbType.VarChar;
                            param[53] = new SqlParameter("@PCN", PCN.Trim());
                            param[53].SqlDbType = SqlDbType.VarChar;
                            param[54] = new SqlParameter("@RVD", RVD.Trim());
                            param[54].SqlDbType = SqlDbType.VarChar;
                            param[55] = new SqlParameter("@GROUP_NAME", GROUP_NAME.Trim());
                            param[55].SqlDbType = SqlDbType.VarChar;
                            param[56] = new SqlParameter("@ACT_DAY_VST", ACT_DAY_VST.Trim());
                            param[56].SqlDbType = SqlDbType.VarChar;
                            param[57] = new SqlParameter("@LOC", LOC.Trim());
                            param[57].SqlDbType = SqlDbType.VarChar;
                            param[58] = new SqlParameter("@SP", SP.Trim());
                            param[58].SqlDbType = SqlDbType.VarChar;
                            param[59] = new SqlParameter("@FAC_PHY_NAME", FAC_PHY_NAME.Trim());
                            param[59].SqlDbType = SqlDbType.VarChar;
                            param[60] = new SqlParameter("@RISK_ARR", RISK_ARR.Trim());
                            param[60].SqlDbType = SqlDbType.VarChar;
                            param[61] = new SqlParameter("@RISK_WH", RISK_WH.Trim());
                            param[61].SqlDbType = SqlDbType.VarChar;
                            param[62] = new SqlParameter("@REASON_BOTTOM", REASON_BOTTOM.Trim());
                            param[62].SqlDbType = SqlDbType.VarChar;
                            param[63] = new SqlParameter("@ATT_PH_OTH", ATT_PH_OTH.Trim());
                            param[63].SqlDbType = SqlDbType.VarChar;
                            param[64] = new SqlParameter("@LC_STS", LC_STS.Trim());
                            param[64].SqlDbType = SqlDbType.VarChar;
                            param[65] = new SqlParameter("@ICN", ICN.Trim());
                            param[65].SqlDbType = SqlDbType.VarChar;
                            int result = connObj.ExecuteSqlHelper("[dbo].[sp_insertNICEData]", param);

                            //Console.WriteLine(result >= 1 ? "Data Saved to Database !" : "Some error occurred !");
                            Console.WriteLine("Claim Processed - " + clmNo + " by " + user);
                            Helper.LogMessage("Claim Processed - " + clmNo + " by " + user + " at " + DateTime.Now.ToString("MM/dd/yyyy HH:mm"));
                        }
                    }

                }
                else
                {
                    //ExecuteClaimDataExtraction();
                }
            }
            catch (IndexOutOfRangeException ex)
            {
                // Console.WriteLine(ex.Message);
                Helper.LogMessage(ex.Message + " \n" + ex.StackTrace + " for Claim - " + _currentClaim + " - " + DateTime.Now.ToString("MM/dd/yyyy HH:mm"), true);
                Process[] _proceses = null;
                _proceses = Process.GetProcessesByName("Attachmate.Emulation.Frame");
                foreach (Process proces in _proceses)
                {
                    proces.Kill();
                }

            }
            catch (Exception ex)
            {
                // Console.WriteLine(ex.Message);
                Helper.LogMessage(ex.Message + " \n" + ex.StackTrace + " for Claim - " + _currentClaim + " - " + DateTime.Now.ToString("MM/dd/yyyy HH:mm"), true);
                Process[] _proceses = null;
                _proceses = Process.GetProcessesByName("Attachmate.Emulation.Frame");
                foreach (Process proces in _proceses)
                {
                    proces.Kill();
                }

            }
        }
        static void Main(string[] args)
        {
            Helper.LogMessage("Task started - " + DateTime.Now.ToString("MM/dd/yyyy HH:mm"));
            ExecuteClaimDataExtraction();
            Helper.LogMessage("Task ended - " + DateTime.Now.ToString("MM/dd/yyyy HH:mm"));
            //for manual run

            //manaualRun();
        }

        static void manaualRun()
        {
            List<Claim> claims = new List<Claim>() {
            //new Claim(){ claimNo="351579401037"},    
            //new Claim(){ claimNo="335115601043"},
            new Claim(){ claimNo="956941701023"}
            };
            var tasks = new List<Task>();
            tasks.Add(Task.Factory.StartNew(() => ProcessClaimDataExtraction(claims, "BAA")));
            Task.WaitAll(tasks.ToArray());

            Process[] _proceses = null;
            _proceses = Process.GetProcessesByName("Attachmate.Emulation.Frame");
            foreach (Process proces in _proceses)
            {
                proces.Kill();
            }

           //data formatting
            SQLConnection connObj = new SQLConnection();
            connObj.ExecuteSqlHelper("[dbo].[sp_nice_data_formatting]");//data formatting
        }
        public class Claim
        {
            public string claimNo { get; set; }
            public DateTime? claimRecievedDate { get; set; }
        }
        private static List<T> ConvertDataTable<T>(DataTable dt)
        {
            List<T> data = new List<T>();
            foreach (DataRow row in dt.Rows)
            {
                T item = GetItem<T>(row);
                data.Add(item);
            }
            return data;
        }
        private static T GetItem<T>(DataRow dr)
        {
            Type temp = typeof(T);
            T obj = Activator.CreateInstance<T>();

            foreach (DataColumn column in dr.Table.Columns)
            {
                foreach (PropertyInfo pro in temp.GetProperties())
                {
                    object value = dr[column.ColumnName];
                    if (value == DBNull.Value)
                        value = null;
                    if (pro.Name == column.ColumnName)
                        pro.SetValue(obj, value, null);
                    else
                        continue;
                }
            }
            return obj;
        }
        public static List<List<T>> Split<T>(List<T> collection, int size)
        {
            var chunks = new List<List<T>>();
            var chunkCount = collection.Count() / size;

            if (collection.Count % size > 0)
                chunkCount++;

            for (var i = 0; i < chunkCount; i++)
                chunks.Add(collection.Skip(i * size).Take(size).ToList());

            return chunks;
        }

        public class ServiceItem
        {
            public int SNo { get; set; }
            public string RevenueCode { get; set; }
            public string CPTCode { get; set; }
            // public string CPTCodeType { get; set; }
            public string PI { get; set; }
            public string DX { get; set; }
        }
        public static dynamic GetAttributeValue(IScreen screen, string attr, out ReturnCode codeReturned, string claimType = null)
        {
            string attrValue = string.Empty;
            codeReturned = ReturnCode.Error;
            switch (attr)
            {
                //search provider by TID - num+y f9 tab enter TID
                case "ProviderSearchByTID":
                    break;
                case "ClaimFound":
                    attrValue = GetText(screen, 1, 1, 2000, true);
                    break;
                case "MEMBER":
                    attrValue = GetText(screen, 6, 9, 40);
                    break;
                case "MBR":
                    attrValue = GetText(screen, 4, 8, 7);
                    break;
                case "SUFFIX":
                    attrValue = GetText(screen, 4, 16, 2);
                    break;
                case "DECNo":
                    attrValue = GetText(screen, 10, 10, 12);
                    break;
                case "CLM":
                    attrValue = GetText(screen, 4, 26, 6);
                    break;
                case "dob":
                    attrValue = GetText(screen, 6, 53, 8);
                    break;
                case "reason":
                    attrValue = GetText(screen, 15, 77, 4);
                    break;
                case "claimType":
                    attrValue = GetText(screen, 4, 37, 2);
                    break;
                case "FDOS":
                    attrValue = GetText(screen, 12, 55, 11);
                    break;
                case "LDOS":
                    attrValue = GetText(screen, 12, 71, 8);
                    break;
                case "ELG":
                    attrValue = GetText(screen, 7, 6, 26);
                    break;
                case "ERSA":
                    attrValue = GetText(screen, 7, 37, 2);
                    break;
                case "TYP":
                    attrValue = GetText(screen, 7, 43, 8);
                    break;
                case "EMP":
                    attrValue = GetText(screen, 7, 55, 7);
                    break;
                case "PL":
                    attrValue = GetText(screen, 7, 65, 4);
                    break;
                case "diagnosisData":
                    GoToSpecificScreen(screen, ControlKeyCode.PF1, "d", 250);// Navigating to diagnosis Data screen


                    attrValue = GetText(screen, 6, 1, 2500);
                    codeReturned = GoToSpecificScreen(screen, ControlKeyCode.PF1, "F", 250);//for next page
                    while (codeReturned == ReturnCode.Success)
                    {
                        string nextPage = GetText(screen, 6, 1, 2500, true);
                        if (nextPage.Contains("CANNOT PAGE FWD; NO ADDITIONAL RECORDS TO DISPLAY"))
                        {
                            codeReturned = ReturnCode.Error;
                        }
                        else if (attrValue.Contains("REQUEST IN PROGRESS"))
                        {
                            codeReturned = ReturnCode.Success;
                        }
                        else
                        {
                            nextPage = GetText(screen, 6, 1, 2500);
                            attrValue += Environment.NewLine + nextPage;
                            codeReturned = GoToSpecificScreen(screen, ControlKeyCode.PF1, "F", 250);//for next page
                        }
                    }
                    GoToSpecificScreen(screen, ControlKeyCode.PF1, "8");// Navigating to main Claim inquiry screen
                    codeReturned = ReturnCode.Success;
                    break;
                case "STS":
                    attrValue = GetText(screen, 16, 37, 2);
                    break;
                case "H":
                    attrValue = GetText(screen, 16, 42, 3);
                    break;
                case "provSpec":
                    attrValue = GetText(screen, 12, 7, 15);
                    break;

                case "assgnProv":
                    attrValue = GetText(screen, 13, 18, 19);
                    break;
                case "prvContract":
                    attrValue = GetText(screen, 11, 45, 2);
                    break;
                case "additionalMOD":
                    codeReturned = GoToSpecificScreen(screen, ControlKeyCode.F8, null, 250);// Navigating to Additional MOD screen
                    if (codeReturned == ReturnCode.Error)
                    {
                        EnterText(screen, "7", true); // Navigating to Claims Inquiry screen again
                        break;
                    }
                    while (codeReturned == ReturnCode.Success)
                    {
                        attrValue = GetText(screen, 15, 1, 1000, true);
                        if (attrValue.Contains("REQUEST IN PROGRESS"))
                        {
                            codeReturned = ReturnCode.Success;
                        }
                        else
                        {
                            attrValue = GetText(screen, 15, 1, 1000);
                            codeReturned = ReturnCode.Error;
                        }
                    }
                    codeReturned = GoToSpecificScreen(screen, ControlKeyCode.PF1, "F", 250);//for next page
                    while (codeReturned == ReturnCode.Success)
                    {
                        string nextPage = GetText(screen, 15, 1, 1100, true);
                        if (nextPage.Contains("CANNOT PAGE FWD; NO ADDITIONAL RECORDS TO DISPLAY"))
                        {
                            codeReturned = ReturnCode.Error;
                        }
                        else if (nextPage.Contains("REQUEST IN PROGRESS"))
                        {
                            codeReturned = ReturnCode.Success;
                        }
                        else
                        {
                            nextPage = GetText(screen, 15, 1, 1100);
                            attrValue += Environment.NewLine + nextPage;
                            codeReturned = GoToSpecificScreen(screen, ControlKeyCode.PF1, "F", 250);//for next page
                        }
                    }
                    GoToSpecificScreen(screen, ControlKeyCode.PF1, "8");// Navigating to main Claim inquiry screen
                    codeReturned = ReturnCode.Success;
                    break;
                case "comments":

                    codeReturned = GoToSpecificScreen(screen, ControlKeyCode.PF1, "I", 250);// Navigating to Comments Maintenece screen
                    if(codeReturned==ReturnCode.Error)
                    {
                        EnterText(screen, "7", true); // Navigating to Claims Inquiry screen again
                        break;
                    }
                    string clm = GetText(screen, 4, 27, 5);

                    if (clm.Trim() == "0")
                    {
                        codeReturned = GoToSpecificScreen(screen, ControlKeyCode.PF1, "X", 250);//for next page
                    }
                    while (codeReturned == ReturnCode.Success)
                    {
                        attrValue = GetText(screen, 8, 1, 1300, true);
                        if (attrValue.Contains("REQUEST IN PROGRESS"))
                        {
                            codeReturned = ReturnCode.Success;
                        }
                        else
                        {
                            attrValue = GetText(screen, 8, 1, 1300);
                            codeReturned = ReturnCode.Error;
                        }
                    }
                    codeReturned = GoToSpecificScreen(screen, ControlKeyCode.PF1, "F", 250);//for next page
                    while (codeReturned == ReturnCode.Success)
                    {
                        string nextPage = GetText(screen, 8, 1, 1500, true);
                        if (nextPage.Contains("CANNOT PAGE FWD; NO ADDITIONAL RECORDS TO DISPLAY"))
                        {
                            codeReturned = ReturnCode.Error;
                        }
                        else if (nextPage.Contains("Cancel resulted from fatal error reaching procedure server condition handler"))
                        {
                            codeReturned = ReturnCode.Timeout;
                        }
                        else if (nextPage.Contains("REQUEST IN PROGRESS"))
                        {
                            codeReturned = ReturnCode.Success;
                        }
                        else
                        {
                            nextPage = GetText(screen, 8, 1, 1500);
                            attrValue += Environment.NewLine + nextPage;
                            codeReturned = GoToSpecificScreen(screen, ControlKeyCode.PF1, "F", 250);//for next page
                            nextPage = GetText(screen, 8, 1, 1500);
                            if (nextPage.Contains("Cancel resulted from fatal error reaching procedure server condition handler"))
                            {
                                codeReturned = ReturnCode.Timeout;
                                break;
                            }
                        }
                    }
                    codeReturned = ReturnCode.Success;
                    //GoToSpecificScreen(screen, ControlKeyCode.PF1, "G");//going back to Claim Inquiry screen
                    break;
                case "comments_zero":

                    // codeReturned = GoToSpecificScreen(screen, ControlKeyCode.PF1, "I", 250);// Navigating to Comments Maintenece screen

                    codeReturned = GoToSpecificScreen(screen, ControlKeyCode.PF1, "X", 250);//for next page

                    while (codeReturned == ReturnCode.Success)
                    {
                        attrValue = GetText(screen, 8, 1, 1300, true);
                        if (attrValue.Contains("REQUEST IN PROGRESS"))
                        {
                            codeReturned = ReturnCode.Success;
                        }
                        else
                        {
                            attrValue = GetText(screen, 8, 1, 1300);
                            codeReturned = ReturnCode.Error;
                        }
                    }
                    codeReturned = GoToSpecificScreen(screen, ControlKeyCode.PF1, "F", 250);//for next page
                    while (codeReturned == ReturnCode.Success)
                    {
                        string nextPage = GetText(screen, 8, 1, 1500, true);
                        if (nextPage.Contains("CANNOT PAGE FWD; NO ADDITIONAL RECORDS TO DISPLAY"))
                        {
                            codeReturned = ReturnCode.Error;
                        }
                        else if (nextPage.Contains("REQUEST IN PROGRESS"))
                        {
                            codeReturned = ReturnCode.Success;
                        }
                        else
                        {
                            nextPage = GetText(screen, 8, 1, 1500);
                            attrValue += Environment.NewLine + nextPage;
                            codeReturned = GoToSpecificScreen(screen, ControlKeyCode.PF1, "F", 250);//for next page
                        }
                    }
                    codeReturned = ReturnCode.Success;
                    //GoToSpecificScreen(screen, ControlKeyCode.PF1, "G");//going back to Claim Inquiry screen
                    break;
                case "commentsY":

                    //codeReturned = GoToSpecificScreen(screen, ControlKeyCode.PF1, "I", 250);// Navigating to Comments Maintenece screen

                    codeReturned = GoToSpecificScreen(screen, ControlKeyCode.PF1, "Y", 250);//for next page

                    while (codeReturned == ReturnCode.Success)
                    {
                        attrValue = GetText(screen, 8, 1, 1300, true);
                        if (attrValue.Contains("REQUEST IN PROGRESS"))
                        {
                            codeReturned = ReturnCode.Success;
                        }
                        else
                        {
                            attrValue = GetText(screen, 8, 1, 1300);
                            codeReturned = ReturnCode.Error;
                        }
                    }
                    codeReturned = GoToSpecificScreen(screen, ControlKeyCode.PF1, "F", 250);//for next page
                    while (codeReturned == ReturnCode.Success)
                    {
                        string nextPage = GetText(screen, 8, 1, 1500, true);
                        if (nextPage.Contains("CANNOT PAGE FWD; NO ADDITIONAL RECORDS TO DISPLAY"))
                        {
                            codeReturned = ReturnCode.Error;
                        }
                        else if (nextPage.Contains("REQUEST IN PROGRESS"))
                        {
                            codeReturned = ReturnCode.Success;
                        }
                        else
                        {
                            nextPage = GetText(screen, 8, 1, 1500);
                            attrValue += Environment.NewLine + nextPage;
                            codeReturned = GoToSpecificScreen(screen, ControlKeyCode.PF1, "F", 250);//for next page
                        }
                    }
                    codeReturned = GoToSpecificScreen(screen, ControlKeyCode.PF1, "G");//going back to Claim Inquiry screen
                    break;
                case "memberAlerts":

                    codeReturned = GoToSpecificScreen(screen, ControlKeyCode.PF1, "O", 250);// Navigating toCLAIM INQUIRY SUBMENU screen 
                    EnterText(screen, "M", true); // Navigating to member alerts screen
                    screen.Wait(1000);
                    while (codeReturned == ReturnCode.Success)
                    {
                        attrValue = GetText(screen, 8, 1, 1300, true);
                        if (attrValue.Contains("REQUEST IN PROGRESS") || attrValue.Contains("PROCESSING <RETURN>"))
                        {
                            codeReturned = ReturnCode.Success;
                        }
                        else
                        {
                            codeReturned = ReturnCode.Error;
                        }
                    }
                    attrValue = GetText(screen, 8, 1, 1000);
                    //codeReturned = GoToSpecificScreen(screen, ControlKeyCode.PF1, "F", 250);//for next page
                    //while (codeReturned == ReturnCode.Success)
                    //{
                    //    string nextPage = GetText(screen, 8, 1, 1500, true);
                    //    if (nextPage.Contains("End of the list"))
                    //    {
                    //        codeReturned = ReturnCode.Error;
                    //    }
                    //    else if (nextPage.Contains("REQUEST IN PROGRESS"))
                    //    {
                    //        codeReturned = ReturnCode.Success;
                    //    }
                    //    else
                    //    {
                    //        nextPage = GetText(screen, 8, 1, 1500);
                    //        attrValue += Environment.NewLine + nextPage;
                    //        codeReturned = GoToSpecificScreen(screen, ControlKeyCode.PF1, "F", 250);//for next page
                    //    }
                    //}

                    codeReturned = GoToSpecificScreen(screen, ControlKeyCode.F20, null, 250);//going back to CLAIM INQUIRY SUBMENU screen
                    EnterText(screen, "L", true); // //going back to CLAIM INQUIRY screen
                    screen.Wait(1000);
                    break;
                case "mbrElgPlans":

                    GoToSpecificScreen(screen, ControlKeyCode.PF1, "O", 250);// Navigating to CLAIM INQUIRY SUBMENU screen 
                    EnterText(screen, "G", true); // Navigating to ELIGIBILITY INQUIRY   screen
                    screen.Wait(1000);
                    //screen.WaitForIncomingData(2000);
                    codeReturned = ReturnCode.Success;
                    while (codeReturned == ReturnCode.Success)
                    {
                        attrValue = GetText(screen, 15, 1, 1000, true);
                        if (attrValue.Contains("ENTER OPTION LETTER:"))
                        {
                            codeReturned = ReturnCode.Success;
                        }
                        else
                        {
                            
                            codeReturned = ReturnCode.Error;
                        }
                    }
                    attrValue = "";
                    //we need to put the check for DOS in the result from above screen if service expired, then we will navigate to shift+f9 to search the provider by last name, first name hi
                    //enter and select the records with this condition MBR!=SUBSCRIBER then enter X enter press enter then we will press F9 and get the records.
                    string elg = claimType.Split('|')[0].Trim();
                    string fdos = claimType.Split('|')[1].Trim();
                    string ldos = claimType.Split('|')[2].Trim();
                    string subsciber = claimType.Split('|')[3].Trim();
                    string _dob1 = GetText(screen, 6, 6, 10);
                    DateTime dob = Convert.ToDateTime(_dob1).Date;
                    //DateTime dob = Convert.ToDateTime(claimType.Split('|')[4].Trim()).Date;
                    bool flag = Boolean.Parse(claimType.Split('|')[4].Trim());
                    string lName = claimType.Split('|')[5].Trim().Substring(0, 21).Trim();
                    string fName = claimType.Split('|')[5].Trim().Substring(21).Trim().Split(' ')[0];
                    //if flag=true then shift+f9
                    if (flag)
                    {
                        
                        codeReturned = GoToSpecificScreen(screen, ControlKeyCode.F19, null, 250);// Navigating to provider search screen
                        codeReturned = GoToSpecificScreen(screen, ControlKeyCode.Remove, null);// remove subscr number
                        codeReturned = GoToSpecificScreen(screen, ControlKeyCode.Tab, null);// navigate to last name
                        EnterText(screen, lName, false);//enter last name
                        codeReturned = GoToSpecificScreen(screen, ControlKeyCode.Tab, null);// navigate to first name
                        EnterText(screen, fName, false);//enter first name
                        codeReturned = GoToSpecificScreen(screen, ControlKeyCode.Find, null,500);// press home key

                        while (codeReturned == ReturnCode.Success)
                        {
                            string nextPage = GetText(screen, 5, 1, 2000, true);
                            if (nextPage.Contains("CANNOT PAGE FWD; NO ADDITIONAL RECORDS TO DISPLAY"))
                            {
                                codeReturned = ReturnCode.Error;
                            }
                            else if (nextPage.Contains("REQUEST IN PROGRESS"))
                            {
                                codeReturned = ReturnCode.Success;
                            }
                            else
                            {
                                nextPage = GetText(screen, 6, 1, 2000);
                                attrValue += Environment.NewLine + nextPage;
                                codeReturned = GoToSpecificScreen(screen, ControlKeyCode.PF1, "F", 250);//for next page
                            }
                        }
                        string[] mbrElgRecords = attrValue.Replace("+------------------------------------------------------------------------------+","")
                            .Replace("CO-I-001  CANNOT PAGE FWD; NO ADDITIONAL RECORDS TO DISPLAY","")
                            .Replace("PF1 F=FORWARD, PF1 B=BACKWARD, PF1 X=CANCEL,  ENTER X AND <RETURN> TO SELECT","")
                            .Replace("          LAST NAME         FIRST NAME   I  LAST NAME    NUMBER      DATE   ID", "")
                            .Replace("                                                             -     /  /       ", "").Replace("|", "").Replace("\n", "").Split('\r');
                        mbrElgRecords = mbrElgRecords.Where(x => !string.IsNullOrEmpty(x.Trim())).ToArray();
                        int tabCount = 0;
                        if (mbrElgRecords.Length > 1)
                        {
                            for (var i = 0; i < mbrElgRecords.Length; i++)
                            {
                                mbrElgRecords[i] = mbrElgRecords[i].Replace("\r", "");
                                if (mbrElgRecords[i] != "")
                                {
                                    var _lName = mbrElgRecords[i].Substring(1, 24).Trim();
                                    var _fName = mbrElgRecords[i].Substring(25, 15).Trim();
                                    var _subscbr = mbrElgRecords[i].Substring(54, 11).Split('-')[0].Trim();
                                    var _dob = Convert.ToDateTime(mbrElgRecords[i].Substring(65, 10)).Date;
                                    if (fName == _fName && lName == _lName && dob == _dob && subsciber != _subscbr)
                                    {
                                        //tabCount += 1;
                                        codeReturned = GoToSpecificScreen(screen, ControlKeyCode.Tab, null);// navigate to first name
                                        EnterText(screen, "X", true);//selecting the subscriber
                                        break;
                                    }
                                }
                            }
                            EnterText(screen, "X", true);//selecting the subscriber
                        }
                        else
                        {
                            EnterText(screen, "X", true);//selecting the subscriber
                        }
                    }

                    codeReturned = GoToSpecificScreen(screen, ControlKeyCode.F9, null, 250);// Navigating to main Claim inquiry screen
                    attrValue = "";
                    while (codeReturned == ReturnCode.Success)
                    {
                        attrValue = GetText(screen, 5, 1, 2000, true);
                        if (attrValue.Contains("LOADING COVERAGE GROUP HISTORY"))
                        {
                            codeReturned = ReturnCode.Success;
                        }
                        else
                        {
                            attrValue = GetText(screen, 5, 1, 2000);
                            codeReturned = ReturnCode.Error;
                        }
                    }

                    codeReturned = GoToSpecificScreen(screen, ControlKeyCode.PF1, "F", 250);//for next page
                    while (codeReturned == ReturnCode.Success)
                    {
                        string nextPage = GetText(screen, 5, 1, 2000, true);
                        if (nextPage.Contains("CANNOT PAGE FWD; NO ADDITIONAL RECORDS TO DISPLAY"))
                        {
                            codeReturned = ReturnCode.Error;
                        }
                        else if (nextPage.Contains("REQUEST IN PROGRESS"))
                        {
                            codeReturned = ReturnCode.Success;
                        }
                        else
                        {
                            nextPage = GetText(screen, 5, 1, 2000);
                            attrValue += Environment.NewLine + nextPage;
                            codeReturned = GoToSpecificScreen(screen, ControlKeyCode.PF1, "F", 250);//for next page
                        }
                    }

                    

                    GoToSpecificScreen(screen, ControlKeyCode.F19);//going back to CLAIM INQUIRY SUBMENU screen
                    codeReturned = GoToSpecificScreen(screen, ControlKeyCode.F20);//going back to CLAIM INQUIRY SUBMENU screen
                    EnterText(screen, "L", true); // //going back to CLAIM INQUIRY screen
                    screen.Wait(1000);
                    break;
                case "cobAnesthesia":
                    GoToSpecificScreen(screen, ControlKeyCode.PF1, "Q", 250);// Navigating to Service Provider Contracts screen 
                    attrValue = GetText(screen, 11, 1, 1500, true);                                          //EnterText(screen, "X", true); // Navigating to Service Provider Contracts screen MENU
                    if (!attrValue.Contains("NO CONTRACT IN EFFECT FOR SVC PROVIDER"))
                    {
                        codeReturned = ReturnCode.Success;                                             //     screen.Wait(2000);
                        while (codeReturned == ReturnCode.Success)
                        {
                            attrValue = GetText(screen, 11, 1, 1500, true);
                            if (attrValue.Contains("REQUEST IN PROGRESS"))
                            {
                                codeReturned = ReturnCode.Success;
                            }
                            else
                            {
                                attrValue = "";
                                codeReturned = ReturnCode.Error;
                            }
                        }
                        GoToSpecificScreen(screen, ControlKeyCode.Enter, null);
                        EnterText(screen, "16", true); // Navigating to Claims Text   screen
                        screen.Wait(1000);
                        codeReturned = ReturnCode.Success;
                        while (codeReturned == ReturnCode.Success)
                        {
                            attrValue = GetText(screen, 11, 1, 1500, true);
                            if (attrValue.Contains("REQUEST IN PROGRESS"))
                            {
                                codeReturned = ReturnCode.Success;
                            }
                            else
                            {
                                attrValue = "";
                                codeReturned = ReturnCode.Error;
                            }
                        }
                        attrValue = GetText(screen, 8, 1, 2000);
                        codeReturned = ReturnCode.Success;
                        if (!attrValue.Contains("CLAIMS CONTRACT TEXT RECORD DOES NOT EXIST"))
                        {
                            while (codeReturned == ReturnCode.Success)
                            {
                                attrValue = GetText(screen, 8, 1, 2000, true);
                                if (attrValue.Contains("PROCESSING <RETURN>"))
                                {
                                    codeReturned = ReturnCode.Success;
                                }
                                else
                                {
                                    attrValue = GetText(screen, 8, 1, 2000);
                                    codeReturned = ReturnCode.Error;
                                }
                            }

                            codeReturned = GoToSpecificScreen(screen, ControlKeyCode.NextScreen, null, 250);//for next page
                            while (codeReturned == ReturnCode.Success)
                            {
                                string nextPage = GetText(screen, 8, 1, 2000, true);
                                if (nextPage.Contains("CANNOT PAGE FORWARD; NO ADDITIONAL RECORDS TO DISPLAY"))
                                {
                                    codeReturned = ReturnCode.Error;
                                }
                                else if (nextPage.Contains("PROCESSING <NEXT> GET NEXT PAGE"))
                                {
                                    codeReturned = ReturnCode.Success;
                                }
                                else
                                {
                                    nextPage = GetText(screen, 8, 1, 2000);
                                    attrValue += Environment.NewLine + nextPage;
                                    codeReturned = GoToSpecificScreen(screen, ControlKeyCode.NextScreen, null, 250);//for next page
                                }
                            }


                        }
                        else
                            attrValue = "";
                        GoToSpecificScreen(screen, ControlKeyCode.F18);//going back to  Provider Contracts screen MENU
                    }
                    else
                        attrValue = "NO CONTRACT IN EFFECT FOR SVC PROVIDER";
                    codeReturned = ReturnCode.Success;
                    break;
                case "servicePayment":

                    if (claimType == "Professional")
                    {
                        EnterText(screen, "10", true); // Navigating to Claims Text   screen
                        screen.Wait(500);
                        codeReturned = ReturnCode.Success;
                        while (codeReturned == ReturnCode.Success)
                        {
                            attrValue = GetText(screen, 8, 1, 2000, true);
                            if (attrValue.Contains("PROCESSING <RETURN>"))
                            {
                                codeReturned = ReturnCode.Success;
                            }
                            else
                            {
                                attrValue = "";
                                codeReturned = ReturnCode.Error;
                            }
                        }
                        attrValue = GetText(screen, 8, 1, 2000);
                        if (attrValue.Contains("SERVICE PAYMENT RECORD DOES NOT EXIST"))
                        {
                            attrValue = "";
                            codeReturned = ReturnCode.Success;
                        }

                        GoToSpecificScreen(screen, ControlKeyCode.PF1, "M");//going back to  Provider Contracts screen MENU
                    }
                    else
                        GoToSpecificScreen(screen, ControlKeyCode.F19);//going back to CLAIM INQUIRY screen
                    break;
                case "feeSchduled":
                    EnterText(screen, "9", true); // Navigating to Claims Text   screen
                    screen.Wait(500);
                    codeReturned = ReturnCode.Success;
                    while (codeReturned == ReturnCode.Success)
                    {
                        attrValue = GetText(screen, 9, 41, 11, true);
                        if (attrValue.Contains("PROCESSING <RETURN>"))
                        {
                            codeReturned = ReturnCode.Success;
                        }
                        else
                        {
                            attrValue = "";
                            codeReturned = ReturnCode.Error;
                        }
                    }

                    attrValue = GetText(screen, 9, 41, 11);

                    GoToSpecificScreen(screen, ControlKeyCode.PF1, "M");//going back to  Provider Contracts screen MENU
                    GoToSpecificScreen(screen, ControlKeyCode.F19);//going back to CLAIM INQUIRY screen
                    codeReturned = ReturnCode.Success;
                    break;
                case "serviceItem":

                    codeReturned = GoToSpecificScreen(screen, ControlKeyCode.PF1, "2", 250);// Navigating to Detail Adjustments screen

                    while (codeReturned == ReturnCode.Success)
                    {
                        attrValue = GetText(screen, 11, 1, 1500, true);
                        if (attrValue.Contains("REQUEST IN PROGRESS"))
                        {
                            codeReturned = ReturnCode.Success;
                        }
                        else
                        {
                            attrValue = GetText(screen, 11, 1, 1500);
                            codeReturned = ReturnCode.Error;
                        }
                    }

                    codeReturned = GoToSpecificScreen(screen, ControlKeyCode.PF1, "F", 250);//for next page

                    while (codeReturned == ReturnCode.Success)
                    {
                        string nextPage = GetText(screen, 11, 1, 1500, true);
                        if (nextPage.Contains("CANNOT PAGE FWD; NO ADDITIONAL RECORDS TO DISPLAY"))
                        {
                            codeReturned = ReturnCode.Error;
                        }
                        else if (nextPage.Contains("REQUEST IN PROGRESS"))
                        {
                            codeReturned = ReturnCode.Success;
                        }
                        else
                        {
                            nextPage = GetText(screen, 11, 1, 1500);
                            attrValue += Environment.NewLine + (nextPage.Length > 600 ? nextPage.Substring(0, 600) : nextPage);
                            codeReturned = GoToSpecificScreen(screen, ControlKeyCode.PF1, "F", 250);//for next page
                        }
                    }
                    codeReturned = GoToSpecificScreen(screen, ControlKeyCode.PF1, "8");// Navigating back to main Claim inquiry screen
                    break;
                    //List<ServiceItem> svcItems = new List<ServiceItem>();

                    ////logic to find out multiple revenue codes
                    //if (!string.IsNullOrEmpty(attrValue))
                    //{
                    //    attrValue = attrValue.Trim();
                    //    foreach (string revCodeLine in attrValue.Split('\n'))
                    //    {
                    //        var svcItem = new ServiceItem();
                    //        if (revCodeLine.Trim() != "" && revCodeLine.Trim().Length > 8 && revCodeLine.Trim().Substring(2, 8).Trim().StartsWith("U") && !revCodeLine.Trim().Contains("+-"))//svc code | PI | DX
                    //        {
                    //            string code = revCodeLine.Trim().Substring(0, 12).Trim().Split(' ')[1].Substring(0, 5);
                    //            int piLoc = 29 + (revCodeLine.Trim().Substring(0, 12).Trim().Split(' ')[0].Length - 1);
                    //            int piDx = 31 + (revCodeLine.Trim().Substring(0, 12).Trim().Split(' ')[0].Length - 1);
                    //            svcItem.SNo = Convert.ToInt32(revCodeLine.Trim().Substring(0, 12).Trim().Split(' ')[0]);
                    //            svcItem.RevenueCode = code;
                    //            svcItem.PI = revCodeLine.Trim().Substring(piLoc, 1);
                    //            svcItem.DX = revCodeLine.Trim().Substring(piDx, 1);
                    //            svcItems.Add(svcItem);
                    //        }
                    //        if (revCodeLine.Trim() != "" && revCodeLine.Trim().Length > 8 && !revCodeLine.Trim().Substring(2, 5).Trim().StartsWith("U") && !revCodeLine.Trim().Contains("+-") && !revCodeLine.Trim().Contains("COMMENTS") && !revCodeLine.Trim().Contains("|") && !revCodeLine.Trim().Contains("PF1 4=DTL CMNT 6=NXT BENE  7=NXT DED  F/B=FWD/BCK  I=COMNT O=OTHER M=MENU F6=FEE") && !revCodeLine.Trim().Contains("PF1 8=CLM INQ  3=PYMT INQ  P=A*PRVDR    Q=S*PRVDR  Y=PRVDR N=AUTH  F8=ADDL"))//svc code | PI | DX
                    //        {
                    //            svcItems.Last().CPTCode = revCodeLine.Trim().Substring(0, 8);
                    //            //double vl = 0;
                    //            //if (Double.TryParse(svcItems.Last().CPTCode, out vl) || svcItems.Last().CPTCode.Contains("-"))
                    //            //{
                    //            //    //range logic has to be added yet
                    //            //    svcItems.Last().CPTCodeType = "Surgical";
                    //            //}
                    //            //else
                    //            //{
                    //            //    svcItems.Last().CPTCodeType = "HCPCS";
                    //            //}
                    //        }

                    //    }

                    //}

                    //List<ServiceItem> nList = svcItems.ToList();
                    //foreach (var item in svcItems)
                    //{
                    //    var matchedCount = nList.Count(s => s.RevenueCode == item.RevenueCode);
                    //    if(matchedCount>1)
                    //    {
                    //        nList.RemoveAll(s => s.RevenueCode == item.RevenueCode && string.IsNullOrEmpty(s.CPTCode));
                    //    }
                    //}
                    //return nList;


            }
            return attrValue;
        }
    }
}
