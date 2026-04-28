using System;

namespace AcadsJulie.Models
{
    public class Achievement
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; }  = string.Empty;
        public string Icon { get; set; } = "🏅";
        public bool IsUnlocked { get; set; }
        public string Condition { get; set; } = string.Empty;
    }
}
