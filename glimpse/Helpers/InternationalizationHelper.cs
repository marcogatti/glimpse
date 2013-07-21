using System;
using System.Collections.Generic;
using System.Web;
using System.Xml;
using glimpse.Exceptions.CommonExceptions;

namespace glimpse.Helpers
{
    public class InternationalizationHelper
    {
        private String defaultLanguage;
        private XmlDocument languageSource;


        public static InternationalizationHelper buildForLanguage(String defaultLanguage = null)
        {
            String relFilePath = System.Configuration.ConfigurationManager.AppSettings["LocalizationFilePath"];
            return buildForLanguage(HttpContext.Current.Server.MapPath(relFilePath), defaultLanguage);
        }

        public static InternationalizationHelper buildForLanguage(String languageFilePath, String defaultLanguage = null)
        {
            return new InternationalizationHelper(languageFilePath, defaultLanguage);
        }

        public String getLanguageElement(String moduleId, String key)
        {
            if (this.defaultLanguage != null)
            {
                return this.getLanguageElement(moduleId, key, this.defaultLanguage);
            }
            else
            {
                throw new LanguageElementNotFoundException("Lang not presetted");
            }

        }

        public String getLanguageElement(String moduleId, String key, String lang)
        {

            String searchString = "//module[@id='" + moduleId + "']/texts[@id='" + key + "']/text[@lang='" + lang + "']";

            XmlNodeList nodeList = this.languageSource.SelectNodes(searchString);

            if (nodeList.Count > 0)
            {
                return nodeList.Item(0).InnerText;
            }
            else
            {
                throw new LanguageElementNotFoundException("Search string: " + searchString);
            }
        }

        private InternationalizationHelper(String languageFilePath, String language)
        {
            loadLanguageFile(languageFilePath);
            this.defaultLanguage = language;
        }

        private void loadLanguageFile(String filePath)
        {
            this.languageSource = new XmlDocument();
            this.languageSource.Load(filePath);
        }
    }
}