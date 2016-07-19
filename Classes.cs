using System.Net.Sockets;
using System.IO;

namespace Server {
    /// <summary>
    /// Usuário carregado do banco de dados.
    /// </summary>
    class User {
        public string Name;
        public string Password;
        public string Group;

        public User (string name, string password, string group = "std") {
            Name = name;
            Password = password;
            Group = group;
        }
    }

    /// <summary>
    /// Usuário conectado.
    /// </summary>
    class Client {
        public Socket Socket;
        public byte[] Buffer;
        public long Index;
        public string Name;
        public string Group;
        public string [] Characters;

        public Client (Socket socket) {
            Socket = socket;
            Buffer = new byte [1024];
        }
    }
}
