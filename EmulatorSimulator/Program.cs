using System;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
        BMGJET 2020" + "\n"; //Header image
        public static string HS = " Emulator Simulator 0.1\n\n"; //Header string
        public static string FS = "\n(Enter The Number To Select Option)"; //Help tip.
        public static bool NoDelay = false;
        private static SerialPort serialPort_0;
        public static byte Protocol;
        public static byte[] Bin;
        public static byte[] Version;
        public static int TimeOut = 60;
        public static string BinFile;
        public static byte[] OK = new byte[] { 0x4F };
        public static byte[] Serial = new byte[] { 0x04, 0x01, 0x12, 0x08, 0x08, 0x11, 0x14, 0x25, 0x46, 0xB7  }; 
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

        public static void SetNoDelay() //Disables delay that emulates bluetooth speed.
        {
            NoDelay = !NoDelay;
            Console.WriteLine("No Delay Mode: "+ NoDelay);
        }

        static byte Setup()
        {
            //Define what protocol to use.
            int P = 0;
            Console.WriteLine("\nWhat Protocol To Emulate: \n(1) Moates Ostrich 1.0\n(2) Moates Ostrich 2.0\n(3) CobraRTP\n(4) ECUTamer\n(0) Moates Demon(WIP)" + FS);
            FS = "";
            int.TryParse(Console.ReadLine(), out P);
            switch (P)  //Switch version array for what emulator your using.
            {
                case 0:
                    Console.WriteLine("Protocol: Moates Demon");
                    Version = new byte[] { 0x01, 0x09, 0x44 };
                    break;
                case 1:
                    Console.WriteLine("Protocol: Moates Ostrich 1.0");
                    Version = new byte[] { 0x10, 0x07, 0x4F };
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
                    Version = new byte[] { 0x01, 0x28, 0x4F };
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
                    serialPort_0.ReadBufferSize = 64000;
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
            BinFile = Directory.GetCurrentDirectory() + "\\EMUSIM.bin";
            if (File.Exists(BinFile)) //Checks if old file already exsists.
            {
                Console.WriteLine("Found a exsisting bin file:\n" + BinFile);
                if (!LoadBin()) //Trys to load the file.
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
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

                    File.WriteAllBytes(BinFile, Bin);
                    return true;
                }
                catch
                {
                    Console.WriteLine("Failed to create temp file EMUSIM.bin");
                    return false;
                }
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
            string keys = "ESC = Exit Program\nSpace = Save Memory to File\nBackspace = Clear Memory\nF1 = Toggle Bluetooth Delay";
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
                case ConsoleKey.F1:
                    SetNoDelay();
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
            if (!NoDelay)
            {
                Thread.Sleep(TimeOut);// Delay to allow bluetooth buffer to fill before reading.
            }
            else
            {
                Thread.Sleep(20);// Delay 20ms to reduce cpu load.
            }
            byte[] inputbytes = new byte[sp.BytesToRead];
            sp.Read(inputbytes, 0, inputbytes.Length);
            if (inputbytes.Length != 0)
            {
                if (Protocol == 0) //Trys parse the Demon protocol
                {
                    if (!DemonAPI(inputbytes, sp))
                    {
                        ErrorLog(Encoding.Default.GetString(inputbytes));
                    }
                }
                else
                {
                    if (!OstrichAPI(inputbytes, sp)) //Trys parse the Ostrich protocol
                    {
                        ErrorLog(Encoding.Default.GetString(inputbytes));
                    }
                }
            }
        }

        private static void ErrorLog(string Debug)
        {
            if (Debug.Length > 16)
            {
                Debug = Debug.Substring(0, 16); //limited to 16 char
            }
            Console.Write("SD: " + Debug); //Dump string that failed
        }
    


        //Handles outgoing data
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
            if (VersionRequested(bytearray, sp)) //Version request VV
            { return true; }
            if (EnableOnboard(bytearray, sp)) //Enable onboard datalogging.
            { return true; }
            if (!StandardCheckSum(bytearray)) //Checksum 
            { return false; }
            if (SlowRead(bytearray, sp)) //Read bin bytes slow
            {
                TimeOut = 60;
                return true;
            }
            if (SlowWrite(bytearray, sp)) //Write bin bytes slow
            {
                TimeOut = 120;
                return true;
            }
            if (FastRead(bytearray, sp)) //Read bin bytes Fast
            {
                TimeOut = 60;
                return true;
            }
            if (FastWrite(bytearray, sp)) //Write bin bytes Fast
            {
                TimeOut = 600;
                return true;
            }
            if (SerialRequest(bytearray, sp)) //Serial number request
            { return true; }
            if (QCSRequest(bytearray, sp)) //Quick CheckSum request BMGJET Protocol
            { return true; }
            if (EEPROMRequest(bytearray, sp)) //EEPROM Information requested
            { return true; }
            if (EEPROMRequest2(bytearray, sp)) //EEPROM Information requested
            { return true; }
            if (BankActive(bytearray, sp)) //Select Active Emulation Bank.
            { return true; }
            if (BankStatic(bytearray, sp)) //Select Static Emulation Bank.
            { return true; }

            return false; //No valid commands found.
        }

        // ██████╗ ███████╗████████╗██████╗ ██╗ ██████╗██╗  ██╗
        //██╔═══██╗██╔════╝╚══██╔══╝██╔══██╗██║██╔════╝██║  ██║
        //██║   ██║███████╗   ██║   ██████╔╝██║██║     ███████║
        //██║   ██║╚════██║   ██║   ██╔══██╗██║██║     ██╔══██║
        //╚██████╔╝███████║   ██║   ██║  ██║██║╚██████╗██║  ██║
        // ╚═════╝ ╚══════╝   ╚═╝   ╚═╝  ╚═╝╚═╝ ╚═════╝╚═╝  ╚═╝
        public static bool OstrichAPI(byte[] bytearray, SerialPort sp)
        {
            if(VersionRequested(bytearray, sp)) //Version request VV
            {return true;}

            //Cobra
            if(SetupLogging(bytearray, sp))
            {return true;}
            if (StartLogging(bytearray, sp))
            {return true;}
            if (StopLogging(bytearray, sp))
            {return true;}
            //Cobra

            if (!StandardCheckSum(bytearray)) //Checksum 
            {return false;}
            if (SlowRead(bytearray, sp)) //Read bin bytes slow
            {
                TimeOut = 60;
                return true;
            }
            if (SlowWrite(bytearray, sp)) //Write bin bytes slow
            {
                TimeOut = 120;
                return true;
            }
            if (FastRead(bytearray, sp)) //Read bin bytes Fast
            {
                TimeOut = 60;
                return true;
            }
            if (FastWrite(bytearray, sp)) //Write bin bytes Fast
            {
                TimeOut = 600;
                return true;
            }
            if (SerialRequest(bytearray, sp)) //Serial number request
            {return true;}
            if (QCSRequest(bytearray, sp)) //Quick CheckSum request BMGJET Protocol
            { return true; }
            if (EEPROMRequest(bytearray, sp)) //EEPROM Information requested
            { return true; }
            if (EEPROMRequest2(bytearray, sp)) //EEPROM Information requested
            { return true; }
            if (BankActive(bytearray, sp)) //Select Active Emulation Bank.
            { return true; }
            if (BankStatic(bytearray, sp)) //Select Static Emulation Bank.
            { return true; }
            return false; //No valid commands found.
        }

        //███╗   ███╗ ██████╗  █████╗ ████████╗███████╗███████╗
        //████╗ ████║██╔═══██╗██╔══██╗╚══██╔══╝██╔════╝██╔════╝
        //██╔████╔██║██║   ██║███████║   ██║   █████╗  ███████╗
        //██║╚██╔╝██║██║   ██║██╔══██║   ██║   ██╔══╝  ╚════██║
        //██║ ╚═╝ ██║╚██████╔╝██║  ██║   ██║   ███████╗███████║
        //╚═╝     ╚═╝ ╚═════╝ ╚═╝  ╚═╝   ╚═╝   ╚══════╝╚══════╝
        public static bool VersionRequested(byte[] bytearray, SerialPort sp)
        {
            //VV request
            if (bytearray[0] == 0x56 && bytearray[1] == 0x56)
            {
                TimeOut = 600;
                Console.WriteLine("Version Requested.");
                DataSender(Version, sp);
                return true;
            }
            return false;
        }
        public static bool BankStatic(byte[] bytearray, SerialPort sp)
        {
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
        public static bool BankActive(byte[] bytearray, SerialPort sp)
        {
            //Get Active Emulation Bank Succeeded: Bank 0
            //42 52 52 Bank write
            //00            read
            if (bytearray[0] == 0x42 && bytearray[1] == 0x52)
            {
                Console.WriteLine("Active Bank Info Requested");
                byte[] BANKINFO = new byte[] { 0x00 };
                DataSender(BANKINFO, sp);
                return true;
            }
            if (bytearray[0] == 0x42 && bytearray[1] == 0x53)
            {
                Console.WriteLine("Active Bank Info Requested");
                DataSender(OK, sp);
                return true;
            }
            return false;
        }

        public static bool EEPROMRequest(byte[] bytearray, SerialPort sp)
        {
            //EEPROM Information requested
            //45 07 00 01 00 00 00 00 00 00 00 4D write
            //50 02 00 55 02 01 10 read
            if (bytearray[0] == 0x45 && bytearray[1] == 0x07 && bytearray[3] == 0x01)
            {
                Console.WriteLine("EEPROM Info Requested");
                byte[] EEPROMINFO = new byte[] { 0x50, 0x02, 0x00, 0x55, 0x02, 0x01, 0x10 };
                DataSender(EEPROMINFO, sp);
                return true;
            }
            return false;
        }


        public static bool EEPROMRequest2(byte[] bytearray, SerialPort sp)
        {
            //EEPROM Information requested
            //48 52 07 00 01 A2 write
            //50 02 00 55 02 01 10  read (Ostrich)
            //44 02 00 55 02 01 10  read (Demon)

            if (bytearray[0] == 0x48 && bytearray[1] == 0x52 && bytearray[2] == 0x07 && bytearray[4] == 0x01 && bytearray[5] == 0xA2)
            {
                Console.WriteLine("EEPROM Info Requested");
                byte[] EEPROMINFO = new byte[] { 0x50, 0x02, 0x00, 0x55, 0x02, 0x01, 0x10 };
                if (Protocol == 0)
                {
                   EEPROMINFO[0] = 0x44;
                }
                DataSender(EEPROMINFO, sp);
                return true;
            }
            return false;
        }

        public static bool SerialRequest(byte[] bytearray, SerialPort sp)
        {
            if (bytearray[0] == 0x4E && bytearray[1] == 0x53)
            {
                Console.WriteLine("Serial Number Requested");
                DataSender(Serial, sp);
                return true;
            }
            return false;
        }

        public static bool StandardCheckSum(byte[] bytearray)
        {
            //Checksum used on most packets.
            byte Tchecksum = 0;
            int bytelen = bytearray.Length;
            byte Cchecksum = bytearray[bytelen - 1];
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
            return true;
        }

        //Fast Write
        public static void BinFastWrite(byte[] bytearray, SerialPort sp)
        {
            int BlockSize = (256 * (bytearray[2]));

            if (bytearray.Length == BlockSize + 6) //normal
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
            else if (bytearray.Length == BlockSize + 8) //encrypt byte 3/4 random encrypt bytes
            {
                //int Address = 0;// GetAddress(bytearray[5], bytearray[6]);

                ////Pack bin with write bytes.
                //for (int i = 0; i < BlockSize; i++)
                //{
                //    Bin[Address + i] = bytearray[i + 7];
                //}
                //byte[] OK = new byte[] { 0x4F };
                //Console.WriteLine(BlockSize + " Bytes written to " + Address);
                //DataSender(OK, sp);
                Console.WriteLine("Encrypted fast write transfere");
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
            else if (bytearray.Length == 8) //encrypted slow write
            {
                Console.WriteLine("Encrypted slow write transfere");
                DataSender(OK, sp);
            }

            }

        //Slow Read
        public static void BinRead(byte[] bytearray, SerialPort sp)
        {
            int BlockSize = GetBlockSize(bytearray[1]);
            if (bytearray.Length == 6) //normal
            {
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
            else if (bytearray.Length == 8) //encrypted fast read
            {
                Console.WriteLine("Encrypted slow read transfere");
                DataSender(OK, sp);
            }

        }
        //Fast read
        public static void BinFastRead(byte[] bytearray, SerialPort sp)
        {
            int BlockSize = (256 * (bytearray[2]));

             if (bytearray.Length == 5) //fast read
            {
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
            else if (bytearray.Length == 8) //encrypted fast read
            {
                Console.WriteLine("Encrypted fast read transfere");
                DataSender(OK, sp);
            }
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
        public static bool SlowRead(byte[] bytearray, SerialPort sp)
        {
            if (bytearray[0] == 0x52 && bytearray.Length == 5) //R
            {
                BinRead(bytearray, sp);
                return true;
            }
            return false;
        }
        public static bool SlowWrite(byte[] bytearray, SerialPort sp) //W
        {
            if (bytearray[0] == 0x57)
            {
                BinWrite(bytearray, sp);
                return true;
            }
            return false;
        }
        public static bool FastRead(byte[] bytearray, SerialPort sp) //ZR
        {
            if (bytearray[0] == 0x5A && bytearray[1] == 0x52)
            {
                BinFastRead(bytearray, sp);
                return true;
            }
            return false;
        }
        public static bool FastWrite(byte[] bytearray, SerialPort sp) //ZW
        {
            if (bytearray[0] == 0x5A && bytearray[1] == 0x57)
            {
                BinFastWrite(bytearray, sp);
                return true;
            }
            return false;
        }

        public static bool EnableOnboard(byte[] bytearray, SerialPort sp) //DOLY Enable onboard
        {
            if (bytearray[0] == 0x44 && bytearray[1] == 0x4F && bytearray[2] == 0x4C  && bytearray[3] == 0x59)
            {
                DataSender(OK, sp);
                return true;
            }
            if (bytearray[0] == 0x44 && bytearray[1] == 0x4F && bytearray[2] == 0x4C && bytearray[3] == 0x79)
            {
                DataSender(OK, sp);
                return true;
            }
            return false;
        }


        //███████╗██╗  ██╗████████╗██████╗  █████╗ 
        //██╔════╝╚██╗██╔╝╚══██╔══╝██╔══██╗██╔══██╗
        //█████╗   ╚███╔╝    ██║   ██████╔╝███████║
        //██╔══╝   ██╔██╗    ██║   ██╔══██╗██╔══██║
        //███████╗██╔╝ ██╗   ██║   ██║  ██║██║  ██║
        //╚══════╝╚═╝  ╚═╝   ╚═╝   ╚═╝  ╚═╝╚═╝  ╚═╝

        //Quick Check Sum
        //5a 43 53 PacketCS     write
        //MemoryCS              read
        public static bool QCSRequest(byte[] bytearray, SerialPort sp)
        {
            if (bytearray[0] == 0x5A && bytearray[1] == 0x43 && bytearray[2] == 0x53)
            {
                QuckCS(sp);
                return true;
            }
            return false;
        }
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

        // ██████╗ ██████╗ ██████╗ ██████╗  █████╗ 
        //██╔════╝██╔═══██╗██╔══██╗██╔══██╗██╔══██╗
        //██║     ██║   ██║██████╔╝██████╔╝███████║
        //██║     ██║   ██║██╔══██╗██╔══██╗██╔══██║
        //╚██████╗╚██████╔╝██████╔╝██║  ██║██║  ██║
        // ╚═════╝ ╚═════╝ ╚═════╝ ╚═╝  ╚═╝╚═╝  ╚═╝
        public static bool CobraLogging = false;
        public static int LoggingSpeed = 70;
        //L+L Start analog
        //; stop analog
        //K change speed
        //Range 0 - 6.34 (0-255)
        //H1+D1+H2+D2+H3+D3

        //Request Cobra Analog start reading.
        public static bool StartLogging(byte[] bytearray, SerialPort sp)
        {
            if (bytearray[0] == 0x4C && bytearray[1] == 0x4C && !CobraLogging)
            {
                CobraLogging = true;
                Task.Factory.StartNew(() =>
                {
                    CobraAnalog(sp);
                });
                Console.WriteLine("CobraRTP Analog Started");
                return true;
            }
            return false;
        }

        //Request stop of cobra analog reading
        public static bool StopLogging(byte[] bytearray, SerialPort sp)
        {
            if (bytearray[0] == 0x3B && CobraLogging)
            {
                Console.WriteLine("CobraRTP Analog Stopped");
                CobraLogging = false;
                return true;
            }
            return false;
        }

        //Setup cobra analog speed.
        public static bool SetupLogging(byte[] bytearray, SerialPort sp)
        {
            if (bytearray[0] == 0x4B && CobraLogging)
            {
                switch (bytearray[0])
                {
                    case 0x30:
                        LoggingSpeed = 70;
                        break;
                    case 0x31:
                        LoggingSpeed = 130;
                        break;
                    case 0x32:
                        LoggingSpeed = 300;
                        break;
                    default:
                        Console.WriteLine("CobraRTP Analog Speed: " + LoggingSpeed + "ms");
                        return false;
                }
                byte[] OK = new byte[1];
                OK[0] = 0x4F;
                Console.WriteLine("CobraRTP Analog Speed Changed To: " + LoggingSpeed + "ms");
                DataSender(OK, sp);
                return true;
            }
            return false;
        }

        public static int RandomNumber(int min, int max)
        {
            Random random = new Random();
            return random.Next(min, max);
        }

        //analog loop.
        public static void CobraAnalog(SerialPort sp)
        {
            while (CobraLogging)
            {
                byte[] Datastream = new byte[6] { 0xAF, 0X00, 0xBF, 0x00, 0xCF, 0x00 };
                Datastream[1] = (byte)RandomNumber(250, 255);
                Datastream[3] = (byte)RandomNumber(250, 255);
                Datastream[5] = (byte)RandomNumber(250, 255);
                DataSender(Datastream, sp);
                Thread.Sleep(LoggingSpeed);
            }
        }
    }
}


