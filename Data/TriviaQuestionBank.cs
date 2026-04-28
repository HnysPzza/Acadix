using AcadsJulie.Models;

namespace AcadsJulie.Data;

public static class TriviaQuestionBank
{
    private static readonly Dictionary<string, HashSet<string>> LastQuestionSets = [];

    private static readonly List<TriviaQuestion> All = [
        // World History
        new TriviaQuestion { QuestionText = "In what year did World War II end?", Options = ["1943", "1944", "1945", "1946"], CorrectAnswer = "1945", Field = "History", SubField = "WorldHistory", Difficulty = "Easy", Depth = "General" },
        new TriviaQuestion { QuestionText = "Who was the first President of the United States?", Options = ["John Adams", "Thomas Jefferson", "George Washington", "Benjamin Franklin"], CorrectAnswer = "George Washington", Field = "History", SubField = "WorldHistory", Difficulty = "Easy", Depth = "General" },
        new TriviaQuestion { QuestionText = "Which document was signed by King John in 1215?", Options = ["Magna Carta", "Treaty of Versailles", "Code of Hammurabi", "Edict of Nantes"], CorrectAnswer = "Magna Carta", Field = "History", SubField = "WorldHistory", Difficulty = "Easy", Depth = "General" },
        new TriviaQuestion { QuestionText = "Which empire conquered Constantinople in 1453?", Options = ["Ottoman Empire", "Mongol Empire", "Roman Empire", "Persian Empire"], CorrectAnswer = "Ottoman Empire", Field = "History", SubField = "WorldHistory", Difficulty = "Easy", Depth = "General" },
        new TriviaQuestion { QuestionText = "Who founded the Mongol Empire?", Options = ["Kublai Khan", "Genghis Khan", "Tamerlane", "Attila"], CorrectAnswer = "Genghis Khan", Field = "History", SubField = "WorldHistory", Difficulty = "Easy", Depth = "General" },
        new TriviaQuestion { QuestionText = "What city was stormed on July 14, 1789?", Options = ["Bastille", "Versailles", "Waterloo", "Vienna"], CorrectAnswer = "Bastille", Field = "History", SubField = "WorldHistory", Difficulty = "Easy", Depth = "General" },
        new TriviaQuestion { QuestionText = "Which war ended with the Treaty of Versailles in 1919?", Options = ["World War I", "World War II", "Crimean War", "Seven Years' War"], CorrectAnswer = "World War I", Field = "History", SubField = "WorldHistory", Difficulty = "Easy", Depth = "General" },
        new TriviaQuestion { QuestionText = "Which country is most associated with the start of the Industrial Revolution?", Options = ["Britain", "Spain", "China", "Brazil"], CorrectAnswer = "Britain", Field = "History", SubField = "WorldHistory", Difficulty = "Easy", Depth = "General" },
        new TriviaQuestion { QuestionText = "Who improved the steam engine with a separate condenser?", Options = ["James Watt", "Isaac Newton", "Michael Faraday", "Eli Whitney"], CorrectAnswer = "James Watt", Field = "History", SubField = "WorldHistory", Difficulty = "Easy", Depth = "General" },
        new TriviaQuestion { QuestionText = "In which country did the Renaissance begin?", Options = ["France", "England", "Italy", "Spain"], CorrectAnswer = "Italy", Field = "History", SubField = "WorldHistory", Difficulty = "Medium", Depth = "General" },
        new TriviaQuestion { QuestionText = "Who built the Great Wall of China?", Options = ["Qin Shi Huang", "Genghis Khan", "Confucius", "Marco Polo"], CorrectAnswer = "Qin Shi Huang", Field = "History", SubField = "WorldHistory", Difficulty = "Medium", Depth = "General" },
        new TriviaQuestion { QuestionText = "Which empire was ruled by Suleiman the Magnificent?", Options = ["Roman", "Byzantine", "Ottoman", "Mughal"], CorrectAnswer = "Ottoman", Field = "History", SubField = "WorldHistory", Difficulty = "Hard", Depth = "Deep" },
        new TriviaQuestion { QuestionText = "When did the French Revolution begin?", Options = ["1776", "1789", "1792", "1804"], CorrectAnswer = "1789", Field = "History", SubField = "WorldHistory", Difficulty = "Medium", Depth = "General" },
        new TriviaQuestion { QuestionText = "Who discovered America in 1492?", Options = ["Amerigo Vespucci", "Christopher Columbus", "Ferdinand Magellan", "Vasco da Gama"], CorrectAnswer = "Christopher Columbus", Field = "History", SubField = "WorldHistory", Difficulty = "Easy", Depth = "General" },
        new TriviaQuestion { QuestionText = "What was abolished by France's National Constituent Assembly on August 4, 1789?", Options = ["Feudal privileges", "The monarchy", "The army", "The metric system"], CorrectAnswer = "Feudal privileges", Field = "History", SubField = "WorldHistory", Difficulty = "Medium", Depth = "General" },
        new TriviaQuestion { QuestionText = "Who conquered Constantinople in 1453?", Options = ["Mehmed II", "Suleiman I", "Osman I", "Selim II"], CorrectAnswer = "Mehmed II", Field = "History", SubField = "WorldHistory", Difficulty = "Medium", Depth = "General" },
        new TriviaQuestion { QuestionText = "In what year did Genghis Khan found the Mongol Empire?", Options = ["1066", "1206", "1271", "1453"], CorrectAnswer = "1206", Field = "History", SubField = "WorldHistory", Difficulty = "Medium", Depth = "General" },
        new TriviaQuestion { QuestionText = "Where was Magna Carta sealed?", Options = ["Runnymede", "Canterbury", "York", "Westminster"], CorrectAnswer = "Runnymede", Field = "History", SubField = "WorldHistory", Difficulty = "Medium", Depth = "General" },
        new TriviaQuestion { QuestionText = "Which palace hosted the signing of the Treaty of Versailles?", Options = ["Palace of Versailles", "Buckingham Palace", "Winter Palace", "Schonbrunn Palace"], CorrectAnswer = "Palace of Versailles", Field = "History", SubField = "WorldHistory", Difficulty = "Medium", Depth = "General" },
        new TriviaQuestion { QuestionText = "Which declaration was adopted during the French Revolution on August 26, 1789?", Options = ["Declaration of the Rights of Man and of the Citizen", "Declaration of Independence", "Bill of Rights", "Edict of Milan"], CorrectAnswer = "Declaration of the Rights of Man and of the Citizen", Field = "History", SubField = "WorldHistory", Difficulty = "Medium", Depth = "General" },
        new TriviaQuestion { QuestionText = "Which Byzantine emperor died during the fall of Constantinople?", Options = ["Constantine XI Palaeologus", "Justinian I", "Basil II", "Alexios I Komnenos"], CorrectAnswer = "Constantine XI Palaeologus", Field = "History", SubField = "WorldHistory", Difficulty = "Hard", Depth = "Deep" },
        new TriviaQuestion { QuestionText = "How long did the Ottoman siege of Constantinople last in 1453?", Options = ["55 days", "12 days", "100 days", "6 months"], CorrectAnswer = "55 days", Field = "History", SubField = "WorldHistory", Difficulty = "Hard", Depth = "Deep" },
        new TriviaQuestion { QuestionText = "What title did Mehmed II claim after taking Constantinople?", Options = ["Kayser-i Rum", "Shahanshah", "Caliph of Cordoba", "Lord Protector"], CorrectAnswer = "Kayser-i Rum", Field = "History", SubField = "WorldHistory", Difficulty = "Hard", Depth = "Deep" },
        new TriviaQuestion { QuestionText = "What was the approximate maximum size of the Mongol Empire according to Britannica?", Options = ["9 million square miles", "1 million square miles", "3 million square miles", "15 million square miles"], CorrectAnswer = "9 million square miles", Field = "History", SubField = "WorldHistory", Difficulty = "Hard", Depth = "Deep" },
        new TriviaQuestion { QuestionText = "Which Mongol khanate was also known as the Kipchak Khanate?", Options = ["Golden Horde", "Il-Khanate", "Chagatai Khanate", "Yuan dynasty"], CorrectAnswer = "Golden Horde", Field = "History", SubField = "WorldHistory", Difficulty = "Hard", Depth = "Deep" },
        new TriviaQuestion { QuestionText = "Which clause principle made Magna Carta historically important to later constitutionalism?", Options = ["The sovereign is subject to the rule of law", "All nobles lost their titles", "Parliament became bicameral", "England became a republic"], CorrectAnswer = "The sovereign is subject to the rule of law", Field = "History", SubField = "WorldHistory", Difficulty = "Hard", Depth = "Deep" },
        new TriviaQuestion { QuestionText = "What technology did the Ottomans use heavily to breach Constantinople's walls?", Options = ["Cannons", "Longbows", "Submarines", "War elephants"], CorrectAnswer = "Cannons", Field = "History", SubField = "WorldHistory", Difficulty = "Hard", Depth = "Deep" },
        new TriviaQuestion { QuestionText = "Which device made Watt's steam engine more efficient than earlier pumping engines?", Options = ["Separate condenser", "Internal combustion chamber", "Electric dynamo", "Water turbine"], CorrectAnswer = "Separate condenser", Field = "History", SubField = "WorldHistory", Difficulty = "Hard", Depth = "Deep" },
        new TriviaQuestion { QuestionText = "When did the Treaty of Versailles go into effect?", Options = ["January 10, 1920", "June 28, 1919", "November 11, 1918", "September 1, 1939"], CorrectAnswer = "January 10, 1920", Field = "History", SubField = "WorldHistory", Difficulty = "Hard", Depth = "Deep" },

        // Philippine History
        new TriviaQuestion { QuestionText = "In what year did the Philippines gain independence from the United States?", Options = ["1945", "1946", "1947", "1948"], CorrectAnswer = "1946", Field = "History", SubField = "PhilippineHistory", Difficulty = "Easy", Depth = "General" },
        new TriviaQuestion { QuestionText = "Who is the national hero of the Philippines?", Options = ["Andres Bonifacio", "Emilio Aguinaldo", "Jose Rizal", "Apolinario Mabini"], CorrectAnswer = "Jose Rizal", Field = "History", SubField = "PhilippineHistory", Difficulty = "Easy", Depth = "General" },
        new TriviaQuestion { QuestionText = "Who was known as the Lakambini of the Katipunan?", Options = ["Gregoria de Jesus", "Melchora Aquino", "Josefa Rizal", "Marina Dizon"], CorrectAnswer = "Gregoria de Jesus", Field = "History", SubField = "PhilippineHistory", Difficulty = "Easy", Depth = "General" },
        new TriviaQuestion { QuestionText = "What secret society was founded on July 7, 1892?", Options = ["Katipunan", "La Liga Filipina", "Propaganda Movement", "Malolos Congress"], CorrectAnswer = "Katipunan", Field = "History", SubField = "PhilippineHistory", Difficulty = "Easy", Depth = "General" },
        new TriviaQuestion { QuestionText = "Who founded La Liga Filipina?", Options = ["Jose Rizal", "Andres Bonifacio", "Emilio Aguinaldo", "Marcelo H. del Pilar"], CorrectAnswer = "Jose Rizal", Field = "History", SubField = "PhilippineHistory", Difficulty = "Easy", Depth = "General" },
        new TriviaQuestion { QuestionText = "Which revolutionary leader is called the Father of the Philippine Revolution?", Options = ["Andres Bonifacio", "Jose Rizal", "Apolinario Mabini", "Antonio Luna"], CorrectAnswer = "Andres Bonifacio", Field = "History", SubField = "PhilippineHistory", Difficulty = "Easy", Depth = "General" },
        new TriviaQuestion { QuestionText = "What certificates were torn during the Cry of Pugad Lawin?", Options = ["Cedulas", "Passports", "Ballots", "Land titles"], CorrectAnswer = "Cedulas", Field = "History", SubField = "PhilippineHistory", Difficulty = "Easy", Depth = "General" },
        new TriviaQuestion { QuestionText = "Where was the First Philippine Republic inaugurated?", Options = ["Malolos", "Kawit", "Manila", "Dapitan"], CorrectAnswer = "Malolos", Field = "History", SubField = "PhilippineHistory", Difficulty = "Easy", Depth = "General" },
        new TriviaQuestion { QuestionText = "Which revolutionary group did Emilio Jacinto join as a teenager?", Options = ["Katipunan", "Guardia Civil", "Malolos Congress", "Federal Party"], CorrectAnswer = "Katipunan", Field = "History", SubField = "PhilippineHistory", Difficulty = "Easy", Depth = "General" },
        new TriviaQuestion { QuestionText = "When was the EDSA Revolution?", Options = ["1984", "1985", "1986", "1987"], CorrectAnswer = "1986", Field = "History", SubField = "PhilippineHistory", Difficulty = "Medium", Depth = "General" },
        new TriviaQuestion { QuestionText = "Which Spanish governor-general initiated the Galleon trade?", Options = ["Miguel Lopez de Legazpi", "Antonio de Morga", "Juan de Salcedo", "Diego de los Rios"], CorrectAnswer = "Miguel Lopez de Legazpi", Field = "History", SubField = "PhilippineHistory", Difficulty = "Hard", Depth = "Deep" },
        new TriviaQuestion { QuestionText = "What was the first Filipino novel written in Spanish?", Options = ["Noli Me Tangere", "El Filibusterismo", "Florante at Laura", "Mi Ultimo Adios"], CorrectAnswer = "Noli Me Tangere", Field = "History", SubField = "PhilippineHistory", Difficulty = "Medium", Depth = "General" },
        new TriviaQuestion { QuestionText = "Who led the Katipunan during the Philippine Revolution?", Options = ["Jose Rizal", "Andres Bonifacio", "Apolinario Mabini", "Manuel Quezon"], CorrectAnswer = "Andres Bonifacio", Field = "History", SubField = "PhilippineHistory", Difficulty = "Easy", Depth = "General" },
        new TriviaQuestion { QuestionText = "What was the official publication of the Katipunan?", Options = ["La Solidaridad", "Kalayaan", "La Independencia", "El Renacimiento"], CorrectAnswer = "Kalayaan", Field = "History", SubField = "PhilippineHistory", Difficulty = "Medium", Depth = "General" },
        new TriviaQuestion { QuestionText = "Who edited Kalayaan, the Katipunan newspaper?", Options = ["Apolinario Mabini", "Emilio Jacinto", "Graciano Lopez Jaena", "Antonio Luna"], CorrectAnswer = "Emilio Jacinto", Field = "History", SubField = "PhilippineHistory", Difficulty = "Hard", Depth = "Deep" },
        new TriviaQuestion { QuestionText = "At the Tejeros Convention, which office was Andres Bonifacio elected to?", Options = ["President", "Vice-President", "Director of the Interior", "Captain General"], CorrectAnswer = "Director of the Interior", Field = "History", SubField = "PhilippineHistory", Difficulty = "Hard", Depth = "Deep" },
        new TriviaQuestion { QuestionText = "Who challenged Bonifacio's election as Director of the Interior at Tejeros?", Options = ["Daniel Tirona", "Mariano Trias", "Artemio Ricarte", "Emiliano Riego de Dios"], CorrectAnswer = "Daniel Tirona", Field = "History", SubField = "PhilippineHistory", Difficulty = "Hard", Depth = "Deep" },
        new TriviaQuestion { QuestionText = "Which document did Bonifacio use to declare the Tejeros proceedings null and void?", Options = ["Acta de Tejeros", "Pact of Biak-na-Bato", "Malolos Constitution", "Kartilya ng Katipunan"], CorrectAnswer = "Acta de Tejeros", Field = "History", SubField = "PhilippineHistory", Difficulty = "Hard", Depth = "Deep" },
        new TriviaQuestion { QuestionText = "Which agreement terminated the Biak-na-Bato Republic in December 1897?", Options = ["Treaty of Paris", "Pact of Biak-na-Bato", "Naic Pact", "Bates Treaty"], CorrectAnswer = "Pact of Biak-na-Bato", Field = "History", SubField = "PhilippineHistory", Difficulty = "Hard", Depth = "Deep" },
        new TriviaQuestion { QuestionText = "When was the First Philippine Republic inaugurated in Malolos?", Options = ["June 12, 1898", "January 23, 1899", "March 22, 1897", "December 15, 1897"], CorrectAnswer = "January 23, 1899", Field = "History", SubField = "PhilippineHistory", Difficulty = "Hard", Depth = "Deep" },
        new TriviaQuestion { QuestionText = "Who was elected President by the Tejeros Convention?", Options = ["Andres Bonifacio", "Emilio Aguinaldo", "Mariano Trias", "Artemio Ricarte"], CorrectAnswer = "Emilio Aguinaldo", Field = "History", SubField = "PhilippineHistory", Difficulty = "Medium", Depth = "General" },
        new TriviaQuestion { QuestionText = "Who was elected Vice-President at the Tejeros Convention?", Options = ["Mariano Trias", "Apolinario Mabini", "Daniel Tirona", "Antonio Luna"], CorrectAnswer = "Mariano Trias", Field = "History", SubField = "PhilippineHistory", Difficulty = "Hard", Depth = "Deep" },
        new TriviaQuestion { QuestionText = "Who was elected Captain General at the Tejeros Convention?", Options = ["Artemio Ricarte", "Emiliano Riego de Dios", "Mariano Noriel", "Pio del Pilar"], CorrectAnswer = "Artemio Ricarte", Field = "History", SubField = "PhilippineHistory", Difficulty = "Hard", Depth = "Deep" },
        new TriviaQuestion { QuestionText = "Who was elected Director of War at the Tejeros Convention?", Options = ["Emiliano Riego de Dios", "Artemio Ricarte", "Mariano Alvarez", "Baldomero Aguinaldo"], CorrectAnswer = "Emiliano Riego de Dios", Field = "History", SubField = "PhilippineHistory", Difficulty = "Hard", Depth = "Deep" },
        new TriviaQuestion { QuestionText = "Where were Andres and Procopio Bonifacio executed?", Options = ["Mt. Nagpatong", "Tirad Pass", "Biak-na-Bato", "Pugad Lawin"], CorrectAnswer = "Mt. Nagpatong", Field = "History", SubField = "PhilippineHistory", Difficulty = "Hard", Depth = "Deep" },
        new TriviaQuestion { QuestionText = "Who led the court martial that tried Andres and Procopio Bonifacio?", Options = ["Mariano Noriel", "Antonio Luna", "Apolinario Mabini", "Gregorio del Pilar"], CorrectAnswer = "Mariano Noriel", Field = "History", SubField = "PhilippineHistory", Difficulty = "Hard", Depth = "Deep" },
        new TriviaQuestion { QuestionText = "Which Katipunan leader used the alias Pingkian and pen name Dimasilaw?", Options = ["Emilio Jacinto", "Andres Bonifacio", "Pio Valenzuela", "Ladislao Diwa"], CorrectAnswer = "Emilio Jacinto", Field = "History", SubField = "PhilippineHistory", Difficulty = "Hard", Depth = "Deep" },
        new TriviaQuestion { QuestionText = "What work earned Emilio Jacinto the title Brains of the Katipunan?", Options = ["Kartilya ng Katipunan", "Noli Me Tangere", "La Solidaridad", "True Decalogue"], CorrectAnswer = "Kartilya ng Katipunan", Field = "History", SubField = "PhilippineHistory", Difficulty = "Medium", Depth = "General" },
        new TriviaQuestion { QuestionText = "What was Gregoria de Jesus's Katipunan nom de guerre?", Options = ["Lakambini", "Tandang Sora", "Henerala", "Dimasilaw"], CorrectAnswer = "Lakambini", Field = "History", SubField = "PhilippineHistory", Difficulty = "Medium", Depth = "General" },
        new TriviaQuestion { QuestionText = "Who was the first woman named among early Katipunan members in NHCP's Bonifacio article?", Options = ["Gregoria de Jesus", "Gabriela Silang", "Marcela Agoncillo", "Teresa Magbanua"], CorrectAnswer = "Gregoria de Jesus", Field = "History", SubField = "PhilippineHistory", Difficulty = "Medium", Depth = "General" },
        new TriviaQuestion { QuestionText = "Which publication carried Bonifacio's 'Pag-ibig sa Tinubuang Lupa'?", Options = ["Kalayaan", "La Solidaridad", "La Independencia", "El Renacimiento"], CorrectAnswer = "Kalayaan", Field = "History", SubField = "PhilippineHistory", Difficulty = "Medium", Depth = "General" },
        new TriviaQuestion { QuestionText = "Which event exposed a power struggle between Magdiwang and Magdalo factions?", Options = ["Tejeros Convention", "EDSA Revolution", "Battle of Mactan", "Pact of Paris"], CorrectAnswer = "Tejeros Convention", Field = "History", SubField = "PhilippineHistory", Difficulty = "Medium", Depth = "General" },
        new TriviaQuestion { QuestionText = "Which periodical did Jose Palma write for before composing the national anthem's lyrics?", Options = ["La Independencia", "Kalayaan", "La Solidaridad", "El Filibusterismo"], CorrectAnswer = "La Independencia", Field = "History", SubField = "PhilippineHistory", Difficulty = "Hard", Depth = "Deep" },
        new TriviaQuestion { QuestionText = "What was the Katipunan's initiation ritual symbolized by?", Options = ["Blood compact", "Public oath at a church", "Military salute", "Cedula registration"], CorrectAnswer = "Blood compact", Field = "History", SubField = "PhilippineHistory", Difficulty = "Medium", Depth = "General" },
        new TriviaQuestion { QuestionText = "Who was the first Supremo of the Katipunan?", Options = ["Deodato Arellano", "Roman Basa", "Andres Bonifacio", "Emilio Jacinto"], CorrectAnswer = "Deodato Arellano", Field = "History", SubField = "PhilippineHistory", Difficulty = "Hard", Depth = "Deep" },
        new TriviaQuestion { QuestionText = "Who succeeded Deodato Arellano as Supremo before Bonifacio?", Options = ["Roman Basa", "Teodoro Plata", "Ladislao Diwa", "Valentin Diaz"], CorrectAnswer = "Roman Basa", Field = "History", SubField = "PhilippineHistory", Difficulty = "Hard", Depth = "Deep" },
        new TriviaQuestion { QuestionText = "Which cave is associated with the Katipunan's 1895 'Viva la Independencia de Filipinas' inscription?", Options = ["Pamitinan Cave", "Tabon Cave", "Callao Cave", "Sumaguing Cave"], CorrectAnswer = "Pamitinan Cave", Field = "History", SubField = "PhilippineHistory", Difficulty = "Hard", Depth = "Deep" },
        new TriviaQuestion { QuestionText = "Which ancient civilization built Machu Picchu?", Options = ["Maya", "Aztec", "Inca", "Olmec"], CorrectAnswer = "Inca", Field = "History", SubField = "WorldHistory", Difficulty = "Medium", Depth = "General" },
        new TriviaQuestion { QuestionText = "Who was the British prime minister during most of World War II?", Options = ["Neville Chamberlain", "Winston Churchill", "Clement Attlee", "Anthony Eden"], CorrectAnswer = "Winston Churchill", Field = "History", SubField = "WorldHistory", Difficulty = "Medium", Depth = "General" },

        // Math
        new TriviaQuestion { QuestionText = "What is 15% of 200?", Options = ["25", "30", "35", "40"], CorrectAnswer = "30", Field = "Math", SubField = "Arithmetic", Difficulty = "Easy", Depth = "Beginner" },
        new TriviaQuestion { QuestionText = "What is the square root of 144?", Options = ["10", "11", "12", "13"], CorrectAnswer = "12", Field = "Math", SubField = "Arithmetic", Difficulty = "Easy", Depth = "Beginner" },
        new TriviaQuestion { QuestionText = "If x + 5 = 12, what is x?", Options = ["5", "6", "7", "8"], CorrectAnswer = "7", Field = "Math", SubField = "Algebra", Difficulty = "Easy", Depth = "General" },
        new TriviaQuestion { QuestionText = "How many degrees are in a triangle?", Options = ["90", "180", "270", "360"], CorrectAnswer = "180", Field = "Math", SubField = "Geometry", Difficulty = "Easy", Depth = "General" },
        new TriviaQuestion { QuestionText = "What is 2^5?", Options = ["16", "25", "32", "64"], CorrectAnswer = "32", Field = "Math", SubField = "Arithmetic", Difficulty = "Medium", Depth = "General" },
        new TriviaQuestion { QuestionText = "In a right triangle with legs 3 and 4, what is the hypotenuse?", Options = ["5", "6", "7", "8"], CorrectAnswer = "5", Field = "Math", SubField = "Geometry", Difficulty = "Medium", Depth = "General" },
        new TriviaQuestion { QuestionText = "Solve for x: 2x - 7 = 11", Options = ["7", "8", "9", "10"], CorrectAnswer = "9", Field = "Math", SubField = "Algebra", Difficulty = "Medium", Depth = "General" },
        new TriviaQuestion { QuestionText = "What is the area of a circle with radius 7? (Use π ≈ 3.14)", Options = ["153.86", "154.86", "155.86", "156.86"], CorrectAnswer = "153.86", Field = "Math", SubField = "Geometry", Difficulty = "Hard", Depth = "Deep" },

        new TriviaQuestion { QuestionText = "What is 9 x 8?", Options = ["63", "72", "81", "89"], CorrectAnswer = "72", Field = "Math", SubField = "Arithmetic", Difficulty = "Easy", Depth = "Beginner" },
        new TriviaQuestion { QuestionText = "What is the value of 3 squared plus 4 squared?", Options = ["12", "18", "25", "49"], CorrectAnswer = "25", Field = "Math", SubField = "Algebra", Difficulty = "Medium", Depth = "General" },
        new TriviaQuestion { QuestionText = "What is 45 divided by 5?", Options = ["7", "8", "9", "10"], CorrectAnswer = "9", Field = "Math", SubField = "Arithmetic", Difficulty = "Easy", Depth = "Beginner" },
        new TriviaQuestion { QuestionText = "What is the perimeter of a square with side length 6?", Options = ["12", "18", "24", "36"], CorrectAnswer = "24", Field = "Math", SubField = "Geometry", Difficulty = "Easy", Depth = "General" },
        new TriviaQuestion { QuestionText = "Simplify 3x + 2x.", Options = ["5", "5x", "6x", "x5"], CorrectAnswer = "5x", Field = "Math", SubField = "Algebra", Difficulty = "Easy", Depth = "General" },
        new TriviaQuestion { QuestionText = "What is 7 cubed?", Options = ["49", "147", "343", "512"], CorrectAnswer = "343", Field = "Math", SubField = "Arithmetic", Difficulty = "Medium", Depth = "General" },
        new TriviaQuestion { QuestionText = "What is the median of 2, 5, 9, 12, and 20?", Options = ["5", "9", "10", "12"], CorrectAnswer = "9", Field = "Math", SubField = "Statistics", Difficulty = "Medium", Depth = "General" },

        // Science
        new TriviaQuestion { QuestionText = "What is the chemical symbol for gold?", Options = ["Go", "Gd", "Au", "Ag"], CorrectAnswer = "Au", Field = "Science", SubField = "Chemistry", Difficulty = "Easy", Depth = "Beginner" },
        new TriviaQuestion { QuestionText = "What is the speed of light in a vacuum (approximately)?", Options = ["300,000 km/s", "150,000 km/s", "500,000 km/s", "100,000 km/s"], CorrectAnswer = "300,000 km/s", Field = "Science", SubField = "Physics", Difficulty = "Easy", Depth = "General" },
        new TriviaQuestion { QuestionText = "What is the smallest unit of life?", Options = ["Atom", "Molecule", "Cell", "Organ"], CorrectAnswer = "Cell", Field = "Science", SubField = "General", Difficulty = "Easy", Depth = "Beginner" },
        new TriviaQuestion { QuestionText = "What planet is known as the Red Planet?", Options = ["Venus", "Mars", "Jupiter", "Saturn"], CorrectAnswer = "Mars", Field = "Science", SubField = "General", Difficulty = "Easy", Depth = "General" },
        new TriviaQuestion { QuestionText = "What is the atomic number of carbon?", Options = ["4", "5", "6", "7"], CorrectAnswer = "6", Field = "Science", SubField = "Chemistry", Difficulty = "Medium", Depth = "General" },
        new TriviaQuestion { QuestionText = "What force keeps planets in orbit around the sun?", Options = ["Magnetism", "Gravity", "Friction", "Centrifugal force"], CorrectAnswer = "Gravity", Field = "Science", SubField = "Physics", Difficulty = "Easy", Depth = "General" },
        new TriviaQuestion { QuestionText = "What is H2O commonly known as?", Options = ["Salt", "Sugar", "Water", "Alcohol"], CorrectAnswer = "Water", Field = "Science", SubField = "Chemistry", Difficulty = "Easy", Depth = "Beginner" },

        new TriviaQuestion { QuestionText = "What gas do plants absorb during photosynthesis?", Options = ["Oxygen", "Carbon dioxide", "Nitrogen", "Helium"], CorrectAnswer = "Carbon dioxide", Field = "Science", SubField = "Biology", Difficulty = "Easy", Depth = "General" },
        new TriviaQuestion { QuestionText = "Which part of an atom has a negative charge?", Options = ["Proton", "Neutron", "Electron", "Nucleus"], CorrectAnswer = "Electron", Field = "Science", SubField = "Physics", Difficulty = "Medium", Depth = "General" },
        new TriviaQuestion { QuestionText = "What scale is commonly used to measure earthquake magnitude?", Options = ["Beaufort", "Richter", "Celsius", "Kelvin"], CorrectAnswer = "Richter", Field = "Science", SubField = "EarthScience", Difficulty = "Medium", Depth = "General" },
        new TriviaQuestion { QuestionText = "What is the chemical symbol for oxygen?", Options = ["Ox", "O", "Og", "Om"], CorrectAnswer = "O", Field = "Science", SubField = "Chemistry", Difficulty = "Easy", Depth = "Beginner" },
        new TriviaQuestion { QuestionText = "What tool is used to measure temperature?", Options = ["Barometer", "Thermometer", "Hygrometer", "Anemometer"], CorrectAnswer = "Thermometer", Field = "Science", SubField = "General", Difficulty = "Easy", Depth = "Beginner" },
        new TriviaQuestion { QuestionText = "What type of energy is stored in food?", Options = ["Chemical", "Sound", "Nuclear", "Light"], CorrectAnswer = "Chemical", Field = "Science", SubField = "General", Difficulty = "Medium", Depth = "General" },
        new TriviaQuestion { QuestionText = "Which planet has the strongest surface gravity among the terrestrial planets?", Options = ["Mercury", "Venus", "Earth", "Mars"], CorrectAnswer = "Earth", Field = "Science", SubField = "Physics", Difficulty = "Medium", Depth = "General" },
        new TriviaQuestion { QuestionText = "What is the pH of a neutral solution?", Options = ["0", "3", "7", "14"], CorrectAnswer = "7", Field = "Science", SubField = "Chemistry", Difficulty = "Medium", Depth = "General" },

        // Space
        new TriviaQuestion { QuestionText = "What is the largest planet in our solar system?", Options = ["Saturn", "Neptune", "Jupiter", "Uranus"], CorrectAnswer = "Jupiter", Field = "Space", SubField = "Astronomy", Difficulty = "Easy", Depth = "General" },
        new TriviaQuestion { QuestionText = "What is a light-year?", Options = ["A year with extra daylight", "Distance light travels in one year", "Speed of light", "A unit of time"], CorrectAnswer = "Distance light travels in one year", Field = "Space", SubField = "Astronomy", Difficulty = "Medium", Depth = "General" },
        new TriviaQuestion { QuestionText = "Which planet has the most rings?", Options = ["Jupiter", "Uranus", "Neptune", "Saturn"], CorrectAnswer = "Saturn", Field = "Space", SubField = "Astronomy", Difficulty = "Easy", Depth = "General" },
        new TriviaQuestion { QuestionText = "What is a black hole?", Options = ["A dark star", "A region with gravity so strong nothing escapes", "A collapsed galaxy", "An empty space"], CorrectAnswer = "A region with gravity so strong nothing escapes", Field = "Space", SubField = "Astronomy", Difficulty = "Medium", Depth = "Deep" },
        new TriviaQuestion { QuestionText = "When did humans first land on the Moon?", Options = ["1967", "1969", "1971", "1973"], CorrectAnswer = "1969", Field = "Space", SubField = "SpaceExploration", Difficulty = "Easy", Depth = "General" },
        new TriviaQuestion { QuestionText = "What galaxy is our Solar System part of?", Options = ["Andromeda", "Milky Way", "Triangulum", "Sombrero"], CorrectAnswer = "Milky Way", Field = "Space", SubField = "Astronomy", Difficulty = "Easy", Depth = "General" },

        new TriviaQuestion { QuestionText = "Which planet is closest to the Sun?", Options = ["Venus", "Earth", "Mercury", "Mars"], CorrectAnswer = "Mercury", Field = "Space", SubField = "Astronomy", Difficulty = "Easy", Depth = "General" },
        new TriviaQuestion { QuestionText = "What is the name of NASA's most famous space telescope launched in 1990?", Options = ["Kepler", "Hubble", "Chandra", "Spitzer"], CorrectAnswer = "Hubble", Field = "Space", SubField = "SpaceExploration", Difficulty = "Medium", Depth = "General" },
        new TriviaQuestion { QuestionText = "What object lies at the center of the Milky Way?", Options = ["A neutron star", "A supermassive black hole", "A red giant", "A white dwarf"], CorrectAnswer = "A supermassive black hole", Field = "Space", SubField = "Astronomy", Difficulty = "Hard", Depth = "Deep" },
        new TriviaQuestion { QuestionText = "Which planet is famous for its Great Red Spot?", Options = ["Mars", "Jupiter", "Saturn", "Neptune"], CorrectAnswer = "Jupiter", Field = "Space", SubField = "Astronomy", Difficulty = "Easy", Depth = "General" },
        new TriviaQuestion { QuestionText = "What is Earth's natural satellite called?", Options = ["The Moon", "Titan", "Europa", "Phobos"], CorrectAnswer = "The Moon", Field = "Space", SubField = "Astronomy", Difficulty = "Easy", Depth = "General" },
        new TriviaQuestion { QuestionText = "Which planet is known for its tilted rotation axis?", Options = ["Mars", "Uranus", "Venus", "Mercury"], CorrectAnswer = "Uranus", Field = "Space", SubField = "Astronomy", Difficulty = "Medium", Depth = "General" },
        new TriviaQuestion { QuestionText = "What do astronauts use to breathe during spacewalks?", Options = ["A parachute", "A space suit", "A telescope", "A rover"], CorrectAnswer = "A space suit", Field = "Space", SubField = "SpaceExploration", Difficulty = "Easy", Depth = "General" },
        new TriviaQuestion { QuestionText = "Which mission first carried humans around the Moon?", Options = ["Apollo 8", "Apollo 10", "Apollo 11", "Gemini 4"], CorrectAnswer = "Apollo 8", Field = "Space", SubField = "SpaceExploration", Difficulty = "Hard", Depth = "Deep" },
        new TriviaQuestion { QuestionText = "What is the Sun mostly made of?", Options = ["Iron and nickel", "Hydrogen and helium", "Oxygen and carbon", "Rock and ice"], CorrectAnswer = "Hydrogen and helium", Field = "Space", SubField = "Astronomy", Difficulty = "Medium", Depth = "General" },

        // Biology
        new TriviaQuestion { QuestionText = "What organ pumps blood through the body?", Options = ["Lungs", "Liver", "Heart", "Kidneys"], CorrectAnswer = "Heart", Field = "Biology", SubField = "HumanBody", Difficulty = "Easy", Depth = "Beginner" },
        new TriviaQuestion { QuestionText = "What is photosynthesis?", Options = ["Making food using light", "Breathing", "Digestion", "Circulation"], CorrectAnswer = "Making food using light", Field = "Biology", SubField = "Ecology", Difficulty = "Easy", Depth = "General" },
        new TriviaQuestion { QuestionText = "What is the powerhouse of the cell?", Options = ["Nucleus", "Ribosome", "Mitochondria", "Golgi apparatus"], CorrectAnswer = "Mitochondria", Field = "Biology", SubField = "Cells", Difficulty = "Medium", Depth = "General" },
        new TriviaQuestion { QuestionText = "DNA stands for?", Options = ["Deoxyribonucleic Acid", "Dynamic Nuclear Acid", "Dual Nucleic Acid", "Dense Nucleotide Array"], CorrectAnswer = "Deoxyribonucleic Acid", Field = "Biology", SubField = "Cells", Difficulty = "Medium", Depth = "Deep" },
        new TriviaQuestion { QuestionText = "What blood type is the universal donor?", Options = ["A", "B", "AB", "O"], CorrectAnswer = "O", Field = "Biology", SubField = "HumanBody", Difficulty = "Medium", Depth = "General" },
        new TriviaQuestion { QuestionText = "How many bones are in the adult human body?", Options = ["186", "206", "226", "246"], CorrectAnswer = "206", Field = "Biology", SubField = "HumanBody", Difficulty = "Hard", Depth = "Deep" },

        new TriviaQuestion { QuestionText = "Which organ is mainly responsible for filtering blood?", Options = ["Heart", "Kidney", "Stomach", "Lung"], CorrectAnswer = "Kidney", Field = "Biology", SubField = "HumanBody", Difficulty = "Easy", Depth = "General" },
        new TriviaQuestion { QuestionText = "What pigment gives plants their green color?", Options = ["Melanin", "Chlorophyll", "Hemoglobin", "Keratin"], CorrectAnswer = "Chlorophyll", Field = "Biology", SubField = "Ecology", Difficulty = "Easy", Depth = "General" },
        new TriviaQuestion { QuestionText = "What is the process of cell division in body cells called?", Options = ["Meiosis", "Mitosis", "Osmosis", "Diffusion"], CorrectAnswer = "Mitosis", Field = "Biology", SubField = "Cells", Difficulty = "Medium", Depth = "General" },
        new TriviaQuestion { QuestionText = "Which body system includes the brain and spinal cord?", Options = ["Digestive", "Nervous", "Circulatory", "Respiratory"], CorrectAnswer = "Nervous", Field = "Biology", SubField = "HumanBody", Difficulty = "Medium", Depth = "General" },
        new TriviaQuestion { QuestionText = "What do red blood cells mainly carry?", Options = ["Oxygen", "Calcium", "Glucose", "Hormones"], CorrectAnswer = "Oxygen", Field = "Biology", SubField = "HumanBody", Difficulty = "Easy", Depth = "General" },
        new TriviaQuestion { QuestionText = "Which molecule stores genetic information?", Options = ["DNA", "ATP", "Water", "Glucose"], CorrectAnswer = "DNA", Field = "Biology", SubField = "Cells", Difficulty = "Easy", Depth = "General" },
        new TriviaQuestion { QuestionText = "What is the study of ecosystems called?", Options = ["Ecology", "Anatomy", "Genetics", "Geology"], CorrectAnswer = "Ecology", Field = "Biology", SubField = "Ecology", Difficulty = "Easy", Depth = "General" },
        new TriviaQuestion { QuestionText = "Which part of a plant absorbs most water?", Options = ["Flower", "Stem", "Root", "Leaf"], CorrectAnswer = "Root", Field = "Biology", SubField = "Ecology", Difficulty = "Easy", Depth = "General" },
        new TriviaQuestion { QuestionText = "What is the liquid part of blood called?", Options = ["Platelet", "Plasma", "Marrow", "Lymph"], CorrectAnswer = "Plasma", Field = "Biology", SubField = "HumanBody", Difficulty = "Medium", Depth = "General" },

        // Animals - Land
        new TriviaQuestion { QuestionText = "What is the fastest land animal?", Options = ["Lion", "Cheetah", "Leopard", "Horse"], CorrectAnswer = "Cheetah", Field = "Animals", SubField = "Land", Difficulty = "Easy", Depth = "General" },
        new TriviaQuestion { QuestionText = "What is the largest land animal?", Options = ["Rhinoceros", "Elephant", "Hippopotamus", "Giraffe"], CorrectAnswer = "Elephant", Field = "Animals", SubField = "Land", Difficulty = "Easy", Depth = "General" },
        new TriviaQuestion { QuestionText = "How many hearts does an octopus have?", Options = ["1", "2", "3", "4"], CorrectAnswer = "3", Field = "Animals", SubField = "Sea", Difficulty = "Medium", Depth = "General" },
        new TriviaQuestion { QuestionText = "What is the largest animal on Earth?", Options = ["Elephant", "Blue Whale", "Great White Shark", "Giraffe"], CorrectAnswer = "Blue Whale", Field = "Animals", SubField = "Sea", Difficulty = "Easy", Depth = "General" },
        new TriviaQuestion { QuestionText = "Which bird is known for its ability to mimic human speech?", Options = ["Crow", "Parrot", "Owl", "Eagle"], CorrectAnswer = "Parrot", Field = "Animals", SubField = "Air", Difficulty = "Easy", Depth = "General" },
        new TriviaQuestion { QuestionText = "What is the only mammal capable of true sustained flight?", Options = ["Flying squirrel", "Bat", "Gliding possum", "Colugo"], CorrectAnswer = "Bat", Field = "Animals", SubField = "Air", Difficulty = "Medium", Depth = "General" },
        new TriviaQuestion { QuestionText = "Which sea creature has three hearts?", Options = ["Dolphin", "Squid", "Octopus", "Jellyfish"], CorrectAnswer = "Octopus", Field = "Animals", SubField = "Sea", Difficulty = "Medium", Depth = "General" },

        new TriviaQuestion { QuestionText = "What animal is known as the king of the jungle?", Options = ["Tiger", "Lion", "Leopard", "Bear"], CorrectAnswer = "Lion", Field = "Animals", SubField = "Land", Difficulty = "Easy", Depth = "General" },
        new TriviaQuestion { QuestionText = "Which animal is the tallest on land?", Options = ["Elephant", "Giraffe", "Moose", "Camel"], CorrectAnswer = "Giraffe", Field = "Animals", SubField = "Land", Difficulty = "Easy", Depth = "General" },
        new TriviaQuestion { QuestionText = "Which marine animal uses echolocation to navigate?", Options = ["Dolphin", "Sea turtle", "Starfish", "Clownfish"], CorrectAnswer = "Dolphin", Field = "Animals", SubField = "Sea", Difficulty = "Medium", Depth = "General" },
        new TriviaQuestion { QuestionText = "Which animal is known for changing color to blend in?", Options = ["Chameleon", "Penguin", "Elephant", "Horse"], CorrectAnswer = "Chameleon", Field = "Animals", SubField = "Land", Difficulty = "Easy", Depth = "General" },
        new TriviaQuestion { QuestionText = "What bird is often associated with delivering messages?", Options = ["Pigeon", "Ostrich", "Swan", "Pelican"], CorrectAnswer = "Pigeon", Field = "Animals", SubField = "Air", Difficulty = "Easy", Depth = "General" },
        new TriviaQuestion { QuestionText = "Which sea animal has a hard shell and claws?", Options = ["Crab", "Eel", "Seal", "Tuna"], CorrectAnswer = "Crab", Field = "Animals", SubField = "Sea", Difficulty = "Easy", Depth = "General" },
        new TriviaQuestion { QuestionText = "What do pandas mostly eat?", Options = ["Bamboo", "Fish", "Grass", "Fruit only"], CorrectAnswer = "Bamboo", Field = "Animals", SubField = "Land", Difficulty = "Easy", Depth = "General" },
        new TriviaQuestion { QuestionText = "Which bird is the largest living bird?", Options = ["Eagle", "Ostrich", "Albatross", "Emu"], CorrectAnswer = "Ostrich", Field = "Animals", SubField = "Air", Difficulty = "Medium", Depth = "General" },

        // General
        new TriviaQuestion { QuestionText = "How many continents are there?", Options = ["5", "6", "7", "8"], CorrectAnswer = "7", Field = "General", SubField = "GeneralKnowledge", Difficulty = "Easy", Depth = "General" },
        new TriviaQuestion { QuestionText = "What is the capital of France?", Options = ["Lyon", "Marseille", "Paris", "Nice"], CorrectAnswer = "Paris", Field = "General", SubField = "GeneralKnowledge", Difficulty = "Easy", Depth = "General" },
        new TriviaQuestion { QuestionText = "How many sides does a hexagon have?", Options = ["5", "6", "7", "8"], CorrectAnswer = "6", Field = "General", SubField = "GeneralKnowledge", Difficulty = "Easy", Depth = "Beginner" },
        new TriviaQuestion { QuestionText = "What is the largest ocean on Earth?", Options = ["Atlantic", "Indian", "Arctic", "Pacific"], CorrectAnswer = "Pacific", Field = "General", SubField = "GeneralKnowledge", Difficulty = "Easy", Depth = "General" },
        new TriviaQuestion { QuestionText = "In what year did the Titanic sink?", Options = ["1910", "1911", "1912", "1913"], CorrectAnswer = "1912", Field = "General", SubField = "GeneralKnowledge", Difficulty = "Medium", Depth = "General" },
        new TriviaQuestion { QuestionText = "What is the largest desert in the world?", Options = ["Sahara", "Antarctic", "Gobi", "Kalahari"], CorrectAnswer = "Antarctic", Field = "General", SubField = "GeneralKnowledge", Difficulty = "Medium", Depth = "General" },
        new TriviaQuestion { QuestionText = "Which language has the most native speakers worldwide?", Options = ["English", "Spanish", "Mandarin Chinese", "Hindi"], CorrectAnswer = "Mandarin Chinese", Field = "General", SubField = "GeneralKnowledge", Difficulty = "Medium", Depth = "General" },
        new TriviaQuestion { QuestionText = "How many players are on the court for one basketball team?", Options = ["4", "5", "6", "7"], CorrectAnswer = "5", Field = "General", SubField = "GeneralKnowledge", Difficulty = "Easy", Depth = "General" },
        new TriviaQuestion { QuestionText = "Which instrument has keys, pedals, and strings?", Options = ["Violin", "Piano", "Flute", "Drum"], CorrectAnswer = "Piano", Field = "General", SubField = "GeneralKnowledge", Difficulty = "Easy", Depth = "General" },
        new TriviaQuestion { QuestionText = "What currency is used in Japan?", Options = ["Yen", "Won", "Yuan", "Peso"], CorrectAnswer = "Yen", Field = "General", SubField = "GeneralKnowledge", Difficulty = "Easy", Depth = "General" },
        new TriviaQuestion { QuestionText = "What is the freezing point of water in Celsius?", Options = ["0", "10", "32", "100"], CorrectAnswer = "0", Field = "General", SubField = "GeneralKnowledge", Difficulty = "Easy", Depth = "General" },
        new TriviaQuestion { QuestionText = "Which country is shaped like a boot?", Options = ["Italy", "Greece", "Portugal", "Norway"], CorrectAnswer = "Italy", Field = "General", SubField = "GeneralKnowledge", Difficulty = "Easy", Depth = "General" },
        new TriviaQuestion { QuestionText = "How many months have 31 days?", Options = ["5", "6", "7", "8"], CorrectAnswer = "7", Field = "General", SubField = "GeneralKnowledge", Difficulty = "Medium", Depth = "General" },
        new TriviaQuestion { QuestionText = "Which board game uses hotels and houses?", Options = ["Chess", "Monopoly", "Scrabble", "Clue"], CorrectAnswer = "Monopoly", Field = "General", SubField = "GeneralKnowledge", Difficulty = "Easy", Depth = "General" },
        new TriviaQuestion { QuestionText = "What is the main language spoken in Brazil?", Options = ["Spanish", "Portuguese", "French", "Italian"], CorrectAnswer = "Portuguese", Field = "General", SubField = "GeneralKnowledge", Difficulty = "Medium", Depth = "General" },
    ];

