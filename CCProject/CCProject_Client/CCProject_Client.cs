/*
 * Dan Gomez
 * 
 * The client side of the application. A simple user interface to prompt the user
 * and send messages to the server.
 * 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetworkCommsDotNet;

namespace CCProject_Client
{
    class CCProject_Client
    {
        static void Main(string[] args)
        {

            // Connect to the server based on the info given (or by default).
            //
            string ServerInfo;
            if (args.Length == 0) ServerInfo = "127.0.0.1:20000";
            else ServerInfo = args[0];

            string ServerIP = ServerInfo.Split(':').First();
            int ServerPort = int.Parse(ServerInfo.Split(':').Last());
            TCPConnection connection = TCPConnection.GetConnection(new ConnectionInfo(ServerIP, 20000));

            // Loop forever taking commands from the user.
            //
            while (true)
            {
                Console.WriteLine("Please choose an action:\n get - get a single entry\n add - add an entry\n bal - check the balance\n all - see all entries\n exit - exit the program");
                string command = Console.ReadLine();
                Console.Clear();

                // Send a message requesting a single entry.
                //
                if (command == "get")
                {
                    Console.WriteLine("Enter the tag for the entry: ");
                    string message = Console.ReadLine();

                    float flt = connection.SendReceiveObject<float>("Get", "item", 2000, message);
                    if (flt != 0.0) Console.WriteLine("$" + flt + "\n");
                    else Console.WriteLine("Error: entry was not found\n");
                }
                // Send a message requesting a new entry be added.
                //
                else if (command == "add")
                {
                    Console.WriteLine("Type the new entry (type it as \"tag:amount\" (positive numbers for deposits and negative for withdrawls))");
                    string message = Console.ReadLine();
                    connection.SendObject("Add", message);
                    Console.WriteLine("");
                }
                // Send a message requesting the balance.
                //
                else if (command == "bal")
                {
                    float flt = connection.SendReceiveObject<float>("Bal", "balance", 2000, "foo");
                    Console.WriteLine("Your balance: $" + flt + "\n");
                }
                // Send a message requesting all transactions made until now.
                //
                else if (command == "all")
                {
                    Dictionary<string, float> book = new Dictionary<string, float>();
                    book = connection.SendReceiveObject<Dictionary<string, float>>("All", "all", 2000, "foo");
                    foreach (string temp in book.Keys)
                    {
                        Console.WriteLine(temp + ":" + book[temp]);
                    }
                    Console.WriteLine("");
                }
                // Quit if the user enters 'exit'.
                //
                else if (command == "exit") break;
                // In case the user enters a bad command.
                //
                else
                {
                    Console.WriteLine("Sorry, I didn't recognize that.\n");
                    continue;
                }
            }

            // Shutdown the connection when we're done.
            //
            NetworkComms.Shutdown();
        }
    }
}
