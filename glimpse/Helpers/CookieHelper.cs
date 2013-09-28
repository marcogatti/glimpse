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

        public HttpCookie AddUsernameCookie(String username)
        {
            return this.AddUsernameCookie(username, DateTime.Now.AddDays(365));
        }
        public HttpCookie AddUsernameCookie(String username, DateTime expirationDate)
        {
            HttpCookie myCookie = this.responseCookies[LOGIN_COOKIE] ?? new HttpCookie(LOGIN_COOKIE);
            myCookie.Values["User"] = username;
            myCookie.Expires = expirationDate;
            myCookie.HttpOnly = true;
            this.responseCookies.Add(myCookie);

            return myCookie;
        }
        public String GetUserFromCookie()
        {
            HttpCookie myCookie = this.requestCookies[LOGIN_COOKIE];

            if (myCookie != null)
            {
                return myCookie.Values["User"];

            }
            else
            {
                throw new CookieNotFoundException("Login cookie");
            }
        }
        public void clearMailAddressCookie()
        {
            try
            {
                String address = this.GetUserFromCookie();

                this.responseCookies.Remove(address);
                this.AddUsernameCookie(address, DateTime.Now.AddMonths(-1));
            }
            catch (CookieNotFoundException){ /* Cookie already cleared or does not exist*/ }
        }
    }
}