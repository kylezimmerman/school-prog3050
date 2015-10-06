/* IVeilDataAccess.cs
 * Purpose: Interface for database services for the application
 * 
 * Revision History:
 *      Drew Matheson, 2015.09.29: Created
 */ 

using System.Data.Entity;
using Veil.DataModels.Models;

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