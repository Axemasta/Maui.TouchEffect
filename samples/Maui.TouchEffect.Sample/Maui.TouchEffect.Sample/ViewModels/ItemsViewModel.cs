#nullable enable

using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.Extensions.Logging;
namespace Maui.TouchEffect.Sample.ViewModels;

public partial class ItemsViewModel : ObservableObject
{
    private readonly ILogger logger;

    public ObservableCollection<DisplayItem> Items { get; }

    public ItemsViewModel(ILogger<ItemsViewModel> logger)
    {
        this.logger = logger;
        
        Items = new ObservableCollection<DisplayItem>
        {
            new DisplayItem
            {
                Title = "Nintendo",
                SubTitle =
                    "Nintendo Co., Ltd. is a Japanese multinational video game company headquartered in Kyoto. It develops, publishes and releases both video games and video game consoles. Nintendo was founded in 1889 as Nintendo Koppai by craftsman Fusajiro Yamauchi and originally produced handmade hanafuda playing cards.",
                Color = Color.FromArgb("#E60012"),
            },
            new DisplayItem
            {
                Title = "Capcom",
                SubTitle =
                    "Capcom Co., Ltd. is a Japanese video game company. It has created a number of multi-million-selling game franchises, with its most commercially successful being Resident Evil, Monster Hunter, Street Fighter, Mega Man, Devil May Cry, Dead Rising, Ace Attorney, and Marvel vs. Capcom.",
                Color = Color.FromArgb("#ffcb08"),
            },
            new DisplayItem
            {
                Title = "FromSoftware",
                SubTitle =
                    "FromSoftware, Inc. is a Japanese video game development and publishing company. It was founded by Naotoshi Zin in Tokyo on November 1, 1986. Initially a developer of business software, the company released their first video game, King's Field, for the PlayStation in 1994.",
                Color = Colors.Black,
            },
            new DisplayItem
            {
                Title = "Jagex",
                SubTitle =
                    "Jagex Limited is a British video game developer and publisher based at the Cambridge Science Park in Cambridge, England. It is best known for RuneScape and Old School RuneScape, both free-to-play massively multiplayer online role-playing games.",
                Color = Color.FromArgb("#A4CF5B"),
            },
            new DisplayItem
            {
                Title = "Blizzard",
                SubTitle = "Blizzard Entertainment, Inc. is an American video game developer and publisher based in Irvine, California.",
                Color = Color.FromArgb("#009AE4"),
            },
            new DisplayItem
            {
                Title = "Devolver Digital",
                SubTitle = "Devolver Digital, Inc. is an American video game publisher based in Austin, Texas, specializing in the publishing of indie games. ",
                Color = Color.FromArgb("#DC4144"),
            },
        };
    }

    [RelayCommand]
    private void ItemSelected(DisplayItem? item)
    {
        if (item is null)
        {
            logger.LogWarning("Selected item was null");
            return;
        }

        logger.LogInformation("Selected {Title}!", item.Title);
    }
}

public class DisplayItem
{
    public required string Title { get; set; }

    public required Color Color { get; set; }

    public required string SubTitle { get; set; }
}