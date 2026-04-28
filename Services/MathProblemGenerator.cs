using System;
using System.Collections.Generic;
using System.Linq;

namespace AcadsJulie.Services
{
    public record MathProblem(string Expression, int Answer, List<int> Options);

    public class MathProblemGenerator
    {
        public MathProblem Generate(string difficulty)
        {
            Random random = Random.Shared;
            int a, b, c;
            int answer = 0;
            string expression = "";
            
            // Easy: + and - only, up to 20
            // Medium: +, -, ×, up to 50, times tables <= 10
            // Hard: + - ×, bigger numbers, occasional (a+b)×c
            
            if (difficulty == "Easy")
            {
                bool isAdd = random.NextDouble() > 0.5;
                if (isAdd)
                {
                    a = random.Next(1, 20);
                    b = random.Next(1, 20);
                    answer = a + b;
                    expression = $"{a} + {b}";
                }
                else
                {
                    a = random.Next(10, 30);
                    b = random.Next(1, a);
                    answer = a - b;
                    expression = $"{a} - {b}";
                }
            }
            else if (difficulty == "Medium")
            {
                int op = random.Next(3);
                if (op == 0) // Add
                {
                    a = random.Next(10, 50);
                    b = random.Next(10, 50);
                    answer = a + b;
                    expression = $"{a} + {b}";
                }
                else if (op == 1) // Sub
                {
                    a = random.Next(20, 80);
                    b = random.Next(1, a/2 + 10);
                    answer = a - b;
                    expression = $"{a} - {b}";
                }
                else // Mul
                {
                    a = random.Next(2, 11);
                    b = random.Next(2, 11);
                    answer = a * b;
                    expression = $"{a} × {b}";
                }
            }
            else // Hard
            {
                int op = random.Next(4);
                if (op == 0) // Advanced Add
                {
                    a = random.Next(50, 200);
                    b = random.Next(50, 200);
                    answer = a + b;
                    expression = $"{a} + {b}";
                }
                else if (op == 1) // Advanced Sub
                {
                    a = random.Next(100, 300);
                    b = random.Next(20, 100);
                    answer = a - b;
                    expression = $"{a} - {b}";
                }
                else if (op == 2) // Advanced Mul
                {
                    a = random.Next(4, 15);
                    b = random.Next(4, 15);
                    answer = a * b;
                    expression = $"{a} × {b}";
                }
                else // Mixed (a+b) x c or a x (b-c)
                {
                    a = random.Next(2, 10);
                    b = random.Next(2, 10);
                    c = random.Next(2, 6);
                    bool isAdd = random.NextDouble() > 0.5;
                    
                    if (isAdd)
                    {
                        answer = (a + b) * c;
                        expression = $"({a} + {b}) × {c}";
                    }
                    else
                    {
                        b = random.Next(c + 1, c + 10); // ensure b > c
                        answer = a * (b - c);
                        expression = $"{a} × ({b} - {c})";
                    }
                }
            }

            var options = GenerateOptions(answer);
            return new MathProblem(expression, answer, options);
        }

        private List<int> GenerateOptions(int correct)
        {
            var options = new HashSet<int> { correct };
            Random random = Random.Shared;

            while (options.Count < 4)
            {
                // Variation 1 to 15, occasionally big shift for hard bounds
                int variation = random.Next(1, 16);
                bool add = random.NextDouble() > 0.5;
                
                int wrong = add ? correct + variation : correct - variation;
                
                if (wrong >= 0)
                {
                    options.Add(wrong);
                }
                else
                {
                    options.Add(correct + variation + 1); // fallback
                }
            }

            return options.OrderBy(_ => random.Next()).ToList();
        }
    }
}
