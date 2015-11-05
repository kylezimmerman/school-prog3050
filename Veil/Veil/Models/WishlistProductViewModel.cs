using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veil.DataModels.Models;

namespace Veil.Models
{
    public class WishlistPhysicalGameProductViewModel
    {
        public PhysicalGameProduct Product { get; set; }
        public bool MemberIsCurrentUser { get; set; }
    }
}
