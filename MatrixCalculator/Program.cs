using System.Diagnostics;

#region Sequential_Loop
void Multiply(double[,] A, double[,] B, double[,] C, long size) // C = A*B
{
    for (long i = 0; i < size; i++)
        for (long j = 0; j < size; j++)
        {
            C[i, j] = 0.0;
            for (long k = 0; k < size; k++)
                C[i, j] += A[i, k] * B[k, j];
        }
}
#endregion

#region Parallel_Loop
void MultiplyParallel(double[,] A, double[,] B, double[,] C, long size)
{
    Parallel.For(0, size, i =>
    {
        for (long j = 0; j < size; j++)
        {
            C[i, j] = 0.0;
            for (long k = 0; k < size; k++)
                C[i, j] += A[i, k] * B[k, j];
        }
    });
}
#endregion

#region Print_Matrix
void PrintMatrix(double[,] A, long size)
{
    for (long i = 0; i < size; i++)
    {
        for (long j = 0; j < size; j++)
            Console.Write(" " + A[i, j]);
        Console.WriteLine();
    }
}
#endregion

void Test()
{
    long size = 3;

    double[,] A = new double[size, size];
    double[,] B = new double[size, size];
    double[,] C = new double[size, size];

    A[0, 0] = 14.0; A[0, 1] = 9.0; A[0, 2] = 3.0;
    A[1, 0] = 2.0; A[1, 1] = 11.0; A[1, 2] = 15.0;
    A[2, 0] = 0.0; A[2, 1] = 12.0; A[2, 2] = 17.0;

    B[0, 0] = 12.0; B[0, 1] = 25.0; B[0, 2] = 5.0;
    B[1, 0] = 9.0; B[1, 1] = 10.0; B[1, 2] = 0.0;
    B[2, 0] = 8.0; B[2, 1] = 5.0; B[2, 2] = 1.0;

    Multiply(A, B, C, size);

    PrintMatrix(C, size);
}

#region Main
Test();

Console.WriteLine("Now test with custom size!");
Console.Write("Enter size: ");
long size = long.Parse(Console.ReadLine());
double[,] A = new double[size, size];
double[,] B = new double[size, size];
double[,] C = new double[size, size];
Console.WriteLine("size = " + size);

// First do the sequential version.
Console.WriteLine("Executing sequential loop...");
Stopwatch stopwatch = new Stopwatch();
stopwatch.Start();
Multiply(A, B, C, size);
stopwatch.Stop();
Console.WriteLine("Sequential loop time in milliseconds: {0}",
                        stopwatch.ElapsedMilliseconds);

// Reset timer and results matrix.
stopwatch.Reset();
C = new double[size, size];

// Do the parallel loop.
Console.WriteLine("Executing parallel loop...");
stopwatch.Start();
MultiplyParallel(A, B, C, size);
stopwatch.Stop();
Console.WriteLine("Parallel loop time in milliseconds: {0}",
                        stopwatch.ElapsedMilliseconds);

// Keep the console window open in debug mode.
Console.WriteLine("Press any key to exit.");
Console.ReadKey();
#endregion