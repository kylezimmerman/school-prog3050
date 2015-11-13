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
using Veil.DataModels.Models;
using Veil.Helpers;
using Veil.Models;

namespace Veil.Controllers
{
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
                this.AddAlert(AlertType.Error, "Are you so lonely that you want to friend yourself?");

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

                //db.Friendships.AddOrUpdate(existingFriendship);
                await db.SaveChangesAsync();

                this.AddAlert(AlertType.Success, $"Request from {targetUser.UserAccount.UserName} approved!");
            }
            
            return RedirectToAction("Index");
        }

        [HttpPost]
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

        [HttpPost]
        public async Task<ActionResult> Delete(Guid memberId)
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

            db.Friendships.Remove(existingFriendship);
            await db.SaveChangesAsync();

            this.AddAlert(AlertType.Success,
                existingFriendship.RequestStatus == FriendshipRequestStatus.Accepted
                    ? "Friend removed."
                    : "Friend request declined.");

            return RedirectToAction("Index");
        }
    }
}