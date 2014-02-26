﻿using System;
using System.Web.Mvc;
using Epi.Web.MVC.Models;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Linq;
using Epi.Core.EnterInterpreter;
using System.Collections.Generic;
using System.Web.Security;
using System.Configuration;
using System.Web;
using System.Web.Caching;
using System.Web.UI;
using System.Reflection;
using System.Diagnostics;
using Epi.Web.Common.Message;
using Epi.Web.MVC.Utility;
using Epi.Web.Common.DTO;
using System.Web.Configuration;
namespace Epi.Web.MVC.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private Epi.Web.MVC.Facade.ISurveyFacade _isurveyFacade;
        private IEnumerable<XElement> PageFields;
        private string RequiredList = "";
        private int NumberOfPages = -1;
        private int PageSize = -1;
        private int NumberOfResponses = -1;
        List<KeyValuePair<int, string>> Columns = new List<KeyValuePair<int, string>>();

        /// <summary>
        /// injecting surveyFacade to the constructor 
        /// </summary>
        /// <param name="surveyFacade"></param>
        public HomeController(Epi.Web.MVC.Facade.ISurveyFacade isurveyFacade)
        {
            _isurveyFacade = isurveyFacade;
        }

        public ActionResult Default()
        {
            return View("Default");
        }

        [HttpGet]
        public ActionResult Index(string surveyid)
        {

            int UserId = SurveyHelper.GetDecryptUserId(Session["UserId"].ToString());



            Guid UserId1 = new Guid();
            try
            {
                string SurveyMode = "";
                //SurveyInfoModel surveyInfoModel = GetSurveyInfo(surveyid);
                //  List<FormInfoModel> listOfformInfoModel = GetFormsInfoList(UserId1);

                FormModel FormModel = new Models.FormModel();
                FormModel.FormList = GetFormsInfoList(UserId1);
                Epi.Web.Common.Message.UserAuthenticationResponse result = _isurveyFacade.GetUserInfo(UserId);

                FormModel.UserFirstName = result.User.FirstName;
                FormModel.UserLastName = result.User.LastName;

                Session["UserFirstName"] = result.User.FirstName;
                Session["UserLastName"] = result.User.LastName;
                System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(@"(\r\n|\r|\n)+");

                //if (surveyInfoModel.IntroductionText != null)
                //{
                //    string introText = regex.Replace(surveyInfoModel.IntroductionText.Replace("  ", " &nbsp;"), "<br />");
                //    surveyInfoModel.IntroductionText = MvcHtmlString.Create(introText).ToString();
                //}

                //if (surveyInfoModel.IsDraftMode)
                //{
                //    surveyInfoModel.IsDraftModeStyleClass = "draft";
                //    SurveyMode = "draft";
                //}
                //else
                //{
                //    surveyInfoModel.IsDraftModeStyleClass = "final";
                //    SurveyMode = "final";
                //}
                bool IsMobileDevice = false;
                IsMobileDevice = this.Request.Browser.IsMobileDevice;
                Omniture OmnitureObj = Epi.Web.MVC.Utility.OmnitureHelper.GetSettings(SurveyMode, IsMobileDevice);

                ViewBag.Omniture = OmnitureObj;

                string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                ViewBag.Version = version;

                return View(Epi.Web.MVC.Constants.Constant.INDEX_PAGE, FormModel);
            }
            catch (Exception ex)
            {
                Epi.Web.Utility.ExceptionMessage.SendLogMessage(ex, this.HttpContext);
                return View(Epi.Web.MVC.Constants.Constant.EXCEPTION_PAGE);
            }
        }

        /// <summary>
        /// redirecting to Survey controller to action method Index
        /// </summary>
        /// <param name="surveyModel"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Index(string surveyid, string AddNewFormId, string EditForm)
        {
        int UserId = SurveyHelper.GetDecryptUserId(Session["UserId"].ToString());
            if (!string.IsNullOrEmpty(EditForm))
            {
                 Session["RootFormId"] = surveyid;
                 Session["RootResponseId"] = EditForm;
                Epi.Web.Common.DTO.SurveyAnswerDTO surveyAnswerDTO = GetSurveyAnswer(EditForm);
                string ChildRecordId = GetChildRecordId(surveyAnswerDTO);
                return RedirectToAction(Epi.Web.MVC.Constants.Constant.INDEX, Epi.Web.MVC.Constants.Constant.SURVEY_CONTROLLER, new { responseid = ChildRecordId, PageNumber = 1, Edit = "Edit" });
            }
            bool IsMobileDevice = this.Request.Browser.IsMobileDevice;


            if (IsMobileDevice == false)
            {
                IsMobileDevice = Epi.Web.MVC.Utility.SurveyHelper.IsMobileDevice(this.Request.UserAgent.ToString());
            }

            //if (IsMobileDevice == true)
            // {
            //     if (!string.IsNullOrEmpty(surveyid))
            //     {
            //         //return RedirectToAction(new { Controller = "FormResponse", Action = "Index", surveyid = surveyid });
            //         return RedirectToAction(Epi.Web.MVC.Constants.Constant.INDEX, "FormResponse", new { surveyid = surveyid  });
            //     }
            // }

            FormsAuthentication.SetAuthCookie("BeginSurvey", false);

            //create the responseid
            Guid ResponseID = Guid.NewGuid();
            TempData[Epi.Web.MVC.Constants.Constant.RESPONSE_ID] = ResponseID.ToString();

            // create the first survey response
            // Epi.Web.Common.DTO.SurveyAnswerDTO SurveyAnswer = _isurveyFacade.CreateSurveyAnswer(surveyModel.SurveyId, ResponseID.ToString());
            Session["RootFormId"] = AddNewFormId;
            Session["RootResponseId"] = ResponseID;
            Epi.Web.Common.DTO.SurveyAnswerDTO SurveyAnswer = _isurveyFacade.CreateSurveyAnswer(AddNewFormId, ResponseID.ToString(), UserId);
            SurveyInfoModel surveyInfoModel = GetSurveyInfo(SurveyAnswer.SurveyId);

            // set the survey answer to be production or test 
            SurveyAnswer.IsDraftMode = surveyInfoModel.IsDraftMode;
            XDocument xdoc = XDocument.Parse(surveyInfoModel.XML);

            MvcDynamicForms.Form form = _isurveyFacade.GetSurveyFormData(SurveyAnswer.SurveyId, 1, SurveyAnswer, IsMobileDevice);

            var _FieldsTypeIDs = from _FieldTypeID in
                                     xdoc.Descendants("Field")
                                 select _FieldTypeID;

            TempData["Width"] = form.Width + 100;

            XDocument xdocResponse = XDocument.Parse(SurveyAnswer.XML);

            XElement ViewElement = xdoc.XPathSelectElement("Template/Project/View");
            string checkcode = ViewElement.Attribute("CheckCode").Value.ToString();

            form.FormCheckCodeObj = form.GetCheckCodeObj(xdoc, xdocResponse, checkcode);

            ///////////////////////////// Execute - Record Before - start//////////////////////
            Dictionary<string, string> ContextDetailList = new Dictionary<string, string>();
            EnterRule FunctionObject_B = (EnterRule)form.FormCheckCodeObj.GetCommand("level=record&event=before&identifier=");
            SurveyResponseXML SurveyResponseXML = new SurveyResponseXML(PageFields, RequiredList);
            if (FunctionObject_B != null && !FunctionObject_B.IsNull())
            {
                try
                {
                SurveyAnswer.XML = SurveyResponseXML.CreateResponseDocument(xdoc, SurveyAnswer.XML);
                    //SurveyAnswer.XML = Epi.Web.MVC.Utility.SurveyHelper.CreateResponseDocument(xdoc, SurveyAnswer.XML, RequiredList);

                    form.RequiredFieldsList = this.RequiredList;
                    FunctionObject_B.Context.HiddenFieldList = form.HiddenFieldsList;
                    FunctionObject_B.Context.HighlightedFieldList = form.HighlightedFieldsList;
                    FunctionObject_B.Context.DisabledFieldList = form.DisabledFieldsList;
                    FunctionObject_B.Context.RequiredFieldList = form.RequiredFieldsList;

                    FunctionObject_B.Execute();

                    // field list
                    form.HiddenFieldsList = FunctionObject_B.Context.HiddenFieldList;
                    form.HighlightedFieldsList = FunctionObject_B.Context.HighlightedFieldList;
                    form.DisabledFieldsList = FunctionObject_B.Context.DisabledFieldList;
                    form.RequiredFieldsList = FunctionObject_B.Context.RequiredFieldList;


                    ContextDetailList = Epi.Web.MVC.Utility.SurveyHelper.GetContextDetailList(FunctionObject_B);
                    form = Epi.Web.MVC.Utility.SurveyHelper.UpdateControlsValuesFromContext(form, ContextDetailList);

                    _isurveyFacade.UpdateSurveyResponse(surveyInfoModel, ResponseID.ToString(), form, SurveyAnswer, false, false, 0, SurveyHelper.GetDecryptUserId(Session["UserId"].ToString()));
                }
                catch (Exception ex)
                {
                    // do nothing so that processing
                    // can continue
                }
            }
            else
            {
            SurveyAnswer.XML = SurveyResponseXML.CreateResponseDocument(xdoc, SurveyAnswer.XML);//, RequiredList);
                form.RequiredFieldsList = RequiredList;
                _isurveyFacade.UpdateSurveyResponse(surveyInfoModel, SurveyAnswer.ResponseId, form, SurveyAnswer, false, false, 0, SurveyHelper.GetDecryptUserId(Session["UserId"].ToString()));
            }

            SurveyAnswer = _isurveyFacade.GetSurveyAnswerResponse(SurveyAnswer.ResponseId).SurveyResponseList[0];

            ///////////////////////////// Execute - Record Before - End//////////////////////
            //string page;
            // return RedirectToAction(Epi.Web.Models.Constants.Constant.INDEX, Epi.Web.Models.Constants.Constant.SURVEY_CONTROLLER, new {id="page" });
            return RedirectToAction(Epi.Web.MVC.Constants.Constant.INDEX, Epi.Web.MVC.Constants.Constant.SURVEY_CONTROLLER, new { responseid = ResponseID, PageNumber = 1 });
            //}
            //catch (Exception ex)
            //{
            //    //Epi.Web.Utility.ExceptionMessage.SendLogMessage(ex, this.HttpContext);
            //    //return View(Epi.Web.MVC.Constants.Constant.EXCEPTION_PAGE);
            //}
        }

        private string GetChildRecordId(SurveyAnswerDTO surveyAnswerDTO)
        {
            SurveyAnswerRequest SurveyAnswerRequest = new SurveyAnswerRequest();
            SurveyAnswerResponse SurveyAnswerResponse = new SurveyAnswerResponse();
            string ChildId = Guid.NewGuid().ToString();
            surveyAnswerDTO.ParentRecordId = surveyAnswerDTO.ResponseId;
            surveyAnswerDTO.ResponseId = ChildId;
            surveyAnswerDTO.Status = 1;
            SurveyAnswerRequest.SurveyAnswerList.Add(surveyAnswerDTO);
            string result = ChildId;

            //responseId = TempData[Epi.Web.MVC.Constants.Constant.RESPONSE_ID].ToString();
            string Id = Session["UserId"].ToString();
            SurveyAnswerRequest.Criteria.UserId = SurveyHelper.GetDecryptUserId(Id);//_UserId;
            SurveyAnswerRequest.RequestId = ChildId;
            SurveyAnswerRequest.Action = "Create";
            SurveyAnswerResponse = _isurveyFacade.SetChildRecord(SurveyAnswerRequest);

            return result;
        }
        //[HttpPost]
        //public ActionResult Index(List<FormInfoModel> model) {
        //    return View("ListResponses", model);
        //}

        [HttpGet]
        [Authorize]
        public ActionResult ReadResponseInfo(string formid, int page = 1)//List<FormInfoModel> ModelList, string formid)
        {
            bool IsMobileDevice = this.Request.Browser.IsMobileDevice;

            var model = new FormResponseInfoModel();


            model = GetFormResponseInfoModel(formid, page);

            if (IsMobileDevice == false)
            {
                return PartialView("ListResponses", model);
            }
            else
            {
                return View("ListResponses", model);
            }
        }

        /// <summary>
        /// Following Action method takes ResponseId as a parameter and deletes the response.
        /// For now it returns nothing as a confirmation of deletion, we may add some error/success
        /// messages later. TBD
        /// </summary>
        /// <param name="ResponseId"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Delete(string ResponseId)
        {
            SurveyAnswerRequest SARequest = new SurveyAnswerRequest();
            SARequest.SurveyAnswerList.Add(new SurveyAnswerDTO() { ResponseId = ResponseId });
            string Id = Session["UserId"].ToString();
            SARequest.Criteria.UserId = SurveyHelper.GetDecryptUserId(Id);

            SurveyAnswerResponse SAResponse = _isurveyFacade.DeleteResponse(SARequest);

            return Json(string.Empty);


        }


        private Epi.Web.Common.DTO.SurveyAnswerDTO GetCurrentSurveyAnswer()
        {
            Epi.Web.Common.DTO.SurveyAnswerDTO result = null;

            if (TempData.ContainsKey(Epi.Web.MVC.Constants.Constant.RESPONSE_ID)
                && TempData[Epi.Web.MVC.Constants.Constant.RESPONSE_ID] != null
                && !string.IsNullOrEmpty(TempData[Epi.Web.MVC.Constants.Constant.RESPONSE_ID].ToString())
                )
            {
                string responseId = TempData[Epi.Web.MVC.Constants.Constant.RESPONSE_ID].ToString();

                //TODO: Now repopulating the TempData (by reassigning to responseId) so it persisits, later we will need to find a better 
                //way to replace it. 
                TempData[Epi.Web.MVC.Constants.Constant.RESPONSE_ID] = responseId;
                return _isurveyFacade.GetSurveyAnswerResponse(responseId).SurveyResponseList[0];
            }

            return result;
        }
         


        public SurveyInfoModel GetSurveyInfo(string SurveyId)
        {
            SurveyInfoModel surveyInfoModel = _isurveyFacade.GetSurveyInfoModel(SurveyId);
            return surveyInfoModel;
        }

        public List<FormInfoModel> GetFormsInfoList(Guid UserId)
        {
            FormsInfoRequest formReq = new FormsInfoRequest();

            formReq.Criteria.UserId = SurveyHelper.GetDecryptUserId(Session["UserId"].ToString());//Hard coded user for now.
            // formReq.Criteria.UserId = UserId;
            //define filter criteria here.
            //define sorting criteria here.
            List<FormInfoModel> listOfFormsInfoModel = _isurveyFacade.GetFormsInfoModelList(formReq);



            return listOfFormsInfoModel;
        }

        private int Compare(KeyValuePair<int, string> a, KeyValuePair<int, string> b)
        {
            return a.Key.CompareTo(b.Key);
        }

        private ResponseModel ConvertXMLToModel(SurveyAnswerDTO item, List<KeyValuePair<int, string>> Columns)
        {
            ResponseModel ResponseModel = new Models.ResponseModel();


            var MetaDataColumns = Epi.Web.MVC.Constants.Constant.MetaDaTaColumnNames();
            
            try
            {
                ResponseModel.Column0 = item.ResponseId;
                ResponseModel.IsLocked = item.IsLocked;
                IEnumerable<XElement> nodes;
                var document = XDocument.Parse(item.XML);
                if (MetaDataColumns.Contains(Columns[0].Value.ToString()))
                    {

                    ResponseModel.Column1 = GetColumnValue(item, Columns[0].Value.ToString());
                    }
                else
                    {
                     nodes = document.Descendants().Where(e => e.Name.LocalName.StartsWith("ResponseDetail") && e.Attribute("QuestionName").Value == Columns[0].Value.ToString());
                    ResponseModel.Column1 = nodes.First().Value;
                    }
                if (Columns.Count >= 2)
                {
                if (MetaDataColumns.Contains(Columns[1].Value.ToString()))
                    {

                    ResponseModel.Column2 = GetColumnValue(item,Columns[1].Value.ToString());
                    }
                else 
                    {
                    nodes = document.Descendants().Where(e => e.Name.LocalName.StartsWith("ResponseDetail") && e.Attribute("QuestionName").Value == Columns[1].Value.ToString());
                    ResponseModel.Column2 = nodes.First().Value;
                    }
                }


                if (Columns.Count >= 3)
                {
                if (MetaDataColumns.Contains(Columns[2].Value.ToString()))
                    {

                    ResponseModel.Column3 = GetColumnValue(item, Columns[2].Value.ToString());
                    }
                else
                    {
                    nodes = document.Descendants().Where(e => e.Name.LocalName.StartsWith("ResponseDetail") && e.Attribute("QuestionName").Value == Columns[2].Value.ToString());
                    ResponseModel.Column3 = nodes.First().Value;
                    }
                }

                if (Columns.Count >= 4)
                {
                if (MetaDataColumns.Contains(Columns[3].Value.ToString()))
                    {

                    ResponseModel.Column4 = GetColumnValue(item, Columns[3].Value.ToString());
                    }
                else
                    {
                    nodes = document.Descendants().Where(e => e.Name.LocalName.StartsWith("ResponseDetail") && e.Attribute("QuestionName").Value == Columns[3].Value.ToString());
                    ResponseModel.Column4 = nodes.First().Value;
                    }
                }

                if (Columns.Count >= 5)
                {
                if (MetaDataColumns.Contains(Columns[4].Value.ToString()))
                    {

                    ResponseModel.Column5 = GetColumnValue(item, Columns[4].Value.ToString());
                    }
                else
                    {
                    nodes = document.Descendants().Where(e => e.Name.LocalName.StartsWith("ResponseDetail") && e.Attribute("QuestionName").Value == Columns[4].Value.ToString());
                    ResponseModel.Column5 = nodes.First().Value;
                    }
                }


                return ResponseModel;

            }
            catch (Exception Ex)
            {

                throw new Exception(Ex.Message);
            }
        }

        private string GetColumnValue(SurveyAnswerDTO item, string columnName)
            {
            string ColumnValue ="";
            switch (columnName)
                {
                case "_UserEmail":
                    ColumnValue = item.UserEmail;
                break;
                case "_DateUpdated":
                ColumnValue = item.DateUpdated.ToString();
                break;
                case "_DateCreated":
                ColumnValue = item.DateCreated.ToString();
                break;
                case "IsDraftMode":
                case "_Mode":
                if (item.IsDraftMode.ToString().ToUpper() == "TRUE")
                    {
                    ColumnValue = "Staging";
                    }
                else 
                    {
                    ColumnValue = "Production";
                    
                    }
                break;
                }
            return ColumnValue;
            }

        public FormResponseInfoModel GetFormResponseInfoModel(string SurveyId, int PageNumber)
        {
            int UserId = SurveyHelper.GetDecryptUserId(Session["UserId"].ToString());
            FormResponseInfoModel FormResponseInfoModel = new FormResponseInfoModel();
            if (!string.IsNullOrEmpty(SurveyId))
            {
                SurveyAnswerRequest FormResponseReq = new SurveyAnswerRequest();
                FormSettingRequest FormSettingReq = new Common.Message.FormSettingRequest();

                //Populating the request

                FormSettingReq.FormInfo.FormId = SurveyId;
                FormSettingReq.FormInfo.UserId = UserId;
                //Getting Column Name  List
                FormSettingResponse FormSettingResponse = _isurveyFacade.GetFormSettings(FormSettingReq);
                Columns = FormSettingResponse.FormSetting.ColumnNameList.ToList();
                Columns.Sort(Compare);
               
                // Setting  Column Name  List
                FormResponseInfoModel.Columns = Columns;

                //Getting Resposes
                FormResponseReq.Criteria.SurveyId = SurveyId.ToString();
                FormResponseReq.Criteria.PageNumber = PageNumber;
                FormResponseReq.Criteria.UserId = UserId;
                SurveyAnswerResponse FormResponseList = _isurveyFacade.GetFormResponseList(FormResponseReq);

                //Setting Resposes List
                List<ResponseModel> ResponseList = new List<ResponseModel>();
                foreach (var item in FormResponseList.SurveyResponseList)
                {
                    ResponseList.Add(ConvertXMLToModel(item, Columns));
                }

                FormResponseInfoModel.ResponsesList = ResponseList;
                //Setting Form Info 
                FormResponseInfoModel.FormInfoModel = Mapper.ToFormInfoModel(FormResponseList.FormInfo);
                //Setting Additional Data

                FormResponseInfoModel.NumberOfPages = FormResponseList.NumberOfPages;
                FormResponseInfoModel.PageSize = ReadPageSize();
                FormResponseInfoModel.NumberOfResponses = FormResponseList.NumberOfResponses;
                FormResponseInfoModel.CurrentPage = PageNumber;
            }
            return FormResponseInfoModel;
        }

        private int ReadPageSize()
        {
            return Convert.ToInt16(WebConfigurationManager.AppSettings["RESPONSE_PAGE_SIZE"].ToString());
        }

        //  [HttpPost]

        //    public ActionResult Edit(string ResId)
        //    {
        ////    Epi.Web.Common.DTO.SurveyAnswerDTO surveyAnswerDTO = GetSurveyAnswer(ResId);

        //    return RedirectToAction(Epi.Web.MVC.Constants.Constant.INDEX, Epi.Web.MVC.Constants.Constant.SURVEY_CONTROLLER, new { responseid = ResId, PageNumber = 1 });
        //    }
        private Epi.Web.Common.DTO.SurveyAnswerDTO GetSurveyAnswer(string responseId)
        {
            Epi.Web.Common.DTO.SurveyAnswerDTO result = null;

            //responseId = TempData[Epi.Web.MVC.Constants.Constant.RESPONSE_ID].ToString();
            result = _isurveyFacade.GetSurveyAnswerResponse(responseId).SurveyResponseList[0];

            return result;

        }

        [HttpGet]
        public ActionResult LogOut()
        {

            FormsAuthentication.SignOut();
            return RedirectToAction("Index", "Login");


        }
        [HttpGet]

        public ActionResult GetSettings(string formid)//List<FormInfoModel> ModelList, string formid)
        {
            FormSettingRequest FormSettingReq = new Common.Message.FormSettingRequest();
            List<KeyValuePair<int, string>> TempColumns = new List<KeyValuePair<int, string>>();
            
            FormSettingReq.GetXml = true;
            FormSettingReq.FormInfo.FormId = new Guid(formid).ToString();
            FormSettingReq.FormInfo.UserId = SurveyHelper.GetDecryptUserId(Session["UserId"].ToString());
            //Getting Column Name  List
            FormSettingResponse FormSettingResponse = _isurveyFacade.GetFormSettings(FormSettingReq);
            Columns = FormSettingResponse.FormSetting.ColumnNameList.ToList();
            TempColumns = Columns;
            Columns.Sort(Compare);
             

            Dictionary<int, string> dictionary = Columns.ToDictionary(pair => pair.Key, pair => pair.Value);
            SettingsInfoModel Model = new SettingsInfoModel();
            Model.SelectedControlNameList = dictionary;

            Columns = FormSettingResponse.FormSetting.FormControlNameList.ToList();
            // Get Additional Metadata columns 

            var MetaDataColumns =Epi.Web.MVC.Constants.Constant.MetaDaTaColumnNames();
            Dictionary<int, string> Columndictionary = TempColumns.ToDictionary(pair => pair.Key, pair => pair.Value);
           
            foreach (var item in MetaDataColumns)
                {

                if (!Columndictionary.ContainsValue(item))
                    {
                    Columns.Add(new KeyValuePair<int, string>(Columns.Count() + 1, item));
                    }

                }

            Columns.Sort(Compare);
             

            Dictionary<int, string> dictionary1 = Columns.ToDictionary(pair => pair.Key, pair => pair.Value);

            Model.FormControlNameList = dictionary1;




            Columns = FormSettingResponse.FormSetting.AssignedUserList.ToList();
            Columns.Sort(Compare);

            Dictionary<int, string> dictionary2 = Columns.ToDictionary(pair => pair.Key, pair => pair.Value);

            Model.AssignedUserList = dictionary2;





            Columns = FormSettingResponse.FormSetting.UserList.ToList();
            Columns.Sort(Compare);

            Dictionary<int, string> dictionary3 = Columns.ToDictionary(pair => pair.Key, pair => pair.Value);

            Model.UserList = dictionary3;




            Model.IsDraftMode = FormSettingResponse.FormInfo.IsDraftMode;
            Model.FormOwnerFirstName = FormSettingResponse.FormInfo.OwnerFName;
            Model.FormOwnerLastName = FormSettingResponse.FormInfo.OwnerLName;
            Model.FormName = FormSettingResponse.FormInfo.FormName;
            return PartialView("Settings", Model);

        }

        [HttpPost]
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult SaveSettings(string formid)
        {

            int UserId = SurveyHelper.GetDecryptUserId(Session["UserId"].ToString());
            FormSettingRequest FormSettingReq = new Common.Message.FormSettingRequest();
            FormSettingReq.GetXml = true;
            FormSettingReq.FormInfo.FormId = new Guid(formid).ToString();
            FormSettingReq.FormInfo.UserId = UserId;
            FormSettingReq.FormSetting.ColumnNameList = GetDictionary(this.Request.Form["SelectedColumns"]);

            FormSettingReq.FormSetting.AssignedUserList = GetDictionary(this.Request.Form["SelectedUser"]);
            FormSettingReq.FormInfo.IsDraftMode = GetFormMode(this.Request.Form["Mode"]);

            FormSettingResponse FormSettingResponse = _isurveyFacade.SaveSettings(FormSettingReq);



            bool IsMobileDevice = this.Request.Browser.IsMobileDevice;

            var model = new FormResponseInfoModel();


            model = GetFormResponseInfoModel(formid, 1);

            if (IsMobileDevice == false)
            {
                return PartialView("ListResponses", model);

            }
            else
            {
                return View("ListResponses", model);
            }




        }



        public Dictionary<int, string> GetDictionary(string List)
        {
            Dictionary<int, string> Dictionary = new Dictionary<int, string>();
            if (!string.IsNullOrEmpty(List))
            {
                Dictionary = List.Split(',').ToList().Select((s, i) => new { s, i }).ToDictionary(x => x.i, x => x.s);
            }
            return Dictionary;
        }
        public bool GetFormMode(string Mode)
        {
            bool IsDraftMode = false;
            if (!string.IsNullOrEmpty(Mode))
            {
                int FormMode = int.Parse(Mode);
                if (FormMode == 1)
                {
                    IsDraftMode = true;
                }
            }


            return IsDraftMode;
        }

    }
}
