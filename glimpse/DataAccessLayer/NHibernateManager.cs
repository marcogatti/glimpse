using System.Configuration;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate;
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