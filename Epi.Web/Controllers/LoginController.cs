﻿using System;
using System.Web.Mvc;
using Epi.Web.MVC.Facade;
using Epi.Web.MVC.Models;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Epi.Core.EnterInterpreter;
using System.Web.Security;
using System.Reflection;
using System.Diagnostics;
using Epi.Web.Enter.Common.Constants;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Web.Configuration;
using System.Configuration;
using Epi.Web.Enter.Common.Message;
using System.Web.Hosting;
using Microsoft.AspNet.Identity;
using System.Security.Principal;
using System.Web.Hosting;
using System.DirectoryServices.AccountManagement;
using System.Security.Principal;
using System.IO;

namespace Epi.Web.MVC.Controllers
{
   
    public class LoginController : Controller
    {
        //declare SurveyTransactionObject object
        private Epi.Web.MVC.Facade.ISurveyFacade _isurveyFacade;
        /// <summary>
        /// Injectinting SurveyTransactionObject through Constructor
        /// </summary>
        /// <param name="iSurveyInfoRepository"></param>

        public LoginController(Epi.Web.MVC.Facade.ISurveyFacade isurveyFacade)
        {
            _isurveyFacade = isurveyFacade;
        }      
        
        // GET: /Login/
       
        [HttpGet]     
        public ActionResult Index(string responseId, string ReturnUrl)
        {
            string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            UserLoginModel UserLoginModel = new Models.UserLoginModel();          
            ViewBag.Version = version;
            SetTermOfUse();
            if (ConfigurationManager.AppSettings["IsDemoMode"] != null)
                Session["IsDemoMode"] = ConfigurationManager.AppSettings["IsDemoMode"].ToUpper();
            else
                Session["IsDemoMode"] = "null";
            //   //get the responseId
            //    responseId = GetResponseId(ReturnUrl);
            //    //get the surveyId
            //     string SurveyId = _isurveyFacade.GetSurveyAnswerResponse(responseId).SurveyResponseList[0].SurveyId;
            //     //put surveyId in viewbag so can be retrieved in Login/Index.cshtml
            //     ViewBag.SurveyId = SurveyId;
            if (System.Configuration.ConfigurationManager.AppSettings["IsDemoMode"] != null)
            {
                var IsDemoMode = System.Configuration.ConfigurationManager.AppSettings["IsDemoMode"];
                string UserId = Epi.Web.Enter.Common.Security.Cryptography.Encrypt("1");
                if (!string.IsNullOrEmpty(IsDemoMode) && IsDemoMode.ToUpper() == "TRUE")
                {
                  FormsAuthentication.SetAuthCookie("Guest@cdc.gov", false);
                  
                    Session["UserId"] = UserId;
                     Session["UserHighestRole"] = 3;
                     Session["UserFirstName"] = "John";
                    Session["UserLastName"]= "Doe";
                    Session["UserEmailAddress"] = "Guest@cdc.gov";
                    return RedirectToAction(Epi.Web.MVC.Constants.Constant.INDEX, "Home", new { surveyid = "" });
                }
            }
            var configuration = WebConfigurationManager.OpenWebConfiguration("/");
            var authenticationSection = (AuthenticationSection)configuration.GetSection("system.web/authentication");
            if (authenticationSection.Mode == AuthenticationMode.Forms)
            {
                return View("Index", UserLoginModel);
            }
            else
            {
              
               try
               {
                   var CurrentUserName = System.Web.HttpContext.Current.User.Identity.Name;
                    var UserAD = Utility.WindowsAuthentication.GetCurrentUserFromAd(CurrentUserName);
                    // validate user in EWE system
                    UserRequest User = new UserRequest();
                    User.IsAuthenticated = true;
                    User.User.EmailAddress = UserAD.EmailAddress;
             
                    UserResponse result = _isurveyFacade.GetUserInfo(User);
                    if (result  != null && result.User != null && result.User.Count() > 0)
                    {
                    FormsAuthentication.SetAuthCookie(CurrentUserName.Split('\\')[0].ToString(), false);
                    string UserId = Epi.Web.Enter.Common.Security.Cryptography.Encrypt(result.User[0].UserId.ToString());
                    Session["UserId"] = UserId;
                    //Session["UsertRole"] = result.User.Role;
                    Session["UserHighestRole"] = result.User[0].UserHighestRole;

                    Session["UserEmailAddress"] = result.User[0].EmailAddress;
                    Session["UserFirstName"] = result.User[0].FirstName;
                    Session["UserLastName"] = result.User[0].LastName;
                    Session["UGuid"] = result.User[0].UGuid;
                    return RedirectToAction(Epi.Web.MVC.Constants.Constant.INDEX, "Home", new { surveyid = "" });
                   } 
                    else
                    {

                        //return View("Index", UserLoginModel);
                        ViewBag.ErrorName = "--You are not an authorized user of the system.--";
                        return View("Error");
                   }
               }
               catch (Exception ex)
               {                   
                   //ViewBag.ErrorName = ex.Message;  
                   //return View("Error");
                   return View("Index", UserLoginModel);
               }
            }

        }

