using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veil.Models;

namespace Veil.DataAccess.Interfaces
{
    public interface IVeilDataAccess
    {
        DbSet<Member> Members { get; }
    }
}
