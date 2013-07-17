using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using glimpse.ViewModels;

namespace glimpse.Helpers
{
    public class CookieHelper
    {

        private HttpCookieCollection ResponseCookies;
        private HttpCookieCollection RequestCookies;


        public CookieHelper()
        {
            ResponseCookies = HttpContext.Current.Response.Cookies;
            RequestCookies = HttpContext.Current.Request.Cookies;
        }

        public CookieHelper(HttpCookieCollection requestCookies, HttpCookieCollection responseCookies)
        {
            this.RequestCookies = requestCookies;
            this.ResponseCookies = responseCookies;
        }


        public void addMailAddressCookie(UserViewModel user)
        {
            this.addMailAddressCookie(user, DateTime.Now.AddDays(365));
        }

        public void addMailAddressCookie(UserViewModel user, DateTime expirationDate)
        {
            HttpCookie myCookie = this.ResponseCookies["Login"] ?? new HttpCookie("Login");
            myCookie.Values["Email"] = user.Email;
            myCookie.Values["Password"] = CryptoHelper.EncryptDefaultKey(user.Password);
            myCookie.Expires = expirationDate;
            this.ResponseCookies.Add(myCookie);
        }

        public UserViewModel getLoginCookie()
        {
            HttpCookie myCookie = this.RequestCookies["Login"];
            UserViewModel user = new UserViewModel(myCookie.Values["Email"], CryptoHelper.DecryptDefaultKey(myCookie.Values["Password"]));

            return user;
        }

        public void clearLoginCookie(String cookieName)
        {
            HttpCookie myCookie = this.RequestCookies["Login"];
            UserViewModel user = new UserViewModel(myCookie.Values["Email"], CryptoHelper.DecryptDefaultKey(myCookie.Values["Password"]));

            this.ResponseCookies.Remove(cookieName);
            this.addMailAddressCookie(user, DateTime.Now.AddMonths(-1));

        }
    }
}