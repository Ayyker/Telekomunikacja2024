using System;
using System.Linq;

class ErrorCorrectingCode {
    private static readonly int[,] HSingleError = new int[4, 12] {
        { 1, 1, 0, 1, 1, 0, 0, 0, 1, 0, 0, 0 },
        { 1, 0, 1, 1, 0, 1, 0, 0, 0, 1, 0, 0 },
        { 0, 1, 1, 1, 0, 0, 1, 0, 0, 0, 1, 0 },
        { 0, 0, 0, 0, 1, 1, 1, 1, 0, 0, 0, 1 }
    };

    private static readonly int[,] HDoubleError = new int[8, 16]
    {
        {1, 1, 1, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0},
        {1, 1, 0, 0, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0},
        {1, 0, 1, 0, 1, 0, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0},
        {0, 1, 0, 1, 0, 1, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0},
        {1, 1, 1, 0, 1, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0},
        {1, 0, 0, 1, 0, 1, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0},
        {0, 1, 1, 1, 1, 0, 1, 1, 0, 0, 0, 0, 0, 0, 1, 0},
        {1, 1, 1, 0, 0, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 1}
    };

    public static int[] EncodeSingleError(int[] data) {
        int[] encodedBits = new int[12];
        Array.Copy(data, encodedBits, 8);

        for (int i = 0; i < 4; i++) {
            int parityBit = 0;
            for (int j = 0; j < 8; j++) {
                if (HSingleError[i, j] == 1) {
                    parityBit ^= data[j];
                }
            }
            encodedBits[8 + i] = parityBit;
        }

        return encodedBits;
    }

    public static int[] DecodeSingleError(int[] encodedData) {
        int[] syndrome = new int[4];
        for (int i = 0; i < 4; i++) {
            for (int j = 0; j < 12; j++) {
                if (HSingleError[i, j] == 1) {
                    syndrome[i] ^= encodedData[j];
                }
            }
        }

        int errorPosition = -1;
        for (int i = 0; i < 12; i++) {
            bool match = true;
            for (int j = 0; j < 4; j++) {
                if (syndrome[j] != HSingleError[j, i]) {
                    match = false;
                    break;
                }
            }
            if (match) {
                errorPosition = i;
                break;
            }
        }

        if (errorPosition != -1) {
            encodedData[errorPosition] ^= 1;
        }

        return encodedData.Take(8).ToArray();
    }

    public static int[] EncodeDoubleError(int[] data) {
        int[] encodedBits = new int[16];
        Array.Copy(data, encodedBits, 8);

        for (int i = 0; i < 8; i++) {
            int parityBit = 0;
            for (int j = 0; j < 8; j++) {
                if (HDoubleError[i, j] == 1) {
                    parityBit ^= data[j];
                }
            }
            encodedBits[8 + i] = parityBit;
        }

        return encodedBits;
    }

    public static int[] DecodeDoubleError(int[] encodedData) {
        Console.WriteLine($"Wartość przekazana do DecodeDoubleError: {String.Join("", encodedData)}");
        int[] syndrome = new int[8];
        for (int i = 0; i < 8; i++) {
            for (int j = 0; j < 16; j++) {
                if (HDoubleError[i, j] == 1) {
                    syndrome[i] ^= encodedData[j];
                }
            }
        }

        int[] errorPositions = FindErrorPositions(HDoubleError, syndrome);

        foreach (var pos in errorPositions) {
            if (pos >= 0 && pos < 16) {
                encodedData[pos] ^= 1;
            }
        }

        return encodedData.Take(8).ToArray();
    }

    private static int[] FindErrorPositions(int[,] H, int[] syndrome) {
        int rows = H.GetLength(0);
        int cols = H.GetLength(1);
        int[] errorPositions = { -1, -1 }; // Assume no errors initially

        for (int i = 0; i < cols; i++) {
            for (int j = i + 1; j < cols; j++) {
                bool match = true;
                for (int k = 0; k < rows; k++) {
                    int expectedSyndromeBit = H[k, i] ^ H[k, j];
                    if (syndrome[k] != expectedSyndromeBit) {
                        match = false;
                        break;
                    }
                }
                if (match) {
                    errorPositions[0] = i;
                    errorPositions[1] = j;
                    return errorPositions;
                }
            }
        }

        return errorPositions; // Return positions of errors if found, otherwise -1, -1
    }

    public static void TestErrorCorrection(int[] data, bool isDoubleError) {
        int[] encodedData;
        int[] decodedData;

        if (isDoubleError) {
            encodedData = EncodeDoubleError(data);

            // Introduce two bit errors
            Console.WriteLine("Podaj metode wprowadzania bledu:\n1. Losowa\n2. Wprowadz sam ");
            string input = Console.ReadLine();

            int choice;
            int errorPosition1 = -1;
            int errorPosition2 = -1;
            if (int.TryParse(input, out choice)) {
                if (choice == 1) {
                    Random random = new Random();
                    errorPosition1 = random.Next(16);
                    errorPosition2 = random.Next(16);
                    while (errorPosition2 == errorPosition1) {
                        errorPosition2 = random.Next(16);
                    }
                } else if (choice == 2) {
                    Console.WriteLine("Wprowadź pierwszą pozycję błędu:");
                    while (!int.TryParse(Console.ReadLine(), out errorPosition1) || errorPosition1 < 0 || errorPosition1 > 15) {
                        Console.WriteLine("Nieprawidłowa wartość, wprowadź liczbę od 0 do 7:");
                    }

                    Console.WriteLine("Wprowadź drugą pozycję błędu:");
                    while (!int.TryParse(Console.ReadLine(), out errorPosition2) || errorPosition2 < 0 || errorPosition2 > 15 || errorPosition2 == errorPosition1) {
                        Console.WriteLine("Nieprawidłowa wartość lub taka sama jak pierwsza pozycja, wprowadź inną liczbę od 0 do 7:");
                    }
                } else {
                    Console.WriteLine("Nieprawidłowy wybór");
                }
            } else {
                Console.WriteLine("Proszę podać liczbę");
            }
            
            Console.WriteLine($"Pierwsza pozycja: {errorPosition1+1}, druga pozycja: {errorPosition2+1}");
            Console.WriteLine($"Zdekodowana data przed zepsuciem: {String.Join("", encodedData)}");
            
            encodedData[errorPosition1] ^= 1;
            encodedData[errorPosition2] ^= 1;
            Console.WriteLine($"Zdekodowana data po zepsuciu: {String.Join("", encodedData)}");

            decodedData = DecodeDoubleError(encodedData);
        } else {
            encodedData = EncodeSingleError(data);

            // Introduce a single bit error
            Random random = new Random();
            int errorPosition = random.Next(16);
            encodedData[errorPosition] ^= 1;

            decodedData = DecodeSingleError(encodedData);
        }

        Console.WriteLine($"Original: {String.Join("", data)}");
        Console.WriteLine($"Encoded: {String.Join("", encodedData)}");
        Console.WriteLine($"Decoded: {String.Join("", decodedData)}");
    }
}

class Program {
    static void Main() {
        // Sample 8-bit message
        int[] message = new int[] { 1, 0, 0, 1, 1, 0, 1, 0 };

        ErrorCorrectingCode.TestErrorCorrection(message, true);
    }
}

