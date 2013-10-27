using Glimpse.DataAccessLayer;
using Glimpse.DataAccessLayer.Entities;
using Glimpse.Helpers;
using NHibernate;
using NHibernate.Criterion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace Glimpse.Models
{
    public class Extra
    {
        public ExtraEntity Entity;

        public Extra(ExtraEntity entity)
        {
            this.Entity = entity;
        }

        public Boolean BelongsToUser(User user)
        {
            return user.GetAccounts().Any(x => x.Entity.Id == this.Entity.MailEntity.MailAccountEntity.Id);
        }

        public static String SaveToFS(HttpPostedFileBase file)
        {
            String filePath = HttpContext.Current.Server.MapPath("~") + "temp\\" + StringHelper.GenerateRandomString() + file.FileName;
            file.SaveAs(filePath);
            return filePath;
        }
        public static Boolean IsValidFile(HttpPostedFileBase file)
        {
            Int32 maxByteLength = 5 * 1024 * 1024;
            return (file != null && file.ContentLength <= maxByteLength && !String.IsNullOrEmpty(file.FileName) &&
                    !file.ContentType.Contains("exe"));
        }
        public static Extra FindByID(Int64 id, ISession session)
        {
            ExtraEntity extraEntity = session.CreateCriteria<ExtraEntity>()
                                         .Add(Restrictions.Eq("Id", id))
                                         .UniqueResult<ExtraEntity>();
            if (extraEntity == null)
                return null;
            else
                return new Extra(extraEntity);
        }
        public static IList<ExtraEntity> FindByMailId(Int64 mailID, ISession session)
        {
            IList<ExtraEntity> mailExtras = session.CreateCriteria<ExtraEntity>()
                                            .Add(Restrictions.Eq("MailEntity.Id", mailID))
                                            .Add(Restrictions.Eq("ExtraType", Convert.ToInt16(0)))
                                            .List<ExtraEntity>();
            return mailExtras;
        }
    }

    public class ExtraFile
    {
        public String Name { get; set; }
        public Int64 Size { get; set; }
        public String Type { get; set; }
        public String Path { get; set; }
        public String Id { get; set; }
        public byte[] Content { get; set; }
    }
}