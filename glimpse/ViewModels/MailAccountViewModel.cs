using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Glimpse.ViewModels
{
    public class MailAccountViewModel
    {
        [RegularExpression( @"^[A-Za-z0-9]([\w\.\-]*)@([A-Za-z0-9-]+)((\.(\w){2,3})+)$", 
                                ErrorMessage= "Dirección de correo inválida.")]
        public String Address { get; set; }

        [RegularExpression(@"^(?!.{20})[A-Za-z0-9\!#\$%&'\*\.\+\-/=\?\^`\{|\}~_]{4,20}",
                                ErrorMessage = "Contraseña inválida.")]
        public String Password { get; set; }

        public Boolean IsMainAccount { get; set; }
    }
}