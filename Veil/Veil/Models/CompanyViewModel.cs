using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using Veil.DataModels.Validation;

namespace Veil.Models
{
    public class CompanyViewModel
    {
        [Required]
        [StringLength(512, ErrorMessageResourceName = nameof(ErrorMessages.StringLength), ErrorMessageResourceType = typeof(ErrorMessages))]
        [DisplayName("New Company Name")]
        public string NewCompany { get; set; }

        public SelectList Deletable { get; set; }
    }
}