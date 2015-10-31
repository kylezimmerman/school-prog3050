using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veil.DataModels.Models;

namespace Veil.Models
{
    public class SearchViewModel
    {
        public ICollection<Platform> Platforms { get; set; }
        public ICollection<Tag> Tags { get; set; }
    }
}