using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Jellyfin.Plugin.ServerSync
{
    [ApiController]
    [Route("internal/sync")]
    public class SyncController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IUserDataManager _userDataRepository;
        private readonly IUserManager _userManager;
        private readonly ILibraryManager _libraryManager;
        private readonly ISessionManager _sessionManager;

        public SyncController(ILogger<SyncController> logger, IUserDataManager userDataRepository, IUserManager userManager, ILibraryManager libraryManager, ISessionManager sessionManager)
        {
            _logger = logger;
            _userDataRepository = userDataRepository;
            _userManager = userManager;
            _libraryManager = libraryManager;
            _sessionManager = sessionManager;
        }

        private Boolean CheckAuth()
        {
            if (!HttpContext.Request.Headers.TryGetValue("X-Internal-Sync-Auth", out StringValues auth))
            {
                return false;
            }

            if (string.IsNullOrEmpty(auth.ToString()))
            {
                return false;
            }

            if (!string.Equals(auth, Plugin.Instance.Configuration.AuthToken, StringComparison.Ordinal))
            {
                return false;
            }

            return true;
        }

        [HttpPost("update")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public ActionResult Update([FromQuery, Required] Guid userId, [FromBody, Required] UserItemDataDto data)
        {
            if (!CheckAuth())
            {
                return Forbid();
            }

            var user = _userManager.GetUserById(userId);

            if (user == null)
            {
                _logger.LogWarning("Cannot sync user data: Received user id that doesn't exist locally!");
                return NoContent();
            }

            var item = _libraryManager.GetItemById(data.ItemId);

            if (item == null)
            {
                _logger.LogWarning("Cannot sync user data: Received item id that doesn't exist locally!");
                return NoContent();
            }

            UserItemData itemData = _userDataRepository.GetUserData(user, item);

            // TODO: Convert this automatically (or at least in a better way)
            itemData.IsFavorite = data.IsFavorite;
            itemData.LastPlayedDate = data.LastPlayedDate;
            itemData.Likes = data.Likes;
            itemData.PlaybackPositionTicks = data.PlaybackPositionTicks;
            itemData.PlayCount = data.PlayCount;
            itemData.Played = data.Played;
            itemData.Rating = data.Rating;

            _userDataRepository.SaveUserData(user, item, itemData, UserDataSaveReason.Import, CancellationToken.None);

            return NoContent();
        }

        [HttpPost("createUser")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public ActionResult CreateUser([FromBody, Required] UserDto data)
        {
            if (!CheckAuth())
            {
                return Forbid();
            }

            ServerSync.Instance.SyncUserCreation(data);

            return NoContent();
        }

        [HttpPost("deleteUser")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public ActionResult DeleteUser([FromBody, Required] Guid userId)
        {
            if (!CheckAuth())
            {
                return Forbid();
            }

            ServerSync.Instance.SyncUserDeletion(userId);

            return NoContent();
        }
    }
}
