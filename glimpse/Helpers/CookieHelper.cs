using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using glimpse.ViewModels;

namespace glimpse.Helpers
{
    public class CookieHelper
    {

        private HttpCookieCollection _responseCookies = HttpContext.Current.Response.Cookies;
        private HttpCookieCollection _requestCookies = HttpContext.Current.Request.Cookies;


        public CookieHelper() { }

        public CookieHelper(HttpCookieCollection requestCookies, HttpCookieCollection responseCookies)
        {
            this._requestCookies = requestCookies;
            this._responseCookies = responseCookies;
        }


        public void addMailAddressCookie(UserViewModel user)
        {
            this.addMailAddressCookie(user, DateTime.Now.AddDays(365));
        }

        public void addMailAddressCookie(UserViewModel user, DateTime expirationDate)
        {
            HttpCookie myCookie = this._responseCookies["Login"] ?? new HttpCookie("Login");
            myCookie.Values["Email"] = user.Email;
            myCookie.Values["Password"] = CryptoHelper.EncryptDefaultKey(user.Password);
            myCookie.Expires = expirationDate;
            this._responseCookies.Add(myCookie);
        }

        public UserViewModel getLoginCookie()
        {
            HttpCookie myCookie = this._requestCookies["Login"];
            UserViewModel user = new UserViewModel();

            user.Email = myCookie.Values["Email"];
            user.Password = myCookie.Values["Password"];

            return user;
        }

        public void clearLoginCookie(String cookieName)
        {
            HttpCookie myCookie = this._requestCookies["Login"];
            UserViewModel user = new UserViewModel(myCookie.Values["Email"], CryptoHelper.DecryptDefaultKey(myCookie.Values["Password"]));

            this._responseCookies.Remove(cookieName);
            this.addMailAddressCookie(user, DateTime.Now.AddMonths(-1));

        }
    }
}