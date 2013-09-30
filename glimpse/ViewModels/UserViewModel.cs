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
    [Bind(Include = "Username, Password, rememberMe")]
    public class UserViewModel
    {
        [Required]
        [RegularExpression(@"^[A-Za-z]{1}[A-Za-z0-9]{3,15}$" + //Usuario Glimpse: debe comenzar con una letra 
                                 //seguido de cualquier combinacion de letras (total entre 4 y 16 caracteres)
                           "|" + //o ser Email:
                           @"^[A-Za-z0-9]" + //email debe empezar con una letra o un numero
                           @"([\w\.\-]*)" + //seguido de una combinacion de letras, numeros,  puntos o guiones (altos o bajos) 0 o mas veces
                           @"@([A-Za-z0-9-]+)" + //seguido de arroba y una combinacion de letras, numeros o guiones medios 1 o mas veces
                           @"((\.(\w){2,3})+)$", 
                           ErrorMessage= "Username must a valid email or Glimpse Account")]
        [DataType(DataType.Text)]
        [Display(Name = "Glimpse User or Email")]
        public String Username { get; set; }

        [Required]
        //caracteres validos para un passoword de email segun RFC, entre 6 y 20 caracteres
        [RegularExpression(@"^(?!.{20})[A-Za-z0-9\!#\$%&'\*\.\+\-/=\?\^`\{|\}~]{6,20}",
                           ErrorMessage= "Invalid password characters")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public String Password { get; set; }

        [Display(Name = "Remember me?")]
        public bool rememberMe { get; set; }

        public UserViewModel() { }

        public UserViewModel(String email, String password)
        {
            this.Username = email;
            this.Password = password;
        }

    }
}