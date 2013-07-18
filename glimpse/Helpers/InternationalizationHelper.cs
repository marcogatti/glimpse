using System;
using System.Collections.Generic;
using System.Web;
using System.Xml;
using glimpse.Exceptions.CommonExceptions;

namespace glimpse.Helpers
{
    public class InternationalizationHelper
    {
        private XmlDocument languageSource;

        public InternationalizationHelper()
        {
            String relFilePath = System.Configuration.ConfigurationManager.AppSettings["InternationalizationFilePath"];
            this.languageSource = new XmlDocument();
            this.languageSource.Load(HttpContext.Current.Server.MapPath(relFilePath));
        }

        public InternationalizationHelper(String languageFilePath)
        {
            this.languageSource = new XmlDocument();
            this.languageSource.Load(languageFilePath);
        }

        public String GetLanguageElement(String moduleId, String key, String lang)
        {
            XmlNodeList nodeList = this.languageSource.SelectNodes("//module[@id='" + moduleId +
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

        private string searchCriteriaToString(String moduleId, String key, String lang)
        {
            return "Module: " + moduleId + " Texts: " + key + " Text: " + lang;
        }
    }
}