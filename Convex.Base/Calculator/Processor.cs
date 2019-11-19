﻿#region

using System;

#endregion

namespace Convex.Base.Calculator
{
    public partial class InlineCalculator
    {
        private void Calculate(string op, double op1, double op2)
        {
            try
            {
                double res = op switch
                {
                    Token.ADD => (op1 + op2),
                    Token.SUBTRACT => (op1 - op2),
                    Token.MULTIPLY => (op1 * op2),
                    Token.DIVIDE => (op1 / op2),
                    Token.MOD => (op1 % op2),
                    Token.POWER => Math.Pow(op1, op2),
                    Token.LOG => Math.Log(op2, op1),
                    Token.ROOT => Math.Pow(op2, 1 / op1),
                    _ => throw new ArgumentOutOfRangeException()
                };

                _Operands.Push(PostProcess(res));
            }
            catch (Exception e)
            {
                ThrowException(e.Message);
            }
        }

        private void Calculate(string op, double operand)
        {
            double res = 1;

            try
            {
                switch (op)
                {
                    case Token.UNARY_MINUS:
                        res = -operand;
                        break;

                    case Token.ABS:
                        res = Math.Abs(operand);
                        break;

                    case Token.A_COSINE:
                        res = Math.Acos(operand);
                        break;

                    case Token.A_SINE:
                        res = Math.Asin(operand);
                        break;

                    case Token.A_TANGENT:
                        res = Math.Atan(operand);
                        break;

                    case Token.COSINE:
                        res = Math.Cos(operand);
                        break;

                    case Token.SINE:
                        res = Math.Sin(operand);
                        break;

                    case Token.TANGENT:
                        res = Math.Tan(operand);
                        break;

                    case Token.LN:
                        res = Math.Log(operand);
                        break;

                    case Token.LOG10:
                        res = Math.Log10(operand);
                        break;

                    case Token.SQRT:
                        res = Math.Sqrt(operand);
                        break;

                    case Token.EXP:
                        res = Math.Exp(operand);
                        break;

                    case Token.FACTORIAL:
                        //for (int i = 2; i <= (int) operand; res *= i++)
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }

                _Operands.Push(PostProcess(res));
            }
            catch (Exception e)
            {
                ThrowException(e.Message);
            }
        }

        private static double PostProcess(double result)
        {
            return Math.Round(result, 10);
        }
    }
}