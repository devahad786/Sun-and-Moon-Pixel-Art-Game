using System.Collections.Generic;
using UnityEngine;

namespace SunAndMoon
{
    public sealed partial class SunMoonGame
    {
        public sealed class Link
        {
            public Rect deck;
            public Vector2 plate, bell;
            public int owner = -1, keepsakes;
            public float duration, remaining;
            public bool gate, active, secured, fadeWarned;
        }
        public sealed class Lift
        {
            public Rect origin, current;
            public Vector2 travel;
            public float speed;
        }
        readonly List<Link> links = new List<Link>();
        readonly List<Lift> lifts = new List<Lift>();
        public IReadOnlyList<Link> Links => links;
        public IReadOnlyList<Lift> Lifts => lifts;
        public IReadOnlyList<Rect> Platforms => platforms;
        public Rect SunExit => exitSun;
        public Rect MoonExit => exitMoon;
        public bool HasStarted { get; private set; }
        public float Elapsed => elapsed;
        public int RequiredHearts { get; private set; }
        Vector2 checkpoint;
        bool checkpointActive;
        readonly string[] regions = { "THE OVERGROWN GARDEN", "THE TWILIGHT CANAL", "THE SKYWARD TOWER", "THE AMBER ISLANDS", "THE KEEPSAKE VAULT", "THE CLOUD OBSERVATORY", "THE FADING CAUSEWAY", "THE MIRROR COURTYARD", "THE STARFALL TERRACES", "THE DAWN SANCTUARY" };
        readonly string[] skyTop = { "142936", "20253e", "27364d", "342b30", "292339", "253b4b", "282d3e", "263b3c", "23233e", "453643" };
        readonly string[] skyBottom = { "30483f", "3a485e", "4a606c", "6a5040", "4e3c58", "54747c", "4b4d63", "46665c", "4d456d", "7d6255" };

