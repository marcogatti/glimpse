using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FluentNHibernate.Mapping;
using Glimpse.DataAccessLayer.Entities;

namespace Glimpse.DataAccessLayer.Mappings
{
    public class MailMap : ClassMap<Mail>
    {
        public MailMap()
        {
            Table("Mail");

            Id(x => x.Id).Column("Id_Mail");
            Map(x => x.IdMailAccount).Column("ID_MailAccount");
            //Map(x => x.From).Column("ID_From");
            References<Address>(x => x.From, "ID_From");
            Map(x => x.gm_tid).Column("GM_ThreadID");
            Map(x => x.gm_mid).Column("GM_MailID");
            Map(x => x.Date).Column("Date");
            Map(x => x.To).Column("To");
            Map(x => x.CC).Column("CC");
            Map(x => x.BCC).Column("BCC");
            Map(x => x.Subject).Column("Subject");
            Map(x => x.Body).Column("Body");
            Map(x => x.UidInbox).Column("UID_Inbox");
            Map(x => x.UidTrash).Column("UID_Trash");
            Map(x => x.UidSent).Column("UID_Sent");
            Map(x => x.UidDraft).Column("UID_Draft");
            Map(x => x.UidSpam).Column("UID_Spam");
            Map(x => x.UidAll).Column("UID_All");
            Map(x => x.Answered).Column("FG_Answered");
            Map(x => x.Flagged).Column("FG_Flagged");
            Map(x => x.Seen).Column("FG_Seen");
            Map(x => x.Draft).Column("FG_Draft");
            Map(x => x.HasExtras).Column("FG_HasExtras"); 
        }
    }
}