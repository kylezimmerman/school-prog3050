/* GameReview.cs
 * Purpose: Class for a review of a specific game product
 * 
 * Revision History:
 *      Drew Matheson, 2015.10.03: Created
 */

using System;

namespace Veil.DataModels.Models
{
    /// <summary>
    /// A review for a specific GameProduct by a Member
    /// </summary>
    public class GameReview : Review<GameProduct, Guid> { }
}