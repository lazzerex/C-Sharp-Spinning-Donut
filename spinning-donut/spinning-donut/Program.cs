using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace spinning_donut
{
    internal class Program
    {
        private const int Width = 80;
        private const int Height = 22;
        private const int BufferSize = Width * Height;
        private const string Luminance = ".,-~:;=!*#$@";
        private static readonly double[] SinLookup = new double[628];
        private static readonly double[] CosLookup = new double[628];

        static Program()
        {
            // Precompute trigonometric values
            for (int i = 0; i < 628; i++)
            {
                double angle = i * 0.01;
                SinLookup[i] = Math.Sin(angle);
                CosLookup[i] = Math.Cos(angle);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double FastSin(double x)
        {
            // Normalize angle to 0-2π range
            x = x % (2 * Math.PI);
            if (x < 0) x += 2 * Math.PI;

            int index = (int)(x * 100) % 628;
            return SinLookup[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double FastCos(double x)
        {
            // Normalize angle to 0-2π range
            x = x % (2 * Math.PI);
            if (x < 0) x += 2 * Math.PI;

            int index = (int)(x * 100) % 628;
            return CosLookup[index];
        }

        private static void ClearArray<T>(T[] array, T value)
        {
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = value;
            }
        }

        static void Main()
        {
            double A = 0, B = 0;

            // Use ArrayPool to reduce garbage collection
            double[] z = ArrayPool<double>.Shared.Rent(BufferSize);
            char[] output = ArrayPool<double>.Shared.Rent(BufferSize).Select(x => ' ').ToArray();
            char[] buffer = new char[BufferSize + Height]; // Extra space for newlines

            try
            {
                Console.CursorVisible = false;
                while (true)
                {
                    ClearArray(z, 0.0);
                    ClearArray(output, ' ');

                    for (double j = 0; j < 6.28; j += 0.07)
                    {
                        double cosj = FastCos(j);
                        double sinj = FastSin(j);
                        double sina = FastSin(A);
                        double cosa = FastCos(A);
                        double cosb = FastCos(B);
                        double sinb = FastSin(B);

                        for (double i = 0; i < 6.28; i += 0.02)
                        {
                            double sini = FastSin(i);
                            double cosi = FastCos(i);

                            double h = cosj + 2;
                            double D = 1 / (sini * h * sina + sinj * cosa + 5);
                            double t = sini * h * cosa - sinj * sina;

                            int x = (int)(40 + 30 * D * (cosi * h * cosb - t * sinb));
                            int y = (int)(11 + 15 * D * (cosi * h * sinb + t * cosb));

                            if (y >= 0 && y < Height && x >= 0 && x < Width)
                            {
                                int o = x + Width * y;
                                if (D > z[o])
                                {
                                    z[o] = D;
                                    int luminanceIndex = (int)(8 * ((sinj * sina - sini * cosj * cosa) * cosb - sini * cosj * sina - sinj * cosa - cosi * cosj * sinb));
                                    output[o] = Luminance[Math.Max(0, Math.Min(luminanceIndex, Luminance.Length - 1))];
                                }
                            }
                        }
                    }

                    // Efficiently build output with newlines
                    int bufferIndex = 0;
                    for (int y = 0; y < Height; y++)
                    {
                        Array.Copy(output, y * Width, buffer, bufferIndex, Width);
                        buffer[bufferIndex + Width] = '\n';
                        bufferIndex += Width + 1;
                    }

                    Console.SetCursorPosition(0, 0);
                    Console.Write(buffer, 0, bufferIndex);

                    A += 0.04;
                    B += 0.02;
                    Thread.Sleep(20);
                }
            }
            finally
            {
                Console.CursorVisible = true;
                ArrayPool<double>.Shared.Return(z);
                ArrayPool<double>.Shared.Return(output.Select(c => (double)c).ToArray());
            }
        }
    }
}