        private void SetTermOfUse()
        {
            string filepath = Server.MapPath("~\\Content\\Text\\TermOfUse.txt");
            string content = string.Empty;
            try
            {
                using (var stream = new StreamReader(filepath))
                {
                    content = stream.ReadToEnd();
                }
            }
            catch (Exception exc)
            {

            }
            if (ConfigurationManager.AppSettings["SHOW_TERMS_OF_USE"].ToUpper() == "TRUE")
            {
                ViewData["TermOfUse"] = content;
            }
        }

        
        [HttpPost]

        public ActionResult Index(UserLoginModel Model, string Action, string ReturnUrl)
        {

            return ValidateUser(Model, ReturnUrl);

            //if (ReturnUrl == null || !ReturnUrl.Contains("/"))
            //{
            //    ReturnUrl = "/Home/Index";
            //}


            //Epi.Web.Enter.Common.Message.UserAuthenticationResponse result = _isurveyFacade.ValidateUser(Model.UserName, Model.Password);

            //if (result.UserIsValid)
            //{
            //    if (result.User.ResetPassword)
            //    {
            //        return ResetPassword(Model.UserName);
            //    }
            //    else
            //    {

            //        FormsAuthentication.SetAuthCookie(Model.UserName, false);
            //        string UserId = Epi.Web.Enter.Common.Security.Cryptography.Encrypt(result.User.UserId.ToString());
            //        Session["UserId"] = UserId;
            //        return RedirectToAction(Epi.Web.MVC.Constants.Constant.INDEX, "Home", new { surveyid = "" });
            //    }
            //}
            //else
            //{
            //    ModelState.AddModelError("", "The email or password you entered is incorrect.");
            //    return View();
            //}
        }
        /// <summary>
        /// parse and return the responseId from response Url 
        /// </summary>
        /// <param name="returnUrl"></param>
        /// <returns></returns>
        private string GetResponseId(string returnUrl)
        {
            string responseId = string.Empty;
            string[] expressions = returnUrl.Split('/');

            foreach (var expression in expressions)
            {
                if (Epi.Web.MVC.Utility.SurveyHelper.IsGuid(expression))
                {

                    responseId = expression;
                    break;
                }

            }
            return responseId;
        }


        [HttpGet]
        public ActionResult ForgotPassword()
        {
            return View("ForgotPassword");
        }

        //[HttpGet]
        public ActionResult ResetPassword(UserResetPasswordModel model)
        {
            return View("ResetPassword", model);
        }

        [HttpPost]
        public ActionResult ForgotPassword(UserForgotPasswordModel Model, string Action, string ReturnUrl)
        {
            switch (Action.ToUpper())
            {
                case "CANCEL":
                    return RedirectToAction(Epi.Web.MVC.Constants.Constant.INDEX, "Login");
                default:
                    break;
            }

            if (!ModelState.IsValid)
            {
                var allErrors = ModelState.Values.SelectMany(v => v.Errors);
                List<string> errorMessages = new List<string>();

                string msg = ModelState.First().Value.Errors.First().ErrorMessage.ToString();

                ModelState.AddModelError("", msg);


                return View("ForgotPassword", Model);
            }

            bool success = _isurveyFacade.UpdateUser(new Enter.Common.DTO.UserDTO() { UserName = Model.UserName, Operation = Constant.OperationMode.UpdatePassword });
            if (success)
            {
                return RedirectToAction(Epi.Web.MVC.Constants.Constant.INDEX, "Login");
            }
            else
            {
                UserForgotPasswordModel model = new UserForgotPasswordModel();
                model.UserName = Model.UserName;

                ModelState.AddModelError("UserName", "You may have entered an email address that does not match our records. Please try again.");
                return View("ForgotPassword", model);
            }

        }

