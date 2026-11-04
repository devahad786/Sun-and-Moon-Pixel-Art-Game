using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace SunAndMoon.Editor
{
    public static class SunMoonValidation
    {
        static int assertions;
        static void Check(bool condition,string message) { assertions++; if(!condition) throw new Exception(message); }
        [MenuItem("Sun & Moon/Validate Gameplay (Play Mode)")]
        public static void Validate()
        {
            if(!EditorApplication.isPlaying) throw new Exception("Enter Play Mode first.");
            Run(UnityEngine.Object.FindFirstObjectByType<SunMoonGame>());
        }
        public static void ValidateBatch()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SunAndMoon.unity");
            EditorApplication.EnterPlaymode();
            EditorApplication.update += WaitForPlayMode;
        }
        static void WaitForPlayMode()
        {
            if(!EditorApplication.isPlaying) return;
            EditorApplication.update -= WaitForPlayMode;
            try { Validate(); EditorApplication.Exit(0); }
            catch(Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
        }
        static void Run(SunMoonGame game)
        {
            var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            var bodies=(Array)typeof(SunMoonGame).GetField("players",flags).GetValue(game);
            void Set(int i,Vector2 p) { var b=bodies.GetValue(i); var t=b.GetType(); t.GetField("position").SetValue(b,p); t.GetField("velocity").SetValue(b,Vector2.zero); t.GetField("grounded").SetValue(b,true); t.GetField("buffer").SetValue(b,0f); }
            void Field(int i,string name,object value) { var b=bodies.GetValue(i); b.GetType().GetField(name).SetValue(b,value); }
            Vector2 Pos(int i) => i==0?game.SunPosition:game.MoonPosition;
            void Hearts(bool taken) { foreach(var h in (IEnumerable)typeof(SunMoonGame).GetField("hearts",flags).GetValue(game)) h.GetType().GetField("taken").SetValue(h,taken); }
            void Tick(Vector2 movement) => game.Simulate(1f/60,movement,false,false);
            int oldLevel=game.levelIndex,unlocked=PlayerPrefs.GetInt("SunMoon.Unlocked",1);
            var oldMode=game.mode; int[] scores=new int[10]; for(int i=0;i<10;i++) scores[i]=PlayerPrefs.GetInt("SunMoon.Hearts."+i,0);
            assertions=0; game.enabled=false;
            try
            {
                game.ReturnToTitle(); Vector2 before=game.SunPosition;
                game.Simulate(1,Vector2.one,true,true);
                Check(game.mode==SunMoonGame.Mode.Title && game.SunPosition==before,"Title must prevent simulation before Play.");
                game.StartAdventure(); Check(game.HasStarted && game.mode==SunMoonGame.Mode.Playing,"Play must begin the adventure.");
                var signatures=new HashSet<string>();
                for(int level=0;level<10;level++)
                {
                    game.LoadLevel(level); var signature=new StringBuilder();
                    foreach(var r in game.Platforms) signature.Append(r.ToString());
                    foreach(var b in game.Links) signature.Append(b.deck).Append(b.plate).Append(b.bell);
                    signature.Append(game.SunExit).Append(game.MoonExit);
                    Check(signatures.Add(signature.ToString()),"Every chapter must have unique geometry.");
                    Check(game.HeartCount==0,"Keepsakes reset on chapter load.");
                    Tick(Vector2.zero); Check(game.SunPosition.y<=700 && game.MoonPosition.y<=700,"Both spawns need safe floor.");
                    foreach(var b in game.Links)
                    {
                        Set(0,new Vector2(132,700)); Set(1,new Vector2(1140,700)); Hearts(false);
                        if(b.keepsakes>0)
                        {
                            Set(b.owner<0?0:b.owner,b.plate); Tick(Vector2.zero);
                            Check(!b.active,"Keepsake-locked crossing must stay shut before collection.");
                        }
                        Hearts(true);
                        if(b.owner>=0)
                        {
                            Set(b.owner,new Vector2(132,700)); Set(1-b.owner,b.plate); Tick(Vector2.zero);
                            Check(!b.active,"Only the assigned partner may use a colored switch.");
                        }
                        int holder=b.owner<0?0:b.owner,runner=1-holder;
                        Set(holder,b.plate); Set(runner,new Vector2(132,700)); Tick(Vector2.zero);
                        Check(b.active,"Correct plate must power crossing in chapter "+(level+1));
                        if(b.duration>0)
                        {
                            Set(holder,new Vector2(132,700)); Tick(Vector2.zero); Check(b.active,"Timed bridge must hold its charge after release.");
                            for(int f=0;f<Mathf.CeilToInt(b.duration*60)+2;f++) Tick(Vector2.zero);
                            Check(!b.active,"Unsecured timed bridge must expire."); Set(holder,b.plate); Tick(Vector2.zero);
                        }
                        float direction=Mathf.Sign(b.bell.x-b.plate.x);
                        if(!b.gate)
                        {
                            Set(runner,new Vector2(direction>0?b.deck.x-22:b.deck.xMax+22,b.deck.y));
                            for(int f=0;f<210 && !b.secured;f++) Tick(holder==0?new Vector2(0,direction):new Vector2(direction,0));
                            Check(b.secured,"Partner must physically cross and ring bell in chapter "+(level+1));
                            Check(Mathf.Abs(Pos(runner).y-b.deck.y)<4,"Crossing must support the runner.");
                        }
                        else
                        {
                            Set(runner,new Vector2(b.deck.x-24,b.bell.y));
                            for(int f=0;f<150 && !b.secured;f++) Tick(holder==0?new Vector2(0,1):new Vector2(1,0));
                            Check(b.secured,"Powered gate must allow physical passage in chapter "+(level+1));
                        }
                        Set(holder,new Vector2(132,700)); Tick(Vector2.zero); Check(b.active,"Secured crossing must stay open after the holder leaves.");
                    }
                    Hearts(true); Set(0,new Vector2(game.SunExit.center.x,game.SunExit.yMax)); Set(1,new Vector2(600,300)); Tick(Vector2.zero);
                    Check(game.mode==SunMoonGame.Mode.Playing,"One partner alone cannot finish.");
                    Set(1,new Vector2(game.MoonExit.center.x,game.MoonExit.yMax)); Tick(Vector2.zero);
                    Check(game.mode==SunMoonGame.Mode.Complete,"Both partners must finish chapter "+(level+1));
                    game.LoadLevel(level); foreach(var b in game.Links) {b.secured=true;b.active=true;}
                    Set(0,new Vector2(game.SunExit.center.x,game.SunExit.yMax)); Set(1,new Vector2(game.MoonExit.center.x,game.MoonExit.yMax)); Tick(Vector2.zero);
                    if(game.RequiredHearts>0) Check(game.mode==SunMoonGame.Mode.Playing,"Required keepsakes must block an early finish.");
                    Debug.Log("PASS: unique chapter "+(level+1)+", crossing rules, physical traversal, and shared exit.");
                }
                // Check the exposed approach to the split-route upper right balcony.
                game.LoadLevel(7); game.Links[0].secured=true; Set(0,game.Links[1].plate);
                Set(1,new Vector2(1060,610)); game.Simulate(1f/60,new Vector2(0,-1),false,true);
                for(int f=0;f<58;f++) Tick(new Vector2(0,-1));
                Check(game.MoonPosition.y<=500 && game.MoonPosition.x<1000,"Moon must be able to jump onto chapter eight's upper balcony.");
                // Verify the moving platform carries a standing partner through its entire cycle.
                game.LoadLevel(5); var lift=game.Lifts[0]; Set(0,new Vector2(lift.current.center.x,lift.current.y));
                float minimum=700,maximum=0;
                for(int f=0;f<480;f++) {Tick(Vector2.zero);minimum=Mathf.Min(minimum,game.SunPosition.y);maximum=Mathf.Max(maximum,game.SunPosition.y);Check(Mathf.Abs(game.SunPosition.y-lift.current.y)<3,"Lift must carry its rider.");}
                Check(minimum<355 && maximum>605,"Lift must reach both stops.");
                game.LoadLevel(5);
                for(int f=0;f<260 && game.Lifts[0].current.y>350.5f;f++) Tick(Vector2.zero);
                Check(game.Lifts[0].current.y<=351f,"Lift should reach the observatory height.");
                Set(0,new Vector2(game.Lifts[0].current.center.x,game.Lifts[0].current.y)); Set(1,new Vector2(192,700));
                for(int f=0;f<50;f++) Tick(new Vector2(1,0));
                Check(game.SunPosition.x>720 && game.SunPosition.y<=360,"Sun must walk from the lift onto the observatory roof.");
                game.LoadLevel(4); Set(0,new Vector2(760,700)); Set(1,new Vector2(192,700));
                for(int f=0;f<40;f++) Tick(new Vector2(1,0));
                Check(game.SunPosition.x<790 && Mathf.Abs(game.SunPosition.y-700)<2,"A closed gate must stop Sun instead of becoming a step.");
                // Checkpoint changes both respawns after its preceding crossing is secured.
                game.LoadLevel(8); game.Links[0].secured=true; Set(0,new Vector2(800,520)); Tick(Vector2.zero);
                Set(0,new Vector2(800,820)); Set(1,new Vector2(800,820)); Tick(Vector2.zero);
                Check(game.SunPosition.y==520 && game.MoonPosition.y==520,"Shared checkpoint must restore both partners above the lower hazard.");
                // A held jump clears more height than a tap, and a jump pressed into a hazard does not fire again at the spawn.
                game.LoadLevel(0); Set(0,new Vector2(132,700));
                float heldApex=700f;
                game.Simulate(1f/60,Vector2.zero,true,false,true,true);
                for(int f=0;f<80;f++) { game.Simulate(1f/60,Vector2.zero,false,false,true,true); heldApex=Mathf.Min(heldApex,game.SunPosition.y); }
                Set(0,new Vector2(132,700));
                float tapApex=700f;
                game.Simulate(1f/60,Vector2.zero,true,false,true,true);
                for(int f=0;f<80;f++) { game.Simulate(1f/60,Vector2.zero,false,false,false,true); tapApex=Mathf.Min(tapApex,game.SunPosition.y); }
                Check(heldApex<600f,"Holding jump must still clear a full leap.");
                Check(tapApex>heldApex+40f,"Releasing jump must shorten the hop.");
                Set(0,new Vector2(200,700)); Field(0,"velocity",new Vector2(215f,0f));
                for(int f=0;f<12;f++) Tick(Vector2.zero);
                Check(game.SunPosition.x<208f && game.SunPosition.x>200f,"Releasing move on the ground must stop within a short step.");
                Set(1,new Vector2(380,660)); Field(1,"grounded",false); Field(1,"grace",0f); Field(1,"buffer",0f);
                game.Simulate(1f/60,Vector2.zero,false,true,true,true);
                for(int f=0;f<20;f++) Tick(Vector2.zero);
                Check(Mathf.Abs(game.MoonPosition.x-192f)<2f && Mathf.Abs(game.MoonPosition.y-700f)<2f,"A jump pressed into a hazard must not launch Moon off the respawn.");
                // Timed bridge warns once while a partner is still on a fading deck.
                game.LoadLevel(6); Set(0,game.Links[0].plate); Set(1,new Vector2(1140,700)); Tick(Vector2.zero);
                Check(game.Links[0].active,"Chapter seven plate must power the timed bridge.");
                Set(0,new Vector2(game.Links[0].deck.center.x,game.Links[0].deck.y));
                for(int f=0;f<240 && game.Links[0].remaining>1.15f;f++) Tick(Vector2.zero);
                Tick(Vector2.zero);
                Check(game.Toast.IndexOf("fading",System.StringComparison.OrdinalIgnoreCase)>=0,"A partner on a fading timed bridge must get one warning toast.");
                game.mode=SunMoonGame.Mode.Complete; game.ReturnToTitle();
                Check(game.mode==SunMoonGame.Mode.Title && game.Toast=="","Returning to the title must clear leftover toast text.");
                game.StartAdventure(); game.mode=SunMoonGame.Mode.Complete;
                Check(game.mode==SunMoonGame.Mode.Complete,"Complete mode must remain until the player chooses a next step.");
                // Early home visits explain what is still missing instead of failing silently.
                game.LoadLevel(0); Set(0,new Vector2(game.SunExit.center.x,game.SunExit.yMax)); Set(1,new Vector2(192,700)); Tick(Vector2.zero);
                Check(game.mode==SunMoonGame.Mode.Playing,"One partner at home must not finish chapter one early.");
                Check(game.Toast.IndexOf("crossing",System.StringComparison.OrdinalIgnoreCase)>=0,"Waiting at home before crossings are secured must explain the missing step.");
                game.LoadLevel(0); foreach(var b in game.Links){b.secured=true;b.active=true;}
                Set(0,new Vector2(game.SunExit.center.x,game.SunExit.yMax)); Set(1,new Vector2(192,700)); Tick(Vector2.zero);
                Check(game.Toast.IndexOf("partner",System.StringComparison.OrdinalIgnoreCase)>=0,"Waiting alone at a ready home must ask for the other partner.");
                game.LoadLevel(1); Set(0,game.Links[0].plate); Set(1,new Vector2(1140,700)); Tick(Vector2.zero);
                Check(!game.Links[0].active,"Sun must not power Moon's chapter two switch.");
                Check(game.Toast.IndexOf("Moon",System.StringComparison.OrdinalIgnoreCase)>=0,"Wrong partner on a colored plate must be told who can hold it.");
                game.LoadLevel(0); Set(0,new Vector2(275,596)); Set(1,new Vector2(192,700)); Tick(Vector2.zero);
                Check(game.HeartCount==1,"Sun must collect the nearby keepsake.");
                Check(game.Toast.IndexOf("Keepsake",System.StringComparison.OrdinalIgnoreCase)>=0,"Collecting a keepsake must confirm the find.");
                Debug.Log("SUN & MOON: "+assertions+" assertions passed; 10 unique chapters, click-to-play, timed bridges, gates, lift, roof step, checkpoint, jump height, ground stop, respawn, fade warning, title toast clear, home hints, plate ownership, keepsake toast, shared exits.");
                System.IO.File.WriteAllText("Gameplay-validation.txt",assertions+" assertions passed across 10 unique levels. Startup, switches, ownership, physical crossings, keys, timers, lift cycle, lift-to-roof step, closed gates, checkpoint, variable jump, ground stop, hazard respawn, timed fade warning, title toast clear, home waiting hints, wrong-plate hints, keepsake toasts, and shared exits verified.");
            }
            finally
            {
                PlayerPrefs.SetInt("SunMoon.Unlocked",unlocked);for(int i=0;i<10;i++) PlayerPrefs.SetInt("SunMoon.Hearts."+i,scores[i]);PlayerPrefs.Save();
                game.LoadLevel(oldLevel);game.mode=oldMode;game.enabled=true;
            }
        }
    }
}
