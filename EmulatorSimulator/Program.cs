using System;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Threading;
namespace EmulatorSimulator
{
    class Program
    {
        public static string Header = @"
        ███████╗███╗   ███╗██╗   ██╗███████╗██╗███╗   ███╗
        ██╔════╝████╗ ████║██║   ██║██╔════╝██║████╗ ████║
        █████╗  ██╔████╔██║██║   ██║███████╗██║██╔████╔██║
        ██╔══╝  ██║╚██╔╝██║██║   ██║╚════██║██║██║╚██╔╝██║
        ███████╗██║ ╚═╝ ██║╚██████╔╝███████║██║██║ ╚═╝ ██║
        ╚══════╝╚═╝     ╚═╝ ╚═════╝ ╚══════╝╚═╝╚═╝     ╚═╝
        BMGJET 2020" + "\n";
        public static string HS = " Emulator Simulator 0.1\n\n";
        public static string FS = "\n(Enter The Number To Select Option)";
        private static SerialPort serialPort_0;
        public static byte[] Bin;
        public static byte Protocol;
        public static string BinFile;
        public static byte[] Version;
        public static int TimeOut = 60;
        public static byte[] Serial = new byte[] { 0x00, 0x00, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06 };
        public static byte[] EEPROM = new byte[] { 0x42, 0x4d, 0x47, 0x4a, 0x45, 0x54, 0x20, 0x32, 0x30, 0x32, 0x30 };
        //███████╗██╗   ██╗███╗   ██╗ ██████╗████████╗██╗ ██████╗ ███╗   ██╗███████╗
        //██╔════╝██║   ██║████╗  ██║██╔════╝╚══██╔══╝██║██╔═══██╗████╗  ██║██╔════╝
        //█████╗  ██║   ██║██╔██╗ ██║██║        ██║   ██║██║   ██║██╔██╗ ██║███████╗
        //██╔══╝  ██║   ██║██║╚██╗██║██║        ██║   ██║██║   ██║██║╚██╗██║╚════██║
        //██║     ╚██████╔╝██║ ╚████║╚██████╗   ██║   ██║╚██████╔╝██║ ╚████║███████║
        //╚═╝      ╚═════╝ ╚═╝  ╚═══╝ ╚═════╝   ╚═╝   ╚═╝ ╚═════╝ ╚═╝  ╚═══╝╚══════╝

        static bool LoadBin()
        {
            //Loads bin that was dragged onto application.
            try
            {
                byte[] file = System.IO.File.ReadAllBytes(BinFile);
                if (file.Length == 32768L)
                {
                    Bin = new byte[32768]; //Sets byte array size 32K
                    Bin = file;
                    Console.WriteLine("Loaded 32K");
                    return true;
                }
                else if (file.Length == 65536L)
                {
                    Bin = new byte[65536]; //Sets byte array size 64K
                    Bin = file;
                    Console.WriteLine("Loaded 64K");
                    return true;
                }

                return false; //Unsupported size fail.
            }
            catch
            {
                return false; //Something had issue fail.
            }
        }

        static byte Setup()
        {
            //Define what protocol to use.
            int P = 0;
            Console.WriteLine("\nWhat Protocol To Emulate: \n(1) Moates Ostrich 1.0\n(2) Moates Ostrich 2.0\n(3) CobraRTP\n(4) ECUTamer\n(0) Moates Demon(WIP)" + FS);
            FS = "";
            int.TryParse(Console.ReadLine(), out P);
            switch (P)
            {
                case 0:
                    Console.WriteLine("Protocol: Moates Demon");
                    Version = new byte[] { 0x01, 0x09, 0x44 };
                    break;
                case 1:
                    Console.WriteLine("Protocol: Moates Ostrich 1.0");
                    Version = new byte[] { 0x01, 0x28, 0x4F };
                    break;
                case 2:
                    Console.WriteLine("Protocol: Moates Ostrich 2.0");
                    Version = new byte[] { 0x14, 0x09, 0x4F };
                    break;
                case 3:
                    Console.WriteLine("Protocol: CobraRTP");
                    Version = new byte[] { 0x14, 0x18, 0x43 };
                    break;
                default:
                    Console.WriteLine("Protocol: ECUTamer");
                    Version = new byte[] { 0x14, 0x09, 0x4F };
                    break;
            }
            try
            {
                Console.WriteLine("\nSelect Port:");
                //Builds list of avaliable COMPorts
                foreach (string s in SerialPort.GetPortNames())
                {
                    Console.WriteLine(s);
                }
            }
            catch { Console.WriteLine("No Avaliable ComPorts"); }

            //Read back COMPORT selection.
            bool NC = true;
            while (NC)
            {
                string CP = Console.ReadLine().ToUpper();
                if (CP.Contains("COM"))
                {
                    serialPort_0 = new SerialPort();
                    //Setup Baud rate
                    Console.WriteLine("\nWhat Baud Rate:\n(1) 115.2K\n(2) 921.6K");
                    int B = 1;
                    int.TryParse(Console.ReadLine(), out B);
                    if (B == 2)
                    {
                        serialPort_0.BaudRate = 921600;
                    }
                    else
                    {
                        serialPort_0.BaudRate = 115200;
                    }
                    serialPort_0.PortName = CP;
                    serialPort_0.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
                    NC = false;
                }
                else
                {
                    Console.WriteLine("Must Use Format COM# (#=number)");
                }
            }
            return (byte)P; //Returns protocol selection.
        }


