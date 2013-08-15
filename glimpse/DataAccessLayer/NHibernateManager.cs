using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
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
            _sessionFactory = Fluently.Configure()
                .Database(MySQLConfiguration.Standard.ConnectionString(c => c.FromConnectionStringWithKey("ConnectionString")))
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