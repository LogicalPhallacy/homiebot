using System;
using System.Collections.Generic;
using System.Linq;

namespace Homiebot.Models;
public class SongLink
{
    public string entityUniqueId {get; set;}
    public string userCountry{get;set;}
    public string pageUrl {get;set;}
    public Dictionary<string,PlatformObject> linksByPlatform {get;set;}
    public Dictionary<string,entityObject> entitiesByUniqueId {get;set;}

    public Tuple<string,int,int>? GetBestThumbnail() {
        entityObject? bestThumbnail = entitiesByUniqueId?.Values
            .Where(
                v => !string.IsNullOrWhiteSpace(v.thumbnailUrl)
            )?.OrderByDescending(
                v => v.thumbnailWidth
            )?.First();
        return bestThumbnail?.ThumbnailForDiscord;
    }
    public string FirstTitleEntry =>
        entitiesByUniqueId.Values.First(e => !string.IsNullOrWhiteSpace(e.title)).title;
    public string FirstArtistEntry =>
        entitiesByUniqueId.Values.First(e => !string.IsNullOrWhiteSpace(e.artistName)).artistName;
    public string FirstTypeEntry => 
        entitiesByUniqueId.Values.First().type.ToString();
    public string Description => $"A {FirstTypeEntry} by {FirstArtistEntry}";
}

public class PlatformObject
{
    public string entityUniqueId {get;set;}
    public string url {get;set;}
    public string? nativeAppUriMobile {get;set;}
    public string? nativeAppUriDesktop {get;set;}
    public string GetEmbedLink(string platformName)
        => $"[{platformName}]({url})";
}
public class entityObject
{
    public string id {get;set;}
    public ItemType type {get;set;}
    public string? title {get;set;}
    public string? artistName {get;set;}
    public string? thumbnailUrl {get;set;}
    public int? thumbnailWidth {get;set;}
    public int? thumbnailHeight {get;set;}
    public string apiProvider {get;set;}
    public string[] platforms {get;set;}
    public Tuple<string,int,int> ThumbnailForDiscord => new(thumbnailUrl, thumbnailHeight.Value, thumbnailWidth.Value);
}

public enum ItemType
{
    song,
    album
}

public enum Platform
{
  spotify,
  itunes,
  appleMusic,
  youtube,
  youtubeMusic,
  google,
  googleStore,
  pandora,
  deezer,
  tidal,
  amazonStore,
  amazonMusic,
  soundcloud,
  napster,
  yandex,
  spinrilla,
  audius,
  audiomack,
}

public enum APIProvider
{
  spotify,
  itunes,
  youtube,
  google,
  pandora,
  deezer,
  tidal,
  amazon,
  soundcloud,
  napster,
  yandex,
  spinrilla,
  audius,
  audiomack,
}