using ACT_Plugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SkillReplay
{
	class WebSocketServer
	{
		private List<WebSocket> sockets=new List<WebSocket>();
		private Logger log;
		private ArraySegment<byte> sendCharName;
		private ArraySegment<byte> addCombatant;

		public WebSocketServer(Logger log)
		{
			this.log = log;
		}
		public async Task Start()
		{
			HttpListener s = new HttpListener();
			s.Prefixes.Add("http://127.0.0.1:10601/BeforeLogLineRead/");
			s.Start();
			log.Log("start websocket server");
			while (true)
			{
				var hc = await s.GetContextAsync();
				log.Log("overlay connect");

				if (!hc.Request.IsWebSocketRequest)
				{
					hc.Response.StatusCode = 400;
					hc.Response.Close();
					continue;
				}

				log.Log("overlay accept");
				var wsc = await hc.AcceptWebSocketAsync(null);
				var ws = wsc.WebSocket;
				sockets.Add(ws);
				await InitSend(ws);
			}
		}

		public void Disconnect()
		{
			foreach (var ws in sockets)
			{
				ws.CloseAsync(WebSocketCloseStatus.NormalClosure,"Done", CancellationToken.None);
			}
			sockets.Clear();
		}

		private async Task InitSend(WebSocket ws)
		{
			if (sendCharName.Count==0)
			{
				string json = $@"{{ ""type"": ""broadcast"", ""msgtype"": ""SendCharName"", ""msg"": {{ ""charName"": ""Skill Replay"", ""charID"": 305419896}} }}";
				var buffer = Encoding.UTF8.GetBytes(json);
				sendCharName = new ArraySegment<byte>(buffer);
			}
			if (sendCharName.Count!=0)
			{
				await ws.SendAsync(sendCharName, WebSocketMessageType.Text, true, CancellationToken.None);
			}
			if (addCombatant.Count!=0)
			{
				await ws.SendAsync(addCombatant, WebSocketMessageType.Text, true, CancellationToken.None);
			}
		}

		public void AddCombatant(Friendly friend)
		{
			var time = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffffffzzz");
			int job = 0;

			string json1 = $@"{{ ""type"": ""broadcast"", ""msgtype"": ""Chat"", ""msg"":""03|{time}|12345678|Skill Replay|{job}|50|-|-|Server"" }}";
			var buffer1 = Encoding.UTF8.GetBytes(json1);
			addCombatant = new ArraySegment<byte>(buffer1);

			foreach (var ws in sockets)
			{
				ws.SendAsync(addCombatant, WebSocketMessageType.Text, true, CancellationToken.None);
			}
			sockets.RemoveAll(ws => ws.State == WebSocketState.Closed);
		}

		public void UseAbility(Ability ab)
		{
			var time = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffffffzzz");
			var id = ab.guid.ToString("X");

			string json1 = $@"{{ ""type"": ""broadcast"", ""msgtype"": ""Chat"", ""msg"":""21|{time}|12345678|Skill Replay|{id}|{ab.name}|"" }}";
			var buffer1 = Encoding.UTF8.GetBytes(json1);
			var segment1 = new ArraySegment<byte>(buffer1);

			string json2 = $@"{{ ""type"": ""broadcast"", ""msgtype"": ""Chat"", ""msg"":""00|{time}|082b|Skill Replayの「{ab.name}」|"" }}";
			var buffer2 = Encoding.UTF8.GetBytes(json2);
			var segment2 = new ArraySegment<byte>(buffer2);

			foreach(var ws in sockets)
			{
				ws.SendAsync(segment1, WebSocketMessageType.Text, true, CancellationToken.None);
				ws.SendAsync(segment2, WebSocketMessageType.Text, true, CancellationToken.None);
			}
			sockets.RemoveAll( ws => ws.State==WebSocketState.Closed);
		}

	}

	public class SkillPlayer
	{
		private Logger log;
		public bool IsReplay { get; private set; } = false;
		private CancellationTokenSource tokenSource = new CancellationTokenSource();
		private WebSocketServer server;

		public SkillPlayer(Logger logger)
		{
			log = logger;
			server = new WebSocketServer(logger);
			_ = server.Start();
		}

		public void Stop()
		{
			tokenSource.Cancel();
			tokenSource = new CancellationTokenSource();
			IsReplay = false;
		}

		public async Task Play(Friendly friend, SummaryEvents se)
		{
			log.Log("start replay");

			var events= se.events;
			if (events.Count == 0) return;

			Stop();
			var ct = tokenSource.Token;

			IsReplay = true;

			server.AddCombatant(friend);

			var first = events.First();
			var start = DateTime.Now;
			foreach (var ev in events)
			{
				if (ct.IsCancellationRequested || ev == null)
				{
					log.Log("stop replay");
					IsReplay = false;
					break;
				}
				if(ev.sourceID!= friend.id) continue;

				if (ev != first)
				{
					var dt = ev.timestamp - first.timestamp;
					var delay = start + TimeSpan.FromMilliseconds(dt) - DateTime.Now;
					if (delay > TimeSpan.Zero)
					{
						await Task.Delay(delay);
					}
				}
				server.UseAbility(ev.ability);
			}
		}
	}
}
