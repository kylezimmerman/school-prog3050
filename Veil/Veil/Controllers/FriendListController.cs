using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using LinqKit;
using Microsoft.AspNet.Identity;
using Veil.DataAccess.Interfaces;
using Veil.DataModels;
using Veil.DataModels.Models;
using Veil.Helpers;
using Veil.Models;

namespace Veil.Controllers
{
    [Authorize(Roles = VeilRoles.MEMBER_ROLE)]
    public class FriendListController : BaseController
    {
        protected readonly IVeilDataAccess db;
        private readonly IGuidUserIdGetter idGetter;

        public FriendListController(IVeilDataAccess veilDataAccess, IGuidUserIdGetter idGetter)
        {
            db = veilDataAccess;
            this.idGetter = idGetter;
        }

        // GET: FriendList
        /// <summary>
        /// Returns the list of friends and requests sent/received for the current user
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult> Index()
        {
            FriendsListViewModel friendsListViewModel = new FriendsListViewModel
            {
                ConfirmedFriends = new List<Member>(),
                PendingReceivedFriendships = new List<Member>(),
                PendingSentFriendships = new List<Member>()
            };

            Guid currentMemberGuid = idGetter.GetUserId(User.Identity);

            Member member = await db.Members
                .Include(m => m.RequestedFriendships)
                .Include(m => m.ReceivedFriendships)
                .Where(m => m.UserId == currentMemberGuid)
                .FirstOrDefaultAsync();

            var friendships = member.RequestedFriendships
                .Concat(member.ReceivedFriendships);

            friendsListViewModel.ConfirmedFriends = member.ConfirmedFriends.ToList();

            var pendingReceivedFriendships = friendships
                .Where(f => f.RequestStatus == FriendshipRequestStatus.Pending &&
                    f.ReceiverId == currentMemberGuid);
            foreach (var friendship in pendingReceivedFriendships)
            {
                friendsListViewModel.PendingReceivedFriendships
                    .Add(friendship.Requester);
            }

            var pendingSentFriendships = friendships
                .Where(f => f.RequestStatus == FriendshipRequestStatus.Pending &&
                    f.RequesterId == currentMemberGuid);
            foreach (var friendship in pendingSentFriendships)
            {
                friendsListViewModel.PendingSentFriendships
                    .Add(friendship.Receiver);
            }

            return View(friendsListViewModel);
        }

