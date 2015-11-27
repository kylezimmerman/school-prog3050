using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Veil.DataModels.Models;

namespace Veil.Models
{
    public class ESRBDescriptionViewModel
    {
        public IEnumerable<ESRBContentDescriptor> All { get; set; }
        public IEnumerable<ESRBContentDescriptor> Selected { get; set; }
    }
}