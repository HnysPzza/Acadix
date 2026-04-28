using CommunityToolkit.Mvvm.ComponentModel;

namespace AcadsJulie.Models
{
    public partial class CardItem : ObservableObject
    {
        public int Id { get; set; }
        public string Emoji { get; set; } = string.Empty;

        [ObservableProperty]
        private bool _isFlipped;

        [ObservableProperty]
        private bool _isMatched;
    }
}