        // POST: FriendList
        /// <summary>
        /// Processes a friends request made by the user.
        /// </summary>
        /// <param name="username">The username entered by the user.</param>
        /// <returns>Redirect to 'Index' action.</returns>
        [HttpPost]
        public async Task<ActionResult> AddFriendRequest(string username)
        {
            Guid currentMemberGuid = idGetter.GetUserId(User.Identity);

            Member currentUser = await db.Members
                .Where(m => m.UserId == currentMemberGuid)
                .FirstOrDefaultAsync();

            Member targetUser = await db.Members
                .Where(m => m.UserAccount.UserName == username)
                .FirstOrDefaultAsync();

            if (currentUser == targetUser)
            {
                this.AddAlert(AlertType.Error, "Cannot add yourself as a friend.");

                return RedirectToAction("Index");
            }

            if (targetUser == null)
            {
                this.AddAlert(AlertType.Error, $"No member with the username '{username}' exists.");

                return RedirectToAction("Index");
            }

            Friendship existingFriendship = await db.Friendships
                .Where(f => (f.Requester.UserId == currentUser.UserId && f.Receiver.UserId == targetUser.UserId) ||
                            (f.Requester.UserId == targetUser.UserId && f.Receiver.UserId == currentUser.UserId))
                .FirstOrDefaultAsync();

            if (existingFriendship == null)
            {
                Friendship friendship = new Friendship()
                {
                    RequesterId = currentUser.UserId,
                    ReceiverId = targetUser.UserId,
                    RequestStatus = FriendshipRequestStatus.Pending
                };
                
                db.Friendships.Add(friendship);
                await db.SaveChangesAsync();

                this.AddAlert(AlertType.Success, $"Request sent to {targetUser.UserAccount.UserName}!");
            }
            else if (existingFriendship.RequestStatus == FriendshipRequestStatus.Accepted)
            {
                this.AddAlert(AlertType.Info, $"Already friends with {targetUser.UserAccount.UserName}!");
            }
            else if (existingFriendship.Requester == currentUser)
            {
                this.AddAlert(AlertType.Info, $"Request already sent to {targetUser.UserAccount.UserName}.");
            }
            else if (existingFriendship.Receiver == currentUser)
            {
                existingFriendship.RequestStatus = FriendshipRequestStatus.Accepted;

                await db.SaveChangesAsync();

                this.AddAlert(AlertType.Success, $"Request from {targetUser.UserAccount.UserName} approved!");
            }
            
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Approves a received friend request.
        /// </summary>
        /// <param name="memberId">The GUID of the requesting user.</param>
        /// <returns>Redirect to 'Index' action.</returns>
        public async Task<ActionResult> Approve(Guid memberId)
        {
            Guid currentMemberGuid = idGetter.GetUserId(User.Identity);

            Friendship existingFriendship = await db.Friendships
                .Where(f => (f.Requester.UserId == currentMemberGuid && f.Receiver.UserId == memberId) ||
                            (f.Requester.UserId == memberId && f.Receiver.UserId == currentMemberGuid))
                .FirstOrDefaultAsync();

            if (existingFriendship == null)
            {
                return RedirectToAction("Index");
            }

            existingFriendship.RequestStatus = FriendshipRequestStatus.Accepted;
            await db.SaveChangesAsync();

            this.AddAlert(AlertType.Success, "Friend request approved!");

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Declines the a received friend request.
        /// </summary>
        /// <param name="memberId">The GUID of the requesting user.</param>
        /// <returns>Result from Delete method.</returns>
        public async Task<ActionResult> Decline(Guid memberId)
        {
            this.AddAlert(AlertType.Success, "Friend request declined.");

            Task<ActionResult> result = Delete(memberId);

            return await result;
        }

        /// <summary>
        /// Removes a friend from the user's friend list.
        /// </summary>
        /// <param name="memberId">The GUID of the user being removed from the friend list.</param>
        /// <returns>Result from Delete method.</returns>
        public async Task<ActionResult> Remove(Guid memberId)
        {
            this.AddAlert(AlertType.Success, "Friend removed.");

            Task<ActionResult> result = Delete(memberId);

            return await result;
        }

        /// <summary>
        /// Cancels the current user's friend request sent to another user.
        /// </summary>
        /// <param name="memberId">The GUID of the requested user.</param>
        /// <returns>Result from Delete method.</returns>
        public async Task<ActionResult> Cancel(Guid memberId)
        {
            this.AddAlert(AlertType.Success, "Friend request cancelled.");

            Task<ActionResult> result = Delete(memberId);

            return await result;
        }

        /// <summary>
        /// Method to delete any friendship between the current user and another user.
        /// </summary>
        /// <param name="memberId">The GUID of the requesting/requested/friend user (not the current user).</param>
        /// <returns>Redirect to 'Index' action.</returns>
        [NonAction]
        private async Task<ActionResult> Delete(Guid memberId)
        {
            Guid currentMemberGuid = idGetter.GetUserId(User.Identity);

            Friendship existingFriendship = await db.Friendships
                .Where(f => (f.Requester.UserId == currentMemberGuid && f.Receiver.UserId == memberId) ||
                            (f.Requester.UserId == memberId && f.Receiver.UserId == currentMemberGuid))
                .FirstOrDefaultAsync();

            if (existingFriendship == null)
            {
                this.ClearAlerts();
                this.AddAlert(AlertType.Error, "Error processing request. Please try again.");

                return RedirectToAction("Index");
            }

            db.Friendships.Remove(existingFriendship);
            await db.SaveChangesAsync();

            return RedirectToAction("Index");
        }
    }
}