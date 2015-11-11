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
            FriendsListViewModel friendsListViewModel = new FriendsListViewModel();

            Guid currentMemberGuid = idGetter.GetUserId(User.Identity);

            Member member = await db.Members
                .Include(m => m.RequestedFriendships)
                .Include(m => m.ReceivedFriendships)
                .Where(m => m.UserId == currentMemberGuid)
                .FirstOrDefaultAsync();

            var friendships = member.RequestedFriendships
                .Concat(member.ReceivedFriendships);

            friendsListViewModel.ConfirmedFriends = member.ConfirmedFriends.ToList();

            //TODO: change the Member entity's RequestedFriendships and ReceivedFriendships get methods

            //TODO: decide if ViewModel is List<Member>
            var pendingReceivedFriendships = friendships
                .Where(f => f.RequestStatus == FriendshipRequestStatus.Pending)
                .Where(f => f.ReceiverId == idGetter.GetUserId(User.Identity));
            foreach (var friendship in pendingReceivedFriendships)
            {
                friendsListViewModel.PendingReceivedFriendships
                    .Add((friendship.Requester == member) ? friendship.Receiver : friendship.Requester);
            }

            var pendingSentFriendships = friendships
                .Where(f => f.RequestStatus == FriendshipRequestStatus.Pending)
                .Where(f => f.RequesterId == idGetter.GetUserId(User.Identity));
            foreach (var friendship in pendingSentFriendships)
            {
                friendsListViewModel.PendingSentFriendships
                    .Add((friendship.Requester == member) ? friendship.Receiver : friendship.Requester);
            }
            //TODO: or ViewModel is List<Friendship>
            //friendsListViewModel.PendingReceivedFriendships = friendships
            //    .Where(f => f.RequestStatus == FriendshipRequestStatus.Pending)
            //    .Where(f => f.ReceiverId == idGetter.GetUserId(User.Identity))
            //    .ToList();
            //friendsListViewModel.PendingSentFriendships = friendships
            //    .Where(f => f.RequestStatus == FriendshipRequestStatus.Pending)
            //    .Where(f => f.RequesterId == idGetter.GetUserId(User.Identity))
            //    .ToList();

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

            if (targetUser == null)
            {
                this.AddAlert(AlertType.Error, $"No member with the username '{username}' exists.");

                return RedirectToAction("Index");
            }

            Friendship existingFriendship = await db.Friendships
                .Where(f => (f.Requester == currentUser && f.Receiver == targetUser) ||
                            (f.Requester == targetUser && f.Receiver == currentUser))
                .FirstOrDefaultAsync();

            if (existingFriendship == null)
            {
                Friendship friendship = new Friendship()
                {
                    Requester = currentUser,
                    RequesterId = currentUser.UserId,
                    Receiver = targetUser,
                    ReceiverId = targetUser.UserId,
                    RequestStatus = FriendshipRequestStatus.Pending
                };
                
                db.Friendships.Add(friendship);
                await db.SaveChangesAsync();

                this.AddAlert(AlertType.Success, $"Request sent to {username}!");

                return RedirectToAction("Index");
            }

            if (existingFriendship.RequestStatus == FriendshipRequestStatus.Accepted)
            {
                this.AddAlert(AlertType.Warning, $"Already friends with {username}!");
            }
            else if (existingFriendship.Requester == currentUser)
            {
                this.AddAlert(AlertType.Warning, $"Request already sent to {username}.");
            }
            else if (existingFriendship.Receiver == currentUser)
            {
                existingFriendship.RequestStatus = FriendshipRequestStatus.Accepted;

                //db.Friendships.AddOrUpdate(existingFriendship);
                await db.SaveChangesAsync();

                this.AddAlert(AlertType.Success, $"Request from {username} approved!");
            }
            
            //TODO: what if someone has declined? should they be able to send another? (-Chelsea)

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<ActionResult> Delete(Friendship friendship)
        {
            if (friendship != null)
            {
                db.Friendships.Remove(friendship);
                await db.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }
    }
}