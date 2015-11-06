using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veil.DataModels.Models;

namespace Veil.Models
{
    public class TagViewModel
    {
        public IEnumerable<Tag> Selected { get; set; }
        public IEnumerable<Tag> AllTags { get; set; }
    }
}
