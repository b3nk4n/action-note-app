using ActionNote.Common.Models;
using UWPCore.Framework.Notifications;
using Ninject;
using System.Threading.Tasks;
using UWPCore.Framework.Notifications.Models;
using UWPCore.Framework.Storage;
using Windows.UI;
using System;
using Windows.UI.StartScreen;

namespace ActionNote.Common.Services
{
    public class TilePinService : ITilePinService
    {
        private ITileService _tileService;

        [Inject]
        public TilePinService(ITileService tileService)
        {
            _tileService = tileService;
        }

        public async Task PinOrUpdateAsync(NoteItem noteItem)
        {
            var tileSmallModel = GetTileSmallModel(noteItem);
            var tileMediumModel = GetTileMediumModel(noteItem);
            //var tileWideModel = GetTileWideModel(noteItem);
            var tileSmall = _tileService.AdaptiveFactory.Create(tileSmallModel);
            var tileMedium = _tileService.AdaptiveFactory.Create(tileMediumModel);
            //var tileWide = _tileService.AdaptiveFactory.Create(tileWideModel);

            if (_tileService.Exists(noteItem.Id))
            {
                _tileService.GetUpdaterForSecondaryTile(noteItem.Id).Update(tileSmall);
                _tileService.GetUpdaterForSecondaryTile(noteItem.Id).Update(tileMedium);
                //_tileService.GetUpdaterForSecondaryTile(noteItem.Id).Update(tileWide);
            }
            else
            {
                var picturePath = IOConstants.APPDATA_LOCAL_SCHEME + "/" + AppConstants.ATTACHEMENT_BASE_PATH + noteItem.AttachementFile;
                var secondaryTile = new SecondaryTileModel()
                {
                    Arguments = noteItem.Id,
                };
                secondaryTile.VisualElements.Square150x150Logo = new Uri(IOConstants.APPX_SCHEME + "/Assets/Square150x150Logo.scale-200.png", UriKind.Absolute);
                //secondaryTile.VisualElements.Wide310x150Logo = new Uri(IOConstants.APPX_SCHEME + "/Assets/Square310x150Logo.scale-200.png", UriKind.Absolute);
                secondaryTile.VisualElements.ShowNameOnWide310x150Logo = true;

                await _tileService.PinAsync(noteItem.Id, secondaryTile, "bla bla args"); // TODO: args needed?
                _tileService.GetUpdaterForSecondaryTile(noteItem.Id).Update(tileSmall);
                _tileService.GetUpdaterForSecondaryTile(noteItem.Id).Update(tileMedium);
                //_tileService.GetUpdaterForSecondaryTile(noteItem.Id).Update(tileWide); // TODO: fixme! updating a secondary tile, only the last updated tilesize is working. Updating a combines adaptive tile with various sizes also leads to that only the first listed size is updated, and the other only show the image.
            }

        }

        public bool Contains(string noteId)
        {
            return _tileService.Exists(noteId);
        }

        public async Task UnpinAsync(string noteId)
        {
            await _tileService.UnpinAsync(noteId);
        }

        private AdaptiveTileModel GetTileSmallModel(NoteItem noteItem)
        {
            var tileModel = new AdaptiveTileModel()
            {
                Visual = new AdaptiveVisual()
                {
                    Bindings =
                    {
                       new AdaptiveBinding()
                       {
                           Template = VisualTemplate.TileSmall,
                           Children =
                           {
                               new AdaptiveText()
                               {
                                   Content = noteItem.Title
                               }
                           }
                       }
                    }
                }
            };

            TrySetAttachementAsBackground(noteItem, tileModel);

            return tileModel;
        }

        private AdaptiveTileModel GetTileMediumModel(NoteItem noteItem)
        {
            var tileModel = new AdaptiveTileModel()
            {
                Visual = new AdaptiveVisual()
                {
                    Bindings =
                    {
                       new AdaptiveBinding()
                       {
                           Template = VisualTemplate.TileMedium,
                           Children =
                           {
                               new AdaptiveText()
                               {
                                   Content = noteItem.Title,
                                   HintStyle = TextStyle.Base
                               },
                               new AdaptiveText()
                               {
                                   Content = noteItem.Content,
                                   HintWrap = true
                               }
                           }
                       },
                    }
                }
            };

            TrySetAttachementAsBackground(noteItem, tileModel);

            return tileModel;
        }

        //private AdaptiveTileModel GetTileWideModel(NoteItem noteItem)
        //{
        //    var tileModel = new AdaptiveTileModel()
        //    {
        //        Visual = new AdaptiveVisual()
        //        {
        //            Bindings =
        //            {
        //               new AdaptiveBinding()
        //               {
        //                   Template = VisualTemplate.TileWide,
        //                   Children =
        //                   {
        //                       new AdaptiveText()
        //                       {
        //                           Content = noteItem.Title,
        //                           HintStyle = TextStyle.Subtitle
        //                       },
        //                       new AdaptiveText()
        //                       {
        //                           Content = noteItem.Content,
        //                           HintStyle = TextStyle.Body,
        //                           HintWrap = true
        //                       }
        //                   }
        //               }
        //            }
        //        }
        //    };

        //    TrySetAttachementAsBackground(noteItem, tileModel);

        //    return tileModel;
        //}

        private static void TrySetAttachementAsBackground(NoteItem noteItem, AdaptiveTileModel tileModel)
        {
            if (noteItem.HasAttachement)
            {
                var picturePath = IOConstants.APPDATA_LOCAL_SCHEME + "/" + AppConstants.ATTACHEMENT_BASE_PATH + noteItem.AttachementFile;
                tileModel.Visual.Bindings[0].Children.Add(new AdaptiveImage()
                {
                    Placement = ImagePlacement.Background,
                    Source = picturePath
                });
            }
        }
    }
}
