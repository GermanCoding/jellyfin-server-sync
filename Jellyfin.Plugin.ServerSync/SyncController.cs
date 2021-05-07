using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
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

        public SyncController(ILogger<SyncController> logger, IUserDataManager userDataRepository, IUserManager userManager, ILibraryManager libraryManager)
        {
            _logger = logger;
            _userDataRepository = userDataRepository;
            _userManager = userManager;
            _libraryManager = libraryManager;
        }

        [HttpPost("update")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public ActionResult Update([FromQuery, Required] Guid userId, [FromBody, Required] UserItemDataDto data)
        {
            if (!HttpContext.Request.Headers.TryGetValue("X-Internal-Sync-Auth", out StringValues auth))
            {
                return Forbid();
            }
            if (string.IsNullOrEmpty(auth.ToString()))
            {
                return Forbid();
            }

            if (!string.Equals(auth, Plugin.Instance.Configuration.AuthToken, StringComparison.Ordinal))
            {
                return Forbid();
            }

            var user = _userManager.GetUserById(userId);

            if (user == null)
            {
                _logger.LogWarning("Cannot sync user data: Received user id that doesn't exist locally!");
                return NoContent();
            }

            var item = _libraryManager.GetItemById(Guid.Parse(data.ItemId));

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

            _userDataRepository.SaveUserData(userId, item, itemData, UserDataSaveReason.Import, CancellationToken.None);

            return NoContent();
        }
    }
}
