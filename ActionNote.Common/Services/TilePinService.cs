using ActionNote.Common.Models;
using UWPCore.Framework.Notifications;
using Ninject;
using System.Threading.Tasks;
using UWPCore.Framework.Notifications.Models;
using UWPCore.Framework.Storage;
using Windows.UI;
using System;

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
            var tileModel = GetTileModel(noteItem);
            var tile = _tileService.AdaptiveFactory.Create(tileModel);

            if (_tileService.Exists(noteItem.Id))
                _tileService.GetUpdaterForSecondaryTile(noteItem.Id).Update(tile);
            else
            {
                var picturePath = IOConstants.APPDATA_LOCAL_SCHEME + "/" + AppConstants.ATTACHEMENT_BASE_PATH + noteItem.AttachementFile;
                var secondaryTile = new SecondaryTileModel()
                {
                    Arguments = noteItem.Id,
                };
                secondaryTile.VisualElements.Square150x150Logo = new Uri(IOConstants.APPX_SCHEME + "/Assets/Square150x150Logo.png", UriKind.Absolute);
                secondaryTile.VisualElements.Wide310x150Logo = new Uri(IOConstants.APPX_SCHEME + "/Assets/Square310x150Logo.png", UriKind.Absolute);
                secondaryTile.VisualElements.BackgroundColor = Colors.Red;
                secondaryTile.VisualElements.ForegroundText = Windows.UI.StartScreen.ForegroundText.Light;


                await _tileService.PinAsync(noteItem.Id, secondaryTile, "bla bla args"); // TODO: args needed?
                _tileService.GetUpdaterForSecondaryTile(noteItem.Id).Update(tile);
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

        private AdaptiveTileModel GetTileModel(NoteItem noteItem)
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
                       },
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
                       new AdaptiveBinding()
                       {
                           Template = VisualTemplate.TileWide,
                           Children =
                           {
                               new AdaptiveText()
                               {
                                   Content = noteItem.Title,
                                   HintStyle = TextStyle.Subtitle
                               },
                               new AdaptiveText()
                               {
                                   Content = noteItem.Content,
                                   HintStyle = TextStyle.Body,
                                   HintWrap = true
                               }
                           }
                       }
                    }
                }
            };

            if (noteItem.HasAttachement)
            {
                var picturePath = IOConstants.APPDATA_LOCAL_SCHEME + "/" + AppConstants.ATTACHEMENT_BASE_PATH + noteItem.AttachementFile;
                tileModel.Visual.Bindings[0].Children.Add(new AdaptiveImage()
                {
                    Placement = ImagePlacement.Background,
                    Source = picturePath
                });
            }

            return tileModel;
        }
    }
}
