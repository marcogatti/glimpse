using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using glimpse.ViewModels;

namespace glimpse.Helpers
{
    public class CookieHelper
    {

        private HttpCookieCollection responseCookies;
        private HttpCookieCollection requestCookies;


        public CookieHelper()
        {
            responseCookies = HttpContext.Current.Response.Cookies;
            requestCookies = HttpContext.Current.Request.Cookies;
        }

        public CookieHelper(HttpCookieCollection requestCookies, HttpCookieCollection responseCookies)
        {
            this.requestCookies = requestCookies;
            this.responseCookies = responseCookies;
        }


        public void addMailAddressCookie(UserViewModel user)
        {
            this.addMailAddressCookie(user, DateTime.Now.AddDays(365));
        }

        public void addMailAddressCookie(UserViewModel user, DateTime expirationDate)
        {
            HttpCookie myCookie = this.responseCookies["Login"] ?? new HttpCookie("Login");
            myCookie.Values["Email"] = user.Email;
            myCookie.Values["Password"] = CryptoHelper.EncryptDefaultKey(user.Password);
            myCookie.Expires = expirationDate;
            this.responseCookies.Add(myCookie);
        }

        public UserViewModel getLoginCookie()
        {
            HttpCookie myCookie = this.requestCookies["Login"];
            UserViewModel user = new UserViewModel(myCookie.Values["Email"], CryptoHelper.DecryptDefaultKey(myCookie.Values["Password"]));

            return user;
        }

        public void clearLoginCookie(String cookieName)
        {
            HttpCookie myCookie = this.requestCookies["Login"];
            UserViewModel user = new UserViewModel(myCookie.Values["Email"], CryptoHelper.DecryptDefaultKey(myCookie.Values["Password"]));

            this.responseCookies.Remove(cookieName);
            this.addMailAddressCookie(user, DateTime.Now.AddMonths(-1));

        }
    }
}