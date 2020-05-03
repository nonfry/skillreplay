using ACT_Plugin;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SkillReplay
{


	[DataContract]
	public class Fight
	{
		[DataMember]
		public int id;
		[DataMember]
		public int start_time;
		[DataMember]
		public int end_time;
		[DataMember]
		public int boss;
		[DataMember]
		public string name;
		[DataMember]
		public int zoneID;
		[DataMember]
		public string zoneName;
		[DataMember]
		public int size;
		[DataMember]
		public int difficulty;
		[DataMember]
		public bool kill;
		[DataMember]
		public int partial;
		[DataMember]
		public bool standardComposition;
		[DataMember]
		public int bossPercentage;
		[DataMember]
		public int fightPercentage;
		[DataMember]
		public int lastPhaseForPercentageDisplay;

		[IgnoreDataMember]
		public Dictionary<Friendly, SummaryEvents> events = new Dictionary<Friendly, SummaryEvents>();

		public Fight()
		{
			events = new Dictionary<Friendly, SummaryEvents>();
		}

		override public string ToString()
		{
			TimeSpan ts = new TimeSpan(0, 0, 0, 0, end_time - start_time);
			string time = ts.ToString(@"mm\:ss");
			string killed = (kill ? "killed" : "wipe");
			return $"{id}:{name} - {zoneName} ({time}) {killed}";
		}
	}

	[DataContract]
	public class Friendly
	{
		[DataMember]
		public string name;
		[DataMember]
		public int id;
		[DataMember]
		public string guid;
		[DataMember]
		public string type;
		[DataMember]
		public string fights;
		[DataMember]
		public string bosses;

		override public string ToString()
		{
			return $"{type} - {name}";
		}
	}

	[DataContract]
	public class FightsAndFiends
	{
		[DataMember]
		public List<Fight> fights;

		[DataMember]
		public List<Friendly> friendlies;
	}

	[DataContract]
	public class Ability
	{
		[DataMember]
		public string name;
		[DataMember]
		public int guid;
		[DataMember]
		public int type;
	}

	[DataContract]
	public class Event
	{
		[DataMember]
		public int timestamp;
		[DataMember]
		public string type;
		[DataMember]
		public int sourceID;
		[DataMember]
		public bool sourceIsFriendly;
		[DataMember]
		public int targetID;
		[DataMember]
		public int targetInstanc;
		[DataMember]
		public bool targetIsFriendly;
		[DataMember]
		public Ability ability;
		[DataMember]
		public int hitType;
		[DataMember]
		public int amount;
		[DataMember]
		public int absorbed;
		[DataMember]
		public bool multistrike;
		[DataMember]
		public float debugMultiplier;
		[DataMember]
		public int packetID;
	}

	[DataContract]
	public class SummaryEvents
	{
		[DataMember]
		public List<Event> events;
		[DataMember]
		public long nextPageTimestamp;

		public void concat(SummaryEvents se)
		{
			events.AddRange(se.events);
			nextPageTimestamp = se.nextPageTimestamp;
		}
	}


	class Cache<T>
	{
		class Data
		{
			public string key;
			public T obj;
		}

		List<Data> cache = new List<Data>();
		int limit;


		public Cache(int size)
		{
			limit = size;
		}

		public bool Has(string key)
		{
			return cache.Exists(c => c.key == key);
		}

		public T GetItem(string key)
		{
			var data = cache.Find(d => d.key == key);
			return data.obj;
		}

		public void SetItem(string key, T val)
		{
			var data = cache.Find(d => d.key == key);
			if( data != null )
			{
				data.obj = val;
				cache.Remove(data);
				cache.Add(data);
			}
			else
			{
				data = new Data { key = key, obj = val };
				cache.Add(data);
				if( cache.Count > limit )
				{
					cache.RemoveAt(0);
				}
			}
		}
	}

	public class FFLogs
	{
		public FightsAndFiends fights;

		Logger log;
		public string id;
		public int fight_id = 0;
		private Cache<FightsAndFiends> cache = new Cache<FightsAndFiends>(30);

		public FFLogs(Logger logger)
		{
			log = logger;
		}

		private async Task<string> Download(string url)
		{
			var request = new HttpRequestMessage(HttpMethod.Get, url);
			request.Headers.Add("ContentType", "application/json");
			request.Headers.Add("Accept-Language", "ja");

			HttpClient httpClient = new HttpClient();
			var res = httpClient.SendAsync(request);
			var html = await res.Result.Content.ReadAsStringAsync();
			return html;
		}

		private const string URL_BASE = "https://www.fflogs.com/reports";

		private string FightsAndParticipantsUrl()
		{
			return $"{URL_BASE}/fights-and-participants/{id}/0";
		}

		private string EventsUrl(Fight fight, Friendly friend, SummaryEvents prev)
		{
			var start = prev != null ? prev.nextPageTimestamp : fight.start_time;
			var end = fight.end_time;
			return $"{URL_BASE}/summary-events/{id}/{fight.id}/{start}/{end}/{friend.id}/0/Any/0/-1.0.-1/0";
		}

		private void Clear()
		{
			id = null;
			fight_id = 0;
			fights = null;
		}

		public async Task<bool> GetFights(string url_)
		{
			Uri uri = new Uri(url_);
			Clear();
			try
			{
				if( !uri.AbsolutePath.StartsWith("/reports/fights-and-participants/") )
				{
					if( uri.Segments.Length != 3 )
					{
						return false;
					}
					id = uri.Segments[2];
				}
				else if( uri.AbsolutePath.StartsWith("/reports/fights-and-participants/") )
				{
					id = uri.Segments[3];
				}

				string sfight = null;

				var m = Regex.Match(uri.AbsoluteUri, "#fight=(\\d+|last)");
				if( m.Success )
				{
					sfight = m.Groups[1].Value;
				}

				if( id != null )
				{
					var url = FightsAndParticipantsUrl();
					if( cache.Has(url) )
					{
						fights = cache.GetItem(url);
						log.Log("fights from cache");
					}
					else
					{
						log.Log("fights download");
						var html = await Download(url);
						var serializer = new DataContractJsonSerializer(typeof(FightsAndFiends));
						var ms = new MemoryStream(Encoding.UTF8.GetBytes(html));
						fights = serializer.ReadObject(ms) as FightsAndFiends;
						cache.SetItem(url, fights);
						log.Log("download ok");
					}
					fight_id = 0;
					if( sfight != null && fights != null && fights.fights != null && fights.fights.Count > 0 )
					{
						if( sfight == "last" )
						{
							fight_id = fights.fights.Last().id;
						}
						else if( int.TryParse(sfight, out fight_id) )
						{
							if( !fights.fights.Exists(f => f.id == fight_id) )
							{
								fight_id = 0;
							}
						}
					}
				}

				return true;
			}
			catch( Exception )
			{
				return false;
			}
		}

		public async Task<SummaryEvents> GetEvents(Fight fight, Friendly friend)
		{
			SummaryEvents evts = null;
			if( id == null || this.fights == null || !this.fights.friendlies.Contains(friend) )
			{
				return null;
			}

			try
			{
				if( fight.events == null )
				{
					fight.events = new Dictionary<Friendly, SummaryEvents>();
				}
				fight.events.TryGetValue(friend, out evts);

				log.Log("skill download");
				while( evts == null || (evts.nextPageTimestamp - fight.start_time) < 3 * 60 * 1000 )
				{
					log.Log("downloading ...");
					var url = EventsUrl(fight, friend, evts);
					var html = await Download(url);
					var serializer = new DataContractJsonSerializer(typeof(SummaryEvents));
					var ms = new MemoryStream(Encoding.UTF8.GetBytes(html));
					SummaryEvents ev = serializer.ReadObject(ms) as SummaryEvents;
					if( ev.events.Count == 0 ) break;
					ev.events = ev.events.Where(e => e.sourceID == friend.id && e.type == "cast" && e.ability.guid > 8).ToList();

					if( evts != null )
					{
						evts.concat(ev);
					}
					else
					{
						evts = ev;
						fight.events.Add(friend, evts);
					}
					await Task.Delay(500);
				}
				log.Log("download ok");
				return evts;
			}
			catch( Exception )
			{
				return null;
			}
		}

	}

}
