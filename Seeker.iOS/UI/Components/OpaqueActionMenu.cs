// Copyright (c) 2026 AnimaSeek iOS 15 contributors.
// Licensed under GPLv3 with Additional Terms; see LICENSE and the bundled notices.
using Foundation;
using UIKit;

namespace AnimaSeek.iOS.UI.Components;

/// <summary>A scrollable, opaque alternative to translucent file and folder action sheets.</summary>
internal sealed class OpaqueActionMenu : UINavigationController
{
    private readonly MenuContent content;

    private OpaqueActionMenu(MenuContent content) : base(content)
    {
        this.content = content;
        ModalPresentationStyle = UIModalPresentationStyle.PageSheet;
        UIKitFactory.ApplyOpaqueNavigationBar(NavigationBar);
        View!.BackgroundColor = UIColor.SystemGroupedBackground;
        View.Opaque = true;
        View.AccessibilityViewIsModal = true;
        if (SheetPresentationController is { } sheet)
        {
            sheet.Detents = [UISheetPresentationControllerDetent.CreateLargeDetent()];
            sheet.PrefersGrabberVisible = true;
            sheet.PrefersScrollingExpandsWhenScrolledToEdge = true;
        }
    }

    public static OpaqueActionMenu Create(string? title, string? message) => new(new MenuContent(title, message));

    public void AddAction(OpaqueMenuAction action) => content.AddAction(action);

#if MOCK
    // Deterministic component preview; absent from the network-enabled Release app.
    internal static OpaqueActionMenu Preview(bool folder)
    {
        var menu = Create(folder ? "An example album with a longer folder name" : "01 - An example track with a longer filename.flac",
            "Preview user · Music / Example album · FLAC · 44.1 kHz");
        string[] keys = folder
            ? ["IosUiExpandFolder", "IosUiDownloadFolder", "IosUiBrowseLocation", "IosUiBrowseUser", "IosUiSendMessage", "IosUiCopyLink", "IosUiShareLink"]
            : ["IosUiDownload", "IosUiQueuePaused", "IosUiBrowseLocation", "IosUiBrowseUser", "IosUiViewProfile", "IosUiSendMessage", "IosUiCopyLink", "IosUiShareLink"];
        foreach (string key in keys) menu.AddAction(OpaqueMenuAction.Create(AppStrings.Get(key), UIAlertActionStyle.Default, _ => { }));
        menu.AddAction(OpaqueMenuAction.Create(AppStrings.Get("IosUiCancel"), UIAlertActionStyle.Cancel, null));
        return menu;
    }
#endif

    private sealed class MenuContent(string? contextTitle, string? message) : UITableViewController(UITableViewStyle.InsetGrouped)
    {
        private const string CellIdentifier = "opaque-menu.cell";
        private readonly List<OpaqueMenuAction> actions = [];
        private OpaqueMenuAction? cancelAction;
        private bool selectionMade;

        public void AddAction(OpaqueMenuAction action)
        {
            if (action.Style == UIAlertActionStyle.Cancel)
            {
                cancelAction = action;
            }
            else
            {
                actions.Add(action);
            }
            if (IsViewLoaded) TableView.ReloadData();
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            Title = AppStrings.Get("IosUiMoreActions");
            NavigationItem.LargeTitleDisplayMode = UINavigationItemLargeTitleDisplayMode.Never;
            NavigationItem.RightBarButtonItem = new UIBarButtonItem(
                AppStrings.Get("IosUiCancel"), UIBarButtonItemStyle.Plain, (_, _) => Finish(cancelAction))
            {
                AccessibilityIdentifier = "action-menu.cancel",
            };
            TableView.BackgroundColor = UIColor.SystemGroupedBackground;
            TableView.Opaque = true;
            TableView.AccessibilityIdentifier = "action-menu.list";
            TableView.RegisterClassForCellReuse(typeof(UITableViewCell), CellIdentifier);
            TableView.RowHeight = UITableView.AutomaticDimension;
            TableView.EstimatedRowHeight = 52;
        }

        public override nint NumberOfSections(UITableView tableView) => 2;

        public override nint RowsInSection(UITableView tableView, nint section) => section == 0 ? 1 : actions.Count;

        public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
        {
            UITableViewCell cell = tableView.DequeueReusableCell(CellIdentifier, indexPath);
            bool context = indexPath.Section == 0;
            OpaqueMenuAction? action = context ? null : actions[indexPath.Row];
            UIListContentConfiguration configuration = UIListContentConfiguration.SubtitleCellConfiguration;
            configuration.Text = context ? contextTitle : action!.Title;
            configuration.SecondaryText = context ? message : null;
            configuration.TextProperties.Font = UIKitFactory.PreferredFont(context ? UIFontTextStyle.Headline : UIFontTextStyle.Body);
            configuration.TextProperties.AdjustsFontForContentSizeCategory = true;
            configuration.TextProperties.NumberOfLines = 0;
            configuration.TextProperties.Color = context ? UIColor.Label
                : action!.Style == UIAlertActionStyle.Destructive ? UIColor.SystemRed : UIColor.SystemBlue;
            configuration.SecondaryTextProperties.Font = UIKitFactory.PreferredFont(UIFontTextStyle.Subheadline);
            configuration.SecondaryTextProperties.AdjustsFontForContentSizeCategory = true;
            configuration.SecondaryTextProperties.NumberOfLines = 0;
            configuration.SecondaryTextProperties.Color = UIColor.SecondaryLabel;
            configuration.DirectionalLayoutMargins = new NSDirectionalEdgeInsets(12, 16, 12, 16);
            cell.ContentConfiguration = configuration;
            UIBackgroundConfiguration background = UIBackgroundConfiguration.ListGroupedCellConfiguration;
            background.BackgroundColor = UIColor.SecondarySystemGroupedBackground;
            cell.BackgroundConfiguration = background;
            cell.BackgroundColor = UIColor.SecondarySystemGroupedBackground;
            cell.Opaque = true;
            cell.SelectionStyle = context ? UITableViewCellSelectionStyle.None : UITableViewCellSelectionStyle.Default;
            cell.AccessibilityTraits = context ? UIAccessibilityTrait.StaticText : UIAccessibilityTrait.Button;
            cell.AccessibilityIdentifier = context ? "action-menu.context" : $"action-menu.action.{indexPath.Row}";
            return cell;
        }

        public override void RowSelected(UITableView tableView, NSIndexPath indexPath)
        {
            tableView.DeselectRow(indexPath, animated: true);
            if (indexPath.Section == 1) Finish(actions[indexPath.Row]);
        }

        private void Finish(OpaqueMenuAction? action)
        {
            if (selectionMade) return;
            selectionMade = true;
            // Finish dismissal before routing, sharing, or showing a second menu.
            NavigationController!.DismissViewController(true, () => action?.Invoke());
        }
    }
}

internal sealed record OpaqueMenuAction(string Title, UIAlertActionStyle Style, Action<OpaqueMenuAction>? Handler)
{
    public static OpaqueMenuAction Create(string title, UIAlertActionStyle style, Action<OpaqueMenuAction>? handler) =>
        new(title, style, handler);

    public void Invoke() => Handler?.Invoke(this);
}
