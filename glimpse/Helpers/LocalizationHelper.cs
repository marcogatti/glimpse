using System;
using System.Collections.Generic;
using System.Web;
using System.Xml;
using glimpse.Exceptions.CommonExceptions;

namespace glimpse.Helpers
{
    public class LocalizationHelper
    {

        private XmlTextReader localization;



        public LocalizationHelper()
        {
            String relFilePath = System.Configuration.ConfigurationManager.AppSettings["LocalizationFilePath"];
            this.localization = new XmlTextReader(HttpContext.Current.Server.MapPath(relFilePath));
        }

        public String getString(LocSearchCriteria criteria)
        {
            while (this.moreTagsComming("module"))
            {
                if (!this.currentAttributeHasValue(criteria.ModuleId)) continue;
                while (this.moreTagsComming("texts"))
                {
                    if (!this.currentAttributeHasValue(criteria.Key)) continue;
                    while (this.moreTagsComming("text"))
                    {
                        if (!this.currentAttributeHasValue(criteria.Lang)) continue;
                        return this.getNextElement();
                    }

                    throw new ItemNotFoundException(searchToString(criteria));
                }

            }

            throw new ItemNotFoundException(searchToString(criteria));

        }


        private static string searchToString(LocSearchCriteria criteria)
        {
            return "Module: " + criteria.ModuleId + " Texts: " + criteria.Key + " Text: " + criteria.Lang;
        }

        private string getNextElement()
        {
            this.localization.MoveToElement();
            return this.localization.ReadElementContentAsString();
        }

        private bool moreTagsComming(String tag)
        {
            return this.localization.ReadToFollowing(tag);
        }

        private bool currentAttributeHasValue(String value)
        {
            this.localization.MoveToFirstAttribute();
            return localization.ReadContentAsString().Equals(value);
        }
    }



    public class LocSearchCriteria
    {
        public String ModuleId { set; get; }
        public String Key { set; get; }
        public String Lang { set; get; }

        public LocSearchCriteria(String moduleId, String key, String lang)
        {
            this.ModuleId = moduleId;
            this.Key = key;
            this.Lang = lang;
        }

    }
}