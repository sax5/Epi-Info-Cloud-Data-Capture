﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Epi.Web.Interfaces.DataInterface;
using Epi.Web.Common.BusinessObject;
using Epi.Web.Common.Security;
using System.Configuration;
using Epi.Web.Common.Constants;
using Epi.Web.Common.Email;
using Epi.Web.Common.Constants;
namespace Epi.Web.BLL
{
    public class User
    {
        public IUserDao UserDao;

        public User(IUserDao pUserDao)
        {
            UserDao = pUserDao;
        }

        public UserBO GetUser(UserBO User)
        {
            UserBO UserResponseBO;
            string KeyForUserPasswordSalt = ReadSalt();
            PasswordHasher PasswordHasher = new Web.Common.Security.PasswordHasher(KeyForUserPasswordSalt);
            string salt = PasswordHasher.CreateSalt(User.UserName);

            User.PasswordHash = PasswordHasher.HashPassword(salt, User.PasswordHash);

            UserResponseBO = UserDao.GetUser(User);

            return UserResponseBO;
        }

        private string ReadSalt()
        {
            return ConfigurationManager.AppSettings["KeyForUserPasswordSalt"];
        }

        public UserBO GetUserByUserId(UserBO User)
        {
            UserBO UserResponseBO;

            UserResponseBO = UserDao.GetUserByUserId(User);

            return UserResponseBO;
        }

        public bool UpdateUser(UserBO User)
        {
            switch (User.Operation)
            {
                case Constant.OperationMode.UpdatePassword:
                    string password = string.Empty;

                    if (User.ResetPassword)
                    {
                        password = User.PasswordHash;
                        User.ResetPassword = false;
                    }
                    else
                    {
                        PasswordGenerator passGen = new PasswordGenerator();
                        password = passGen.Generate();
                        User.ResetPassword = true;
                    }


                    string KeyForUserPasswordSalt = ReadSalt();
                    PasswordHasher PasswordHasher = new Web.Common.Security.PasswordHasher(KeyForUserPasswordSalt);
                    string salt = PasswordHasher.CreateSalt(User.UserName);

                    User.PasswordHash = PasswordHasher.HashPassword(salt, password);
                    bool success = UserDao.UpdateUserPassword(User);

                    if (success)
                    {
                        List<string> EmailList = new List<string>();
                        EmailList.Add(User.UserName);
                        Email email = new Email()
                        {
                            To = EmailList,
                            Password = password
                        };

                        if (User.ResetPassword)
                        {
                            success = SendEmail(email, Constant.EmailCombinationEnum.ResetPassword);
                        }
                        else
                        {
                            success = SendEmail(email, Constant.EmailCombinationEnum.PasswordChanged);
                        }

                    }
                    return success;

                case Constant.OperationMode.UpdateUserInfo:
                    break;
                default:
                    break;
            }
            return false;
        }

        private bool SendEmail(Email email, Constant.EmailCombinationEnum Combination)
        {

            Epi.Web.Common.Email.Email Email = new Web.Common.Email.Email();

            switch (Combination)
            {
                case Constant.EmailCombinationEnum.ResetPassword:
                    Email.Subject = "Your Visualization Dashboard Password";
                    Email.Body = string.Format("You recently accessed our Forgot Password service for the Epi Info Web Enter. \n \n Your new temporary password is: {0} \n \n If you have not accessed password help, please contact the administrator. \n \n Please click the link below to launch the Visualization Dashboard and log in with your temporary password. You will then be asked to create a new password.", email.Password);
                    break;
                case Constant.EmailCombinationEnum.PasswordChanged:
                    Email.Subject = "Your Web Enter Password has been updated";
                    Email.Body = " You recently updated your password for the Epi Info Web Enter. \n \n If you have not accessed password help, please contact the administrator for you organization. \n \n Please click the link below to launch the Visualization Dashboard.";
                    break;
                default:
                    break;
            }

            Email.Body = Email.Body.ToString() + " \n \n" + ConfigurationManager.AppSettings["BaseURL"];
            Email.From = ConfigurationManager.AppSettings["EMAIL_FROM"];

            return Epi.Web.Common.Email.EmailHandler.SendMessage(Email);

        }
    }
}
