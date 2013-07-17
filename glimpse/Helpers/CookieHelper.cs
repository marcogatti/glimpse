using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using glimpse.ViewModels;

namespace glimpse.Helpers
{
    public class CookieHelper
    {

        public static void addMailAddressCookie(UserViewModel user)
        {
            HttpCookie myCookie = HttpContext.Current.Request.Cookies["Login"] ?? new HttpCookie("Login");
            myCookie.Values["Email"] = user.Email;
            myCookie.Values["Password"] = CryptoHelper.EncryptDefaultKey(user.Password);
            myCookie.Expires = DateTime.Now.AddDays(1);
            HttpContext.Current.Response.Cookies.Add(myCookie);
        }

        public static UserViewModel getMailAddressCookie()
        {
            HttpCookie myCookie = HttpContext.Current.Request.Cookies["Login"];
            UserViewModel user = new UserViewModel();

            user.Email = myCookie.Values["Email"];
            user.Password = myCookie.Values["Password"];

            return user;
        }
    }
}