    public static List<TriviaQuestion> GetQuestions(string field, string? subField, string difficulty, int count)
    {
        var scoped = All.Where(q =>
            (string.IsNullOrEmpty(field) || q.Field.Equals(field, StringComparison.OrdinalIgnoreCase)) &&
            (string.IsNullOrEmpty(subField) || q.SubField.Equals(subField, StringComparison.OrdinalIgnoreCase))
        ).ToList();

        if (scoped.Count < count && !string.IsNullOrEmpty(subField))
        {
            scoped = All.Where(q =>
                string.IsNullOrEmpty(field) || q.Field.Equals(field, StringComparison.OrdinalIgnoreCase)
            ).ToList();
        }

        if (scoped.Count == 0)
        {
            scoped = All.ToList();
        }

        var selected = new List<TriviaQuestion>(Math.Min(count, scoped.Count));
        var selectedTexts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var level in GetDifficultyPriority(difficulty))
        {
            AddUniqueQuestions(
                scoped.Where(q => q.Difficulty.Equals(level, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(_ => Random.Shared.Next()),
                selected,
                selectedTexts,
                count);
        }

        if (selected.Count < count)
        {
            AddUniqueQuestions(scoped.OrderBy(_ => Random.Shared.Next()), selected, selectedTexts, count);
        }

        selected = AvoidRepeatingLastSet(selected, scoped, field, subField, difficulty);

        var result = selected
            .OrderBy(_ => Random.Shared.Next())
            .Select(CloneQuestion)
            .ToList();

        LastQuestionSets[BuildSetKey(field, subField, difficulty)] = result
            .Select(q => q.QuestionText.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return result;
    }

    private static TriviaQuestion CloneQuestion(TriviaQuestion q) => new()
    {
        QuestionText = q.QuestionText,
        Options = [.. q.Options],
        CorrectAnswer = q.CorrectAnswer,
        Explanation = q.Explanation,
        Field = q.Field,
        SubField = q.SubField,
        Difficulty = q.Difficulty,
        Depth = q.Depth
    };

    private static void AddUniqueQuestions(
        IEnumerable<TriviaQuestion> candidates,
        List<TriviaQuestion> selected,
        HashSet<string> selectedTexts,
        int count)
    {
        foreach (var question in candidates)
        {
            if (selected.Count >= count) return;
            if (selectedTexts.Add(question.QuestionText.Trim()))
            {
                selected.Add(question);
            }
        }
    }

    private static string[] GetDifficultyPriority(string difficulty) => difficulty switch
    {
        "Easy" => ["Easy", "Medium", "Hard"],
        "Hard" => ["Hard", "Medium", "Easy"],
        "Medium" => ["Medium", "Easy", "Hard"],
        _ => ["Easy", "Medium", "Hard"]
    };

    private static List<TriviaQuestion> AvoidRepeatingLastSet(
        List<TriviaQuestion> selected,
        List<TriviaQuestion> scoped,
        string field,
        string? subField,
        string difficulty)
    {
        var key = BuildSetKey(field, subField, difficulty);
        if (!LastQuestionSets.TryGetValue(key, out var lastSet))
        {
            return selected;
        }

        var selectedSet = selected
            .Select(q => q.QuestionText.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!selectedSet.SetEquals(lastSet))
        {
            return selected;
        }

        var replacement = scoped
            .Where(q => !selectedSet.Contains(q.QuestionText.Trim()))
            .OrderBy(q => GetDifficultyRank(q.Difficulty, difficulty))
            .ThenBy(_ => Random.Shared.Next())
            .FirstOrDefault();

        if (replacement == null || selected.Count == 0)
        {
            return selected;
        }

        var replaceIndex = Random.Shared.Next(selected.Count);
        selected[replaceIndex] = replacement;
        return selected;
    }

    private static int GetDifficultyRank(string questionDifficulty, string requestedDifficulty)
    {
        var priority = GetDifficultyPriority(requestedDifficulty);
        var index = Array.FindIndex(priority, q => q.Equals(questionDifficulty, StringComparison.OrdinalIgnoreCase));
        return index < 0 ? priority.Length : index;
    }

    private static string BuildSetKey(string field, string? subField, string difficulty) =>
        $"{field}|{subField}|{difficulty}".ToLowerInvariant();
}
