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
using Glimpse.DataAccessLayer.Mappings;

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
                    InitializeSessionFactory();

                return _sessionFactory;
            }
        }

        private static void InitializeSessionFactory()
        {
            String connectionString = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
            _sessionFactory = Fluently.Configure()
                .Database(MySQLConfiguration.Standard.ConnectionString(connectionString))
                .Mappings(m => m.FluentMappings
                                .AddFromAssemblyOf<AddressMap>())
                .BuildSessionFactory();
        } 

        public static ISession OpenSession()
        {
            return SessionFactory.OpenSession();
        }

    }
}