using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using Veil.DataModels.Models;

namespace Veil.Models
{
    public class CompanyViewModel
    {
        [Required]
        [StringLength(maximumLength: 512, MinimumLength = 1)]
        [DisplayName("New Company Name")]
        public string NewCompany { get; set; }
        public SelectList Deletable { get; set; }
    }
}