        public void StartAdventure()
        {
            HasStarted = true;
            audioSource.enabled = true;
            LoadLevel(Mathf.Max(0, Unlocked - 1));
        }
        public void ReturnToTitle() { mode = Mode.Title; toast = ""; toastTime = 0; }
        public void LoadLevel(int index)
        {
            levelIndex = Mathf.Clamp(index, 0, 9); mode = Mode.Playing;
            elapsed = 0; retries = 0; homeHintCooldown = 0; plateHintCooldown = 0; checkpointHintCooldown = 0; platforms.Clear(); pools.Clear(); hearts.Clear(); links.Clear(); lifts.Clear();
            RequiredHearts = 0; checkpoint = Vector2.zero; checkpointActive = false;
            players[0].spawn = new Vector2(132, 700); players[1].spawn = new Vector2(192, 700);
            exitSun = new Rect(1090, 628, 42, 72); exitMoon = new Rect(1145, 628, 42, 72);
            switch (levelIndex)
            {
                case 0: // Learn to hold a bridge; a gap wider than a normal jump.
                    P(72,700,398,64); P(730,700,478,64);
                    P(210,610,145); P(350,520,150); P(815,610,145); P(970,520,155);
                    B(470,700,260, new Vector2(400,700),new Vector2(790,700));
                    H(275,580,0); H(420,490,1); H(880,580,1); H(1040,490,0); PoolAt(330,700,65,0); break;
                case 1: // Start apart; Moon opens the way from the opposite bank.
                    players[1].spawn=new Vector2(1140,700);
                    P(72,700,428,64); P(760,700,448,64);
                    P(235,600,150); P(900,600,150); P(795,510,125); P(355,510,125);
                    B(500,700,260,new Vector2(900,700),new Vector2(415,700),1);
                    Exits(100,700,160,700); PoolAt(270,700,72,0); PoolAt(1000,700,72,1);
                    H(300,570,0); H(420,480,1); H(965,570,1); H(855,480,0); break;
                case 2: // Climb a tall staircase; the bridge is on the upper storey.
                    P(72,700,1136,64); P(170,610,210); P(360,520,210); P(550,430,150); P(900,430,308);
                    B(700,430,200,new Vector2(615,430),new Vector2(955,430));
                    Exits(1050,430,1110,430); PoolAt(725,700,110,2);
                    H(230,580,0); H(430,490,1); H(620,400,0); H(1005,400,1); break;
                case 3: // Relay across two isolated islands; swap who holds the switch.
                    P(72,700,278,64); P(610,700,130,64); P(1000,700,208,64);
                    P(170,600,130); P(620,590,110); P(1040,600,135);
                    B(350,700,260,new Vector2(280,700),new Vector2(655,700));
                    B(740,700,260,new Vector2(687,700),new Vector2(1050,700));
                    H(235,570,0); H(670,560,1); H(1090,570,0); H(1140,670,1); break;
                case 4: // Both partners climb the vault to collect their keys before opening its gate.
                    RequiredHearts=4; P(72,700,1136,64);
                    P(180,610,160); P(350,520,150); P(510,430,170); P(660,520,135); P(870,600,130);
                    B(800,700,0,new Vector2(710,700),new Vector2(865,700),-1,0,true,4);
                    H(250,580,0); H(420,490,1); H(590,400,0); H(725,490,1);
                    PoolAt(960,700,84,2); break;
                case 5: // Ride a lift from a low balcony to the observatory.
                    P(72,700,1136,64); P(190,610,160); P(365,520,155); P(700,350,508);
                    lifts.Add(new Lift { origin=new Rect(545,610,125,22), current=new Rect(545,610,125,22), travel=new Vector2(0,-260), speed=.85f });
                    B(930,350,0,new Vector2(805,350),new Vector2(1005,350),1,0,true);
                    Exits(1080,350,1140,350);
                    H(260,580,0); H(435,490,1); H(750,320,0); H(1040,320,1); PoolAt(800,700,120,2); break;
                case 6: // A temporary charge lets both partners leave the switch before the bridge fades.
                    P(72,700,328,64); P(820,700,388,64); P(180,600,140); P(900,600,140); P(1040,510,140);
                    B(400,700,420,new Vector2(330,700),new Vector2(875,700),-1,3.5f);
                    H(245,570,0); H(350,670,1); H(965,570,1); H(1110,480,0); PoolAt(1045,700,60,0); break;
                case 7: // Two independent crossings at different heights; each has a different owner.
                    players[1].spawn=new Vector2(1140,700);
                    P(72,700,428,64); P(800,700,408,64); P(175,610,170); P(350,500,150); P(800,500,200); P(985,610,160);
                    B(500,700,300,new Vector2(880,700),new Vector2(430,700),1);
                    B(500,500,300,new Vector2(425,500),new Vector2(860,500),0);
                    Exits(860,500,920,500);
                    H(240,580,0); H(415,470,1); H(1050,580,1); H(950,470,0); break;
                case 8: // Ascending zigzag with a shared checkpoint after the first crossing.
                    P(72,700,1136,64); P(150,610,140); P(295,520,125); P(700,520,180); P(790,430,110); P(1100,430,108);
                    B(420,520,280,new Vector2(350,520),new Vector2(750,520));
                    B(900,430,200,new Vector2(840,430),new Vector2(1150,430));
                    checkpoint=new Vector2(800,520); Exits(1105,430,1160,430);
                    H(215,580,0); H(350,490,1); H(840,400,0); H(1150,400,1); PoolAt(500,700,150,2); break;
                case 9: // Finale combines keys, timed crossing, relay, checkpoint, and an elevated home.
                    RequiredHearts=4; P(72,700,278,64); P(610,700,140,64); P(1010,700,198,64);
                    P(170,610,130); P(300,520,135); P(625,610,110); P(1035,610,150); P(1080,520,128);
                    B(350,700,260,new Vector2(280,700),new Vector2(660,700),-1,3.2f);
                    B(750,700,260,new Vector2(700,700),new Vector2(1060,700),0,0,false,2);
                    checkpoint=new Vector2(680,700); Exits(1090,520,1150,520);
                    H(230,580,0); H(365,490,1); H(680,580,1); H(1110,580,0); break;
            }
            foreach(var p in players) { p.position=p.spawn; p.velocity=Vector2.zero; p.grounded=true; p.wasGrounded=true; p.grace=.12f; p.buffer=0; p.squash=0; p.face=1; }
            if (backdrop) Destroy(backdrop); backdrop=MakeBackdrop();
            toast=""; toastTime=0;
        }
        void P(float x,float y,float w,float h=28) { platforms.Add(new Rect(x,y,w,h)); }
        void H(float x,float y,int owner) { hearts.Add(new Heart { p=new Vector2(x,y),owner=owner }); }
        void Exits(float x0,float floor0,float x1,float floor1) { exitSun=new Rect(x0,floor0-72,42,72); exitMoon=new Rect(x1,floor1-72,42,72); }
        void PoolAt(float x,float floor,float w,int owner) { pools.Add(new Pool { r=new Rect(x,floor-24,w,24),owner=owner }); }
        void B(float x,float y,float width,Vector2 plate,Vector2 bell,int owner=-1,float duration=0,bool gate=false,int keepsakes=0)
        { links.Add(new Link { deck=gate?new Rect(x,200,24,y-200):new Rect(x,y,width,24),plate=plate,bell=bell,owner=owner,duration=duration,gate=gate,keepsakes=keepsakes }); }
        bool At(Partner p, Vector2 point, float range=28) => Mathf.Abs(p.position.x-point.x)<range && Mathf.Abs(p.position.y-point.y)<7 && p.grounded;
        bool AllSecured() => links.TrueForAll(b=>b.secured);
        void UpdatePuzzles(float dt)
        {
            foreach(var b in links)
            {
                bool held=false;
                for(int i=0;i<2;i++) if((b.owner<0 || b.owner==i) && At(players[i],b.plate)) held=true;
                if(held && HeartCount>=b.keepsakes) { b.remaining=b.duration; b.fadeWarned=false; }
                else b.remaining=Mathf.Max(0,b.remaining-dt);
                b.active=b.secured || (HeartCount>=b.keepsakes && (held || b.remaining>0));
                if(!b.secured && b.active && (At(players[0],b.bell) || At(players[1],b.bell)))
                { b.secured=true; b.active=true; toast="Crossing secured. Your turn to follow!"; toastTime=3; PlaySound(chime); }
                // Timed bridges give one warning while a partner is still on the deck and the charge is nearly gone.
                if(b.duration>0 && !b.secured && b.active && !held && !b.fadeWarned && b.remaining>0 && b.remaining<=FadeWarnSeconds)
                {
                    bool onDeck=false;
                    for(int i=0;i<2;i++)
                    {
                        var p=players[i];
                        if(p.grounded && Mathf.Abs(p.position.y-b.deck.y)<4 && p.position.x+10>b.deck.x && p.position.x-10<b.deck.xMax) onDeck=true;
                    }
                    if(onDeck) { toast="Bridge fading — keep moving!"; toastTime=2.2f; b.fadeWarned=true; }
                }
            }
            foreach(var lift in lifts)
            {
                Rect before=lift.current;
                // Starts at the low stop, then travels smoothly up and down.
                Vector2 offset=lift.travel*(.5f-.5f*Mathf.Cos(elapsed*lift.speed));
                lift.current=new Rect(lift.origin.position+offset,lift.origin.size);
                foreach(var p in players) if(p.grounded && Mathf.Abs(p.position.y-before.y)<3 && p.position.x+10>before.x && p.position.x-10<before.xMax)
                    p.position+=lift.current.position-before.position;
            }
            if(checkpoint!=Vector2.zero && !checkpointActive && links[0].secured)
                foreach(var p in players) if(At(p,checkpoint,42))
                {
                    checkpointActive=true;
                    players[0].spawn=checkpoint+new Vector2(-16,0); players[1].spawn=checkpoint+new Vector2(16,0);
                    toast="A place to return to. Shared checkpoint saved."; toastTime=3; PlaySound(chime); break;
                }
        }
        void PlaySound(AudioClip clip) { if(HasStarted && audioSource.enabled) audioSource.PlayOneShot(clip); }
        void OnApplicationFocus(bool focus) { if(!focus && HasStarted && mode==Mode.Playing) mode=Mode.Paused; }
    }
}