        [HttpPost]
        public ActionResult ResetPassword(UserResetPasswordModel Model, string Action, string ReturnUrl)
        {

            switch (Action.ToUpper())
            {
                case "CANCEL":
                    return RedirectToAction(Epi.Web.MVC.Constants.Constant.INDEX, "Login");
                default:
                    break;
            }

            if (!ModelState.IsValid)
            {
                UserResetPasswordModel model = new UserResetPasswordModel();
                model.UserName = Model.UserName;
                ReadPasswordPolicy(model);
               // ModelState.AddModelError("", "Passwords do not match. Please try again.");
                return View("ResetPassword", model);
            }

            if (!ValidatePassword(Model))
            {

                ModelState.AddModelError("", "Password is not strong enough. Please try again.");
                return View("ResetPassword", Model);
            }

            _isurveyFacade.UpdateUser(new Enter.Common.DTO.UserDTO() { UserName = Model.UserName, PasswordHash = Model.Password, Operation = Constant.OperationMode.UpdatePassword, ResetPassword = true });
            UserLoginModel UserLoginModel = new UserLoginModel();
            UserLoginModel.Password = Model.Password;
            UserLoginModel.UserName = Model.UserName;
            return ValidateUser(UserLoginModel, ReturnUrl);

        }

        private ActionResult ValidateUser(UserLoginModel Model, string ReturnUrl)
        {
            SetTermOfUse();
            string formId = "", pageNumber;
            
            if (ReturnUrl == null || !ReturnUrl.Contains("/"))
            {
                ReturnUrl = "/Home/Index";
            }
            else
            {
                formId = ReturnUrl.Substring(0, ReturnUrl.IndexOf('/'));
                pageNumber = ReturnUrl.Substring(ReturnUrl.LastIndexOf('/') + 1);
            }

            try
            {
                Epi.Web.Enter.Common.Message.UserAuthenticationResponse result = _isurveyFacade.ValidateUser(Model.UserName, Model.Password);
                if (result.UserIsValid)
                {
                    if (result.User.ResetPassword)
                    {
                        UserResetPasswordModel model = new UserResetPasswordModel();
                        model.UserName = Model.UserName;
                        model.FirstName = result.User.FirstName;
                        model.LastName = result.User.LastName;
                        ReadPasswordPolicy(model);
                        return ResetPassword(model);
                    }
                    else
                    {

                        FormsAuthentication.SetAuthCookie(Model.UserName, false);
                        string UserId = Epi.Web.Enter.Common.Security.Cryptography.Encrypt(result.User.UserId.ToString());
                        Session["UserId"] = UserId;
                        //Session["UsertRole"] = result.User.Role;
                        Session["UserHighestRole"] = result.User.UserHighestRole;
                        Session["UserEmailAddress"] = result.User.EmailAddress;
                        Session["UserFirstName"] = result.User.FirstName;
                        Session["UserLastName"] = result.User.LastName;
                        Session["UGuid"] = result.User.UGuid;
                        return RedirectToAction(Epi.Web.MVC.Constants.Constant.INDEX, "Home", new { surveyid = formId });
                        //return Redirect(ReturnUrl);
                    }
                }
                //else
                {
                ModelState.AddModelError("", "The email or password you entered is incorrect.");
                  Model.ViewValidationSummary = true;
                 return View(Model);
                }
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "The email or password you entered is incorrect.");
                Model.ViewValidationSummary = true;
                return View(Model);
                throw;
            }



        }

