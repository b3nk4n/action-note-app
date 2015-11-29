using ActionNote.Common.Models;
using UWPCore.Framework.Notifications;
using Ninject;
using System.Threading.Tasks;
using UWPCore.Framework.Notifications.Models;
using UWPCore.Framework.Storage;
using System;
using Windows.UI.StartScreen;
using ActionNote.Common.Helpers;
using Windows.UI;
using System.Collections.Generic;

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
            {
                _tileService.GetUpdaterForSecondaryTile(noteItem.Id).Update(tile);
            }
            else
            {
                var picturePath = IOConstants.APPDATA_LOCAL_SCHEME + "/" + AppConstants.ATTACHEMENT_BASE_PATH + noteItem.AttachementFile;
                var secondaryTile = new SecondaryTileModel()
                {
                    Arguments = noteItem.Id,
                    DisplayName = "Action Note"
                };
                secondaryTile.VisualElements.Square150x150Logo = new Uri(IOConstants.APPX_SCHEME + "/Assets/Square150x150Logo.scale-200.png", UriKind.Absolute);
                secondaryTile.VisualElements.Wide310x150Logo = new Uri(IOConstants.APPX_SCHEME + "/Assets/Square310x150Logo.scale-200.png", UriKind.Absolute);
                secondaryTile.VisualElements.ShowNameOnWide310x150Logo = true;

                UpdateSecondaryTileColor(noteItem, secondaryTile);

                if (await _tileService.PinAsync(noteItem.Id, secondaryTile, noteItem.Id))
                {
                    _tileService.GetUpdaterForSecondaryTile(noteItem.Id).Update(tile);
                }
            }
        }

        public async Task UpdateAsync(NoteItem noteItem)
        {
            if (!_tileService.Exists(noteItem.Id))
                return;

            var tileModel = GetTileModel(noteItem);
            var tile = _tileService.AdaptiveFactory.Create(tileModel);

            var secondaryTile = new SecondaryTileModel();
            UpdateSecondaryTileColor(noteItem, secondaryTile);

            await _tileService.UpdateAsync(noteItem.Id, secondaryTile);
            _tileService.GetUpdaterForSecondaryTile(noteItem.Id).Update(tile);
        }

        private static void UpdateSecondaryTileColor(NoteItem noteItem, SecondaryTileModel secondaryTile)
        {
            if (noteItem.Color == ColorCategory.Neutral)
            {
                secondaryTile.VisualElements.BackgroundColor = Colors.Transparent;
                secondaryTile.VisualElements.ForegroundText = ForegroundText.Light;
            }
            else
            {
                var color = ColorCategoryConverter.ToColor(noteItem.Color, false);
                secondaryTile.VisualElements.BackgroundColor = ColorCategoryConverter.ToColor(noteItem.Color, false);
                secondaryTile.VisualElements.ForegroundText = ForegroundText.Light;
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

        public async Task UnpinUnreferencedTilesAsync(IList<string> noteIds)
        {
            var allTiles = await _tileService.GetAllSecondaryTilesAsync();

            foreach (var tile in allTiles)
            {
                if (!noteIds.Contains(tile.Arguments))
                {
                    await _tileService.UnpinAsync(tile.TileId);
                }
            }
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
                                   HintStyle = TextStyle.Base,
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

            TrySetAttachementAsBackground(noteItem, tileModel);

            return tileModel;
        }

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

                tileModel.Visual.Bindings[1].Children.Add(new AdaptiveImage()
                {
                    Placement = ImagePlacement.Background,
                    Source = picturePath
                });

                tileModel.Visual.Bindings[2].Children.Add(new AdaptiveImage()
                {
                    Placement = ImagePlacement.Background,
                    Source = picturePath
                });
            }
        }
    }
}
