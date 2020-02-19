using System;
using System.IO;
using System.IO.Ports;
using System.Threading;

namespace EmulatorSimulator
{
    class Program
    {
        private static SerialPort serialPort_0;
        public static byte[] Bin;
        public static int TimeOut = 20;
        public static byte Protocol;
        public static string BinFile;

        static bool LoadBin()
        {
            try
            {
                byte[] file = System.IO.File.ReadAllBytes(BinFile);
                if (file.Length == 32768L)
                {
                    Bin = new byte[32768];
                    Bin = file;
                    Console.WriteLine("Loaded 32K");
                    return true;
                }
                else if (file.Length == 65536L)
                {
                    Bin = new byte[65536];
                    Bin = file;
                    Console.WriteLine("Loaded 64K");
                    return true;
                }
                return false;
            }
            catch
            { return false; }
        }

        static byte Setup()
        {
            int P = 0;
            Console.WriteLine("What Protocol To Emulate: \n(1) Moates Ostrich\n(2) Moates Demon");
            int.TryParse(Console.ReadLine(), out P);
            switch (P)
            {
                case 1:
                    Console.WriteLine("Protocol: Moates Ostrich\nSelect Port:");
                    break;
                case 2:
                    Console.WriteLine("Protocol: Moates Demon\nSelect Port:");
                    break;
                default:
                    Console.WriteLine("Protocol: Auto\nSelect Port:");
                    break;
            }
            try
            {
                foreach (string s in SerialPort.GetPortNames())
                {
                    Console.WriteLine(s);
                }
            }
            catch { Console.WriteLine("No Avaliable ComPorts"); }

            string CP = Console.ReadLine().ToUpper();
            if (CP.Contains("COM"))
            {
                serialPort_0 = new SerialPort();
                Console.WriteLine("What Baud Rate:\n(1) 115.2K\n(2) 921.6K");
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
            }

            return (byte)P;
        }

        public static void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            Thread.Sleep(TimeOut);
            byte[] inputbytes = new byte[sp.BytesToRead];
            sp.Read(inputbytes, 0, inputbytes.Length);
            if (inputbytes.Length != 0)
            {
               Ostrich1API(inputbytes, sp);
            }

        }

        public static void DataSender(byte[] DataByte, SerialPort sp)
        {
            if (sp.IsOpen)
            {
                sp.Write(DataByte, 0, DataByte.Length);
            }
        }

        static bool MakeBin()
        {
            int P;
            Console.WriteLine("No Bin Loaded Creating Blank.\nWhat Chip size do you want to emulate\n(1) 32KB\n(2) 64KB");
            int.TryParse(Console.ReadLine(), out P);
            switch (P)
            {
                case 2:
                    Console.WriteLine("Creating 64K");
                    Bin = new byte[65536];
                    break;
                default:
                    Console.WriteLine("Creating 32K");
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

            static void Main(string[] args)
        {
            
            Console.WriteLine("BMGJET Emulator Simulator");
            if (args.Length > 0 && File.Exists(args[0]))
            {
                string path;
                BinFile = args[0];
                Console.WriteLine("Loading Bin:" + BinFile);
                if (!LoadBin())
                {
                    Console.WriteLine("Failed to load to byte array.");
                }
            }
            else
            {
                if (!MakeBin())
                {
                    
                }
            }
            Protocol = Setup();
            serialPort_0.Open();
            if (serialPort_0.IsOpen)
            {
                Console.WriteLine("EMU Running....");
            }
            else
            {
                Console.WriteLine("EMU Failed....");
            }
            while (true)
            {
                if (Console.ReadKey().Key == ConsoleKey.Escape)
                {
                    Environment.Exit(0);
                }
                else if (Console.ReadKey().Key == ConsoleKey.Enter)
                {
                    try
                    {
                        System.IO.File.WriteAllBytes(BinFile, Bin);
                        Console.WriteLine("Saved Bytes to Bin: " + BinFile);
                    }
                    catch
                    {
                        Console.WriteLine("Failed To Saved Bytes to Bin: " + BinFile);
                    }
                }
            }
        }


        //
        //
        //
        //
        public static bool Ostrich1API(byte[] bytearray, SerialPort sp)
        {
            int bytelen = bytearray.Length;
            byte Cchecksum = bytearray[bytelen - 1];
            byte Tchecksum = 0;
            byte[] Version = new byte[] { 0x01, 0x28, 0x4F };
            byte[] Serial = checksum(new byte[] { 0x00, 0x00, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06 });

            //Version request VV
            if (bytearray[0] == 0x56 && bytearray[1] == 0x56)
            {
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

            //Read bin bytes.
            if (bytearray[0] == 0x52 && bytearray.Length == 5)
            {
                TimeOut = 40;
                BinRead(bytearray, sp);
                return true;
            }

            //Write bin bytes.
            if (bytearray[0] == 0x57)
            {
                TimeOut = 120;
                BinWrite(bytearray, sp);
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
            return false;
        }

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

        //52 00 80 00 D2
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

        public static int GetBlockSize(int BlockSize)
        {
            if (BlockSize == 0)
            {
                BlockSize = 256;
            }
            return BlockSize;
        }

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

    }
}
