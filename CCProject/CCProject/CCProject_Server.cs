/*
 * Dan Gomez
 * 
 * A simple server that stores basic financial transactions for personal
 * use. It uses Isis so that multiple servers and clients can share data
 * and maintain accurate records.
 * 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetworkCommsDotNet;
using Isis;

namespace CCProject
{

    // The finance book class stores transactions by a key-value pair.
    // It also contains functions to handle the incoming and outgoing
    // messages to and from the client. 
    //
    class FinanceBook
    {
        // Stores financial info
        // Key - note about the transaction
        // Value - amount withdrawn/deposited
        Dictionary<string, float> book;
        float balance;
        private Group isisGroup;
        private static int UPDATE = 0;
        private static int QUERY = 1;
        private static int BALANCE = 2;
        private static int ALL = 3;
        private delegate void IsisUpdateDelegate(string info, float money);
        private delegate void IsisQueryDelegate(string info);
        private delegate void IsisBalanceDelegate();
        private delegate void IsisAllDelegate();

        delegate void loadcheckpoint(string info, float money);

        // Simply add and entry.
        //
        public void IsisUpdate(string info, float money)
        {
            Console.WriteLine("Update " + info + ":" + money);
            addEntry(info, money);
        }

        // Respond to a query.
        //
        public void IsisQuery(string info)
        {
            Console.WriteLine("Query " + info);
            if (book.ContainsKey(info))
            {
                isisGroup.Reply(info);
            }
            else isisGroup.Reply("");
        }

        // Respond with the balance.
        //
        public void IsisBalance()
        {
            Console.WriteLine("Balance " + balance);
            isisGroup.Reply(balance);
        }

        // Respond with the entire finance book.
        //
        public void IsisAll()
        {
            Console.WriteLine("All");
            isisGroup.Reply(book);
        }

        // Simple Constructor.
        //
        public FinanceBook()
        {
            book = new Dictionary<string, float>();
            balance = 0;
        }

        // Start the server. 
        //
        public void Start(string ip, int port)
        {
            System.Net.IPEndPoint endpoint = new System.Net.IPEndPoint(System.Net.IPAddress.Parse(ip), port);
            TCPConnection.StartListening(endpoint);

            // Packet handlers for the four types of messages we are expecting.
            //
            NetworkComms.AppendGlobalIncomingPacketHandler<string>("Get", ClientGet);
            NetworkComms.AppendGlobalIncomingPacketHandler<string>("Add", ClientUpdate);
            NetworkComms.AppendGlobalIncomingPacketHandler<string>("Bal", ClientBalance);
            NetworkComms.AppendGlobalIncomingPacketHandler<string>("All", ClientAll);

            isisGroup = new Group("book");
            IsisUpdateDelegate func1 = IsisUpdate;
            isisGroup.Handlers[UPDATE] += func1;
            IsisQueryDelegate func2 = IsisQuery;
            isisGroup.Handlers[QUERY] += func2;
            IsisBalanceDelegate func3 = IsisBalance;
            isisGroup.Handlers[BALANCE] += func3;
            IsisAllDelegate func4 = IsisAll;
            isisGroup.Handlers[ALL] += func4;

            // Create and load checkpoints to keep multiple books up to date with the same data.
            //
            isisGroup.MakeChkpt += (Isis.ChkptMaker)delegate(View nv)
            {
                Console.WriteLine("Sending checkpoint!");
                foreach (KeyValuePair<string, float> pair in book)
                    isisGroup.SendChkpt(pair.Key, pair.Value);
                isisGroup.EndOfChkpt();
            };

            isisGroup.LoadChkpt += (loadcheckpoint)delegate(string info, float money)
            {
                Console.WriteLine("Checkpoint recieved!" + info + ":" + money);
                addEntry(info, money);
            };

            byte[] key = new byte[32];
            for (int i = 0; i < 32; i++) key[i] = (byte)i;
            isisGroup.SetSecure(key);

            isisGroup.Join();
        }

        // Add an entry to the finance book and update the balance.
        //
        private void addEntry(string info, float money)
        {
            if (book.ContainsKey(info))
            {
                balance -= book[info];
                book.Remove(info);
            }
            book.Add(info, money);
            balance += money;
        }

        // Query the fianance book (and the book for other servers if necessary) when the user
        // looks up a certain entry.
        //
        private void ClientGet(PacketHeader header, Connection connection, string info)
        {
            Console.WriteLine("Querying");
            if (book.ContainsKey(info))
            {
                Console.WriteLine("Found it!");
                connection.SendObject("item", book[info]);
            }
            else
            {
                Console.WriteLine("Querying");
                List<string> replies = new List<string>();
                int nreplies = isisGroup.Query(Group.ALL,
                    new Timeout(1000,Timeout.TO_FAILURE),
                    QUERY,
                    info,
                    new EOLMarker(), replies);
                Console.WriteLine("Replies: " + nreplies);
                foreach (string reply in replies)
                {
                    if (reply != null)
                    {
                        //addEntry(info, reply);
                        connection.SendObject("item", reply);
                        break;
                    }
                }
                Console.WriteLine("shiku shiku!");
                connection.SendObject("item", null);
            }
        }

        // Handle messages to add entries to the finance book.
        //
        private void ClientUpdate(PacketHeader header, Connection connection, string itembook)
        {
            Console.WriteLine("Client update");
            string info = itembook.Split(':').First();
            string temp = itembook.Split(':').Last();
            float money = float.Parse(temp);
 
            addEntry(info, money);
            isisGroup.OrderedSend(UPDATE, info, money);
        }

        // Send the balance to the client for the user.
        //
        private void ClientBalance(PacketHeader header, Connection connection, string foo)
        {
            Console.WriteLine("balance" + foo);
            connection.SendObject("balance", balance);
        }

        // Send the finance book to the client for the user.
        //
        private void ClientAll(PacketHeader header, Connection connection, string foo)
        {
            Console.WriteLine("All" + foo);
            connection.SendObject("all", book);
        }

        // Shuts the server down.
        //
        public void Stop()
        {
            NetworkComms.Shutdown();
        }
    }

    // The actual server class. It uses Isis to manage outgoing and incoming messages from the
    // client. 
    //
    class CCProject_Server
    {
        static void Main(string[] args)
        {
            string ip;
            if (args.Length == 0) ip = "127.0.0.1";
            else ip = args[0];

            IsisSystem.Start();
            FinanceBook foobar = new FinanceBook();
            foobar.Start(ip, 20000);

            Console.WriteLine("\nServer is up\n--To close the server, press any key--");
            Console.ReadKey(true);
            foobar.Stop();
            IsisSystem.Shutdown();
        }
    }
}
