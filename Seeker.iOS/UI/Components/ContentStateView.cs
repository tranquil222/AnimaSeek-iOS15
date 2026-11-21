using System.Runtime.CompilerServices;
using UIKit;

namespace AnimaSeek.iOS.UI.Components;

/// <summary>Describes a native empty, loading, offline, or recoverable-error presentation.</summary>
/// <param name="Title">The localized state title.</param>
/// <param name="Message">The optional localized explanation and next-step guidance.</param>
/// <param name="SymbolName">The optional SF Symbol that reinforces, but does not replace, the text.</param>
/// <param name="ActionTitle">The optional localized recovery action.</param>
/// <param name="Action">The recovery operation.</param>
/// <param name="IsLoading">Whether UIKit should render a native loading configuration.</param>
internal sealed record ContentStatePresentation(
    string Title,
    string? Message = null,
    string? SymbolName = null,
    string? ActionTitle = null,
    Action? Action = null,
    bool IsLoading = false);

/// <summary>Renders content state with the native configuration or an accessible iOS 15/16 fallback.</summary>
internal static class ContentStateView
{
    private static readonly ConditionalWeakTable<UIViewController, LegacyContentState> LegacyStates = new();

    /// <summary>Shows a complete-screen state while no useful content exists.</summary>
    /// <param name="controller">The controller that owns the empty content plane.</param>
    /// <param name="presentation">The localized state presentation.</param>
    public static void Show(UIViewController controller, ContentStatePresentation presentation)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(presentation);
        if (!OperatingSystem.IsIOSVersionAtLeast(17))
        {
            ShowLegacy(controller, presentation);
            return;
        }

        UIContentUnavailableConfiguration configuration = presentation.IsLoading
            ? UIContentUnavailableConfiguration.CreateLoadingConfiguration()
            : UIContentUnavailableConfiguration.CreateEmptyConfiguration();
        configuration.Text = presentation.Title;
        configuration.SecondaryText = presentation.Message;
        configuration.Image = string.IsNullOrWhiteSpace(presentation.SymbolName)
            ? null
            : UIImage.GetSystemImage(presentation.SymbolName);
        if (!string.IsNullOrWhiteSpace(presentation.ActionTitle) && presentation.Action is not null)
        {
            configuration.Button = UIButtonConfiguration.TintedButtonConfiguration;
            configuration.Button.Title = presentation.ActionTitle;
            configuration.ButtonProperties.PrimaryAction = UIAction.Create(_ => presentation.Action());
        }

