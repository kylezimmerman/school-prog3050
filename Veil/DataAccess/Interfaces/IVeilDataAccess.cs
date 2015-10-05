using System.Data.Entity;
using Veil.Models;

namespace Veil.DataAccess.Interfaces
{
    public interface IVeilDataAccess
    {
        IDbSet<Member> Members { get; }
        IDbSet<Employee> Employees { get; }
        IDbSet<Game> Games { get; }
        IDbSet<WebOrder> WebOrders { get; }
    }
}
