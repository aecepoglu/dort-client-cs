using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

#nullable enable

namespace Messages {
	public interface Message {
		public String Serialize();
	}
	
	class WelcomeMessage : Message {
		public static WelcomeMessage? Parse(string s) {
			return s.Equals("welcome") ? new WelcomeMessage() : null;
		}
		public String Serialize() {
			return "welcome";
		}
	}
	
	class AuthMessage : Message {
		string username;
		public AuthMessage(string username) {
			this.username = username;
		}
		public static AuthMessage? Parse(String s) {
			return null;
		}
		public String Serialize() {
			return "i am " + username;
		}
	}
	
	class OkMessage : Message {
		public static OkMessage? Parse(string s) {
			return s.Equals("ok.") ? new OkMessage() : null;
		}
		public String Serialize() {
			return "ok.";
		}
	}
}

public class DortTcpClient {
	private Socket sock;
	private IPEndPoint ip;
	private byte[] buffer = new byte[1024];
	private Func<string, Messages.Message?>[] parsers = {
		Messages.WelcomeMessage.Parse,
		Messages.OkMessage.Parse,
		// Messages.AuthMessage.Parse, // AuthMessages get send, not received
	};
	
	public DortTcpClient(IPAddress addr, int port) {
		ip = new IPEndPoint(addr, port);
		sock = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
	}

	public async void Connect() {
		await sock.ConnectAsync(ip);
	}

	public async Task<int> SendStr(String msg) {
		return await sock.SendAsync(
			Encoding.UTF8.GetBytes(msg + "\n"),
			SocketFlags.None);
	}
	public async Task<int> Send(Messages.Message m) {
		String s = m.Serialize();
		return await SendStr(s);
	}

	public async Task<String> ReceiveStr() {
		// TODO what if I receive more than one message in a single read?
		var received = await sock.ReceiveAsync(buffer, SocketFlags.None);
		return Encoding.ASCII.GetString(buffer, 0, received).TrimEnd();
	}

	public async Task<Messages.Message?> Receive() {
		string str = await this.ReceiveStr();
		foreach (var parse in parsers) {
			Messages.Message? found = parse(str);
			if (found != null) {
				return found;
			}
		}
		return null;
	}

	public void Shutdown() {
		 sock.Shutdown(SocketShutdown.Both);
	}
}

public class DortTcpClientTest {
    async static Task Run() {
		 const string username = "aecepoglu";
		 const string ip = "127.0.0.1";
		 const int port = 4000;
		 
		 var c = new DortTcpClient(System.Net.IPAddress.Parse(ip), port);
		 c.Connect();
		 
		 await c.Send(new Messages.AuthMessage(username));
		 Console.WriteLine("received: " + await c.Receive());
		 
		 c.Shutdown();
    }

	public static void Main() {
		Run().Wait();
	}
}
