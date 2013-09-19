using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FluentNHibernate.Mapping;
using Glimpse.DataAccessLayer.Entities;

namespace Glimpse.DataAccessLayer.Mappings
{
    public class MailMap : ClassMap<MailEntity>
    {
        public MailMap()
        {
            Id(x => x.Id).Column("Id").GeneratedBy.Identity();            
            Map(x => x.CC).Column("CC");
            Map(x => x.BCC).Column("BCC");
            Map(x => x.Body).Column("Body").LazyLoad();
            Map(x => x.ToAddress).Column("ToAddress");
            Map(x => x.Gm_tid).Column("Gm_tid");
            Map(x => x.Gm_mid).Column("Gm_mid");
            Map(x => x.Date).Column("Date");
            Map(x => x.Subject).Column("Subject");
            Map(x => x.UidInbox).Column("UidInbox");
            Map(x => x.UidTrash).Column("UidTrash");
            Map(x => x.UidSent).Column("UidSent");
            Map(x => x.UidDraft).Column("UidDraft");
            Map(x => x.UidSpam).Column("UidSpam");
            Map(x => x.UidAll).Column("UidAll");
            Map(x => x.Answered).Column("Answered");
            Map(x => x.Flagged).Column("Flagged");
            Map(x => x.Seen).Column("Seen");
            Map(x => x.Draft).Column("Draft");
            Map(x => x.HasExtras).Column("HasExtras");
            Map(x => x.BodyPeek).Column("BodyPeek");
            References<AddressEntity>(x => x.From).Column("FromId")
                                                    .Cascade.None();
            References<MailAccountEntity>(x => x.MailAccountEntity).Column("MailAccountId")
                                                    .Cascade.None();
            HasMany<ExtraEntity>(x => x.Extras).KeyColumn("Id")
                                                    .Inverse()
                                                    .Cascade.All();
            HasManyToMany<LabelEntity>(x => x.Labels).ParentKeyColumn("MailId")
                                                    .ChildKeyColumn("LabelId")
                                                    .Cascade.All()
                                                    .Not.LazyLoad()
                                                    .Table("LABELSPERMAIL");
            Table("MAIL");
        }
    }
}