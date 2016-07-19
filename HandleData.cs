using System.IO;
using System.Linq;

namespace Server {
    partial class Static {
        /// <summary>
        /// Autentica a conexão.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="password"></param>
        static void HandleAuth (Client client, string password) {
            if (password == "abcdef")
                Send (client, AUTH, "allow");
            else
                Send (client, AUTH, "deny");
        }

        /// <summary>
        /// Login.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="name"></param>
        /// <param name="password"></param>
        static void HandleLogin (Client client, string name, string password) {
            var user = from u in Users
                       where u.Name == name && u.Password == password
                       select u;
            if (user.Count () > 0) {
                client.Name = name;
                client.Group = user.First ().Group;
                Send (client, LOGIN, "allow");
            } else { Send (client, LOGIN, "wp"); }
        }

        /// <summary>
        /// Registro.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="name"></param>
        /// <param name="password"></param>
        static void HandleRegister (Client client, string name, string password) {
            var user = from u in Users where u.Name == name select u;
            if (user.Count () <= 0) {
                Users.Add (new User (name, password));
                Send (client, REGISTER, "success");
            } else { Send (client, LOGIN, "wu"); }
        }

        /// <summary>
        /// Mensagem do dia.
        /// </summary>
        /// <param name="client"></param>
        static void HandleMotd (Client client) {
            Send (client, MOTD, Settings.GetString ("Motd"));
        }

        /// <summary>
        /// Informações básicas.
        /// </summary>
        /// <param name="client"></param>
        static void HandleBasicInfo (Client client) {
            Send (client, BASIC, string.Format ("{0}:{1}:{2}", 0, client.Name, client.Group));
        }

        /// <summary>
        /// Encerra a conexão de um usuário.
        /// </summary>
        /// <param name="client"></param>
        static void HandleCloseUser (Client client) {
            if (Clients.Contains (client))
                Clients.Remove (client);
        }

        /// <summary>
        /// Verifica se o personagem existe.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="id"></param>
        static void HandleCharExists (Client client, string id) {
            string exists = File.Exists (string.Format ("./Characters/{0}-{1}", client.Name, id)).ToString ();
            Send (client, CHAR_EXISTS, string.Format ("{0}:{1}", id, exists));
        }

        /// <summary>
        /// Carrega os dados de um personagem.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="id"></param>
        static void HandleLoadChar (Client client, string id) {
            string path = string.Format ("./Characters/{0}-{1}", client.Name, id);
            if (File.Exists (path))
                Send (client, LOAD_CHAR, File.ReadAllText (path));
        }

        /// <summary>
        /// Envia uma lista com gráficos disponíveis.
        /// </summary>
        /// <param name="client"></param>
        static void HandleGraphics (Client client) {

        }

        /// <summary>
        /// Cria o novo personagem
        /// </summary>
        /// <param name="client"></param>
        /// <param name="data"></param>
        static void HandleCreateChar (Client client, string data) {
            string id = data.Split (':') [0];
            string path = string.Format ("./Characters/{0}-{1}", client.Name, id);
            File.WriteAllText (path, data);
            Send (client, CREATE_CHAR, id);
        }

        /// <summary>
        /// Deleta um personagem existente.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="id"></param>
        static void HandleDeleteChar(Client client, string id) {
            File.Delete (string.Format ("./Characters/{0}-{1}", client.Name, id));
            Send (client, DELETE_CHAR, id);
        }
    }
}
