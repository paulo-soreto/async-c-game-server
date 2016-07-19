using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace Server {
    partial class Static {
        /**
         * Todo código dentro desta #region não é de minha autoria, encontrei em algum site aleatório e não lembro
         * o nome do cara que desenvolveu. Qualquer erro relacionado a este código não é de forma alguma minha culpa.
         * Esse código serve de alguma maneira pra receber um "sinal" de que o console está fechando e assim eu possa
         * salvar o banco de dados.
         */
        #region Console HOOK
        [DllImport ("Kernel32")]
        public static extern bool SetConsoleCtrlHandler (HandlerRoutine Handler, bool Add);

        public delegate bool HandlerRoutine (CtrlTypes CtrlType);

        public enum CtrlTypes {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT,
            CTRL_CLOSE_EVENT,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT
        }

        private static bool ConsoleCtrlCheck (CtrlTypes ctrlType) {
            OnClose ();
            return true;
        }
        #endregion

        #region HEADERS
        const string AUTH = "000";
        const string LOGIN = "001";
        const string REGISTER = "002";
        const string MOTD = "003";
        const string BASIC = "004";
        const string CLOSE = "005";
        const string CHAR_EXISTS = "006";
        const string LOAD_CHAR = "007";
        const string GRAPHICS = "008";
        const string CREATE_CHAR = "009";
        const string DELETE_CHAR = "010";
        #endregion

        static HashSet<Client> Clients;
        static HashSet<User> Users;
        static Socket Listener;
        static long HighIndex;

        /// <summary>
        /// Ponto de entrada do programa.
        /// </summary>
        /// <param name="args"></param>
        static void Main (string [] args) {
            SetConsoleCtrlHandler (new HandlerRoutine (ConsoleCtrlCheck), true);
            Settings.Initialize ();

            Console.Title = Settings.GetString ("Title");
            Console.WriteLine ("Iniciando servidor...");

            Users = new HashSet<User> ();
            Clients = new HashSet<Client> ();
            HighIndex = 0;

            LoadDatabase ();
            CreateListener ();

            Console.WriteLine ("Servidor iniciado!");
            Console.Read ();
        }

        /// <summary>
        /// Cria o listener
        /// </summary>
        static void CreateListener () {
            Listener = new Socket (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Listener.Bind (new IPEndPoint (IPAddress.Any, Settings.GetInt ("Port")));
            Listener.Listen (Settings.GetInt("Backlog"));
            Listener.BeginAccept (OnConnectRequest, null);
        }

        /// <summary>
        /// Callback para novas conexões
        /// </summary>
        /// <param name="ar"></param>
        static void OnConnectRequest (IAsyncResult ar) {
            Client client = new Client (Listener.EndAccept (ar));
            client.Index = HighIndex;
            client.Socket.BeginReceive (client.Buffer, 0, client.Buffer.Length, SocketFlags.None, OnRead, client);
            Clients.Add (client);
            HighIndex++;
            Listener.BeginAccept (OnConnectRequest, null);
            Console.WriteLine ("Usuário {0} conectado!", client.Socket.RemoteEndPoint);
        }

        /// <summary>
        /// Callback para recebimento de mensagens.
        /// </summary>
        /// <param name="ar"></param>
        static void OnRead (IAsyncResult ar) {
            try {
                Client client = (Client)ar.AsyncState;
                int length = client.Socket.EndReceive (ar);
                if (length <= 0) {
                    Console.WriteLine ("Usuário {0} desconectado!", client.Socket.RemoteEndPoint);
                    client.Socket.Close ();
                    if (Clients.Contains (client))
                        Clients.Remove (client);
                } else {
                    /* O padrão de mensagens trocadas entre client/server é: {header}{message}{separador}
                       O header sempre terá 3 caracteres de comprimento, já a mensagem é ilimitada, ao final
                       é adicionado um "\n" como separador. */
                    string recv = Encoding.UTF8.GetString (client.Buffer, 0, length);
                    string [] lines = recv.Split ('\n');
                    for (int i = 0; i < lines.Length; i++) {
                        if (string.IsNullOrWhiteSpace (lines [i]))
                            continue;
                        Process (client, lines [i].Substring (0, 3), lines [i].Substring (3, lines [i].Length - 3));
                    }
                    client.Socket.BeginReceive (client.Buffer, 0, client.Buffer.Length, SocketFlags.None, OnRead, client);
                }
            } catch (SocketException) { }
        }

        /// <summary>
        /// Envia uma mensagem para um usuário.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="header"></param>
        /// <param name="message"></param>
        static void Send (Client client, string header, string message) {
            StringBuilder stringBuilder = new StringBuilder ();
            stringBuilder.Append (header);
            stringBuilder.Append (message);
            stringBuilder.Append ("\n");

            byte [] buffer = Encoding.UTF8.GetBytes (stringBuilder.ToString ());
            client.Socket.BeginSend (buffer, 0, buffer.Length, SocketFlags.None, OnSend, client);
        }

        /// <summary>
        /// Envia uma mensagem para um usuário.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="header"></param>
        /// <param name="message"></param>
        static void Send (long index, string header, string message) {
            Send (Clients.Where (c => c.Index == index).First (), header, message);
        }

        /// <summary>
        /// Callback de envio de mensagens
        /// </summary>
        /// <param name="ar"></param>
        static void OnSend (IAsyncResult ar) {
            Client client = (Client)ar.AsyncState;
            int length = client.Socket.EndSend (ar);
            Console.WriteLine ("Enviado {0} bytes para {1}", length, client.Socket.RemoteEndPoint);
        }

        /// <summary>
        /// Método chamado quando o console está sendo fechado.
        /// </summary>
        static void OnClose () {
            SaveDatabase ();
        }

        /// <summary>
        /// Carrega o banco de dados (usuários).
        /// </summary>
        static void LoadDatabase () {
            Console.WriteLine ("Carregando contas de usuário...");
            if (!File.Exists ("./users"))
                File.Create ("./users").Close ();

            string text = File.ReadAllText ("./users");
            if (string.IsNullOrEmpty (text))
                return;

            string [] users = text.Split (';');
            for (int i = 0; i < users.Length - 1; i++) {
                string [] user = users [i].Split (':');
                Users.Add (new User (user[0], user[1], user[2]));
            }
        }
        
        /// <summary>
        /// Salva os usuários registrados.
        /// </summary>
        static void SaveDatabase () {
            StringBuilder sb = new StringBuilder ();
            foreach (var user in Users) { sb.AppendFormat ("{0}:{1}:{2};", user.Name, user.Password, user.Group); }
            File.WriteAllText ("./users", sb.ToString ());
        }
        
        /// <summary>
        /// Processa os dados recebidos.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="header"></param>
        /// <param name="message"></param>
        static void Process (Client client, string header, string message) {
            Console.WriteLine ("Recebido {1} de {0}", client.Socket.RemoteEndPoint, header);
            string [] data;
            switch (header) {
                case AUTH:
                    HandleAuth (client, message);
                    break;
                case LOGIN:
                    data = message.Split (':');
                    HandleLogin (client, data [0], data [1]);
                    break;
                case REGISTER:
                    data = message.Split (':');
                    HandleRegister (client, data [0], data [1]);
                    break;
                case MOTD:
                    HandleMotd (client);
                    break;
                case BASIC:
                    HandleBasicInfo (client);
                    break;
                case CLOSE:
                    HandleCloseUser (client);
                    break;
                case CHAR_EXISTS:
                    HandleCharExists (client, message);
                    break;
                case LOAD_CHAR:
                    HandleLoadChar (client, message);
                    break;
                case GRAPHICS:
                    HandleGraphics (client);
                    break;
                case CREATE_CHAR:
                    HandleCreateChar (client, message);
                    break;
                case DELETE_CHAR:
                    HandleDeleteChar (client, message);
                    break;
            }
        }
    }
}
