/*
NOD SYSTEM -- EXPRESSION EVALUATOR

Dit bestand voert NOD 2.0 math-expressies uit.

Voorbeelden:
- ans^2
- e^2
- ans * e^2
- ans e^2
- pi * ans^2
- ln(e)
- log(ans,2)
- sqrt((ans^2 + 25) / 3)
- 5!
- comb(5,2)
- |ans|

Belangrijk:
- e en pi worden hier als constanten herkend.
- ans is de actuele invoer-/tussenwaarde.
- variabelen uit EquationEngine kunnen ook worden meegegeven.
*/

using System.Globalization;

namespace Tiedragon.NodSystem.Core;

// Zoek/commentaar: Type-overzicht: class NodExpressionEvaluator bevat de hoofdlogica/data voor dit onderdeel.
public static class NodExpressionEvaluator
{
    // Zoek/commentaar: Evalueert een expressie of berekening voor Evaluate.
    public static decimal Evaluate(string expression, decimal ans)
    {
        return Evaluate(expression, ans, new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase));
    }

    // Zoek/commentaar: Evalueert een expressie of berekening voor Evaluate.
    public static decimal Evaluate(string expression, decimal ans, IReadOnlyDictionary<string, decimal> variables)
    {
        var parser = new Parser(expression, (double)ans, variables);
        var value = parser.ParseExpression();
        parser.ExpectEnd();
        return (decimal)value.AsScalar();
    }

    // Zoek/commentaar: Type-overzicht: class Parser bevat de hoofdlogica/data voor dit onderdeel.
    private sealed class Parser
    {
        private static readonly double ConstE = Math.E;
        private static readonly double ConstPi = Math.PI;

        private readonly string _text;
        private readonly double _ans;
        private readonly IReadOnlyDictionary<string, decimal> _variables;
        private int _position;
        private bool _commasSeparateArguments;

        // Zoek/commentaar: Constructor: maakt en initialiseert Parser.
        public Parser(string text, double ans, IReadOnlyDictionary<string, decimal> variables)
        {
            _text = text;
            _ans = ans;
            _variables = variables;
        }

        // Zoek/commentaar: Leest tekst in en zet die om naar gestructureerde data voor ParseExpression.
        public Value ParseExpression()
        {
            var value = ParseTerm();
            while (true)
            {
                if (Match('+')) value = Add(value, ParseTerm());
                else if (Match('-')) value = Subtract(value, ParseTerm());
                else return value;
            }
        }

        // Zoek/commentaar: Leest tekst in en zet die om naar gestructureerde data voor ParseTerm.
        private Value ParseTerm()
        {
            var value = ParsePower();
            while (true)
            {
                if (Match('*')) value = Multiply(value, ParsePower());
                else if (Match('/')) value = Divide(value, ParsePower());
                else if (Match('%')) value = Modulo(value, ParsePower());
                else if (StartsImplicitMultiplication()) value = Multiply(value, ParsePower());
                else return value;
            }
        }

        // Zoek/commentaar: Leest tekst in en zet die om naar gestructureerde data voor ParsePower.
        private Value ParsePower()
        {
            var value = ParseUnary();
            if (Match('^')) value = Value.Scalar(Math.Pow(value.AsScalar(), ParsePower().AsScalar()));
            return value;
        }

        // Zoek/commentaar: Leest tekst in en zet die om naar gestructureerde data voor ParseUnary.
        private Value ParseUnary()
        {
            if (Match('+')) return ParseUnary();
            if (Match('-')) return Negate(ParseUnary());
            return ParsePostfix();
        }

        private Value ParsePostfix()
        {
            var value = ParsePrimary();
            while (Match('!'))
                value = Value.Scalar(Factorial(value.AsScalar()));
            return value;
        }

        // Zoek/commentaar: Leest tekst in en zet die om naar gestructureerde data voor ParsePrimary.
        private Value ParsePrimary()
        {
            if (Match('('))
            {
                var value = ParseExpression();
                Expect(')');
                return value;
            }

            if (Match('|'))
            {
                var value = Abs(ParseExpression());
                Expect('|');
                return value;
            }

            if (char.IsDigit(Current) || Current is ',' or '.') return Value.Scalar(ParseNumber());
            if (char.IsLetter(Current) || Current == '\u03C0') return ParseIdentifierOrFunction();

            throw Error($"Unexpected character '{Current}'");
        }

        // Zoek/commentaar: Leest tekst in en zet die om naar gestructureerde data voor ParseNumber.
        private double ParseNumber()
        {
            var start = _position;
            while (char.IsDigit(Current) || Current == '.' || (!_commasSeparateArguments && Current == ',')) _position++;

            if (Current is 'e' or 'E' && HasExponentDigits(_position + 1))
            {
                _position++;
                if (Current is '+' or '-') _position++;
                while (char.IsDigit(Current)) _position++;
            }

            var raw = _text[start.._position].Replace(',', '.');

            if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
                return number;

            throw Error($"Invalid number '{raw}'");
        }

        private bool HasExponentDigits(int position)
        {
            if (position >= _text.Length) return false;
            if (_text[position] is '+' or '-') position++;
            return position < _text.Length && char.IsDigit(_text[position]);
        }

        // Zoek/commentaar: Leest tekst in en zet die om naar gestructureerde data voor ParseIdentifierOrFunction.
        private Value ParseIdentifierOrFunction()
        {
            var name = ParseIdentifier();

            if (Match('('))
            {
                var args = new List<Value>();
                var previousCommaMode = _commasSeparateArguments;
                _commasSeparateArguments = true;

                try
                {
                    if (!Peek(')'))
                    {
                        while (true)
                        {
                            args.Add(ParseExpression());
                            if (!Match(',')) break;
                        }
                    }

                    Expect(')');
                }
                finally
                {
                    _commasSeparateArguments = previousCommaMode;
                }

                return CallFunction(name.ToLowerInvariant(), args);
            }

            if (_variables.TryGetValue(name, out var value))
                return Value.Scalar((double)value);

            return name.ToLowerInvariant() switch
            {
                "ans" or "x" => Value.Scalar(_ans),
                "e" => Value.Scalar(ConstE),
                "pi" or "\u03C0" => Value.Scalar(ConstPi),
                _ => throw Error($"Unknown variable '{name}'")
            };
        }

        // Zoek/commentaar: Leest tekst in en zet die om naar gestructureerde data voor ParseIdentifier.
        private string ParseIdentifier()
        {
            var start = _position;
            while (char.IsLetterOrDigit(Current) || Current == '_' || Current == '\u03C0') _position++;
            return _text[start.._position];
        }

        // Zoek/commentaar: Methode CallFunction: centrale logica voor deze stap.
        private static Value CallFunction(string name, IReadOnlyList<Value> args) => name switch
        {
            "vec" or "vector" when args.Count is 2 or 3 => Value.Vector(args.Select(arg => arg.AsScalar()).ToArray()),
            "mat2" or "matrix2" when args.Count == 4 => Value.Matrix2(args.Select(arg => arg.AsScalar()).ToArray()),
            "mat3" or "matrix3" when args.Count == 9 => Value.Matrix3(args.Select(arg => arg.AsScalar()).ToArray()),
            "det" when args.Count == 1 => Value.Scalar(Determinant(args[0].AsMatrix())),
            "det" or "det2" when args.Count == 4 => Value.Scalar(args[0].AsScalar() * args[3].AsScalar() - args[1].AsScalar() * args[2].AsScalar()),
            "trace" when args.Count == 1 => Value.Scalar(MatrixTrace(args[0].AsMatrix())),
            "mget" when args.Count == 3 => Value.Scalar(MatrixComponent(args[0].AsMatrix(), args[1].AsScalar(), args[2].AsScalar())),
            "length" or "norm" or "mag" when args.Count == 1 => Value.Scalar(VectorLength(args[0].AsVector())),
            "unit" when args.Count == 1 => Value.Vector(UnitVector(args[0].AsVector())),
            "dot" when args.Count == 2 => Value.Scalar(Dot(args[0].AsVector(), args[1].AsVector())),
            "cross" when args.Count == 2 => Value.Vector(Cross(args[0].AsVector(), args[1].AsVector())),
            "angle" when args.Count == 2 => Value.Scalar(Angle(args[0].AsVector(), args[1].AsVector())),
            "angled" when args.Count == 2 => Value.Scalar(Angle(args[0].AsVector(), args[1].AsVector()) * 180.0 / Math.PI),
            "distance" or "dist" when args.Count == 2 => Value.Scalar(VectorLength(SubtractVectors(args[0].AsVector(), args[1].AsVector()))),
            "x" when args.Count == 1 => Value.Scalar(Component(args[0].AsVector(), 0)),
            "y" when args.Count == 1 => Value.Scalar(Component(args[0].AsVector(), 1)),
            "z" when args.Count == 1 => Value.Scalar(Component(args[0].AsVector(), 2)),
            "abs" when args.Count == 1 => Abs(args[0]),
            "sqrt" or "sqr" when args.Count == 1 => Value.Scalar(Math.Sqrt(args[0].AsScalar())),
            "pow" when args.Count == 2 => Value.Scalar(Math.Pow(args[0].AsScalar(), args[1].AsScalar())),
            "exp" when args.Count == 1 => Value.Scalar(Math.Exp(args[0].AsScalar())),
            "exp" when args.Count == 2 => Value.Scalar(Math.Pow(args[0].AsScalar(), args[1].AsScalar())),
            "ln" when args.Count == 1 => Value.Scalar(Math.Log(args[0].AsScalar())),
            "log" when args.Count == 2 => Value.Scalar(Math.Log(args[0].AsScalar(), args[1].AsScalar())),
            "log" when args.Count == 1 => Value.Scalar(Math.Log10(args[0].AsScalar())),
            "sin" when args.Count == 1 => Value.Scalar(Math.Sin(args[0].AsScalar())),
            "cos" when args.Count == 1 => Value.Scalar(Math.Cos(args[0].AsScalar())),
            "tan" when args.Count == 1 => Value.Scalar(Math.Tan(args[0].AsScalar())),

            // Inverse trigonometrie in radialen.
            "asin" when args.Count == 1 => Value.Scalar(Math.Asin(args[0].AsScalar())),
            "acos" when args.Count == 1 => Value.Scalar(Math.Acos(args[0].AsScalar())),
            "atan" when args.Count == 1 => Value.Scalar(Math.Atan(args[0].AsScalar())),

            // Graden <-> radialen.
            "rad" when args.Count == 1 => Value.Scalar(args[0].AsScalar() * Math.PI / 180.0),
            "deg" when args.Count == 1 => Value.Scalar(args[0].AsScalar() * 180.0 / Math.PI),

            // Trigonometrie in graden.
            "sind" when args.Count == 1 => Value.Scalar(Math.Sin(args[0].AsScalar() * Math.PI / 180.0)),
            "cosd" when args.Count == 1 => Value.Scalar(Math.Cos(args[0].AsScalar() * Math.PI / 180.0)),
            "tand" when args.Count == 1 => Value.Scalar(Math.Tan(args[0].AsScalar() * Math.PI / 180.0)),

            // Inverse trigonometrie met uitkomst in graden.
            "asind" when args.Count == 1 => Value.Scalar(Math.Asin(args[0].AsScalar()) * 180.0 / Math.PI),
            "acosd" when args.Count == 1 => Value.Scalar(Math.Acos(args[0].AsScalar()) * 180.0 / Math.PI),
            "atand" when args.Count == 1 => Value.Scalar(Math.Atan(args[0].AsScalar()) * 180.0 / Math.PI),
            "mod" when args.Count == 2 => Value.Scalar(args[0].AsScalar() % args[1].AsScalar()),
            "rem" when args.Count == 2 => Value.Scalar(args[0].AsScalar() % args[1].AsScalar()),
            "fact" or "factorial" when args.Count == 1 => Value.Scalar(Factorial(args[0].AsScalar())),
            "comb" or "ncr" or "choose" when args.Count == 2 => Value.Scalar(Combination(args[0].AsScalar(), args[1].AsScalar())),
            "perm" or "npr" when args.Count == 2 => Value.Scalar(Permutation(args[0].AsScalar(), args[1].AsScalar())),
            "expected" or "expect" when args.Count >= 2 && args.Count % 2 == 0 => Value.Scalar(ExpectedValue(args.Select(arg => arg.AsScalar()).ToArray())),
            "sum" when args.Count >= 1 => Value.Scalar(args.Sum(arg => arg.AsScalar())),
            "avg" or "mean" when args.Count >= 1 => Value.Scalar(args.Average(arg => arg.AsScalar())),
            "median" when args.Count >= 1 => Value.Scalar(Median(args.Select(arg => arg.AsScalar()).ToArray())),
            "product" or "prod" when args.Count >= 1 => Value.Scalar(Product(args.Select(arg => arg.AsScalar()).ToArray())),
            "variance" or "var" when args.Count >= 1 => Value.Scalar(Variance(args.Select(arg => arg.AsScalar()).ToArray(), sample: false)),
            "stdev" or "stddev" when args.Count >= 1 => Value.Scalar(Math.Sqrt(Variance(args.Select(arg => arg.AsScalar()).ToArray(), sample: false))),
            "samplevariance" or "svar" when args.Count >= 2 => Value.Scalar(Variance(args.Select(arg => arg.AsScalar()).ToArray(), sample: true)),
            "samplestdev" or "sstddev" when args.Count >= 2 => Value.Scalar(Math.Sqrt(Variance(args.Select(arg => arg.AsScalar()).ToArray(), sample: true))),
            "min" when args.Count == 2 => Value.Scalar(Math.Min(args[0].AsScalar(), args[1].AsScalar())),
            "max" when args.Count == 2 => Value.Scalar(Math.Max(args[0].AsScalar(), args[1].AsScalar())),
            "round" when args.Count == 1 => Value.Scalar(Math.Round(args[0].AsScalar())),
            "floor" when args.Count == 1 => Value.Scalar(Math.Floor(args[0].AsScalar())),
            "ceil" when args.Count == 1 => Value.Scalar(Math.Ceiling(args[0].AsScalar())),
            _ => throw new FormatException($"Unknown function or wrong argument count: {name}")
        };

        private static Value Add(Value left, Value right)
        {
            if (left.IsScalar && right.IsScalar) return Value.Scalar(left.AsScalar() + right.AsScalar());
            if (left.IsMatrix && right.IsMatrix) return Value.Matrix(ZipMatrix(left.AsMatrix(), right.AsMatrix(), static (a, b) => a + b));
            return Value.Vector(ZipVectors(left.AsVector(), right.AsVector(), static (a, b) => a + b));
        }

        private static Value Subtract(Value left, Value right)
        {
            if (left.IsScalar && right.IsScalar) return Value.Scalar(left.AsScalar() - right.AsScalar());
            if (left.IsMatrix && right.IsMatrix) return Value.Matrix(ZipMatrix(left.AsMatrix(), right.AsMatrix(), static (a, b) => a - b));
            return Value.Vector(ZipVectors(left.AsVector(), right.AsVector(), static (a, b) => a - b));
        }

        private static Value Multiply(Value left, Value right)
        {
            if (left.IsScalar && right.IsScalar) return Value.Scalar(left.AsScalar() * right.AsScalar());
            if (left.IsMatrix && right.IsScalar) return Value.Matrix(ScaleMatrix(left.AsMatrix(), right.AsScalar()));
            if (left.IsScalar && right.IsMatrix) return Value.Matrix(ScaleMatrix(right.AsMatrix(), left.AsScalar()));
            if (left.IsMatrix && right.IsVector) return Value.Vector(MultiplyMatrixVector(left.AsMatrix(), right.AsVector()));
            if (left.IsMatrix && right.IsMatrix) return Value.Matrix(MultiplyMatrix(left.AsMatrix(), right.AsMatrix()));
            if (left.IsVector && right.IsScalar) return Value.Vector(ScaleVector(left.AsVector(), right.AsScalar()));
            if (left.IsScalar && right.IsVector) return Value.Vector(ScaleVector(right.AsVector(), left.AsScalar()));
            throw new FormatException("Use dot(...) for vector-vector multiplication.");
        }

        private static Value Divide(Value left, Value right)
        {
            if (left.IsScalar && right.IsScalar) return Value.Scalar(left.AsScalar() / right.AsScalar());
            if (left.IsMatrix && right.IsScalar) return Value.Matrix(ScaleMatrix(left.AsMatrix(), 1.0 / right.AsScalar()));
            if (left.IsVector && right.IsScalar) return Value.Vector(ScaleVector(left.AsVector(), 1.0 / right.AsScalar()));
            throw new FormatException("Vector and matrix division only support value / scalar.");
        }

        private static Value Modulo(Value left, Value right)
        {
            if (left.IsVector || right.IsVector || left.IsMatrix || right.IsMatrix)
                throw new FormatException("Modulo only supports scalar values.");
            return Value.Scalar(left.AsScalar() % right.AsScalar());
        }

        private static Value Negate(Value value)
        {
            if (value.IsScalar) return Value.Scalar(-value.AsScalar());
            if (value.IsMatrix) return Value.Matrix(ScaleMatrix(value.AsMatrix(), -1.0));
            return Value.Vector(ScaleVector(value.AsVector(), -1.0));
        }

        private static Value Abs(Value value)
        {
            return value.IsVector
                ? Value.Scalar(VectorLength(value.AsVector()))
                : Value.Scalar(Math.Abs(value.AsScalar()));
        }

        private static double VectorLength(IReadOnlyList<double> vector)
        {
            var sum = 0.0;
            for (var i = 0; i < vector.Count; i++)
                sum += vector[i] * vector[i];
            return Math.Sqrt(sum);
        }

        private static double Dot(IReadOnlyList<double> left, IReadOnlyList<double> right)
        {
            var dimension = CompatibleDimension(left, right);
            var result = 0.0;
            for (var i = 0; i < dimension; i++)
                result += Component(left, i) * Component(right, i);
            return result;
        }

        private static double[] Cross(IReadOnlyList<double> left, IReadOnlyList<double> right)
        {
            _ = CompatibleDimension(left, right);
            var ax = Component(left, 0);
            var ay = Component(left, 1);
            var az = Component(left, 2);
            var bx = Component(right, 0);
            var by = Component(right, 1);
            var bz = Component(right, 2);

            return
            [
                ay * bz - az * by,
                az * bx - ax * bz,
                ax * by - ay * bx
            ];
        }

        private static double[] UnitVector(IReadOnlyList<double> vector)
        {
            var length = VectorLength(vector);
            if (length == 0.0)
                throw new FormatException("unit expects a non-zero vector.");
            return ScaleVector(vector, 1.0 / length);
        }

        private static double Angle(IReadOnlyList<double> left, IReadOnlyList<double> right)
        {
            var lengths = VectorLength(left) * VectorLength(right);
            if (lengths == 0.0)
                throw new FormatException("angle expects non-zero vectors.");

            var ratio = Math.Clamp(Dot(left, right) / lengths, -1.0, 1.0);
            return Math.Acos(ratio);
        }

        private static double[] SubtractVectors(IReadOnlyList<double> left, IReadOnlyList<double> right)
        {
            return ZipVectors(left, right, static (a, b) => a - b);
        }

        private static double[] ZipVectors(IReadOnlyList<double> left, IReadOnlyList<double> right, Func<double, double, double> operation)
        {
            var dimension = CompatibleDimension(left, right);
            var result = new double[dimension];
            for (var i = 0; i < result.Length; i++)
                result[i] = operation(Component(left, i), Component(right, i));
            return result;
        }

        private static double[] ScaleVector(IReadOnlyList<double> vector, double factor)
        {
            var result = new double[vector.Count];
            for (var i = 0; i < result.Length; i++)
                result[i] = vector[i] * factor;
            return result;
        }

        private static int CompatibleDimension(IReadOnlyList<double> left, IReadOnlyList<double> right)
        {
            if (left.Count is not (2 or 3) || right.Count is not (2 or 3))
                throw new FormatException("Vectors must have 2 or 3 components.");
            return Math.Max(left.Count, right.Count);
        }

        private static double Component(IReadOnlyList<double> vector, int index)
        {
            if (index < 0 || index > 2)
                throw new FormatException("Vector component index is outside x/y/z.");
            return index < vector.Count ? vector[index] : 0.0;
        }

        private static double Determinant(IReadOnlyList<double> matrix)
        {
            var size = MatrixSize(matrix);
            if (size == 2)
                return matrix[0] * matrix[3] - matrix[1] * matrix[2];

            return
                matrix[0] * (matrix[4] * matrix[8] - matrix[5] * matrix[7]) -
                matrix[1] * (matrix[3] * matrix[8] - matrix[5] * matrix[6]) +
                matrix[2] * (matrix[3] * matrix[7] - matrix[4] * matrix[6]);
        }

        private static double MatrixTrace(IReadOnlyList<double> matrix)
        {
            var size = MatrixSize(matrix);
            var trace = 0.0;
            for (var i = 0; i < size; i++)
                trace += matrix[i * size + i];
            return trace;
        }

        private static double MatrixComponent(IReadOnlyList<double> matrix, double rowValue, double columnValue)
        {
            var size = MatrixSize(matrix);
            var row = RequireMatrixIndex(rowValue, "row", size);
            var column = RequireMatrixIndex(columnValue, "column", size);
            return matrix[row * size + column];
        }

        private static int RequireMatrixIndex(double value, string name, int size)
        {
            if (Math.Abs(value - Math.Round(value)) > 0.0000000001)
                throw new FormatException($"mget expects whole-number {name} index 1 to {size}.");
            var index = (int)Math.Round(value);
            if (index < 1 || index > size)
                throw new FormatException($"mget expects {name} index 1 to {size}.");
            return index - 1;
        }

        private static double[] ZipMatrix(IReadOnlyList<double> left, IReadOnlyList<double> right, Func<double, double, double> operation)
        {
            var size = RequireSameMatrixSize(left, right);
            var result = new double[size * size];
            for (var i = 0; i < result.Length; i++)
                result[i] = operation(left[i], right[i]);
            return result;
        }

        private static double[] ScaleMatrix(IReadOnlyList<double> matrix, double factor)
        {
            var size = MatrixSize(matrix);
            var result = new double[size * size];
            for (var i = 0; i < result.Length; i++)
                result[i] = matrix[i] * factor;
            return result;
        }

        private static double[] MultiplyMatrixVector(IReadOnlyList<double> matrix, IReadOnlyList<double> vector)
        {
            var size = MatrixSize(matrix);
            if (vector.Count != size)
                throw new FormatException($"{size}x{size} matrix multiplication expects a {size}D vector.");

            var result = new double[size];
            for (var row = 0; row < size; row++)
            {
                var sum = 0.0;
                for (var column = 0; column < size; column++)
                    sum += matrix[row * size + column] * vector[column];
                result[row] = sum;
            }

            return result;
        }

        private static double[] MultiplyMatrix(IReadOnlyList<double> left, IReadOnlyList<double> right)
        {
            var size = RequireSameMatrixSize(left, right);
            var result = new double[size * size];
            for (var row = 0; row < size; row++)
            {
                for (var column = 0; column < size; column++)
                {
                    var sum = 0.0;
                    for (var i = 0; i < size; i++)
                        sum += left[row * size + i] * right[i * size + column];
                    result[row * size + column] = sum;
                }
            }

            return result;
        }

        private static int RequireSameMatrixSize(IReadOnlyList<double> left, IReadOnlyList<double> right)
        {
            var leftSize = MatrixSize(left);
            var rightSize = MatrixSize(right);
            if (leftSize != rightSize)
                throw new FormatException("Matrix operations require matrices with the same size.");
            return leftSize;
        }

        private static int MatrixSize(IReadOnlyList<double> matrix)
        {
            return matrix.Count switch
            {
                4 => 2,
                9 => 3,
                _ => throw new FormatException("Matrix value must be 2x2 or 3x3.")
            };
        }

        private static double Median(IReadOnlyList<double> values)
        {
            var sorted = values.OrderBy(value => value).ToArray();
            var middle = sorted.Length / 2;
            return sorted.Length % 2 == 1
                ? sorted[middle]
                : (sorted[middle - 1] + sorted[middle]) / 2.0;
        }

        private static double Product(IReadOnlyList<double> values)
        {
            var result = 1.0;
            foreach (var value in values)
                result *= value;
            return result;
        }

        private static double Variance(IReadOnlyList<double> values, bool sample)
        {
            if (sample && values.Count < 2)
                throw new FormatException("sample variance expects at least 2 values.");

            var mean = values.Average();
            var sum = 0.0;
            foreach (var value in values)
                sum += Math.Pow(value - mean, 2.0);

            return sum / (sample ? values.Count - 1 : values.Count);
        }

        public readonly record struct Value(double ScalarValue, double[]? VectorValue, double[]? MatrixValue)
        {
            public bool IsScalar => VectorValue is null && MatrixValue is null;
            public bool IsVector => VectorValue is not null;
            public bool IsMatrix => MatrixValue is not null;

            public static Value Scalar(double value) => new(value, null, null);

            public static Value Vector(double[] values)
            {
                if (values.Length is not (2 or 3))
                    throw new FormatException("vec expects 2 or 3 scalar components.");
                return new(0.0, values, null);
            }

            public static Value Matrix2(double[] values)
            {
                if (values.Length != 4)
                    throw new FormatException("mat2 expects 4 scalar components.");
                return new(0.0, null, values);
            }

            public static Value Matrix3(double[] values)
            {
                if (values.Length != 9)
                    throw new FormatException("mat3 expects 9 scalar components.");
                return new(0.0, null, values);
            }

            public static Value Matrix(double[] values)
            {
                return values.Length switch
                {
                    4 => Matrix2(values),
                    9 => Matrix3(values),
                    _ => throw new FormatException("Matrix value must be 2x2 or 3x3.")
                };
            }

            public double AsScalar()
            {
                if (VectorValue is not null)
                    throw new FormatException("Vector result cannot be returned as decimal. Use length(...), dot(...), or x/y/z(...).");
                if (MatrixValue is not null)
                    throw new FormatException("Matrix result cannot be returned as decimal. Use det(...), trace(...), mget(...), or multiply by a vector.");
                return ScalarValue;
            }

            public double[] AsVector()
            {
                if (VectorValue is null)
                    throw new FormatException("Expected vector value.");
                return VectorValue;
            }

            public double[] AsMatrix()
            {
                if (MatrixValue is null)
                    throw new FormatException("Expected matrix value.");
                return MatrixValue;
            }

            public double[] AsMatrix2()
            {
                var matrix = AsMatrix();
                if (matrix.Length != 4)
                    throw new FormatException("Expected 2x2 matrix value.");
                return matrix;
            }
        }

        private static double Factorial(double value)
        {
            var n = RequireWholeNumber(value, "factorial");
            if (n < 0)
                throw new FormatException("factorial expects a non-negative whole number.");
            if (n > 27)
                throw new FormatException("factorial result is too large for NOD decimal output.");

            double result = 1;
            for (var i = 2; i <= n; i++)
                result *= i;
            return result;
        }

        private static double Combination(double nValue, double rValue)
        {
            var n = RequireWholeNumber(nValue, "comb");
            var r = RequireWholeNumber(rValue, "comb");
            if (n < 0 || r < 0 || r > n)
                throw new FormatException("comb expects whole numbers with 0 <= r <= n.");

            r = Math.Min(r, n - r);
            double result = 1;
            for (var i = 1; i <= r; i++)
                result = result * (n - r + i) / i;
            return result;
        }

        private static double Permutation(double nValue, double rValue)
        {
            var n = RequireWholeNumber(nValue, "perm");
            var r = RequireWholeNumber(rValue, "perm");
            if (n < 0 || r < 0 || r > n)
                throw new FormatException("perm expects whole numbers with 0 <= r <= n.");

            double result = 1;
            for (var i = 0; i < r; i++)
                result *= n - i;
            return result;
        }

        private static double ExpectedValue(IReadOnlyList<double> args)
        {
            double result = 0;
            for (var i = 0; i < args.Count; i += 2)
                result += args[i] * args[i + 1];
            return result;
        }

        private static int RequireWholeNumber(double value, string name)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || Math.Abs(value - Math.Round(value)) > 0.0000000001)
                throw new FormatException($"{name} expects whole numbers.");
            return (int)Math.Round(value);
        }

        // Zoek/commentaar: Methode StartsImplicitMultiplication: centrale logica voor deze stap.
        private bool StartsImplicitMultiplication()
        {
            SkipWhite();
            return Current == '(' || char.IsLetter(Current) || Current == '\u03C0';
        }

        // Zoek/commentaar: Methode ExpectEnd: centrale logica voor deze stap.
        public void ExpectEnd()
        {
            SkipWhite();
            if (_position < _text.Length) throw Error($"Unexpected text '{_text[_position..]}'");
        }

        // Zoek/commentaar: Methode Match: centrale logica voor deze stap.
        private bool Match(char ch)
        {
            SkipWhite();
            if (Current != ch) return false;
            _position++;
            return true;
        }

        // Zoek/commentaar: Methode Peek: centrale logica voor deze stap.
        private bool Peek(char ch)
        {
            SkipWhite();
            return Current == ch;
        }

        // Zoek/commentaar: Methode Expect: centrale logica voor deze stap.
        private void Expect(char ch)
        {
            if (!Match(ch)) throw Error($"Expected '{ch}'");
        }

        // Zoek/commentaar: Methode SkipWhite: centrale logica voor deze stap.
        private void SkipWhite()
        {
            while (char.IsWhiteSpace(Current)) _position++;
        }

        private char Current => _position < _text.Length ? _text[_position] : '\0';

        // Zoek/commentaar: Methode Error: centrale logica voor deze stap.
        private FormatException Error(string message) =>
            new($"{message} at position {_position} in expression '{_text}'.");
    }
}

