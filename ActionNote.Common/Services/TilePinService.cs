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
using System.Text;
using System.Linq;

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

        public void UpdateMainTile(IList<NoteItem> noteItems)
        {
            var updater = _tileService.GetUpdaterForApplication();

            // clear all
            updater.Clear();

            if (noteItems.Count > 0)
            {
                // add new notes to flip through
                updater.EnableNotificationQueue(true);

                // filter out 5 most important notes
                var fiveMostImportantNotes = new List<NoteItem>();
                var sortedNotes = NoteUtils.Sort(noteItems, AppConstants.SORT_DATE);
                var importantNotes = sortedNotes.Where(n => n.IsImportant && !n.IsHidden).Take(5);
                fiveMostImportantNotes.AddRange(importantNotes);
                if (fiveMostImportantNotes.Count < 5)
                {
                    foreach (var item in sortedNotes)
                    {
                        if (item.IsHidden)
                            continue;

                        if (!fiveMostImportantNotes.Contains(item))
                        {
                            fiveMostImportantNotes.Add(item);

                            if (fiveMostImportantNotes.Count == 5)
                                break;
                        }
                    }
                }

                // update the tile sequence
                foreach (var noteItem in fiveMostImportantNotes)
                {
                    var tileModel = GetMainTileModel(noteItem);
                    var tile = _tileService.AdaptiveFactory.Create(tileModel);
                    tile.Tag = noteItem.ShortId;
                    updater.Update(tile);
                }
            }
        }

        public async Task PinOrUpdateAsync(NoteItem noteItem)
        {
            var tileModel = GetSecondaryTileModel(noteItem);
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

            var tileModel = GetSecondaryTileModel(noteItem);
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

        private AdaptiveTileModel GetMainTileModel(NoteItem noteItem)
        {
            // trim the content to 3/9 lines, because for somehow no text will be displayed when there are too many lines (possible minor Windows 10 bug?)
            var contentWith3Lines = new StringBuilder();
            var contentWith9Lines = new StringBuilder();
            if (!string.IsNullOrEmpty(noteItem.Content))
            {
                var splitted = noteItem.Content.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                for (int i = 0; i < splitted.Length; ++i)
                {
                    if (i >= 9)
                        break;


                    if (i < 3)
                    {
                        contentWith3Lines.Append(splitted[i].Trim());
                        contentWith3Lines.Append(Environment.NewLine);
                    }

                    if (i < 9)
                    {
                        contentWith9Lines.Append(splitted[i].Trim());
                        contentWith9Lines.Append(Environment.NewLine);
                    }
                }
            }

            var tileModel = new AdaptiveTileModel()
            {
                Visual = new AdaptiveVisual()
                {
                    Bindings =
                    {
                       new AdaptiveBinding()
                       {
                           Template = VisualTemplate.TileMedium,
                           Branding = VisualBranding.Name,
                           Children =
                           {
                               new AdaptiveText()
                               {
                                   Content = noteItem.Title,
                                   HintStyle = TextStyle.Base,
                               },
                               new AdaptiveText()
                               {
                                   Content = contentWith3Lines.ToString(),
                                   HintWrap = true
                               },
                           }
                       },
                       new AdaptiveBinding()
                       {
                           Template = VisualTemplate.TileWide,
                           Branding = VisualBranding.Name,
                           Children =
                           {
                               new AdaptiveText()
                               {
                                   Content = noteItem.Title,
                                   HintStyle = TextStyle.Base
                               },
                               new AdaptiveText()
                               {
                                   Content = contentWith3Lines.ToString(),
                                   HintStyle = TextStyle.Caption,
                                   HintWrap = true
                               }
                           }
                       },
                       new AdaptiveBinding()
                       {
                           Template = VisualTemplate.TileLarge,
                           Branding = VisualBranding.Name,
                           Children =
                           {
                               new AdaptiveText()
                               {
                                   Content = noteItem.Title,
                                   HintStyle = TextStyle.Base
                               },
                               new AdaptiveText()
                               {
                                   Content = contentWith9Lines.ToString(),
                                   HintStyle = TextStyle.Caption,
                                   HintWrap = true
                               }
                           }
                       }
                    }
                }
            };

            TrySetAttachementAsBackground(noteItem, tileModel, true);

            return tileModel;
        }

        private AdaptiveTileModel GetSecondaryTileModel(NoteItem noteItem)
        {
            // trim the content lines, because for somehow no text will be displayed when there are too many lines (possible minor Windows 10 bug?)
            var contentWith4Lines = new StringBuilder();
            if (!string.IsNullOrEmpty(noteItem.Content))
            {
                var splitted = noteItem.Content.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                for (int i = 0; i < splitted.Length; ++i)
                {
                    if (i >= 8)
                        break;

                    if (i < 4)
                    {
                        contentWith4Lines.Append(splitted[i].Trim());
                        contentWith4Lines.Append(Environment.NewLine);
                    }
                }
            }

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
                           Branding = VisualBranding.None,
                           Children =
                           {
                               new AdaptiveText()
                               {
                                   Content = noteItem.Title,
                                   HintStyle = TextStyle.Base,
                               },
                               new AdaptiveText()
                               {
                                   Content = contentWith4Lines.ToString(),
                                   HintWrap = true
                               }
                           }
                       },
                       new AdaptiveBinding()
                       {
                           Template = VisualTemplate.TileWide,
                           Branding = VisualBranding.None,
                           Children =
                           {
                               new AdaptiveText()
                               {
                                   Content = noteItem.Title,
                                   HintStyle = TextStyle.Base
                               },
                               new AdaptiveText()
                               {
                                   Content = contentWith4Lines.ToString(),
                                   HintStyle = TextStyle.Caption,
                                   HintWrap = true
                               }
                           }
                       }
                    }
                }
            };

            TrySetAttachementAsBackground(noteItem, tileModel, false);

            return tileModel;
        }

        private static void TrySetAttachementAsBackground(NoteItem noteItem, AdaptiveTileModel tileModel, bool useBackgroundColorImage)
        {
            string picturePath = null;
            int overlay = 0;

            if (noteItem.HasAttachement)
            {
                picturePath = IOConstants.APPDATA_LOCAL_SCHEME + "/" + AppConstants.ATTACHEMENT_BASE_PATH + noteItem.AttachementFile;
                overlay = 33;
            }
            else if (useBackgroundColorImage && noteItem.Color != ColorCategory.Neutral)
            {
                picturePath = IOConstants.APPX_SCHEME + "/Assets/Images/" + noteItem.Color.ToString().ToLower() + ".png";
            }

            if (picturePath != null)
            {
                tileModel.Visual.Bindings[0].Children.Add(new AdaptiveImage()
                {
                    Placement = ImagePlacement.Background,
                    HintOverlay = overlay,
                    Source = picturePath
                });

                tileModel.Visual.Bindings[1].Children.Add(new AdaptiveImage()
                {
                    Placement = ImagePlacement.Background,
                    HintOverlay = overlay,
                    Source = picturePath
                });

                tileModel.Visual.Bindings[2].Children.Add(new AdaptiveImage()
                {
                    Placement = ImagePlacement.Background,
                    HintOverlay = overlay,
                    Source = picturePath
                });
            }
        }
    }
}