        static bool MakeBin()
        {
            //Makes a Bin File if one not opened.
            int P;
            Console.WriteLine("No Bin Loaded\nCreating Blank ECUSIM.bin\nWhat Chip Size Fo You Want To Emulate\n(1) 32KB\n(2) 64KB" + FS);
            FS = "";
            int.TryParse(Console.ReadLine(), out P);
            switch (P)
            {
                case 2:
                    Console.WriteLine("Creating 64KB");
                    Bin = new byte[65536];
                    break;
                default:
                    Console.WriteLine("Creating 32KB");
                    Bin = new byte[32768];
                    break;
            }
            try
            {
                BinFile = Directory.GetCurrentDirectory() + "\\EMUSIM.bin";
                File.WriteAllBytes(BinFile, Bin);
                return true;
            }
            catch
            {
                Console.WriteLine("Failed to create temp file EMUSIM.bin");
                return false;
            }
        }

        public static void SaveFile()
        {
            //Saves file to opened file or blank file.
            try
            {
                System.IO.File.WriteAllBytes(BinFile, Bin);
                Console.WriteLine("Saved Bytes To Bin: " + BinFile);
            }
            catch
            {
                Console.WriteLine("Failed To Saved Bytes To Bin: " + BinFile);
            }
        }

        //Erases byte array memory.
        public static void ClearFile()
        {
            Array.Clear(Bin, 0, Bin.Length);
            Console.WriteLine("Zeroed Bin Memory.");
        }

        //Help prompts
        public static void HELP()
        {
            string HelpHeader = @"
                ██╗  ██╗███████╗██╗     ██████╗ 
                ██║  ██║██╔════╝██║     ██╔══██╗
                ███████║█████╗  ██║     ██████╔╝
                ██╔══██║██╔══╝  ██║     ██╔═══╝ 
                ██║  ██║███████╗███████╗██║     
                ╚═╝  ╚═╝╚══════╝╚══════╝╚═╝" + "\n";
            string keys = "ESC = Exit Program\nSpace = Save Memory to File\nBackspace = Clear Memory";
            ConsoleKey H = Console.ReadKey().Key;
            switch (H)
            {
                case ConsoleKey.Escape:
                    Environment.Exit(0);
                    break;
                case ConsoleKey.Spacebar:
                    SaveFile();
                    break;
                case ConsoleKey.Backspace:
                    ClearFile();
                    break;
                default:
                    Console.WriteLine(HelpHeader + Encoding.ASCII.GetString(EEPROM) + HS + keys);
                    break;
            }
        }

        // ██████╗ ██████╗ ███╗   ███╗
        //██╔════╝██╔═══██╗████╗ ████║
        //██║     ██║   ██║██╔████╔██║
        //██║     ██║   ██║██║╚██╔╝██║
        //╚██████╗╚██████╔╝██║ ╚═╝ ██║
        // ╚═════╝ ╚═════╝ ╚═╝     ╚═╝
        public static void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            //Handles Incoming data.
            SerialPort sp = (SerialPort)sender;

            Thread.Sleep(TimeOut);// Delay to allow bluetooth buffer to fill before reading.
            byte[] inputbytes = new byte[sp.BytesToRead];
            sp.Read(inputbytes, 0, inputbytes.Length);
            if (inputbytes.Length != 0)
            {
                if (Protocol == 0) //Trys parse the Demon protocol
                {
                    if (!DemonAPI(inputbytes, sp))
                    {
                        Console.Write("SD: " + Encoding.Default.GetString(inputbytes).Substring(0, 16)); //Dump string that failed limited to 16 char
                    }
                }
                else
                {
                    if (!OstrichAPI(inputbytes, sp))
                    {
                        Console.Write("SD: " + Encoding.Default.GetString(inputbytes).Substring(0, 16)); //Dump string that failed limited to 16 char
                    }
                }
            }
        }

