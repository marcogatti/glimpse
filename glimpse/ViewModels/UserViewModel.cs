using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using Glimpse.MailInterfaces;
using System.Web.Mvc;
using System.Diagnostics;
using Glimpse.Helpers;

namespace Glimpse.ViewModels
{
    public class UserViewModel
    {
        [RegularExpression(@"^[A-Za-z]{1}[A-Za-z0-9]{3,15}$" + //Usuario Glimpse: debe comenzar con una letra 
                                 //seguido de cualquier combinacion de letras (total entre 4 y 16 caracteres)
                           "|" + //o ser Email:
                           @"^[A-Za-z0-9]" + //email debe empezar con una letra o un numero
                           @"([\w\.\-]*)" + //seguido de una combinacion de letras, numeros,  puntos o guiones (altos o bajos) 0 o mas veces
                           @"@([A-Za-z0-9-]+)" + //seguido de arroba y una combinacion de letras, numeros o guiones medios 1 o mas veces
                           @"((\.(\w){2,3})+)$", 
                           ErrorMessage= "Nombre de usuario debe ser una dirección de correo o un nombre de usuario Glimpse válido.")]
        [DataType(DataType.Text)]
        [Display(Name = "Nombre de Usuario/Email")]
        public String Username { get; set; }

        //caracteres validos para un passoword de email segun RFC, entre 6 y 20 caracteres
        [RegularExpression(@"^(?!.{20})[A-Za-z0-9\!#\$%&'\*\.\+\-/=\?\^`\{|\}~_]{6,20}",
                           ErrorMessage= "Contraseña inválida. Debe tener entre 6 y 20 caracteres. No debe tener caracteres inválidos.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public String Password { get; set; }

        [RegularExpression(@"^([A-Za-z]){2,16}$", ErrorMessage = "Caracteres de nombre inválidos.")]
        public String Firstname { get; set; }

        [RegularExpression(@"^([A-Za-z]){2,16}$", ErrorMessage = "Caracteres de apellido inválidos.")]
        public String Lastname { get; set; }

        [RegularExpression(@"^(?!.{20})[A-Za-z0-9\!#\$%&'\*\.\+\-/=\?\^`\{|\}~_]{6,20}",
                           ErrorMessage = "Confirmación de contraseña inválida.")]
        [DataType(DataType.Password)]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public String ConfirmationPassword { get; set; }

        public List<MailAccountViewModel> ListMailAccounts { get; set; }

        public void FilterNullAccounts()
        {
            this.ListMailAccounts = this.ListMailAccounts.Where(x => x.Address != null && x.Password != null).ToList();
        }
    }
}