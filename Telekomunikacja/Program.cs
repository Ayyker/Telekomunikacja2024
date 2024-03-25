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
        if (encodedData.Length < 12) {
            Array.Resize(ref encodedData, 12);
            // Żeby kod hamminga poprawnie liczył musimy dodać (padding) zera na końcu ostatniego bajta, Array.Resize automatycznie inicjalizuje dodane pola zerami
        }
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
        if (encodedData.Length < 16) {
            Array.Resize(ref encodedData, 16);
            // Żeby kod hamminga poprawnie liczył musimy dodać (padding) zera na końcu ostatniego bajta, Array.Resize automatycznie inicjalizuje dodane pola zerami
        }

        //Console.WriteLine($"Wartość przekazana do DecodeDoubleError: {String.Join("", encodedData)}");
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
        int[] errorPositions = { -1, -1 }; //Zakładamy, że na starcie nie ma błędów

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

        return errorPositions; //Zwracamy pozycje błędów, w razie ich braku zwraca -1, -1
    }

    private static byte[] BitsToBytes(int[] bits) {
        List<byte> bytes = new List<byte>();
        for (int i = 0; i < bits.Length; i += 8) {
            byte b = 0;
            for (int j = 0; j < 8 && i + j < bits.Length; j++) {
                b |= (byte)(bits[i + j] << (7 - j));
            }
            bytes.Add(b);
        }
        return bytes.ToArray();
    }

    private static int[] BytesToBits(byte[] bytes) {
        List<int> bits = new List<int>();
        foreach (byte b in bytes) {
            for (int i = 7; i >= 0; i--) {
                bits.Add((b >> i) & 1);
            }
        }
        return bits.ToArray();
    }

    public static void EncodeFile(string inputFile, string outputFile, bool useDoubleError) {
        byte[] fileData = File.ReadAllBytes(inputFile);
        List<int> encodedData = new List<int>();

        foreach (byte b in fileData) {
            int[] dataBits = BytesToBits(new byte[] { b });
            int[] encodedBits = useDoubleError ? EncodeDoubleError(dataBits) : EncodeSingleError(dataBits);
            encodedData.AddRange(encodedBits);
        }

        byte[] encodedBytes = BitsToBytes(encodedData.ToArray());
        File.WriteAllBytes(outputFile, encodedBytes);
    }

    public static void DecodeFile(string inputFile, string outputFile, bool useDoubleError) {
        byte[] fileData = File.ReadAllBytes(inputFile);
        List<int> decodedData = new List<int>();

        int[] bits = BytesToBits(fileData);
        for (int i = 0; i < bits.Length; i += useDoubleError ? 16 : 12) {
            int[] encodedBits = bits.Skip(i).Take(useDoubleError ? 16 : 12).ToArray();
            int[] decodedBits = useDoubleError ? DecodeDoubleError(encodedBits) : DecodeSingleError(encodedBits);
            decodedData.AddRange(decodedBits);
        }

        byte[] decodedBytes = BitsToBytes(decodedData.ToArray());
        File.WriteAllBytes(outputFile, decodedBytes);
    }

    public static void TestErrorCorrection(int[] data, bool isDoubleError) {
        int[] encodedData;
        int[] decodedData;

        if (isDoubleError) {
            encodedData = EncodeDoubleError(data);

            //Symulacja dwóch błędów
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
                        Console.WriteLine("Nieprawidłowa wartość, wprowadź liczbę od 0 do 15:");
                    }

                    Console.WriteLine("Wprowadź drugą pozycję błędu:");
                    while (!int.TryParse(Console.ReadLine(), out errorPosition2) || errorPosition2 < 0 || errorPosition2 > 15 || errorPosition2 == errorPosition1) {
                        Console.WriteLine("Nieprawidłowa wartość lub taka sama jak pierwsza pozycja, wprowadź inną liczbę od 0 do 15:");
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

            Console.WriteLine("Podaj metode wprowadzania bledu:\n1. Losowa\n2. Wprowadz sam ");
            int choice;
            string input = Console.ReadLine();
            int errorPosition = -1;
            if (int.TryParse(input, out choice)) {
                if (choice == 1) {
                    Random random = new Random();
                    errorPosition = random.Next(8);

                } else if (choice == 2) {
                    Console.WriteLine("Wprowadź pozycję błędu:");
                    while (!int.TryParse(Console.ReadLine(), out errorPosition) || errorPosition < 0 || errorPosition > 11) {
                        Console.WriteLine("Nieprawidłowa wartość, wprowadź liczbę od 0 do 11:");
                    }
                } else {
                    Console.WriteLine("Nieprawidłowy wybór");
                }
            } else {
                Console.WriteLine("Proszę podać liczbę");
            }

            // Symulacja jednego błędu
            encodedData[errorPosition] ^= 1;

            decodedData = DecodeSingleError(encodedData);
        }

        Console.WriteLine($"Oryginalna wiadomość: {String.Join("", data)}");
        Console.WriteLine($"Zakodowana: {String.Join("", encodedData)}");
        Console.WriteLine($"Zdekodowana: {String.Join("", decodedData)}");
    }
}

class Program {
    static void Main() {

        // Testowa 8-bitowa wiadomość jako tablica intów
        int[] message = new int[] { 1, 0, 0, 1, 1, 0, 1, 0 };

        Console.WriteLine("Wybierz co chcesz zrobić: \n1. Zakoduj plik\n2. Dekoduj plik z korekcją błędów\n3. Funkcja testowa dla pojedynczego słowa");
        string input = Console.ReadLine();
        char choice1 = input.Length > 0 ? input[0] : '\0'; //Czytaj tylko pierwszy znak

        if (choice1 == '1') {
            Console.WriteLine("Podaj ścieżkę do oryginalnego pliku: ");
            string filePath = Console.ReadLine();
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
            string encodedFileSingleError = $"{fileNameWithoutExtension} SE.ec";
            string encodedFileDoubleError = $"{fileNameWithoutExtension} DE.ec";
            ErrorCorrectingCode.EncodeFile(filePath, encodedFileSingleError, false);
            ErrorCorrectingCode.EncodeFile(filePath, encodedFileDoubleError, true);
        } else if (choice1 == '2') {
            Console.WriteLine("Podaj ścieżkę do oryginalnego pliku: ");
            string filePath = Console.ReadLine();
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
            string fileExtension = Path.GetExtension(filePath);
            string encodedFileSingleError = $"{fileNameWithoutExtension} SE.ec";
            string encodedFileDoubleError = $"{fileNameWithoutExtension} DE.ec";
            string decodedFileSingleError = $"{fileNameWithoutExtension} SE{fileExtension}";
            string decodedFileDoubleError = $"{fileNameWithoutExtension} DE{fileExtension}";
            ErrorCorrectingCode.DecodeFile(encodedFileSingleError, decodedFileSingleError, false);
            ErrorCorrectingCode.DecodeFile(encodedFileDoubleError, decodedFileDoubleError, true);
        } else if (choice1 == '3') {
            Console.WriteLine("Wybierz jaki typ błędu:\n1. Pojedynczy\n2. Podwójny");
            string input2 = Console.ReadLine();
            char choice2 = input2.Length > 0 ? input2[0] : '\0'; //Czytaj tylko pierwszy znak
            if (choice2 == '1') {
                ErrorCorrectingCode.TestErrorCorrection(message, false);
            } else if (choice2 == '2') {
                ErrorCorrectingCode.TestErrorCorrection(message, true);
            } else {
                Console.WriteLine("Nieprawidłowy wybór!");
            }
        } else { 
            Console.WriteLine("Nieprawidłowy wybór!");
        }        
    }
}