        public static void DataSender(byte[] DataByte, SerialPort sp)
        {
            if (sp.IsOpen)
            {
                sp.Write(DataByte, 0, DataByte.Length);
            }
        }

        //███╗   ███╗ █████╗ ██╗███╗   ██╗
        //████╗ ████║██╔══██╗██║████╗  ██║
        //██╔████╔██║███████║██║██╔██╗ ██║
        //██║╚██╔╝██║██╔══██║██║██║╚██╗██║
        //██║ ╚═╝ ██║██║  ██║██║██║ ╚████║
        //╚═╝     ╚═╝╚═╝  ╚═╝╚═╝╚═╝  ╚═══╝

        static void Main(string[] args)
        {
            //Code starts here
            Console.WriteLine(Header);

            //Checks if file has been dragged onto application.
            if (args.Length > 0 && File.Exists(args[0]))
            {
                BinFile = args[0];
                Console.WriteLine("Loading Bin:" + BinFile);
                if (!LoadBin()) //Trys to load the file.
                {
                    Console.WriteLine("Failed to load to byte array.");
                    if (!MakeBin()) //Trys to make new temp file.
                    {
                        Console.WriteLine("Complete Fail, Will close.");
                        Console.ReadKey();
                        Environment.Exit(0);
                    }
                }
            }
            else
            {
                if (!MakeBin()) //Trys to make new temp file.
                {
                    Console.WriteLine("Complete Fail, Will close.");
                    Console.ReadKey();
                    Environment.Exit(0);
                }
            }

            Protocol = Setup(); //Sets up Protocol
            serialPort_0.Open(); //Open COMPort
            if (serialPort_0.IsOpen)
            {
                Console.WriteLine(@"
██████╗ ██╗   ██╗███╗   ██╗███╗   ██╗██╗███╗   ██╗ ██████╗          
██╔══██╗██║   ██║████╗  ██║████╗  ██║██║████╗  ██║██╔════╝          
██████╔╝██║   ██║██╔██╗ ██║██╔██╗ ██║██║██╔██╗ ██║██║  ███╗         
██╔══██╗██║   ██║██║╚██╗██║██║╚██╗██║██║██║╚██╗██║██║   ██║         
██║  ██║╚██████╔╝██║ ╚████║██║ ╚████║██║██║ ╚████║╚██████╔╝██╗██╗██╗
╚═╝  ╚═╝ ╚═════╝ ╚═╝  ╚═══╝╚═╝  ╚═══╝╚═╝╚═╝  ╚═══╝ ╚═════╝ ╚═╝╚═╝╚═╝
Press Enter Key To See Command List.
");
            }
            else
            {
                Console.WriteLine(@"
███████╗ █████╗ ██╗██╗     ███████╗██████╗          
██╔════╝██╔══██╗██║██║     ██╔════╝██╔══██╗         
█████╗  ███████║██║██║     █████╗  ██║  ██║         
██╔══╝  ██╔══██║██║██║     ██╔══╝  ██║  ██║         
██║     ██║  ██║██║███████╗███████╗██████╔╝██╗██╗██╗
╚═╝     ╚═╝  ╚═╝╚═╝╚══════╝╚══════╝╚═════╝ ╚═╝╚═╝╚═╝
Press Any Key To Close!
");
                Console.ReadKey();
                Environment.Exit(0);
            }
            while (true)
            {
                HELP();
            }
        }


        //██████╗ ███████╗███╗   ███╗ ██████╗ ███╗   ██╗
        //██╔══██╗██╔════╝████╗ ████║██╔═══██╗████╗  ██║
        //██║  ██║█████╗  ██╔████╔██║██║   ██║██╔██╗ ██║
        //██║  ██║██╔══╝  ██║╚██╔╝██║██║   ██║██║╚██╗██║
        //██████╔╝███████╗██║ ╚═╝ ██║╚██████╔╝██║ ╚████║
        //╚═════╝ ╚══════╝╚═╝     ╚═╝ ╚═════╝ ╚═╝  ╚═══╝

        public static bool DemonAPI(byte[] bytearray, SerialPort sp)
        {

            return false;
        }

        // ██████╗ ███████╗████████╗██████╗ ██╗ ██████╗██╗  ██╗
        //██╔═══██╗██╔════╝╚══██╔══╝██╔══██╗██║██╔════╝██║  ██║
        //██║   ██║███████╗   ██║   ██████╔╝██║██║     ███████║
        //██║   ██║╚════██║   ██║   ██╔══██╗██║██║     ██╔══██║
        //╚██████╔╝███████║   ██║   ██║  ██║██║╚██████╗██║  ██║
        // ╚═════╝ ╚══════╝   ╚═╝   ╚═╝  ╚═╝╚═╝ ╚═════╝╚═╝  ╚═╝
        public static bool OstrichAPI(byte[] bytearray, SerialPort sp)
        {
            int bytelen = bytearray.Length;
            byte Cchecksum = bytearray[bytelen - 1];
            byte Tchecksum = 0;

            //Version request VV
            if (bytearray[0] == 0x56 && bytearray[1] == 0x56)
            {
                TimeOut = 600;
                Console.WriteLine("Version Requested.");
                DataSender(Version, sp);
                return true;
            }


            //Calculate checksum
            for (int i = 0; i <= bytelen - 2; i++)
            {
                Tchecksum += bytearray[i];
            }

            //Check valid checksum.
            if (Tchecksum != Cchecksum)
            {
                Console.WriteLine("CheckSum Failed");
                return false; //Not valid stop processing
            }

            //Read bin bytes slow
            if (bytearray[0] == 0x52 && bytearray.Length == 5)
            {
                TimeOut = 60;
                BinRead(bytearray, sp);
                return true;
            }

            //Write bin bytes slow
            if (bytearray[0] == 0x57)
            {
                TimeOut = 120;
                BinWrite(bytearray, sp);
                return true;
            }

            //Write bin bytes fast
            if (bytearray[0] == 0x5A && bytearray[1] == 0x57)
            {
                TimeOut = 600;
                BinFastWrite(bytearray, sp);
                return true;
            }

            //Read bin bytes fast
            if (bytearray[0] == 0x5A && bytearray[1] == 0x52)
            {
                TimeOut = 60;
                BinFastRead(bytearray, sp);
                return true;
            }

            //Serial number request
            if (bytearray[0] == 0x4E && bytearray[1] == 0x53)
            {
                Console.WriteLine("Serial Number Requested");
                DataSender(Serial, sp);
                return true;
            }

            //EEPROM Information requested
            //45 07 00 01 00 00 00 00 00 00 00 4D write
            //50 02 00 55 02 01 10 read
            //BA  read
            if (bytearray[0] == 0x45 && bytearray[1] == 0x07 && bytearray[3] == 0x01)
            {
                Console.WriteLine("EEPROM Info Requested");
                byte[] EEPROMINFO = new byte[] { 0x50, 0x02, 0x00, 0x55, 0x02, 0x01, 0x10 };
                DataSender(EEPROMINFO, sp);
                return true;
            }


            //EEPROM Information requested
            //48 52 07 00 01 A2 write
            //50 02 00 55 02 01 10  read
            //BA  read
            if (bytearray[0] == 0x48 && bytearray[1] == 0x52 && bytearray[2] == 0x07 && bytearray[4] == 0x01 && bytearray[5] == 0xA2)
            {
                Console.WriteLine("EEPROM Info Requested");
                byte[] EEPROMINFO = new byte[] { 0x50, 0x02, 0x00, 0x55, 0x02, 0x01, 0x10 };
                DataSender(EEPROMINFO, sp);
                return true;
            }

            //Select which bank to emulate
            //42 45 45 CC write
            //00 read
            if (bytearray[0] == 0x42 && bytearray[1] == 0x45 && bytearray[2] == 0x45 && bytearray[3] == 0xCC)
            {
                Console.WriteLine("Bank Select Requested");
                byte[] BANKINFO = new byte[] { 0x00 };
                DataSender(BANKINFO, sp);
                return true;
            }

            //Get Active Emulation Bank Succeeded: Bank 0
            //42 52 52 E6 write
            //00 read
            if (bytearray[0] == 0x42 && bytearray[1] == 0x52 && bytearray[2] == 0x52 && bytearray[3] == 0xE6)
            {
                Console.WriteLine("Active Bank Info Requested");
                byte[] BANKINFO = new byte[] { 0x00 };
                DataSender(BANKINFO, sp);
                return true;
            }
            //Get Static Emulation Bank
            //42 45 53 DA write
            //oo read
            if (bytearray[0] == 0x42 && bytearray[1] == 0x45 && bytearray[2] == 0x53 && bytearray[3] == 0xDA)
            {
                Console.WriteLine("Static Bank Info Requested");
                byte[] BANKINFO = new byte[] { 0x00 };
                DataSender(BANKINFO, sp);
                return true;
            }

            //Quick CheckSum
            //Not standard Command Added by BMGJET,
            //5a 43 53 PacketCS
            if (bytearray[0] == 0x5A && bytearray[1] == 0x43 && bytearray[2] == 0x53)
            {
                QuckCS(sp);
                return true;
            }

            return false; //No valid commands found.
        }


        //Fast Write
        public static void BinFastWrite(byte[] bytearray, SerialPort sp)
        {
            int BlockSize = (256 * (bytearray[2]));

            if (bytearray.Length == BlockSize + 6)
            {
                int Address = GetAddress(bytearray[3], bytearray[4]);

                //Pack bin with write bytes.
                for (int i = 0; i < BlockSize; i++)
                {
                    Bin[Address + i] = bytearray[i + 5];
                }
                byte[] OK = new byte[] { 0x4F };
                Console.WriteLine(BlockSize + " Bytes written to " + Address);
                DataSender(OK, sp);
            }
        }

        //Slow Write
        public static void BinWrite(byte[] bytearray, SerialPort sp)
        {
            int BlockSize = GetBlockSize(bytearray[1]);

            if (bytearray.Length == BlockSize + 5)
            {
                int Address = GetAddress(bytearray[3], bytearray[2]);

                //Pack bin with write bytes.
                for (int i = 0; i < BlockSize; i++)
                {
                    Bin[Address + i] = bytearray[i + 4];
                }
                byte[] OK = new byte[] { 0x4F };
                Console.WriteLine(BlockSize + " Bytes written to " + Address);
                DataSender(OK, sp);
            }
        }

        //Slow Read
        public static void BinRead(byte[] bytearray, SerialPort sp)
        {
            int BlockSize = GetBlockSize(bytearray[1]);

            //Setup buffer
            byte[] Binbuff = new byte[BlockSize];
            int Address = GetAddress(bytearray[3], bytearray[2]);

            //Pack the buffer
            for (int i = 0; i <= BlockSize - 1; i++)
            {
                Binbuff[i] = Bin[Address + i];
            }
            //Output buffer.
            Console.WriteLine(BlockSize + " Bytes read from " + Address);
            DataSender(checksum(Binbuff), sp);
        }


        //Fast read
        public static void BinFastRead(byte[] bytearray, SerialPort sp)
        {
            int BlockSize = (256 * (bytearray[2]));
            int Address = GetAddress(bytearray[3], bytearray[4]);

            //Setup buffer
            byte[] Binbuff = new byte[BlockSize];

            //Pack the buffer
            for (int i = 0; i <= BlockSize - 1; i++)
            {
                Binbuff[i] = Bin[Address + i];
            }
            //Output buffer.
            Console.WriteLine(BlockSize + " Bytes read from " + Address);
            DataSender(checksum(Binbuff), sp);
        }

        //Block size correction.
        public static int GetBlockSize(int BlockSize)
        {
            if (BlockSize == 0)
            {
                BlockSize = 256;
            }
            return BlockSize;
        }

        //Get byte array address
        public static int GetAddress(int MSB, int LSB)
        {
            int Address = (MSB | LSB << 8);
            if (Address >= Bin.Length)
            {
                Address -= 32768; //Resize 64K back to 32K.
            }
            return Address;
        }

        //Calculate Checksum and create new byte array with it added on.
        public static byte[] checksum(byte[] bArray)
        {
            byte cs = 0;
            foreach (byte raw in bArray)
            {
                cs += raw;
            }
            byte[] newArray = new byte[bArray.Length + 1];
            bArray.CopyTo(newArray, 0);
            newArray[newArray.Length - 1] = cs;
            return newArray;
        }

        //███████╗██╗  ██╗████████╗██████╗  █████╗ 
        //██╔════╝╚██╗██╔╝╚══██╔══╝██╔══██╗██╔══██╗
        //█████╗   ╚███╔╝    ██║   ██████╔╝███████║
        //██╔══╝   ██╔██╗    ██║   ██╔══██╗██╔══██║
        //███████╗██╔╝ ██╗   ██║   ██║  ██║██║  ██║
        //╚══════╝╚═╝  ╚═╝   ╚═╝   ╚═╝  ╚═╝╚═╝  ╚═╝

        //Quick Check Sum
        //5a 43 53 PacketCS            write
        //5a 43 53 MemoryCS PacketCS   read

        public static void QuckCS(SerialPort sp)
        {
            //Sum of Emulator Memory.
            byte[] QCS = new byte[1];
            foreach (byte B in Bin)
            {
                QCS[0] += B;
            }
            Console.WriteLine("Quick CheckSum: " + QCS[0].ToString("X2"));
            DataSender(QCS, sp);
        }
    }
}