        private bool ValidatePassword(UserResetPasswordModel Model)
        {
            //int minLength = Convert.ToInt16(ConfigurationManager.AppSettings["PasswordMinimumLength"]);
            //int maxLength = Convert.ToInt16(ConfigurationManager.AppSettings["PasswordMaximumLength"]);
            //bool useSymbols = Convert.ToBoolean(ConfigurationManager.AppSettings["UseSymbols"]); //= false;
            //bool useNumeric = Convert.ToBoolean(ConfigurationManager.AppSettings["UseNumbers"]); //= false;
            //bool useLowerCase = Convert.ToBoolean(ConfigurationManager.AppSettings["UseLowerCase"]);
            //bool useUpperCase = Convert.ToBoolean(ConfigurationManager.AppSettings["UseUpperCase"]);
            //bool useUserIdInPassword = Convert.ToBoolean(ConfigurationManager.AppSettings["UseUserIdInPassword"]);
            //bool useUserNameInPassword = Convert.ToBoolean(ConfigurationManager.AppSettings["UseUserNameInPassword"]);
            //int numberOfTypesRequiredInPassword = Convert.ToInt16(ConfigurationManager.AppSettings["NumberOfTypesRequiredInPassword"]);

            ReadPasswordPolicy(Model);

            int successCounter = 0;

            if (Model.UseSymbols && HasSymbol(Model.Password))
            {
                successCounter++;
            }

            if (Model.UseUpperCase && HasUpperCase(Model.Password))
            {
                successCounter++;
            }
            if (Model.UseLowerCase && HasLowerCase(Model.Password))
            {
                successCounter++;
            }
            if (Model.UseNumeric && HasNumber(Model.Password))
            {
                successCounter++;
            }

            if (Model.UseUserIdInPassword)
            {
                if (Model.Password.ToString().Contains(Model.UserName.Split('@')[0].ToString()))
                {
                    successCounter = 0;
                }

            }

            if (Model.UseUserNameInPassword)
            {
                if (Model.Password.ToString().Contains(Model.FirstName) || Model.Password.ToString().Contains(Model.LastName))
                {
                    successCounter = 0;
                }
            }

            if (Model.Password.Length < Model.MinimumLength || Model.Password.Length > Model.MaximumLength)
            {
                return false;
            }

            if (Model.NumberOfTypesRequiredInPassword <= successCounter && successCounter != 0)
            {
                return true;
            }

            return false;
        }

        private bool HasNumber(string password)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(password, @"\d");
        }

        private bool HasUpperCase(string password)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(password, @"[A-Z]");
        }

        private bool HasLowerCase(string password)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(password, @"[a-z]");
        }

        private bool HasSymbol(string password)
        {
            bool result = false;

            result = System.Text.RegularExpressions.Regex.IsMatch(password, @"[" + ConfigurationManager.AppSettings["Symbols"].Replace(" ", "") + "]");

            if (result)//Validates if password has only allowed characters.
            {
                foreach (char character in password.ToCharArray())
                {
                    if (Char.IsPunctuation(character))
                    {
                        if (!System.Text.RegularExpressions.Regex.IsMatch(character.ToString(), @"[" + ConfigurationManager.AppSettings["Symbols"].Replace(" ", "") + "]"))
                        {
                            return false;
                        }
                    }
                }
            }

            return result;

        }

        private void ReadPasswordPolicy(UserResetPasswordModel Model)
        {
            Model.MinimumLength = Convert.ToInt16(ConfigurationManager.AppSettings["PasswordMinimumLength"]);
            Model.MaximumLength = Convert.ToInt16(ConfigurationManager.AppSettings["PasswordMaximumLength"]);
            Model.UseSymbols = Convert.ToBoolean(ConfigurationManager.AppSettings["UseSymbols"]); //= false;
            Model.UseNumeric = Convert.ToBoolean(ConfigurationManager.AppSettings["UseNumbers"]); //= false;
            Model.UseLowerCase = Convert.ToBoolean(ConfigurationManager.AppSettings["UseLowerCase"]);
            Model.UseUpperCase = Convert.ToBoolean(ConfigurationManager.AppSettings["UseUpperCase"]);
            Model.UseUserIdInPassword = Convert.ToBoolean(ConfigurationManager.AppSettings["UseUserIdInPassword"]);
            Model.UseUserNameInPassword = Convert.ToBoolean(ConfigurationManager.AppSettings["UseUserNameInPassword"]);
            Model.NumberOfTypesRequiredInPassword = Convert.ToInt16(ConfigurationManager.AppSettings["NumberOfTypesRequiredInPassword"]);
            Model.Symbols = ConfigurationManager.AppSettings["Symbols"];
        }
    }
}
