﻿#region

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

#endregion

namespace Convex.Base.Calculator
{
    public partial class InlineCalculator
    {
        public enum CalcMode
        {
            Numeric,
            Logic
        }

        public static class Token
        {
            public const string P_LEFT = "(",
                P_RIGHT = ")",
                POWER = "^",
                UNARY_MINUS = "_",
                ADD = "+",
                SUBTRACT = "-",
                MULTIPLY = "*",
                DIVIDE = "/",
                FACTORIAL = "!",
                MOD = "%",
                SENTINEL = "#",
                END = ";",
                STORE = "=",
                NONE = " ",
                SEPERATOR = ",";

            public const string SINE = "sin",
                COSINE = "cos",
                TANGENT = "tan",
                A_SINE = "asin",
                A_COSINE = "acos",
                A_TANGENT = "atan",
                LOG = "log",
                LOG10 = "log10",
                LN = "ln",
                EXP = "exp",
                ABS = "abs",
                SQRT = "sqrt",
                ROOT = "rt";

            private static readonly string[] _BinaryOperators =
            {
                MULTIPLY,
                DIVIDE,
                SUBTRACT,
                ADD,
                POWER,
                LOG,
                ROOT,
                MOD
            };

            private static readonly string[] _UnaryOperators =
            {
                SUBTRACT,
                SINE,
                COSINE,
                TANGENT,
                A_SINE,
                A_COSINE,
                A_TANGENT,
                LOG10,
                LN,
                EXP,
                ABS,
                SQRT
            };

            private static readonly string[] _SpecialOperators =
            {
                SENTINEL,
                END,
                STORE,
                NONE,
                SEPERATOR,
                P_RIGHT
            };

            private static readonly string[] _RightSideOperators =
            {
                FACTORIAL
            };

            private static readonly string[] _FunctionList =
            {
                SINE,
                COSINE,
                TANGENT,
                A_SINE,
                A_COSINE,
                A_TANGENT,
                LOG,
                LOG10,
                LN,
                EXP,
                ABS,
                SQRT,
                ROOT
            };

            private static readonly string[] _LastProcessedOperators =
            {
                POWER
            };

            private static int Precedence(string op)
            {
                if (IsFunction(op))
                {
                    return 64;
                }

                switch (op)
                {
                    case SUBTRACT:
                        return 4;
                    case ADD:
                        return 4;
                    case UNARY_MINUS:
                        return 8;
                    case MULTIPLY:
                        return 16;
                    case DIVIDE:
                        return 16;
                    case POWER:
                        return 24;
                    case MOD:
                        return 32;
                    case FACTORIAL:
                        return 48;
                    case P_LEFT:
                        return 64;
                    case P_RIGHT:
                        return 64;

                    default:
                        return 0; // operators:  END, Sentinel, Store
                }
            }

            public static int Compare(string op1, string op2)
            {
                if (op1.Equals(op2) && Contains(op1, _LastProcessedOperators))
                {
                    return -1;
                }

                return Precedence(op1) >= Precedence(op2)
                    ? 1
                    : -1;
            }

            public static string ConvertOperator(string op)
            {
                switch (op)
                {
                    case "-":
                        return "_";
                    default:
                        return op;
                }
            }

            public static string ToString(string op)
            {
                switch (op)
                {
                    case END:
                        return "END";
                    default:
                        return op;
                }
            }

            private static bool Contains(string token, IEnumerable<string> array)
            {
                return array.Any(s => s.Equals(token));
            }

            #region Is... Functions

            public static bool IsBinary(string op) => Contains(op, _BinaryOperators);

            public static bool IsUnary(string op) => Contains(op, _UnaryOperators);

            public static bool IsRightSide(string op) => Contains(op, _RightSideOperators);

            public static bool IsSpecial(string op) => Contains(op, _SpecialOperators);

            public static bool IsFunction(string op) => Contains(op, _FunctionList);

            public static bool IsName(string token) => Regex.IsMatch(token, @"[a-zA-Z0-9]");

            public static bool IsDigit(string token) => Regex.IsMatch(token, @"\d|\.");

            #endregion
        }
    }
}
