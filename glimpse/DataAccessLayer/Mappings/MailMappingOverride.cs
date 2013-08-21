using FluentNHibernate.Automapping;
using FluentNHibernate.Automapping.Alterations;
using Glimpse.DataAccessLayer.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Glimpse.DataAccessLayer.Mappings
{
    public class MailMappingOverride : IAutoMappingOverride<Mail>
    {
        public void Override(AutoMapping<Mail> mapping)
        {
            mapping.Table("Mail");

            mapping.Id(x => x.Id).Column("Id_Mail");
            mapping.References<MailAccount>(x => x.MailAccount).Column("ID_MailAccount");
            //mapping.Map(x => x.From).Column("ID_From");
            mapping.References<Address>(x => x.From, "ID_From");
            mapping.Map(x => x.gm_tid).Column("GM_ThreadID");
            mapping.Map(x => x.gm_mid).Column("GM_MailID");
            mapping.Map(x => x.Date).Column("Date");
            mapping.Map(x => x.To).Column("To");
            mapping.Map(x => x.CC).Column("CC");
            mapping.Map(x => x.BCC).Column("BCC");
            mapping.Map(x => x.Subject).Column("Subject");
            mapping.Map(x => x.Body).Column("Body");
            mapping.Map(x => x.UidInbox).Column("UID_Inbox").CustomSqlType("INT");
            mapping.Map(x => x.UidTrash).Column("UID_Trash");
            mapping.Map(x => x.UidSent).Column("UID_Sent");
            mapping.Map(x => x.UidDraft).Column("UID_Draft");
            mapping.Map(x => x.UidSpam).Column("UID_Spam");
            mapping.Map(x => x.UidAll).Column("UID_All");
            mapping.Map(x => x.Answered).Column("FG_Answered");
            mapping.Map(x => x.Flagged).Column("FG_Flagged");
            mapping.Map(x => x.Seen).Column("FG_Seen");
            mapping.Map(x => x.Draft).Column("FG_Draft");
            mapping.Map(x => x.HasExtras).Column("FG_HasExtras");
        }
    }
}