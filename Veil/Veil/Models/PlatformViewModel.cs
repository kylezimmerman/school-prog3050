using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veil.DataModels.Models;

namespace Veil.Models
{
    public class PlatformViewModel
    {
        public IEnumerable<Platform> Selected { get; set; }
        public IEnumerable<Platform> AllPlatforms { get; set; }
    }
}
