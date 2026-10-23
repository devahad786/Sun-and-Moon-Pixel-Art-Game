using UnityEngine;

namespace SunAndMoon
{
    public sealed partial class SunMoonGame
    {
        void DrawTitle()
        {
            Box(0,0,1280,800,new Color(.035f,.075f,.1f,.45f));
            Label("A COOPERATIVE PIXEL ADVENTURE",340,112,600,13,muted,TextAnchor.MiddleCenter);
            SunIcon(578,205,gold); MoonIcon(699,205,blue);
            Label("SUN & MOON",240,250,800,62,cream,TextAnchor.MiddleCenter,true);
            Label("Different paths. One little world. Together.",290,337,700,21,cream,TextAnchor.MiddleCenter);
            Label("10 unique chapters  /  2 partners  /  1 keyboard",300,387,680,14,muted,TextAnchor.MiddleCenter);
            GUI.DrawTexture(new Rect(527,440,80,88),sunSprite); GUI.DrawTexture(new Rect(674,440,80,88),moonSprite);
            GUI.color=gold; GUI.DrawTexture(new Rect(627,464,26,24),heartSprite); GUI.color=Color.white;
            if(Button(Unlocked>1?"PLAY  /  CONTINUE CHAPTER "+Unlocked.ToString("00"):"PLAY TOGETHER  >",new Rect(430,552,420,64),true)) StartAdventure();
            Label("SUN    A / D to move  ·  W to jump",260,657,360,15,gold,TextAnchor.MiddleCenter);
            Label("MOON    Arrow keys to move & jump",656,657,385,15,blue,TextAnchor.MiddleCenter);
            Label("Desktop browser  ·  Sound begins after Play  ·  Progress saves on this browser",240,728,800,12,muted,TextAnchor.MiddleCenter);
        }
        void DrawPuzzles()
        {
            for(int i=0;i<links.Count;i++)
            {
                var b=links[i]; Color color=b.owner==0?gold:b.owner==1?blue:Hex("c7bd8e");
                if(b.gate)
                {
                    if(!b.active)
                    {
                        Box(b.deck.x,b.deck.y,b.deck.width,b.deck.height,Hex("203239"));
                        for(float y=b.deck.y;y<b.deck.yMax;y+=18) { Box(b.deck.x+3,y,5,12,color); Box(b.deck.x+15,y+7,5,10,color); }
                    }
                    else { Box(b.deck.x,b.deck.y,24,8,color); Box(b.deck.x,b.deck.yMax-4,24,4,color); }
                }
                else if(b.active)
                {
                    DrawPlatform(b.deck);
                    for(float x=b.deck.x+8;x<b.deck.xMax;x+=18) Box(x,b.deck.y+7,10,3,color);
                }
                else
                {
                    for(float x=b.deck.x+5;x<b.deck.xMax;x+=18) Box(x,b.deck.y+2,8,2,Hex("6a807b"));
                    Label("CROSSING "+(i+1),b.deck.x,b.deck.y+28,b.deck.width,9,muted,TextAnchor.MiddleCenter);
                }
                Box(b.plate.x-25,b.plate.y-7,50,7,Color.Lerp(color,ink,.4f)); Box(b.plate.x-19,b.plate.y-(b.active?9:14),38,b.active?4:8,color);
                string pad=(b.owner==0?"SUN ":b.owner==1?"MOON ":"HOLD ")+(i+1);
                Label(pad,b.plate.x-48,b.plate.y+13,96,9,color,TextAnchor.MiddleCenter);
                if(b.keepsakes>HeartCount) Label(b.keepsakes+" HEARTS",b.plate.x-45,b.plate.y-40,90,9,gold,TextAnchor.MiddleCenter);
                float bx=b.bell.x,by=b.bell.y;
                Box(bx-4,by-43,8,42,Hex("776344")); Box(bx-15,by-51,30,5,Hex("c19f63"));
                Box(bx-9,by-45,18,17,b.secured?gold:Hex("97a38b")); Box(bx-13,by-30,26,4,b.secured?gold:Hex("97a38b"));
                Label(b.secured?"SECURED":"BELL "+(i+1),bx-45,by+16,90,9,b.secured?gold:muted,TextAnchor.MiddleCenter);
                if(b.duration>0 && b.active && !b.secured) Label(b.remaining.ToString("0.0")+"s",b.plate.x-40,b.plate.y-43,80,15,gold,TextAnchor.MiddleCenter,true);
            }
            foreach(var lift in lifts)
            {
                Box(lift.origin.center.x-2,lift.origin.y+lift.travel.y,4,-lift.travel.y,Hex("50666b"));
                DrawPlatform(lift.current); Box(lift.current.x+10,lift.current.y+10,lift.current.width-20,4,blue);
                Label("LIFT",lift.current.x,lift.current.y+29,lift.current.width,9,blue,TextAnchor.MiddleCenter);
            }
            if(checkpoint!=Vector2.zero)
            {
                Box(checkpoint.x-2,checkpoint.y-56,4,56,Hex("9e987e")); Box(checkpoint.x+2,checkpoint.y-56,26,16,checkpointActive?gold:Hex("7b9691"));
                Label(checkpointActive?"SAVED":"CHECKPOINT",checkpoint.x-55,checkpoint.y+15,110,9,checkpointActive?gold:muted,TextAnchor.MiddleCenter);
            }
        }
    }
}
