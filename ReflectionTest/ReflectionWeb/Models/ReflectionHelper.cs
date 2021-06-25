
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Attachmate.Reflection.Framework;
using Attachmate.Reflection.Emulation.OpenSystems;
using Attachmate.Reflection.UserInterface;
using Attachmate.Reflection;

namespace ReflectionWeb.Models
{
    public static class ReflectionHelper
    {

        public static void EnterSpecialKey(IScreen screen, ControlKeyCode key)
        {

            screen.SendControlKey(key);
        }
        public static ReturnCode EnterText(IScreen screen, string text, bool pressEnter = false)
        {
            //int row = screen.CursorRow;
            //int column = screen.CursorColumn;
            //screen.SelectAll();
            ReturnCode code = screen.SendKeys(text);
            //int row = screen.MouseRow;
            //int column = screen.MouseColumn;
            if (pressEnter)
                code = screen.SendControlKey(ControlKeyCode.Enter);
            return code;
        }
        public static string GetText(IScreen screen, int row, int col, int length, bool allText = false)
        {
            string text = string.Empty;
            int mrow = screen.MouseRow;
            int mcolumn = screen.MouseColumn;

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
        public static ITerminal OpenSession(string sessionPath)
        {

            //Start a visible instance of Reflection or get the instance running at the given channel name
            Application app = MyReflection.CreateApplication("myWorkspace", true);
            //Create a terminal from the session document file

            ITerminal terminal = (ITerminal)app.CreateControl(sessionPath);

            //Make the session visible in the workspace
            IFrame frame = (IFrame)app.GetObject("Frame");
            frame.CreateView(terminal);
            return terminal;
        }

        public static void CreateSession ()
        {
            //Start a visible instance of Reflection or get the instance running at the given channel name
            Application app = MyReflection.CreateApplication("myWorkspace", true);

            //Create a terminal for an Open Systems session
            ITerminal terminal = (ITerminal)app.CreateControl(new Guid("{BE835A80-CAB2-40d2-AFC0-6848E486BF58}"));

            //Specify the connection type, host name (or IP address), and connect.
            IConnectionSettingsTelnet conn = (IConnectionSettingsTelnet)terminal.ConnectionSettings;
            conn.HostAddress = "yourHostName";
            
            terminal.Connect();

            //Create a View to make the session visible                
            IFrame frame = (IFrame)app.GetObject("Frame");
            frame.CreateView(terminal);
        }

        public static bool CheckMemberInformation(string claimNo)
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

    }
}