using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Configuration;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Xml;
using System.Xml.Serialization;

namespace twitchBot
{

    /// <summary>
    /// Our main application
    /// </summary>
    class Program
    {
        /// <summary>
        /// When set to true, program will end
        /// </summary>
        static bool shouldEnd = false;
        /// <summary>
        /// 7 Days to die socket
        /// </summary>
        static Socket sevenDaysSocket;
        static Socket twitchSocket;

        static twitchBot.Logger logger;
        static twitchBot.Logger chatLog;


        public static Dictionary<string, Viewer> Viewers;

        /// <summary>
        /// Main application thread
        /// It ends when any of the threads sets the "shouldEnd" shared variable to "true"
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            Viewers = new Dictionary<string, Viewer>();

            LoadViewers();
            
            logger = new twitchBot.Logger();
            logger.Open();
            chatLog = new twitchBot.Logger("chat.log");
            chatLog.Open();
            // Start the Seven Days thread
            Thread sevenDays = new Thread(SevenDaysClient);
            sevenDays.Start();

            // Start the twitch thread
            Thread twitch = new Thread(TwitchClient);
            twitch.Start();

            // Main loop
            while (!shouldEnd)
            {

            }
            logger.Close();
            chatLog.Close();
            // Bye
            System.Environment.Exit(1);
        }

        private static Viewer GetViewer(string name)
        {
            string[] nickParts = name.Split('!');
            string nickName = nickParts.GetValue(0).ToString();
            if (Viewers.ContainsKey(nickName))
            {
                return Viewers[nickName];
            }
            Viewer viewer = new Viewer
            {
                Name = nickName
            };
            Viewers.Add(nickName, viewer);

            SaveViewers();
            return viewer;
        }


        /// <summary>
        /// Save all viewers to an xml file
        /// </summary>
        public static void SaveViewers()
        {
            XmlDocument doc = new XmlDocument();

            //(1) the xml declaration is recommended, but not mandatory
            XmlDeclaration xmlDeclaration = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            XmlElement root = doc.DocumentElement;
            doc.InsertBefore(xmlDeclaration, root);

            //(2) string.Empty makes cleaner code
            XmlElement ViewersElement = doc.CreateElement(string.Empty, "viewers", string.Empty);
            doc.AppendChild(ViewersElement);

            foreach (KeyValuePair<string, Viewer> viewer in Viewers)
            {
                XmlElement viewerElemnt = doc.CreateElement(string.Empty, "Viewer", string.Empty);
                ViewersElement.AppendChild(viewerElemnt);

                XmlElement viewerName = doc.CreateElement(string.Empty, "Name", string.Empty);
                XmlText viewerNameValue = doc.CreateTextNode(viewer.Value.Name);
                viewerName.AppendChild(viewerNameValue);
                viewerElemnt.AppendChild(viewerName);

                XmlElement viewerPoints = doc.CreateElement(string.Empty, "Points", string.Empty);
                XmlText viewerPointsValue = doc.CreateTextNode(viewer.Value.Points.ToString());
                viewerPoints.AppendChild(viewerPointsValue);
                viewerElemnt.AppendChild(viewerPoints);

                

            }

            doc.Save("viewers.xml");
        }


        /// <summary>
        /// Load all viewers from an xml file
        /// </summary>
        public static void LoadViewers()
        {
            // Create the XmlDocument.
            XmlDocument doc = new XmlDocument();
            doc.Load("viewers.xml");


            foreach (XmlNode viewer in doc.DocumentElement.ChildNodes)
            {
                foreach (XmlNode viewerDetail in viewer)
                {
                    Console.WriteLine(viewerDetail.Name + ": " + viewerDetail.InnerText);
                }
            }
        }


