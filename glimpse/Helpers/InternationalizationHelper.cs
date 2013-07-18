using System;
using System.Collections.Generic;
using System.Web;
using System.Xml;
using glimpse.Exceptions.CommonExceptions;

namespace glimpse.Helpers
{
    public class InternationalizationHelper
    {

        private XmlDocument localization;



        public InternationalizationHelper()
        {
            String relFilePath = System.Configuration.ConfigurationManager.AppSettings["LocalizationFilePath"];
            this.localization = new XmlDocument();
            this.localization.Load(HttpContext.Current.Server.MapPath(relFilePath));
        }

        public InternationalizationHelper(String languageFilePath)
        {
            this.localization = new XmlDocument();
            this.localization.Load(languageFilePath);
        }


        public String getLanguageElement(String moduleId, String key, String lang)
        {

            XmlNodeList nodeList = this.localization.SelectNodes("//module[@id='" + moduleId +
                                                                 "']/texts[@id='" + key +
                                                                 "']/text[@lang='" + lang + "']");

            if (nodeList.Count > 0)
            {
                return nodeList.Item(0).InnerText;
            }
            else
            {
                throw new LanguageElementNotFoundException(searchCriteriaToString(moduleId, key, lang));
            }

        }


        private static string searchCriteriaToString(String moduleId, String key, String lang)
        {
            return "Module: " + moduleId + " Texts: " + key + " Text: " + lang;
        }
    }
}