﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Epi.Web.Enter.Common.BusinessObject;
using Epi.Web.Enter.Common.Criteria;

using Epi.Web.Enter.Interfaces.DataInterface;
using System.Configuration;
namespace Epi.Web.BLL
{
    public class FormInfo
    {
        private IFormInfoDao FormInfoDao;



        public FormInfo(IFormInfoDao pSurveyInfoDao)
        {
            this.FormInfoDao = pSurveyInfoDao;
        }

        public List<FormInfoBO> GetFormsInfo(int UserId, int CurrentOrgId)
        {
            //Owner Forms
            List<FormInfoBO> result = this.FormInfoDao.GetFormInfo(UserId, CurrentOrgId);



            return result;
        }

        public FormInfoBO GetFormInfoByFormId(string FormId, bool GetXml, int UserId)
        {
            //Owner Forms
            FormInfoBO result = new FormInfoBO();
            if (UserId>0)
            {
               result = this.FormInfoDao.GetFormByFormId(FormId, GetXml, UserId);
             }
            if (ConfigurationManager.AppSettings["IsEWAVLiteIntegrationEnabled"].ToUpper() == "TRUE" && result.IsSQLProject)
            {
                bool toggleSwitchValue = this.FormInfoDao.GetEwavLiteToggleSwitch(FormId, UserId);

                result.EwavLiteToggleSwitch = toggleSwitchValue;
            }
            result.HasDraftModeData = this.FormInfoDao.HasDraftRecords(FormId);

            return result;
        }
    }
}
