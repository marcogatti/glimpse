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
                                ErrorMessage= "La dirección de correo posee caracteres inválidos.")]
        public String Address { get; set; }

        [RegularExpression(@"^(?!.{20})[A-Za-z0-9\!#\$%&'\*\.\+\-/=\?\^`\{|\}~_]{6,20}",
                                ErrorMessage = "Caracteres de contraseña inválidos.")]
        public String Password { get; set; }

        [RegularExpression(@"^(?!.{20})[A-Za-z0-9\!#\$%&'\*\.\+\-/=\?\^`\{|\}~_]{6,20}",
                                ErrorMessage = "Caracteres de contraseña inválidos.")]
        public String ConfirmationPassword { get; set; }

        public Boolean IsMainAccount { get; set; }
    }
}