        controller.ContentUnavailableConfiguration = configuration;
    }

    /// <summary>Restores the controller's normal content presentation.</summary>
    /// <param name="controller">The controller whose state should clear.</param>
    public static void Clear(UIViewController controller)
    {
        ArgumentNullException.ThrowIfNull(controller);
        if (OperatingSystem.IsIOSVersionAtLeast(17))
        {
            controller.ContentUnavailableConfiguration = null;
        }
        if (LegacyStates.TryGetValue(controller, out LegacyContentState? state))
        {
            state.Remove();
            LegacyStates.Remove(controller);
        }
    }

    private static void ShowLegacy(UIViewController controller, ContentStatePresentation presentation)
    {
        Clear(controller);
        UIView root = controller.View!;
        var content = new UIView { BackgroundColor = UIColor.SystemBackground };
        var stack = new UIStackView
        {
            Axis = UILayoutConstraintAxis.Vertical,
            Alignment = UIStackViewAlignment.Center,
            Spacing = 12,
            TranslatesAutoresizingMaskIntoConstraints = false,
        };
        content.AddSubview(stack);

        if (presentation.IsLoading)
        {
            var spinner = new UIActivityIndicatorView(UIActivityIndicatorViewStyle.Medium)
            {
                AccessibilityIdentifier = "content-state.loading",
            };
            spinner.StartAnimating();
            stack.AddArrangedSubview(spinner);
        }
        else if (!string.IsNullOrWhiteSpace(presentation.SymbolName))
        {
            var image = new UIImageView(UIImage.GetSystemImage(presentation.SymbolName))
            {
                TintColor = UIColor.SecondaryLabel,
                ContentMode = UIViewContentMode.ScaleAspectFit,
                IsAccessibilityElement = false,
            };
            image.WidthAnchor.ConstraintEqualTo(40).Active = true;
            image.HeightAnchor.ConstraintEqualTo(40).Active = true;
            stack.AddArrangedSubview(image);
        }

        stack.AddArrangedSubview(CreateLabel(presentation.Title, UIFontTextStyle.Headline,
            UIColor.Label, "content-state.title"));
        if (!string.IsNullOrWhiteSpace(presentation.Message))
        {
            stack.AddArrangedSubview(CreateLabel(presentation.Message, UIFontTextStyle.Body,
                UIColor.SecondaryLabel, "content-state.message"));
        }
        if (!string.IsNullOrWhiteSpace(presentation.ActionTitle) && presentation.Action is not null)
        {
            var configuration = UIButtonConfiguration.TintedButtonConfiguration;
            configuration.Title = presentation.ActionTitle;
            var button = UIButton.GetButton(configuration, UIAction.Create(_ => presentation.Action()));
            button.AccessibilityIdentifier = "content-state.action";
            stack.AddArrangedSubview(button);
        }

        NSLayoutConstraint.ActivateConstraints([
            stack.CenterXAnchor.ConstraintEqualTo(content.SafeAreaLayoutGuide.CenterXAnchor),
            stack.CenterYAnchor.ConstraintEqualTo(content.SafeAreaLayoutGuide.CenterYAnchor),
            stack.LeadingAnchor.ConstraintGreaterThanOrEqualTo(content.SafeAreaLayoutGuide.LeadingAnchor, 20),
            stack.TrailingAnchor.ConstraintLessThanOrEqualTo(content.SafeAreaLayoutGuide.TrailingAnchor, -20),
            stack.WidthAnchor.ConstraintLessThanOrEqualTo(420),
        ]);

        // Background views stay fixed while lists scroll and leave the navigation bar
        // and pull-to-refresh available. Restore an existing background on Clear.
        UIView? previousBackground = null;
        if (root is UITableView table)
        {
            previousBackground = table.BackgroundView;
            table.BackgroundView = content;
        }
        else if (root is UICollectionView collection)
        {
            previousBackground = collection.BackgroundView;
            collection.BackgroundView = content;
        }
        else
        {
            content.TranslatesAutoresizingMaskIntoConstraints = false;
            root.AddSubview(content);
            NSLayoutConstraint.ActivateConstraints([
                content.LeadingAnchor.ConstraintEqualTo(root.SafeAreaLayoutGuide.LeadingAnchor),
                content.TrailingAnchor.ConstraintEqualTo(root.SafeAreaLayoutGuide.TrailingAnchor),
                content.TopAnchor.ConstraintEqualTo(root.SafeAreaLayoutGuide.TopAnchor),
                content.BottomAnchor.ConstraintEqualTo(root.SafeAreaLayoutGuide.BottomAnchor),
            ]);
        }

        LegacyStates.Add(controller, new LegacyContentState(root, content, previousBackground));
    }

    private static UILabel CreateLabel(string text, UIFontTextStyle textStyle, UIColor color, string identifier) => new()
    {
        Text = text,
        Font = UIKitFactory.PreferredFont(textStyle),
        AdjustsFontForContentSizeCategory = true,
        TextColor = color,
        TextAlignment = UITextAlignment.Center,
        Lines = 0,
        AccessibilityIdentifier = identifier,
    };

    private sealed record LegacyContentState(UIView Root, UIView Content, UIView? PreviousBackground)
    {
        public void Remove()
        {
            if (Root is UITableView table && ReferenceEquals(table.BackgroundView, Content))
            {
                table.BackgroundView = PreviousBackground;
            }
            else if (Root is UICollectionView collection && ReferenceEquals(collection.BackgroundView, Content))
            {
                collection.BackgroundView = PreviousBackground;
            }
            else
            {
                Content.RemoveFromSuperview();
            }
        }
    }
}
