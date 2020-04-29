using ACT_Plugin;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace SkillReplay
{
    public abstract class BindableBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] String propertyName = null)
        {
            if (object.Equals(storage, value)) return false;

            storage = value;
            this.OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var eventHandler = this.PropertyChanged;
            if (eventHandler != null)
            {
                eventHandler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    public class SkillReplayControlViewModel : BindableBase , Logger
	{
        public ReactiveProperty<String> TextPlayButton {get; set; }
        public ReactiveProperty<String> TextFFLogsURL { get; set; } = new ReactiveProperty<string>();
        public ReactiveProperty<String> TextDebug { get; set; } = new ReactiveProperty<string>();


        public ReactiveProperty<Fight> CurrentFight { get; set; } = new ReactiveProperty<Fight>();
        public ReactiveProperty<List<Fight>> FightList {  get;set; } = new ReactiveProperty<List<Fight>>(new List<Fight>());

        public ReactiveProperty<List<Friendly>> OriginalFriendlyList { get; set; } = new ReactiveProperty<List<Friendly>>(new List<Friendly>());

        public ReactiveProperty<List<Friendly>> FriendlyList { get; set; }
        public ReactiveProperty<Friendly> CurrentFriendly { get; set; } = new ReactiveProperty<Friendly>();

        public ReactiveProperty<bool> IsLoading { get; set; } = new ReactiveProperty<bool>(false);

        public ReactiveCommand CommandLoadFights { get;set;}
        public ReactiveCommand CommandLoadSkill { get; set; }


        private ReactiveProperty<bool> HasEvent;// { get; set; }
        public ReactiveProperty<bool> IsPlaying { get; set; } = new ReactiveProperty<bool>(false);
        public ReactiveCommand CommandReplay { get; set; }

        public ReactiveProperty<int> NumberPort { get; set; } = new ReactiveProperty<int>(10601);
        public ReactiveProperty<string> OverlayURL { get; set; }

        public ReactiveProperty<int> AutoReplay { get; set; } = new ReactiveProperty<int>(2);

        private FFLogs fflogs;
        private SkillPlayer replayer;

        public SkillReplayControlViewModel()
        { 
            replayer = new SkillPlayer(this);

            var config = SkillReplayConfig.Instance;

            TextFFLogsURL.Value=config.FFLogsURL;
            NumberPort.Value = config.Port;
            AutoReplay.Value = config.AutoReplay;

            TextPlayButton = IsPlaying.Select(play=> play ? "停止" : "再生").ToReactiveProperty();
            OverlayURL = NumberPort.Select(port => $"https://rawrington.github.io/SkillDisplay/?HOST_PORT=ws://127.0.0.1:{port}/").ToReactiveProperty();


            var IsBusy = new[] { IsLoading, IsPlaying }.CombineLatest(a => a.Any(x => x));
            HasEvent = CurrentFight.CombineLatest(CurrentFriendly, CheckEvents).ToReactiveProperty();

            FriendlyList = OriginalFriendlyList.CombineLatest( CurrentFight, (list, fight) =>
            {
                if (fight != null)
                {
                    string key = $".{fight.id}.";
                    return list.Where( friend=>friend.fights.Contains(key) && friend.type!="LimitBreak").ToList();
                }
                else
                {
                    return new List<Friendly>();
                }
            }).ToReactiveProperty();

            FriendlyList.Subscribe(list =>
            {
                if (!list.Contains(CurrentFriendly.Value))
                {
                    CurrentFriendly.Value = null;
                }
            });

            CommandLoadFights = IsBusy.Select(b=>!b).ToReactiveCommand();
            CommandLoadFights.Subscribe(LoadFights);

            CommandLoadSkill = CurrentFight.CombineLatest(CurrentFriendly, IsBusy, (fight, friend, busy) =>
            {
                if (busy) return false;
                if (fight == null) return false;
                if (friend == null) return false;
                return true;
            }).ToReactiveCommand();
            CommandLoadSkill.Subscribe(LoadEvents);

            CommandReplay = HasEvent.ToReactiveCommand();
            CommandReplay.Subscribe(Replay);

            TextFFLogsURL.Subscribe( url => {
                config.FFLogsURL= url;
                config.Save();
            });
            NumberPort.Subscribe(port => {
                config.Port=port;
                config.Save();
            });
            AutoReplay.Subscribe(autoreplay => {
                config.AutoReplay = autoreplay;
                config.Save();
            });
        }

        public bool CheckEvents(Fight fight, Friendly friend)
        {
            if (fight == null || friend == null || fight.events == null) return false;
            return fight.events.TryGetValue(friend,out _);
        }

        public void CheckStart(string code)
        {
            if(AutoReplay.Value==0) return;
            var fight = CurrentFight.Value;
            var friend = CurrentFriendly.Value;
            if (fight == null || friend == null || fight.events == null) return;
            if( IsPlaying.Value ) return;

            SummaryEvents se;
            if (fight.events.TryGetValue(friend, out se))
            {
                bool replay = false;
                if (AutoReplay.Value == 1)
                {
                    replay = true;
                }
                else if (AutoReplay.Value == 2)
                {
                    var first = se.events.First();
                    var guid = first.ability.guid.ToString("X").PadLeft(4, '0');
                    replay =(code == guid);
                }
                if (replay)
                {
                    Replay();
                }
            }
        }

        public bool IsReady { get {  return HasEvent.Value;} }

        void Replay()
        {
            if( IsPlaying.Value)
            {
                replayer.Stop();
                IsPlaying.Value = false;
            }
            else
            {
                var fight = CurrentFight.Value;
                var friend = CurrentFriendly.Value;
                SummaryEvents events;
                if( fight.events.TryGetValue(friend,out events))
                {
                    _=replayer.Play(friend, events);
                    IsPlaying.Value = true;
                }
            }
        }

        void LoadEvents()
        {
            if(fflogs==null) return;
            IsLoading.Value = true;
            Task.Run(async () =>
            {
                var fight = CurrentFight.Value;
                var friend = CurrentFriendly.Value;
                await fflogs.GetEvents(fight,friend);
                IsLoading.Value = false;
                HasEvent.Value = CheckEvents(fight,friend);
            });
        }

        void LoadFights()
        {
            IsLoading.Value = true;
            Task.Run(async () =>
            {
                var url = TextFFLogsURL.Value;
                if ( url.StartsWith("https://ja.fflogs.com/reports/"))
                {
                    fflogs = new FFLogs(this);
                    if (await fflogs.GetFights(url))
                    {
                        FightList.Value=fflogs.fights.fights;
                        OriginalFriendlyList.Value = fflogs.fights.friendlies;
                        CurrentFight.Value = fflogs.fights.fights.Find(f => f.id == fflogs.fight_id);
                    }
                }
                IsLoading.Value = false;
            });
        }

        private List<string> loglist = new List<string>();

        public void Log(string str)
        {
            Debug.WriteLine(str);
            loglist.Add(str);
            if (loglist.Count > 30)
            {
                loglist.RemoveAt(0);
            }
            TextDebug.Value = String.Join("\n", loglist);
        }
    }
}
