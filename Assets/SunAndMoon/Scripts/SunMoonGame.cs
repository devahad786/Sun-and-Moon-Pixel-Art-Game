using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SunAndMoon
{
    // All gameplay coordinates use a 1280 x 800 design surface, with Y pointing down.
    public sealed partial class SunMoonGame : MonoBehaviour
    {
        public enum Mode { Title, Playing, Paused, Map, Complete }
        public Mode mode = Mode.Title;
        public int levelIndex;
        public int Unlocked => Mathf.Clamp(PlayerPrefs.GetInt("SunMoon.Unlocked", 1), 1, 10);
        public int HeartCount => hearts.FindAll(h => h.taken).Count;
        public bool BridgeLatched => AllSecured();
        public Vector2 SunPosition => players[0].position;
        public Vector2 MoonPosition => players[1].position;
        public string Toast => toast;
        class Partner { public Vector2 position, velocity; public bool grounded, wasGrounded = true; public float grace, buffer, face = 1, squash; public Vector2 spawn; }
        class Heart { public Vector2 p; public int owner; public bool taken; }
        struct Pool { public Rect r; public int owner; public Pool(float x, float w, int type) { r = new Rect(x, 676, w, 24); owner = type; } }
        readonly Partner[] players = { new Partner(), new Partner() };
        readonly List<Rect> platforms = new List<Rect>();
        readonly List<Pool> pools = new List<Pool>();
        readonly List<Heart> hearts = new List<Heart>();
        readonly string[] names = { "A little leap of faith", "On opposite shores", "Up where we belong", "Pass it on", "The keepsake vault", "Love lifts us up", "Before the light fades", "Two sides of the story", "A place to return to", "Home is with you" };
        readonly string[] tips = { "Hold switch 1 while your partner crosses to bell 1. Then follow them home.", "Starting apart: Moon holds the blue switch on the right. Sun rings the left bell.", "Climb the tower together. The crossing and your home are high above the garden.", "Two crossings, two acts of kindness. Take turns holding the island switches.", "Find all four colored keepsakes to power the vault gate. Then work the switch.", "Ride the moving lift to the roof. Moon controls the blue observatory gate.", "The bridge stays charged for 3.5 seconds after you leave its switch. A fading deck warns you once.", "Moon controls the lower crossing. Sun controls the upper one. Secure both paths.", "Cross the terraces, save your shared checkpoint, then climb to the final bridge.", "Gather every keepsake. Timed bridge, shared checkpoint, Sun's switch, and home." };
        Color ink = Hex("101c25"), cream = Hex("f6e8ca"), muted = Hex("93a99e"), gold = Hex("edbd62"), blue = Hex("89c9dc");
        Texture2D backdrop, sunSprite, moonSprite, heartSprite;
        GUIStyle textStyle;
        Font font;

        float elapsed, flash, toastTime, homeHintCooldown, plateHintCooldown, checkpointHintCooldown, keepsakeHintCooldown, hazardHintCooldown;
        string toast = "";
        int retries;

        const float MoveSpeed = 215f;
        const float AirAccel = 1700f;
        const float GroundStopAccel = 3400f;
        const float FallGravity = 1150f;
        const float ShortHopGravity = 2800f;
        const float JumpSpeed = -530f;
        const float MaxFall = 650f;
        const float JumpBuffer = .14f;
        const float CoyoteTime = .12f;
        const float FadeWarnSeconds = 1.2f;

        Rect exitSun, exitMoon;
        AudioSource audioSource;
        AudioClip chime, resetSound;
        public static Color Hex(string s) { ColorUtility.TryParseHtmlString("#" + s, out var c); return c; }

        void Awake()
        {
            Application.targetFrameRate = 60;
#if UNITY_WEBGL && !UNITY_EDITOR
            WebGLInput.captureAllKeyboardInput = false;
#endif
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            backdrop = MakeBackdrop();
            sunSprite = MakePartner(false); moonSprite = MakePartner(true); heartSprite = MakeHeart();
            audioSource = gameObject.AddComponent<AudioSource>(); audioSource.volume = .16f;
            chime = Tone(660, .18f); resetSound = Tone(180, .12f);
            audioSource.enabled = false;
            LoadLevel(0); mode = Mode.Title;
        }
        AudioClip Tone(float hz, float duration)
        {
            int count = (int)(44100 * duration); var data = new float[count];
            for (int i = 0; i < count; i++) { float t = i / 44100f; data[i] = Mathf.Sin(t * hz * 6.28318f) * Mathf.Pow(1 - i / (float)count, 2) * .4f; }
            var c = AudioClip.Create("Soft chime", count, 1, 44100, false); c.SetData(data, 0); return c;
        }
        bool Press(Key key) => Keyboard.current != null && Keyboard.current[key].wasPressedThisFrame;
        bool Held(Key key) => Keyboard.current != null && Keyboard.current[key].isPressed;
        void Update()
        {
            if (Press(Key.Escape) || Press(Key.P))
            {
                if (mode == Mode.Playing) mode = Mode.Paused;
                else if (mode == Mode.Paused || mode == Mode.Map) mode = Mode.Playing;
                else if (mode == Mode.Complete) mode = Mode.Map;
            }
            if (mode != Mode.Playing) return;
            if (Press(Key.R)) { LoadLevel(levelIndex); return; }
            Vector2 movement = new Vector2((Held(Key.D) ? 1 : 0) - (Held(Key.A) ? 1 : 0), (Held(Key.RightArrow) ? 1 : 0) - (Held(Key.LeftArrow) ? 1 : 0));
            Simulate(Mathf.Min(Time.deltaTime, .033f), movement, Press(Key.W), Press(Key.UpArrow), Held(Key.W), Held(Key.UpArrow));
        }
        // Shared by live keyboard play and the deterministic editor route checks.
        // Jump-held defaults to true so existing route checks keep the full leap.
        public void Simulate(float dt, Vector2 movement, bool sunJump, bool moonJump, bool sunJumpHeld = true, bool moonJumpHeld = true)
        {
            if (mode != Mode.Playing) return;
            elapsed += dt; flash = Mathf.Max(0, flash - dt); toastTime -= dt;
            UpdatePuzzles(dt);
            for (int i = 0; i < 2; i++)
            {
                var p = players[i]; float direction = Mathf.Clamp(i == 0 ? movement.x : movement.y, -1, 1);
                bool jumpHeld = i == 0 ? sunJumpHeld : moonJumpHeld;
                if (i == 0 ? sunJump : moonJump) p.buffer = JumpBuffer; else p.buffer -= dt;
                p.grace = p.grounded ? CoyoteTime : p.grace - dt;
                if (p.buffer > 0 && p.grace > 0) { p.velocity.y = JumpSpeed; p.buffer = 0; p.grace = 0; p.grounded = false; }
                float accel = direction == 0 && p.grounded ? GroundStopAccel : AirAccel;
                p.velocity.x = Mathf.MoveTowards(p.velocity.x, direction * MoveSpeed, dt * accel);
                // Y points down. Extra gravity while rising and the key is up turns a tap into a shorter hop.
                float gravity = !jumpHeld && p.velocity.y < 0 ? ShortHopGravity : FallGravity;
                p.velocity.y = Mathf.Min(p.velocity.y + gravity * dt, MaxFall);
                Move(p, dt);
                if (Mathf.Abs(p.velocity.x) > 8) p.face = Mathf.Sign(p.velocity.x);
                if (p.grounded && !p.wasGrounded) p.squash = 1f;
                p.squash = Mathf.MoveTowards(p.squash, 0, dt * 5f);
                p.wasGrounded = p.grounded;
                if (p.position.y > 810) { Respawn(i); continue; }
                foreach (var pool in pools)
                    if (p.position.x > pool.r.x + 4 && p.position.x < pool.r.xMax - 4 && p.position.y > pool.r.y + 1 && p.position.y <= pool.r.yMax + 12 && pool.owner != i) { Respawn(i); break; }
                foreach (var h in hearts)
                {
                    if (h.taken || Vector2.Distance(p.position - new Vector2(0, 16), h.p) >= 33) continue;
                    if (h.owner == i)
                    {
                        h.taken = true; PlaySound(chime);
                        if (RequiredHearts > 0 && HeartCount >= RequiredHearts) toast = "Keepsakes ready. The way can open.";
                        else toast = "Keepsake found. " + HeartCount + " / 4";
                        toastTime = 2.2f;
                    }
                    else if (keepsakeHintCooldown <= 0 && toastTime <= 0)
                    {
                        toast = h.owner == 0 ? "That's Sun's keepsake." : "That's Moon's keepsake.";
                        toastTime = 2.2f; keepsakeHintCooldown = 3.5f;
                    }
                }
            }
            keepsakeHintCooldown = Mathf.Max(0, keepsakeHintCooldown - dt);
            UpdatePuzzles(0);
            HintPlateIssues(dt);
            HintLockedCheckpoint(dt);
            HintNearHazard(dt);
            bool sunHome = exitSun.Contains(players[0].position - Vector2.up * 8);
            bool moonHome = exitMoon.Contains(players[1].position - Vector2.up * 8);
            if (AllSecured() && HeartCount >= RequiredHearts && sunHome && moonHome)
            {
                mode = Mode.Complete;
                PlayerPrefs.SetInt("SunMoon.Unlocked", Mathf.Max(Unlocked, Mathf.Min(levelIndex + 2, 10)));
                PlayerPrefs.SetInt("SunMoon.Hearts." + levelIndex, Mathf.Max(PlayerPrefs.GetInt("SunMoon.Hearts." + levelIndex), HeartCount)); PlayerPrefs.Save();
                PlaySound(chime);
            }
            else HintIfWaitingAtHome(sunHome, moonHome, dt);
        }
        // Standing in an exit arch used to fail silently when crossings, keepsakes, or a partner were still missing.
        void HintIfWaitingAtHome(bool sunHome, bool moonHome, float dt)
        {
            homeHintCooldown = Mathf.Max(0, homeHintCooldown - dt);
            if ((!sunHome && !moonHome) || homeHintCooldown > 0 || toastTime > 0) return;
            if (!AllSecured()) toast = "Secure every crossing before home.";
            else if (HeartCount < RequiredHearts) toast = "Gather the keepsakes first. " + HeartCount + " / " + RequiredHearts;
            else if (sunHome != moonHome) toast = "Your partner still needs to reach home.";
            else return;
            toastTime = 2.4f;
            homeHintCooldown = 3.5f;
        }
        // Colored or keepsake-locked plates used to ignore the wrong state with no explanation.
        void HintPlateIssues(float dt)
        {
            plateHintCooldown = Mathf.Max(0, plateHintCooldown - dt);
            if (plateHintCooldown > 0 || toastTime > 0) return;
            foreach (var b in links)
            {
                if (b.secured) continue;
                if (b.owner >= 0)
                {
                    int wrong = 1 - b.owner;
                    if (At(players[wrong], b.plate) && !At(players[b.owner], b.plate))
                    {
                        toast = b.owner == 0 ? "Only Sun can hold this switch." : "Only Moon can hold this switch.";
                        toastTime = 2.4f; plateHintCooldown = 3.5f; return;
                    }
                }
                if (b.keepsakes <= 0 || HeartCount >= b.keepsakes) continue;
                bool onPlate = b.owner < 0 ? (At(players[0], b.plate) || At(players[1], b.plate)) : At(players[b.owner], b.plate);
                if (!onPlate) continue;
                toast = "Need " + b.keepsakes + " keepsakes first. " + HeartCount + " / " + b.keepsakes;
                toastTime = 2.4f; plateHintCooldown = 3.5f; return;
            }
        }
        // The shared checkpoint stays dark until the first crossing is secured.
        void HintLockedCheckpoint(float dt)
        {
            checkpointHintCooldown = Mathf.Max(0, checkpointHintCooldown - dt);
            if (checkpoint == Vector2.zero || checkpointActive || links.Count == 0 || links[0].secured) return;
            if (checkpointHintCooldown > 0 || toastTime > 0) return;
            foreach (var p in players)
            {
                if (!At(p, checkpoint, 42)) continue;
                toast = "Secure the first crossing to save this checkpoint.";
                toastTime = 2.4f; checkpointHintCooldown = 3.5f; return;
            }
        }
        // Colored pools used to teach their rules only after a fall.
        void HintNearHazard(float dt)
        {
            hazardHintCooldown = Mathf.Max(0, hazardHintCooldown - dt);
            if (hazardHintCooldown > 0 || toastTime > 0) return;
            for (int i = 0; i < 2; i++)
            {
                var p = players[i];
                if (!p.grounded) continue;
                foreach (var pool in pools)
                {
                    if (pool.owner == i) continue;
                    bool inside = p.position.x > pool.r.x + 4 && p.position.x < pool.r.xMax - 4 && p.position.y > pool.r.y + 1 && p.position.y <= pool.r.yMax + 12;
                    bool near = p.position.x > pool.r.x - 18 && p.position.x < pool.r.xMax + 18 && p.position.y > pool.r.y - 8 && p.position.y <= pool.r.yMax + 20;
                    if (!near || inside) continue;
                    toast = pool.owner == 2 ? "That pool is not safe for either of you."
                        : pool.owner == 0 ? "That gold pool is only safe for Sun."
                        : "That blue pool is only safe for Moon.";
                    toastTime = 2.4f; hazardHintCooldown = 3.5f; return;
                }
            }
        }
        void Move(Partner p, float dt)
        {
            float previousX = p.position.x;
            p.position.x = Mathf.Clamp(p.position.x + p.velocity.x * dt, 88, 1191);
            var body = Body(p);
            var solids = new List<Rect>(platforms); foreach (var b in links) if (b.gate && !b.active) solids.Add(b.deck);
            foreach (var r in solids) ResolveHorizontal(p, ref body, r, previousX);
            float oldY = p.position.y; p.position.y += p.velocity.y * dt; p.grounded = false;
            foreach (var r in solids) ResolveVertical(p, oldY, r);
            foreach (var b in links) if (!b.gate && b.active) ResolveVertical(p, oldY, b.deck);
            foreach (var lift in lifts) ResolveVertical(p, oldY, lift.current);
        }
        Rect Body(Partner p) => new Rect(p.position.x - 11, p.position.y - 34, 22, 33);
        // The lift meets the observatory a few pixels short of the roof. Treat that lip as a step, not a wall.
        void ResolveHorizontal(Partner p, ref Rect body, Rect r, float previousX)
        {
            if (!body.Overlaps(r)) return;
            const float ledge = 14f;
            if (p.position.y >= r.y && p.position.y <= r.y + ledge)
            {
                if (p.position.x + 10f <= r.x && p.position.x + 16f > r.x) p.position.x = r.x - 9.5f;
                else if (p.position.x - 10f >= r.xMax && p.position.x - 16f < r.xMax) p.position.x = r.xMax + 9.5f;
                if (p.position.x + 10f > r.x && p.position.x - 10f < r.xMax)
                {
                    p.position.y = r.y; if (p.velocity.y > 0) p.velocity.y = 0; body = Body(p); return;
                }
            }
            bool fromLeft = previousX + 11 <= r.x + .05f, fromRight = previousX - 11 >= r.xMax - .05f;
            if (fromLeft) { p.position.x = r.x - 11; p.velocity.x = Mathf.Min(p.velocity.x, 0); }
            else if (fromRight) { p.position.x = r.xMax + 11; p.velocity.x = Mathf.Max(p.velocity.x, 0); }
            else return;
            body = Body(p);
        }
        void ResolveVertical(Partner p, float oldY, Rect r)
        {
            if (p.position.x + 10 <= r.x || p.position.x - 10 >= r.xMax) return;
            if (p.velocity.y >= 0 && oldY <= r.y + 2 && p.position.y >= r.y) { p.position.y = r.y; p.velocity.y = 0; p.grounded = true; }
            else if (p.velocity.y < 0 && oldY - 34 >= r.yMax && p.position.y - 34 < r.yMax) { p.position.y = r.yMax + 34; p.velocity.y = 0; }
        }
        void Respawn(int i)
        {
            var p = players[i];
            p.position = p.spawn; p.velocity = Vector2.zero; p.buffer = 0; p.grace = 0; p.grounded = true; p.wasGrounded = true; p.squash = 0;
            retries++; flash = .2f;
            toast = "Take your time. You've got each other."; toastTime = 2.5f; PlaySound(resetSound);
        }
        void OnGUI()
        {
            if (!backdrop) return;
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 800f);
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1280 * scale) / 2, (Screen.height - 800 * scale) / 2, 0), Quaternion.identity, new Vector3(scale, scale, 1));
            GUI.color = Color.white; GUI.DrawTexture(new Rect(0, 0, 1280, 800), backdrop);
            textStyle = textStyle ?? new GUIStyle(GUI.skin.label) { font = font, padding = new RectOffset(0, 0, 0, 0) };
            if (mode == Mode.Title) { DrawTitle(); return; }
            DrawHeader(); DrawWorld(); DrawFooter();
            if (toastTime > 0 && mode == Mode.Playing) { Box(393, 220, 494, 34, new Color(.04f, .1f, .12f, .9f)); Label(toast, 398, 228, 484, 15, cream, TextAnchor.MiddleCenter); }
            if (mode == Mode.Paused) DrawPause();
            if (mode == Mode.Map) DrawMap();
            if (mode == Mode.Complete) DrawComplete();
        }
        void DrawHeader()
        {
            Box(0, 0, 1280, 99, Hex("111f28")); Box(0, 98, 1280, 2, Hex("31463f"));
            SunIcon(47, 40, gold); MoonIcon(72, 40, blue);
            Label("SUN & MOON", 104, 21, 310, 29, cream, TextAnchor.UpperLeft, true);
            Label("A LITTLE ADVENTURE, TOGETHER", 106, 59, 340, 10, muted);
            Label(regions[levelIndex], 480, 23, 320, 11, muted, TextAnchor.MiddleCenter);
            Label("CHAPTER  " + (levelIndex + 1).ToString("00") + "  /  10", 480, 45, 320, 19, cream, TextAnchor.MiddleCenter);
            if (Button("CHAPTERS", new Rect(997, 29, 132, 40), false)) mode = Mode.Map;
            if (Button(mode == Mode.Paused ? "RESUME" : "PAUSE  II", new Rect(1141, 29, 104, 40), false)) mode = mode == Mode.Paused ? Mode.Playing : Mode.Paused;
            Label((levelIndex + 1).ToString("00"), 45, 125, 60, 37, gold, TextAnchor.UpperLeft, true);
            Label(names[levelIndex], 112, 124, 720, 28, cream, TextAnchor.UpperLeft, true);
            Label(tips[levelIndex], 114, 162, 930, 14, muted);
            GUI.color = gold; GUI.DrawTexture(new Rect(1085, 132, 18, 18), heartSprite); GUI.color = Color.white;
            Label(HeartCount + " / 4", 1114, 129, 87, 20, cream);
            Label("KEEPSAKES", 1083, 162, 120, 10, muted);
        }
        void DrawWorld()
        {
            foreach (var r in platforms) DrawPlatform(r);
            DrawPuzzles();
            foreach (var pool in pools) DrawPool(pool);
            bool ready = AllSecured() && HeartCount >= RequiredHearts;
            Arch(exitSun.x, exitSun.y, exitSun.width, exitSun.height, gold, ready); Arch(exitMoon.x, exitMoon.y, exitMoon.width, exitMoon.height, blue, ready);
            SunIcon(exitSun.center.x, exitSun.y + 20, gold); MoonIcon(exitMoon.center.x, exitMoon.y + 20, blue);
            if(RequiredHearts>0) Label("KEEPSAKES REQUIRED: " + HeartCount + " / " + RequiredHearts, 470, 204, 340, 11, gold, TextAnchor.MiddleCenter);
            foreach (var h in hearts) if (!h.taken) { float bob = Mathf.Sin(Time.unscaledTime * 2 + h.p.x) * 3; GUI.color = h.owner == 0 ? gold : blue; GUI.DrawTexture(new Rect(h.p.x - 10, h.p.y - 10 + bob, 20, 18), heartSprite); GUI.color = Color.white; Box(h.p.x - 4, h.p.y + 19, 8, 2, Hex("41564d")); }
            for (int i = 0; i < 2; i++)
            {
                var p = players[i]; Box(p.position.x - 16, p.position.y - 2, 32, 4, new Color(0, 0, 0, .2f));
                float bob = p.grounded && Mathf.Abs(p.velocity.x) > 10 ? Mathf.Sin(Time.time * 20) * 2 : 0;
                bool rising = !p.grounded && p.velocity.y < -40;
                float fat = rising ? .9f : 1f + p.squash * .22f;
                float tall = rising ? 1.1f : 1f - p.squash * .18f;
                float drawW = 40 * fat, drawH = 44 * tall;
                float drawX = Mathf.Round(p.position.x - drawW * .5f);
                float drawY = Mathf.Round(p.position.y - drawH + bob);
                var sprite = i == 0 ? sunSprite : moonSprite;
                if (p.face < 0) GUI.DrawTexture(new Rect(drawX + drawW, drawY, -drawW, drawH), sprite);
                else GUI.DrawTexture(new Rect(drawX, drawY, drawW, drawH), sprite);
                if (p.squash > .2f) Box(p.position.x - 10 - p.squash * 6, p.position.y - 1, 20 + p.squash * 12, 2, new Color(1, 1, 1, p.squash * .35f));
                Label(i == 0 ? "SUN" : "MOON", p.position.x - 37, p.position.y - 65 - (rising ? 4 : 0), 74, 10, i == 0 ? gold : blue, TextAnchor.MiddleCenter, true);
                Box(p.position.x - 2, p.position.y - 49 - (rising ? 4 : 0), 4, 3, i == 0 ? gold : blue);
            }
            if (flash > 0) Box(72, 205, 1136, 550, new Color(1, .8f, .6f, flash * .25f));
        }
        void DrawFooter()
        {
            Box(0, 766, 1280, 34, Hex("111f28")); Box(0, 766, 1280, 1, Hex("35483f"));
            Label("SUN", 39, 777, 39, 10, gold, TextAnchor.UpperLeft, true); Label("A  D   move     hold W jump", 85, 775, 250, 12, cream);
            Label("MOON", 356, 777, 51, 10, blue, TextAnchor.UpperLeft, true); Label("<  >   move     hold UP jump", 419, 775, 270, 12, cream);
            Label("R  restart     P / ESC  pause", 778, 775, 230, 12, muted); Label("NO RUSH. JUST US.", 1050, 777, 192, 10, gold, TextAnchor.MiddleRight);
        }
        void DrawPause()
        {
            Scrim(); Panel(400, 215, 480, 388); Label("TAKE A BREATHER", 425, 249, 430, 11, gold, TextAnchor.MiddleCenter);
            Label("We'll wait for you.", 425, 305, 430, 32, cream, TextAnchor.MiddleCenter, true);
            Label(Mathf.FloorToInt(elapsed / 60) + ":" + ((int)elapsed % 60).ToString("00") + " together     ·     " + retries + " retries", 425, 356, 430, 16, muted, TextAnchor.MiddleCenter);
            if (Button("BACK TOGETHER  >", new Rect(463, 403, 354, 48), true)) mode = Mode.Playing;
            if (Button("RESTART CHAPTER", new Rect(463, 466, 354, 42), false)) LoadLevel(levelIndex);
            if (Button("MAIN MENU", new Rect(463, 526, 354, 42), false)) ReturnToTitle();
        }
        void DrawMap()
        {
            Scrim(); Panel(190, 212, 900, 451); Label("OUR LITTLE JOURNEY", 225, 242, 830, 12, gold, TextAnchor.MiddleCenter);
            Label("Ten places to find each other.", 225, 276, 830, 30, cream, TextAnchor.MiddleCenter, true);
            for (int i = 0; i < 10; i++)
            {
                float x = 226 + i % 5 * 168, y = 344 + i / 5 * 115; bool unlocked = i < Unlocked;
                if (Button(unlocked ? (i + 1).ToString("00") : "LOCKED", new Rect(x, y, 156, 71), i == levelIndex) && unlocked) LoadLevel(i);
                Label(i < Unlocked ? PlayerPrefs.GetInt("SunMoon.Hearts." + i) + " / 4 keepsakes" : "Keep growing", x, y + 78, 156, 11, muted, TextAnchor.MiddleCenter);
            }
            if (Button("BACK", new Rect(536, 605, 208, 34), false)) mode = HasStarted && levelIndex < Unlocked ? Mode.Playing : Mode.Title;
        }
        void DrawComplete()
        {
            Scrim(); Panel(363, 213, 554, 422);
            GUI.color = gold; GUI.DrawTexture(new Rect(614, 244, 52, 46), heartSprite); GUI.color = Color.white;
            Label(levelIndex == 9 ? "HOME, AT LAST" : "A LITTLE CLOSER", 390, 310, 500, 12, gold, TextAnchor.MiddleCenter);
            Label(levelIndex == 9 ? "Home is with you." : "Made it. Together.", 390, 347, 500, 34, cream, TextAnchor.MiddleCenter, true);
            Label(HeartCount + " / 4 keepsakes     ·     " + Mathf.FloorToInt(elapsed / 60) + ":" + ((int)elapsed % 60).ToString("00") + " together     ·     " + retries + " retries", 390, 406, 500, 16, muted, TextAnchor.MiddleCenter);
            Label("Every small act of kindness builds a bridge.", 390, 443, 500, 14, cream, TextAnchor.MiddleCenter);
            if (Button(levelIndex == 9 ? "OUR JOURNEY  >" : "NEXT CHAPTER  >", new Rect(431, 493, 418, 50), true)) { if (levelIndex < 9) LoadLevel(levelIndex + 1); else mode = Mode.Map; }
            if (Button("PLAY THIS CHAPTER AGAIN", new Rect(431, 559, 418, 38), false)) LoadLevel(levelIndex);
            Label("ESC opens chapters", 390, 605, 500, 12, muted, TextAnchor.MiddleCenter);
        }
        void Scrim() { Box(0, 100, 1280, 666, new Color(.025f, .06f, .08f, .88f)); }
        void Panel(float x, float y, float w, float h) { Box(x + 7, y + 7, w, h, Hex("0c171e")); Box(x, y, w, h, Hex("52614c")); Box(x + 2, y + 2, w - 4, h - 4, Hex("182b30")); Box(x + 18, y + 18, 5, 5, gold); Box(x + w - 23, y + 18, 5, 5, gold); }
        bool Button(string s, Rect r, bool primary)
        {
            bool hover = r.Contains(Event.current.mousePosition); Box(r.x, r.y + 3, r.width, r.height, Hex("0b171d"));
            Box(r.x, r.y, r.width, r.height, primary ? (hover ? Hex("f3ce83") : gold) : (hover ? Hex("374e4c") : Hex("23393b")));
            if (!primary) { Box(r.x, r.y, r.width, 1, Hex("52665a")); }
            Label(s, r.x, r.y + (r.height - 13) / 2 - 1, r.width, 13, primary ? ink : cream, TextAnchor.MiddleCenter, true);
            return GUI.Button(r, GUIContent.none, GUIStyle.none);
        }
        void Label(string s, float x, float y, float w, int size, Color color, TextAnchor anchor = TextAnchor.UpperLeft, bool bold = false)
        { textStyle.fontSize = size; textStyle.normal.textColor = color; textStyle.alignment = anchor; textStyle.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal; GUI.Label(new Rect(x, y, w, size + 10), s, textStyle); }
        void Box(float x, float y, float w, float h, Color c) { GUI.color = c; GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture); GUI.color = Color.white; }
        void SunIcon(float x, float y, Color c) { Box(x - 6, y - 6, 12, 12, c); Box(x - 2, y - 12, 4, 4, c); Box(x - 2, y + 8, 4, 4, c); Box(x - 12, y - 2, 4, 4, c); Box(x + 8, y - 2, 4, 4, c); }
        void MoonIcon(float x, float y, Color c) { Box(x - 6, y - 8, 10, 16, c); Box(x - 2, y - 10, 9, 20, c); Box(x + 2, y - 10, 7, 14, Hex("182b30")); }
        void Arch(float x, float y, float w, float h, Color tint, bool lit)
        {
            Box(x, y + 12, w, h - 12, Hex("0c2028")); Box(x + 8, y + 4, w - 16, h - 4, Hex("0c2028"));
            Color edge = Color.Lerp(tint, Hex("304c49"), lit ? .1f : .45f);
            Box(x - 5, y + 16, 6, h - 16, edge); Box(x + w - 1, y + 16, 6, h - 16, edge);
            Box(x + 2, y + 7, 8, 10, edge); Box(x + w - 10, y + 7, 8, 10, edge); Box(x + 10, y + 2, w - 20, 6, edge);
            if (lit) { Box(x + 8, y + h - 5, w - 16, 5, tint); for (int i = 0; i < 4; i++) Box(x + 8 + i * 7, y + 38 + Mathf.Sin(Time.time * 2 + i) * 10, 2, 2, tint); }
        }
        void DrawPlatform(Rect r)
        {
            Box(r.x, r.y + 5, r.width, r.height - 5, Hex("293f3c"));
            for (float x = r.x; x < r.xMax; x += 24) { float w = Mathf.Min(23, r.xMax - x); Box(x, r.y + 8, w, 12, Hex("3d5148")); Box(x + 2, r.y + 10, Mathf.Max(0, w - 4), 2, Hex("4b5e50")); if (r.height > 30) Box(x + 7, r.y + 26, Mathf.Min(18, r.xMax - x - 7), 12, Hex("33473f")); }
            Box(r.x, r.y, r.width, 5, Hex("88a15f")); Box(r.x, r.y + 5, r.width, 3, Hex("526e45"));
            for (float x = r.x + 5; x < r.xMax - 8; x += 29) { Box(x, r.y - 3, 3, 5, Hex("94ab6a")); Box(x + 5, r.y + 5, 4, 7, Hex("526e45")); }
        }
        void DrawPool(Pool p)
        {
            Color c = p.owner == 0 ? gold : p.owner == 1 ? blue : Hex("b18db8");
            Box(p.r.x, p.r.y + 5, p.r.width, 18, Color.Lerp(c, ink, .5f)); Box(p.r.x, p.r.y + 4, p.r.width, 3, c);
            for (int i = 0; i < p.r.width / 12; i++) { float yy = p.r.y + 8 + Mathf.Sin(Time.time * 2 + i) * 3; Box(p.r.x + i * 12, yy, 7, 2, c); }
            if (p.owner == 2) for (int i = 0; i < 5; i++) { Box(p.r.x + i * 8 + 2, p.r.y - 3, 5, 9, c); Box(p.r.x + i * 8 + 3, p.r.y - 7, 3, 4, c); }
            Label(p.owner == 0 ? "SUN SAFE" : p.owner == 1 ? "MOON SAFE" : "UNSAFE", p.r.x - 10, p.r.y + 26, p.r.width + 20, 9, c, TextAnchor.MiddleCenter);
        }
        Texture2D MakePartner(bool moon)
        {
            var t = new Texture2D(20, 22); t.filterMode = FilterMode.Point; var clear = new Color[440]; t.SetPixels(clear);
            Action<int,int,int,int,Color> r = (x,y,w,h,c) => { for(int xx=x;xx<x+w;xx++) for(int yy=y;yy<y+h;yy++) t.SetPixel(xx,21-yy,c); };
            Color dark = Hex("1a3037"), main = moon ? blue : gold, shade = moon ? Hex("4b839e") : Hex("be783f");
            r(5,4,10,11,dark); r(4,6,12,8,dark); r(6,3,8,2,dark); r(6,5,8,9,main); r(5,7,10,6,main);
            r(7,8,6,5,Hex("f3dcb4")); r(7,9,2,2,dark); r(12,9,1,2,dark); r(10,12,2,1,shade);
            r(5,14,10,5,shade); r(7,14,6,4,main); r(3,15,3,4,main); r(14,15,3,4,main);
            r(6,19,3,3,dark); r(12,19,3,3,dark); r(4,21,5,1,shade); r(12,21,5,1,shade);
            if(moon) { r(12,1,3,5,main); r(14,3,2,3,main); r(11,4,2,2,main); }
            else { r(9,0,2,3,main); r(3,3,3,2,main); r(14,3,3,2,main); }
            t.Apply(); return t;
        }
        Texture2D MakeHeart()
        {
            var t = new Texture2D(9,8); t.filterMode = FilterMode.Point; t.SetPixels(new Color[72]);
            string[] rows = {"01100110", "11111111", "11111111", "11111111", "01111110", "00111100", "00011000", "00000000"};
            for(int y=0;y<8;y++) for(int x=0;x<8;x++) if(rows[y][x]=='1') t.SetPixel(x,7-y,Color.white); t.Apply(); return t;
        }
        Texture2D MakeBackdrop()
        {
            var t = new Texture2D(640,400); t.filterMode = FilterMode.Point; t.wrapMode = TextureWrapMode.Clamp;
            void R(int x,int y,int w,int h,Color c) { for(int yy=Mathf.Max(0,y); yy<Mathf.Min(400,y+h); yy++) for(int xx=Mathf.Max(0,x);xx<Mathf.Min(640,x+w);xx++) t.SetPixel(xx,399-yy,c); }
            for(int y=0;y<400;y++) R(0,y,640,1,Color.Lerp(Hex(skyTop[levelIndex]),Hex(skyBottom[levelIndex]),y/400f));
            var rng = new System.Random(74 + levelIndex * 191);
            for(int i=0;i<100;i++) { int x=rng.Next(30,610),y=rng.Next(100,295); R(x,y,1,1,Hex(i%4==0?"b7ad77":"516d66")); }
            // Layered silhouettes, broken garden walls, distant windows.
            for(int i=0;i<20;i++) { int x=i*36-10,h=rng.Next(35,110); R(x,320-h,31,h,Hex("263e40")); R(x+4,315-h,23,5,Hex("2b4343")); }
            for(int i=0;i<12;i++) { int x=i*61-10,h=rng.Next(20,70); R(x,341-h,45,h,Hex("2b4541")); }
            for(int side=0;side<2;side++) {
                int x=side==0?47:579;
                R(x,118,14,235,Hex("384e45")); R(x+3,118,4,235,Hex("42594b")); R(x-5,119,24,9,Hex("4a5e4d")); R(x-4,340,22,10,Hex("4d604d"));
                for(int y=140;y<334;y+=20) R(x+2,y,12,1,Hex("243d39"));
            }
            // Crescent moon and softly lit stone window.
            R(460,115,18,18,Hex("a7b99b")); R(464,112,12,24,Hex("a7b99b")); R(470,111,12,18,Hex("1d343b"));
            for(int i=0;i<30;i++) {
                int x=rng.Next(28,612),len=rng.Next(5,28); R(x,100,1,len,Hex("425d44"));
                for(int y=105;y<100+len;y+=5) { R(x-3,y,3,2,Hex("536d4c")); R(x+1,y+2,3,2,Hex("3e5a43")); }
            }
            // Ferns, bushes, flowers, and foreground rubble.
            for(int i=0;i<54;i++) {
                int x=rng.Next(37,604),y=rng.Next(357,382),h=rng.Next(4,17);
                R(x,y-h,2,h,Hex("536c4a")); R(x-5,y-h+4,5,2,Hex("658050")); R(x+2,y-h+7,5,2,Hex("425e46"));
                if(i%5==0) { R(x-1,y-h-2,4,3,Hex(i%2==0?"ba9d66":"8ba59a")); R(x,y-h-1,1,1,Hex("ead395")); }
            }
            for(int i=0;i<12;i++) { int x=rng.Next(45,600); R(x,381,10,3,Hex("52634e")); R(x+2,378,6,3,Hex("617156")); }
            t.Apply(); return t;
        }
    }
}