        /// <summary>
        /// Seven days to die client - should be running as a thread
        /// </summary>
        public static void SevenDaysClient()
        {
            byte[] bytes = new byte[1024];

            try
            {
                // Connect to a Remote server  
                // Get Host IP Address that is used to establish a connection  
                // In this case, we get one IP address of localhost that is IP : 127.0.0.1  
                // If a host has multiple addresses, you will get a list of addresses  
                IPHostEntry host = Dns.GetHostEntry(ConfigurationManager.AppSettings.Get("7daysHostname"));
                IPAddress ipAddress = host.AddressList[1];
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, Int32.Parse(ConfigurationManager.AppSettings.Get("7daysPort")));

                // Create a TCP/IP  socket.    
                sevenDaysSocket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                // Connect the socket to the remote endpoint. Catch any errors.    
                try
                {
                    // Connect to Remote EndPoint  
                    sevenDaysSocket.Connect(remoteEP);

                    Console.WriteLine("Socket connected to {0}",
                        sevenDaysSocket.RemoteEndPoint.ToString());

                    // Encode the data string into a byte array.    
                    //byte[] msg = Encoding.ASCII.GetBytes("Ri4509<EOF>");

                    // Send the data through the socket.    
                    //int bytesSent = sender.Send(msg);

                    // Receive the response from the remote device.    
                    //int bytesRec = sender.Receive(bytes);
                    //Console.WriteLine("Echoed test = {0}",
                    //    Encoding.ASCII.GetString(bytes, 0, bytesRec));

                    while (sevenDaysSocket.Connected)
                    {

                        int bytesRec = sevenDaysSocket.Receive(bytes);
                        if (bytesRec != -1)
                        {
                            string data = Encoding.ASCII.GetString(bytes, 0, bytesRec);
                            Console.Write(data);
                            logger.Log(data);
                            if (data.Contains("Please enter password:")) {
                                string password = ConfigurationManager.AppSettings.Get("7daysPassword");
                                SendToSevendays(password);
                            }
                            if (data.Contains("!exit"))
                            {
                                SendToSevendays("Exiting application");
                                Console.WriteLine("Should exit main thread");
                                shouldEnd = true;
                            }
                        }
                        else
                        {
                            // -1 Bytes read should indicate the client shutdown on their end
                            break;
                        }

                        //break;
                    }

                    // Release the socket.    
                    sevenDaysSocket.Shutdown(SocketShutdown.Both);
                    sevenDaysSocket.Close();

                }
                catch (ArgumentNullException ane)
                {
                    Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
                    shouldEnd = true;
                }
                catch (SocketException se)
                {
                    Console.WriteLine("SocketException : {0}", se.ToString());
                    shouldEnd = true;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unexpected exception : {0}", e.ToString());
                    shouldEnd = true;
                }

            }
            catch (Exception e)
            {
                shouldEnd = true;
                Console.WriteLine(e.ToString());
            }
        }

        /// <summary>
        /// Send a command to seven days to die
        /// </summary>
        /// <param name="data"></param>
        public static void SendToSevendays(string data)
        {
            int bytesSent = sevenDaysSocket.Send(Encoding.ASCII.GetBytes(data + "\n"));
        }


        /// <summary>
        /// Twitch IRC client - should be running as a thread
        /// </summary>
        public static void TwitchClient()
        {
            byte[] bytes = new byte[1024];

            try
            {
                // Connect to a Remote server  
                // Get Host IP Address that is used to establish a connection  
                // In this case, we get one IP address of localhost that is IP : 127.0.0.1  
                // If a host has multiple addresses, you will get a list of addresses  
                IPHostEntry host = Dns.GetHostEntry("irc.chat.twitch.tv");
                IPAddress ipAddress = host.AddressList[0];
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, 6667);

                // Create a TCP/IP  socket.    
                twitchSocket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                // Connect the socket to the remote endpoint. Catch any errors.    
                try
                {
                    // Connect to Remote EndPoint  
                    twitchSocket.Connect(remoteEP);

                    Console.WriteLine("Socket connected to {0}",
                        twitchSocket.RemoteEndPoint.ToString());


                    SendToTwitch("PASS " + ConfigurationManager.AppSettings.Get("twitchAuth"));
                    SendToTwitch("NICK " + ConfigurationManager.AppSettings.Get("twitchNickname"));
                    SendToTwitch("CAP REQ :twitch.tv/membership");
                    SendToTwitch("CAP REQ :twitch.tv/tags");
                    SendToTwitch("CAP REQ :twitch.tv/commands");
                    SendToTwitch("JOIN #" + ConfigurationManager.AppSettings.Get("twitchChannel"));

                    // Encode the data string into a byte array.    
                    //byte[] msg = Encoding.ASCII.GetBytes("Ri4509<EOF>");

                    // Send the data through the socket.    
                    //int bytesSent = sender.Send(msg);

                    // Receive the response from the remote device.    
                    //int bytesRec = sender.Receive(bytes);
                    //Console.WriteLine("Echoed test = {0}",
                    //    Encoding.ASCII.GetString(bytes, 0, bytesRec));

                    while (twitchSocket.Connected)
                    {

                        int bytesRec = twitchSocket.Receive(bytes);
                        if (bytesRec != -1)
                        {
                            string data = Encoding.ASCII.GetString(bytes, 0, bytesRec);
                            Console.Write(data);
                            logger.Log(data);
                            


                            //Console.WriteLine(string.Join(", ", nickParts));
                            //Console.WriteLine(string.Join(", ", infoParts));
                            //Console.WriteLine(string.Join(", ", parts));

                            if (data.Contains("PING"))
                            {
                                SendToTwitch("PONG :tmi.twitch.tv");
                            }

                            if (data.Contains("PRIVMSG"))
                            {
                                Char[] separator = new char[] { ':' };
                                string[] parts = data.Split(separator, 3);

                                

                                Viewer viewer = GetViewer(parts.GetValue(1).ToString());
                                string nickName = viewer.Name;


                                string[] infoParts = parts.GetValue(0).ToString().Split(';');

                                
                                char[] charsToTrim = { ' ', '\n', '\r' };
                                string message = parts.GetValue(2).ToString().Trim(charsToTrim);
                                Console.WriteLine(nickName + ": " + message);
                                chatLog.Log(nickName + ": " + message);

                                if (message == "!spawn")
                                {
                                    ChatAfter("Χμμμ... ο " + nickName + " σου έστειλε μια screamer. Καλή τύχη!");
                                    SendToSevendays("spawnscouts " + ConfigurationManager.AppSettings.Get("7daysNickname"));
                                    SendToSevendays("say \"User " + nickName + " sent you a screamer\"");
                                } else
                                {
                                    viewer.Points += 1;
                                    SaveViewers();
                                    SendToSevendays("say \"Twitch-" + nickName + ": " + message +"\"");
                                }

                            }

                            if (data.Contains("!exit"))
                            {
                                SendToTwitch("Exiting application");
                                Console.WriteLine("Should exit main thread");
                                shouldEnd = true;
                            }
                        }
                        else
                        {
                            // -1 Bytes read should indicate the client shutdown on their end
                            break;
                        }

                        //break;
                    }

                    // Release the socket.    
                    twitchSocket.Shutdown(SocketShutdown.Both);
                    twitchSocket.Close();

                }
                catch (ArgumentNullException ane)
                {
                    Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
                    shouldEnd = true;
                }
                catch (SocketException se)
                {
                    Console.WriteLine("SocketException : {0}", se.ToString());
                    shouldEnd = true;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unexpected exception : {0}", e.ToString());
                    shouldEnd = true;
                }

            }
            catch (Exception e)
            {
                shouldEnd = true;
                Console.WriteLine(e.ToString());
            }
        }

        /// <summary>
        /// Send a command to seven days to die
        /// </summary>
        /// <param name="data"></param>
        public static void SendToTwitch(string data)
        {
            int bytesSent = twitchSocket.Send(Encoding.ASCII.GetBytes(data + "\n"));
        }

        /// <summary>
        /// Say something to twitch after a specific or a random interval
        /// </summary>
        /// <param name="whatToSay"></param>
        /// <param name="seconds"></param>
        private static async void ChatAfter(string whatToSay, int seconds = -1)
        {
            if (seconds == -1)
            {
                Random rnd = new Random();
                seconds = rnd.Next(1, 10);
            }
            await Task.Delay(TimeSpan.FromSeconds(seconds));
            string command = "PRIVMSG #" + ConfigurationManager.AppSettings.Get("twitchChannel") + " :" + whatToSay + "\n";
            Console.WriteLine(command);
            int bytesSent = twitchSocket.Send(Encoding.UTF8.GetBytes(command));
        }


    }
}