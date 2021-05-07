# Jellyfin Server Sync plugin

### Warning: Alpha stage. This project is currently in alpha phase of development. Things are not stable yet.

## Description

This is a plugin for Jellyfin that synchronizes user data between two jellyfin instances. The idea is that if you watch a movie on server A, it's also marked as seen on server B.
This can be useful if you have multiple Jellyfin instances you switch between (e.g a beta and a production version), but you want your favorites and series progress to be identical.

## Current Limitations

- This project is currently in very early alpha stage. Right now it's mostly a proof of concept.
- Only two servers are supported right now. Support for larger clusters may be added in the future.
- User ids and media ids must be exactly identical on all Jellyfin instances.
	- Jellyfin generates media ids from the full path & filename. This means that on all instances the full filename, including path, must be identical.
		- This is best achieved using Docker, as you can use bind mounts to mount your media folders to the same virtual path inside docker, independent of the physical location
		of the files.
- Errors are not yet handled correctly and can cause severe issues, e.g if one instance suddenly goes down.
- Sync is pretty basic right now and always sends user updates to the other server, overwriting whatever was there. No merging or other smart sync logic is currently implemented.
- It's highly recommended that both instances are exactly identical, as any other situation can break things.
	- The plugin has no requirement for both jellyfin instances to be in on the same version though, but major changes in Jellyfin can break things.

## What is synced

As of now, only basic user item data is synchronized, including:

- Favorites
- Last played date
- Likes
- Playback position
- Play count
- Played (Yes/No)
- Rating


## Building and installing

I'm not providing official builds, mostly because this project is still at an early stage.

If you want to try it out, build the plugin yourself using Visual Studio (use the included .sln file), build the project, and copy the resulting output .dll
into a subfolder (e.g ServerSync) in your Jellyfin plugins folder. Then restart jellyfin. The .meta file in that folder is generated automatically on first run.
