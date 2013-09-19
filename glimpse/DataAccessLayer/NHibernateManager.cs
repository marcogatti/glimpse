using System;
using System.Data;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Xml.Linq;
using FluentNHibernate;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Tool.hbm2ddl;
using Glimpse.DataAccessLayer.Entities;

namespace Glimpse.DataAccessLayer
{
    public class NHibernateManager
    {
        private static ISessionFactory _sessionFactory;
        private static ISessionFactory SessionFactory
        {
            get
            {
                if (_sessionFactory == null)
                {
                    string strConnectionString = ConfigurationManager.AppSettings["ConnectionString"];

                    FluentConfiguration cfg = Fluently.Configure()
                        .Database(MySQLConfiguration.Standard.ConnectionString(strConnectionString))
                        .Mappings(m => m.FluentMappings.AddFromAssemblyOf<MailEntity>());

                    _sessionFactory = cfg.BuildSessionFactory();
                }
                return _sessionFactory;
            }
        }
        public static ISession OpenSession()
        {
            return SessionFactory.OpenSession();
        }
        public static void CloseSessionFactory()
        {
            if (SessionFactory != null)
            {
                SessionFactory.Close();
            }
        }

    }
}