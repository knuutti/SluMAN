using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace SluMAN
{
    /// <summary>
    /// A calculation shown as a row in the memory window, such as a speedometer:
    /// <list type="bullet">
    /// <item><c>sqrt(speedX^2 + speedY^2)</c>: other watches are used by their names.</item>
    /// <item><c>{Speed X} * 30</c>: a name with spaces or other characters goes in braces.</item>
    /// <item><c>round(height, 2)</c>: functions such as sqrt, abs, min, max and round.</item>
    /// </list>
    /// Numbers are decimal (<c>1.5</c>) or hexadecimal with a 0x prefix (<c>0x1F</c>).
    /// </summary>
    public sealed class WatchFormula
    {
        /// <summary>Shown in the function editor; keep it in step with <see cref="Functions"/>.</summary>
        public const string FunctionList = "sqrt, abs, min, max, round(x, digits), floor, ceil, sin, cos, tan, atan2(y, x), pow(x, y), log, clamp(x, low, high), deg, rad";

        private sealed class Function
        {
            public int minArgs;
            public int maxArgs;
            public Func<double[], double> apply;
        }

        private static readonly Dictionary<string, Function> Functions = new Dictionary<string, Function>(StringComparer.OrdinalIgnoreCase)
        {
            { "sqrt", new Function { minArgs = 1, maxArgs = 1, apply = a => Math.Sqrt(a[0]) } },
            { "abs", new Function { minArgs = 1, maxArgs = 1, apply = a => Math.Abs(a[0]) } },
            { "min", new Function { minArgs = 2, maxArgs = int.MaxValue, apply = a => a.Min() } },
            { "max", new Function { minArgs = 2, maxArgs = int.MaxValue, apply = a => a.Max() } },
            { "round", new Function { minArgs = 1, maxArgs = 2, apply = Round } },
            { "floor", new Function { minArgs = 1, maxArgs = 1, apply = a => Math.Floor(a[0]) } },
            { "ceil", new Function { minArgs = 1, maxArgs = 1, apply = a => Math.Ceiling(a[0]) } },
            { "sin", new Function { minArgs = 1, maxArgs = 1, apply = a => Math.Sin(a[0]) } },
            { "cos", new Function { minArgs = 1, maxArgs = 1, apply = a => Math.Cos(a[0]) } },
            { "tan", new Function { minArgs = 1, maxArgs = 1, apply = a => Math.Tan(a[0]) } },
            { "atan2", new Function { minArgs = 2, maxArgs = 2, apply = a => Math.Atan2(a[0], a[1]) } },
            { "pow", new Function { minArgs = 2, maxArgs = 2, apply = a => Math.Pow(a[0], a[1]) } },
            { "log", new Function { minArgs = 1, maxArgs = 2, apply = a => a.Length == 2 ? Math.Log(a[0], a[1]) : Math.Log(a[0]) } },
            { "clamp", new Function { minArgs = 3, maxArgs = 3, apply = a => Math.Max(a[1], Math.Min(a[2], a[0])) } },
            { "deg", new Function { minArgs = 1, maxArgs = 1, apply = a => a[0] * 180.0 / Math.PI } },
            { "rad", new Function { minArgs = 1, maxArgs = 1, apply = a => a[0] * Math.PI / 180.0 } },
        };

        private static double Round(double[] a)
        {
            int digits = a.Length == 2 ? (int)Math.Max(0, Math.Min(15, a[1])) : 0;
            return Math.Round(a[0], digits, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Looks up a watch's current value by name. Returns null when there is no watch with that
        /// name, or NaN when it has no value right now.
        /// </summary>
        public delegate double? VariableLookup(string name);

        private abstract class Node
        {
            public abstract double Evaluate(VariableLookup lookup);
        }

        private sealed class NumberNode : Node
        {
            public double value;
            public override double Evaluate(VariableLookup lookup) => value;
        }

        private sealed class VariableNode : Node
        {
            public string name;

            public override double Evaluate(VariableLookup lookup)
            {
                double? value = lookup(name);
                if (value.HasValue)
                {
                    return value.Value;
                }
                if (string.Equals(name, "pi", StringComparison.OrdinalIgnoreCase))
                {
                    return Math.PI;
                }
                throw new FormatException($"There's no watch named \"{name}\".");
            }
        }

        private sealed class NegateNode : Node
        {
            public Node operand;
            public override double Evaluate(VariableLookup lookup) => -operand.Evaluate(lookup);
        }

        private sealed class BinaryNode : Node
        {
            public char op;
            public Node left;
            public Node right;

            public override double Evaluate(VariableLookup lookup)
            {
                double a = left.Evaluate(lookup);
                double b = right.Evaluate(lookup);
                switch (op)
                {
                    case '+': return a + b;
                    case '-': return a - b;
                    case '*': return a * b;
                    case '/': return a / b;
                    case '%': return a % b;
                    default: return Math.Pow(a, b);
                }
            }
        }

        private sealed class CallNode : Node
        {
            public Function function;
            public Node[] args;

            public override double Evaluate(VariableLookup lookup)
            {
                double[] values = new double[args.Length];
                for (int i = 0; i < args.Length; i++)
                {
                    values[i] = args[i].Evaluate(lookup);
                }
                return function.apply(values);
            }
        }

        // Where each watch name appears in the text, so renaming a watch can rewrite it.
        private sealed class NameSpan
        {
            public string name;
            public int start;
            public int length;
        }

        private readonly Node root;
        private readonly List<NameSpan> names;

        /// <summary>The formula as the user typed it.</summary>
        public string Text { get; }

        private WatchFormula(string text, Node root, List<NameSpan> names)
        {
            Text = text;
            this.root = root;
            this.names = names;
        }

        /// <summary>The watch names the formula uses, each once.</summary>
        public IEnumerable<string> VariableNames
        {
            get { return names.Select(n => n.name).Distinct(StringComparer.OrdinalIgnoreCase); }
        }

        public bool Uses(string name)
        {
            return names.Any(n => string.Equals(n.name, name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Calculates the value. NaN when a watch it uses has no value right now. Throws
        /// FormatException when it uses a watch that doesn't exist.
        /// </summary>
        public double Evaluate(VariableLookup lookup)
        {
            return root.Evaluate(lookup);
        }

        /// <summary>How <paramref name="name"/> is written in a formula: bare, or in braces when it has to be.</summary>
        public static string Reference(string name)
        {
            bool plain = name.Length > 0 && (char.IsLetter(name[0]) || name[0] == '_')
                && name.All(c => char.IsLetterOrDigit(c) || c == '_')
                && !Functions.ContainsKey(name);
            return plain ? name : "{" + name + "}";
        }

        /// <summary>The text with every use of <paramref name="oldName"/> changed to <paramref name="newName"/>.</summary>
        public string RenameVariable(string oldName, string newName)
        {
            StringBuilder text = new StringBuilder(Text);
            // From the end, so earlier positions stay valid.
            foreach (NameSpan span in names.OrderByDescending(n => n.start))
            {
                if (string.Equals(span.name, oldName, StringComparison.OrdinalIgnoreCase))
                {
                    text.Remove(span.start, span.length);
                    text.Insert(span.start, Reference(newName));
                }
            }
            return text.ToString();
        }

        /// <summary>A calculated value as shown in the list.</summary>
        public static string Format(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return "N/A";
            }
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Parses a formula. Returns false, with a reason in <paramref name="error"/>, when it can't
        /// be read.
        /// </summary>
        public static bool TryParse(string text, out WatchFormula formula, out string error)
        {
            formula = null;
            error = "";
            string source = text ?? "";
            if (source.Trim() == "")
            {
                error = "Enter a formula, for example sqrt(speedX^2 + speedY^2).";
                return false;
            }

            Parser parser = new Parser(source);
            try
            {
                Node root = parser.ParseExpression();
                parser.SkipSpaces();
                if (!parser.AtEnd)
                {
                    throw parser.Error($"Unexpected \"{parser.Current}\".");
                }
                formula = new WatchFormula(source, root, parser.names);
                return true;
            }
            catch (FormatException ex)
            {
                error = ex.Message;
                return false;
            }
        }

        // Recursive descent, lowest precedence first: + -, then * / %, then unary minus, then ^.
        private sealed class Parser
        {
            private readonly string source;
            private int position;
            public readonly List<NameSpan> names = new List<NameSpan>();

            public Parser(string source)
            {
                this.source = source;
            }

            public bool AtEnd => position >= source.Length;
            public char Current => source[position];

            public FormatException Error(string message)
            {
                return new FormatException($"{message} (at character {position + 1})");
            }

            public void SkipSpaces()
            {
                while (!AtEnd && char.IsWhiteSpace(Current))
                {
                    position++;
                }
            }

            private bool Accept(char c)
            {
                SkipSpaces();
                if (!AtEnd && Current == c)
                {
                    position++;
                    return true;
                }
                return false;
            }

            public Node ParseExpression()
            {
                Node node = ParseTerm();
                while (true)
                {
                    SkipSpaces();
                    if (AtEnd || (Current != '+' && Current != '-'))
                    {
                        return node;
                    }
                    char op = Current;
                    position++;
                    node = new BinaryNode { op = op, left = node, right = ParseTerm() };
                }
            }

            private Node ParseTerm()
            {
                Node node = ParseUnary();
                while (true)
                {
                    SkipSpaces();
                    if (AtEnd || (Current != '*' && Current != '/' && Current != '%'))
                    {
                        return node;
                    }
                    char op = Current;
                    position++;
                    node = new BinaryNode { op = op, left = node, right = ParseUnary() };
                }
            }

            private Node ParseUnary()
            {
                if (Accept('-'))
                {
                    return new NegateNode { operand = ParseUnary() };
                }
                if (Accept('+'))
                {
                    return ParseUnary();
                }
                return ParsePower();
            }

            private Node ParsePower()
            {
                Node node = ParseAtom();
                if (Accept('^'))
                {
                    // Right to left, and -x^2 is -(x^2) as in maths.
                    return new BinaryNode { op = '^', left = node, right = ParseUnary() };
                }
                return node;
            }

            private Node ParseAtom()
            {
                SkipSpaces();
                if (AtEnd)
                {
                    throw Error("The formula ends too early.");
                }

                char c = Current;
                if (c == '(')
                {
                    position++;
                    Node inner = ParseExpression();
                    if (!Accept(')'))
                    {
                        throw Error("A \"(\" has no matching \")\".");
                    }
                    return inner;
                }
                if (c == '{')
                {
                    int start = position;
                    int close = source.IndexOf('}', position + 1);
                    if (close < 0)
                    {
                        throw Error("A \"{\" has no matching \"}\".");
                    }
                    string name = source.Substring(position + 1, close - position - 1).Trim();
                    if (name == "")
                    {
                        throw Error("Put a watch name between { and }.");
                    }
                    position = close + 1;
                    names.Add(new NameSpan { name = name, start = start, length = position - start });
                    return new VariableNode { name = name };
                }
                if (char.IsDigit(c) || c == '.')
                {
                    return ParseNumber();
                }
                if (char.IsLetter(c) || c == '_')
                {
                    int start = position;
                    while (!AtEnd && (char.IsLetterOrDigit(Current) || Current == '_'))
                    {
                        position++;
                    }
                    string word = source.Substring(start, position - start);

                    if (Accept('('))
                    {
                        return ParseCall(word);
                    }
                    names.Add(new NameSpan { name = word, start = start, length = word.Length });
                    return new VariableNode { name = word };
                }
                throw Error($"Unexpected \"{c}\". Expected a number, a watch name or \"(\".");
            }

            private Node ParseNumber()
            {
                int start = position;
                if (Current == '0' && position + 1 < source.Length && (source[position + 1] == 'x' || source[position + 1] == 'X'))
                {
                    position += 2;
                    int digitsStart = position;
                    while (!AtEnd && Uri.IsHexDigit(Current))
                    {
                        position++;
                    }
                    ulong hex;
                    if (!ulong.TryParse(source.Substring(digitsStart, position - digitsStart), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out hex))
                    {
                        position = start;
                        throw Error("Write a hex number like 0x1F.");
                    }
                    return new NumberNode { value = hex };
                }

                while (!AtEnd && (char.IsDigit(Current) || Current == '.'))
                {
                    position++;
                }
                double value;
                if (!double.TryParse(source.Substring(start, position - start), NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                {
                    position = start;
                    throw Error("Write a number like 12 or 1.5.");
                }
                return new NumberNode { value = value };
            }

            private Node ParseCall(string name)
            {
                Function function;
                if (!Functions.TryGetValue(name, out function))
                {
                    throw Error($"There's no function called \"{name}\". Functions: {FunctionList}.");
                }

                List<Node> args = new List<Node>();
                if (!Accept(')'))
                {
                    do
                    {
                        args.Add(ParseExpression());
                    }
                    while (Accept(','));
                    if (!Accept(')'))
                    {
                        throw Error($"\"{name}(\" has no matching \")\".");
                    }
                }

                if (args.Count < function.minArgs || args.Count > function.maxArgs)
                {
                    string expected = function.minArgs == function.maxArgs
                        ? function.minArgs.ToString()
                        : function.maxArgs == int.MaxValue ? $"{function.minArgs} or more" : $"{function.minArgs} or {function.maxArgs}";
                    string noun = function.maxArgs == 1 ? "value" : "values";
                    throw Error($"{name} takes {expected} {noun}, not {args.Count}.");
                }
                return new CallNode { function = function, args = args.ToArray() };
            }
        }
    }
}
