using Attachmate.Reflection.Emulation.OpenSystems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ReflectionWeb.Models;

namespace ReflectionWeb.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            
            return View();
        }
        public JsonResult ChooseScreen(string screen)
        {
            string text = string.Empty;
            if (!string.IsNullOrEmpty(screen))
            {
                string sessionPath = Environment.GetEnvironmentVariable("USERPROFILE") + @"\Documents\Micro Focus\Reflection\clrwin.rdox";
                ////ITerminal terminal = OpenSession(sessionPath);
                ITerminal terminal = ReflectionHelper.ConnectToSession(sessionPath);
                ////EnterSpecialKey(terminal.Screen, ControlKeyCode.Enter);
                var code = ReflectionHelper.EnterText(terminal.Screen, screen, true);
                text = code.ToString();
               
            }
            return Json(text, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetClaimInformation(string claimNo)
        {
            string text = string.Empty;
            if (!string.IsNullOrEmpty(claimNo))
            {
                string sessionPath = Environment.GetEnvironmentVariable("USERPROFILE") + @"\Documents\Micro Focus\Reflection\clrwin.rdox";
                ITerminal terminal = ReflectionHelper.ConnectToSession(sessionPath);
              
                ReflectionHelper.EnterText(terminal.Screen, claimNo, true);

                //Console.ReadKey();
                //RunVBScript(@"C:\Users\siklakh\Desktop\test.vbs");

                //string message = "Hello world";
                //Dictionary<char, int> dict = message.Replace(" ", string.Empty).GroupBy(s => s).ToDictionary(grp => grp.Key, grp => grp.Count());

                text = ReflectionHelper.GetText(terminal.Screen, 1, 1, 10, true);
                //string member = ReflectionHelper.GetText(terminal.Screen, 6, 9, 40);
                //string dob = ReflectionHelper.GetText(terminal.Screen, 6, 53, 8);
                /*string reason = ReflectionHelper.GetText(terminal.Screen, 15, 77, 4)*/
                ;
            }
            return Json(text, JsonRequestBehavior.AllowGet);
        }

        public JsonResult ExecuteSpecialKey(string spcKey)
        {
            string text = string.Empty;
            if (!string.IsNullOrEmpty(spcKey))
            {
                string sessionPath = Environment.GetEnvironmentVariable("USERPROFILE") + @"\Documents\Micro Focus\Reflection\clrwin.rdox";
                ////ITerminal terminal = OpenSession(sessionPath);
                ITerminal terminal = ReflectionHelper.ConnectToSession(sessionPath);
                if (spcKey == "MainMenu")
                {
                    ReflectionHelper.EnterSpecialKey(terminal.Screen, ControlKeyCode.PF1);
                    ReflectionHelper.EnterText(terminal.Screen,"M");
                    text = "success";
                }
               
            }
            return Json(text,JsonRequestBehavior.AllowGet);
        }
        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}