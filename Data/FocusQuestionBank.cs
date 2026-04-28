using System.Collections.Generic;

namespace AcadsJulie.Data
{
    public class FocusQuestion
    {
        public string QuestionText { get; set; } = string.Empty;
        public List<string> Options { get; set; } = new List<string>();
        public string CorrectAnswer { get; set; } = string.Empty;
    }

    public static class FocusQuestionBank
    {
        public static List<FocusQuestion> All { get; } = new List<FocusQuestion>
        {
            new FocusQuestion { QuestionText = "Which animal is NOT a mammal?", Options = new List<string> { "Dolphin", "Whale", "Shark", "Bat" }, CorrectAnswer = "Shark" },
            new FocusQuestion { QuestionText = "Which of these is NOT a primary color?", Options = new List<string> { "Red", "Blue", "Yellow", "Green" }, CorrectAnswer = "Green" },
            new FocusQuestion { QuestionText = "Which of these is NOT a planet?", Options = new List<string> { "Mars", "Venus", "Pluto", "Jupiter" }, CorrectAnswer = "Pluto" },
            new FocusQuestion { QuestionText = "Which shape has 5 sides?", Options = new List<string> { "Hexagon", "Pentagon", "Octagon", "Square" }, CorrectAnswer = "Pentagon" },
            new FocusQuestion { QuestionText = "Which direction is opposite to North-West?", Options = new List<string> { "South-East", "South-West", "North-East", "South" }, CorrectAnswer = "South-East" },
            new FocusQuestion { QuestionText = "Which number is NOT a prime number?", Options = new List<string> { "2", "3", "7", "9" }, CorrectAnswer = "9" },
            new FocusQuestion { QuestionText = "Which is NOT a continent?", Options = new List<string> { "Asia", "Europe", "Russia", "Africa" }, CorrectAnswer = "Russia" },
            new FocusQuestion { QuestionText = "Which instrument is NOT a string instrument?", Options = new List<string> { "Violin", "Guitar", "Cello", "Flute" }, CorrectAnswer = "Flute" },
            new FocusQuestion { QuestionText = "Which bird cannot fly?", Options = new List<string> { "Eagle", "Penguin", "Parrot", "Sparrow" }, CorrectAnswer = "Penguin" },
            new FocusQuestion { QuestionText = "Which metric unit measures weight?", Options = new List<string> { "Liter", "Meter", "Gram", "Celsius" }, CorrectAnswer = "Gram" },
            new FocusQuestion { QuestionText = "Which of these is NOT a programming language?", Options = new List<string> { "Python", "Java", "C++", "Cobra" }, CorrectAnswer = "Cobra" },
            new FocusQuestion { QuestionText = "Which element's symbol is O?", Options = new List<string> { "Osmium", "Oxygen", "Gold", "Iron" }, CorrectAnswer = "Oxygen" },
            new FocusQuestion { QuestionText = "Which color is found at the top of a rainbow?", Options = new List<string> { "Red", "Violet", "Green", "Blue" }, CorrectAnswer = "Red" },
            new FocusQuestion { QuestionText = "Which of these is a gas at room temperature?", Options = new List<string> { "Water", "Oxygen", "Mercury", "Iron" }, CorrectAnswer = "Oxygen" },
            new FocusQuestion { QuestionText = "Which is NOT a noble gas?", Options = new List<string> { "Helium", "Neon", "Nitrogen", "Argon" }, CorrectAnswer = "Nitrogen" },
            new FocusQuestion { QuestionText = "Which is an odd number?", Options = new List<string> { "12", "14", "22", "15" }, CorrectAnswer = "15" },
            new FocusQuestion { QuestionText = "Which tree produces acorns?", Options = new List<string> { "Pine", "Oak", "Maple", "Birch" }, CorrectAnswer = "Oak" },
            new FocusQuestion { QuestionText = "Which is the largest ocean?", Options = new List<string> { "Atlantic", "Indian", "Arctic", "Pacific" }, CorrectAnswer = "Pacific" },
            new FocusQuestion { QuestionText = "Which of these is a vegetable?", Options = new List<string> { "Apple", "Banana", "Carrot", "Strawberry" }, CorrectAnswer = "Carrot" },
            new FocusQuestion { QuestionText = "Which is NOT a season?", Options = new List<string> { "Spring", "Autumn", "Winter", "Monsoon" }, CorrectAnswer = "Monsoon" }
        };
    }
}
