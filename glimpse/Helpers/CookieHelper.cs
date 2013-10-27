using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Glimpse.ViewModels;
using Glimpse.Exceptions.ControllersExceptions;

namespace Glimpse.Helpers
{
    public class CookieHelper
    {

        public const String LOGIN_COOKIE = "login-data";

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

        public HttpCookie AddUsernameCookie(String username)
        {
            return this.AddUsernameCookie(username, DateTime.Now.AddDays(365));
        }
        public HttpCookie AddUsernameCookie(String username, DateTime expirationDate)
        {
            HttpCookie myCookie = this.ResponseCookies[LOGIN_COOKIE] ?? new HttpCookie(LOGIN_COOKIE);
            myCookie.Values["User"] = username;
            myCookie.Expires = expirationDate;
            myCookie.HttpOnly = true;
            this.ResponseCookies.Add(myCookie);
            return myCookie;
        }
        public String GetUserFromCookie()
        {
            HttpCookie myCookie = this.RequestCookies[LOGIN_COOKIE];

            if (myCookie != null)
                return myCookie.Values["User"];
            else
                throw new CookieNotFoundException("Login cookie");
        }
        public void ClearUserCookie()
        {
            try
            {
                String user = this.GetUserFromCookie();
                this.ResponseCookies.Remove(user);
                this.AddUsernameCookie(user, DateTime.Now.AddMonths(-1));
            }
            catch (CookieNotFoundException){ /* Cookie already cleared or does not exist*/ }
        }
    }
}