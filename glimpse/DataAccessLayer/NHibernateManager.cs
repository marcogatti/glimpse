using System;
using System.Data;
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
using Glimpse.DataAccessLayer.Mappings;
using FluentNHibernate.Automapping;

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
                    _sessionFactory = CreateFluentConfiguration().BuildSessionFactory();

                return _sessionFactory;
            }
        }

        private static ISession _defaultSession;
        public static ISession DefaultSesion
        {
            get
            {
                if (_defaultSession == null)
                    _defaultSession = OpenSession();

                return _defaultSession;
            }
        }

        private readonly FluentConfiguration fluentConfiguration;

        private Configuration nhibernateConfiguration;
        public Configuration NhibernateConfiguration
        {
            get { return nhibernateConfiguration; }
        }

        public NHibernateManager()
        {
            fluentConfiguration = CreateFluentConfiguration().ExposeConfiguration(cfg => nhibernateConfiguration = cfg);
        }


        public ISessionFactory CreateSessionFactory()
        {
            return _sessionFactory ?? (_sessionFactory = fluentConfiguration.BuildSessionFactory());
        }

        private static FluentConfiguration CreateFluentConfiguration()
        {
            var entitiesToMapConfiguration = new GlimpseAutomappingConfiguration();
            String connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
            return Fluently.Configure()
                        .Database(MySQLConfiguration.Standard.ConnectionString(connectionString))
                        .Mappings(m => m.AutoMappings.Add(AutoMap.AssemblyOf<MailEntity>(entitiesToMapConfiguration)));
        }

        public static ISession OpenSession()
        {
            return SessionFactory.OpenSession();
        }

    }
}