Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
public static class ArkTrainSprites {
    static Color C(int a,int r,int g,int b) { return Color.FromArgb(a,r,g,b); }
    static void Poly(Graphics g, PointF[] p, Color fill, Color line, float width=1.4f) {
        using(var b=new SolidBrush(fill)) g.FillPolygon(b,p);
        using(var pen=new Pen(line,width)) g.DrawPolygon(pen,p);
    }
    static PointF P(float x,float y) { return new PointF(x,y); }
    static void Box(Graphics g,float x,float y,float w,float h,Color fill,Color line) {
        using(var b=new SolidBrush(fill)) g.FillRectangle(b,x,y,w,h);
        using(var p=new Pen(line,1.1f)) g.DrawRectangle(p,x,y,w,h);
    }
    static void Line(Graphics g,float x,float y,float x2,float y2,Color c,float w=1) {
        using(var p=new Pen(c,w)) g.DrawLine(p,x,y,x2,y2);
    }
    static void Oval(Graphics g,float x,float y,float w,float h,Color fill,Color edge,float stroke=1.4f) {
        using(var b=new SolidBrush(fill)) g.FillEllipse(b,x,y,w,h);
        using(var p=new Pen(edge,stroke)) g.DrawEllipse(p,x,y,w,h);
    }
    static void RoundBox(Graphics g,float x,float y,float w,float h,float radius,Color fill,Color edge) {
        using(var path=new GraphicsPath()) {
            float d=radius*2;
            path.AddArc(x,y,d,d,180,90); path.AddArc(x+w-d,y,d,d,270,90);
            path.AddArc(x+w-d,y+h-d,d,d,0,90); path.AddArc(x,y+h-d,d,d,90,90); path.CloseFigure();
            using(var b=new SolidBrush(fill)) g.FillPath(b,path);
            using(var pen=new Pen(edge,1.6f)) g.DrawPath(pen,path);
        }
    }
    static void SteamLocomotive(Graphics g) {
        Color edge=C(225,155,186,201), metal=C(195,14,25,34);
        Color dark=C(235,9,19,27), cyan=C(220,101,218,224), gold=C(210,221,177,94);
        // The logo's flared chimney and round cyan firebox, translated into a left-facing profile.
        Box(g,380,117,20,8,dark,edge);
        RoundBox(g,51,125,334,13,5,dark,edge);
        RoundBox(g,275,38,102,95,9,metal,edge);
        using(var roof=new GraphicsPath()) {
            roof.AddBezier(265,43,276,18,369,18,388,43);
            roof.AddLine(388,43,265,43);
            using(var b=new SolidBrush(C(220,25,43,54))) g.FillPath(b,roof);
            using(var p=new Pen(edge,2)) g.DrawPath(p,roof);
        }
        RoundBox(g,291,49,30,39,9,C(155,40,104,121),cyan);
        RoundBox(g,330,49,30,39,9,C(155,40,104,121),cyan);
        Line(g,298,54,298,72,C(150,167,225,232),1.6f);
        Line(g,337,54,337,72,C(150,167,225,232),1.6f);
        Line(g,294,103,354,103,gold,1.5f);
        for(int x=301;x<352;x+=9) Line(g,x,112,x,122,C(160,128,165,181),1);
        // Long cylindrical boiler, steam dome, and banding.
        RoundBox(g,68,63,215,64,30,metal,edge);
        RoundBox(g,187,44,36,33,15,metal,edge);
        RoundBox(g,233,50,24,23,9,metal,edge);
        Box(g,198,40,13,6,dark,edge);
        using(var pen=new Pen(C(165,141,180,195),1.8f)) {
            g.DrawArc(pen,114,64,25,61,270,180);
            g.DrawArc(pen,191,64,25,61,270,180);
            g.DrawArc(pen,246,65,22,59,270,180);
        }
        Line(g,105,77,270,77,C(100,155,207,217),1.5f);
        Line(g,107,111,270,111,gold,1.6f);
        // Wide old coal-burner smokestack with a rounded lip.
        Poly(g,new[]{P(94,64),P(89,40),P(81,24),P(121,24),P(113,40),P(109,64)},metal,edge,1.8f);
        Oval(g,79,18,44,12,C(225,27,47,59),edge,1.8f);
        Oval(g,88,21,26,5,C(230,5,14,21),C(115,133,177,192),1);
        RoundBox(g,90,58,24,8,3,dark,edge);
        // Round smokebox door: concentric rings, rivets, and the title logo's cyan core.
        Oval(g,44,61,58,69,C(230,17,35,47),edge,2);
        Oval(g,51,68,43,55,C(215,30,51,64),C(220,135,175,192),1.5f);
        Oval(g,57,76,31,39,dark,edge,1.3f);
        Oval(g,63,84,19,23,C(160,17,150,178),cyan,2);
        Oval(g,68,89,9,12,C(255,123,242,242),C(255,197,255,255),1);
        for(int i=0;i<8;i++) {
            double angle=i*Math.PI/4;
            float x=73+(float)Math.Cos(angle)*23,y=96+(float)Math.Sin(angle)*28;
            Oval(g,x-1.3f,y-1.3f,2.6f,2.6f,edge,edge,.6f);
        }
        Line(g,81,79,94,79,edge,2); Line(g,81,113,94,113,edge,2);
        // A second small circular headlamp sits above the round front.
        Oval(g,57,48,20,16,dark,edge);
        Oval(g,62,51,10,10,C(235,61,216,229),cyan);
        // Pilot and front running wheels.
        Poly(g,new[]{P(35,129),P(62,131),P(55,151),P(11,151)},C(215,19,36,46),edge);
        for(int x=22;x<54;x+=8) Line(g,x,148,x+9,133,edge,1.4f);
        Oval(g,61,141,25,25,dark,edge); Oval(g,68,148,11,11,metal,edge);
        Oval(g,347,140,28,28,dark,edge); Oval(g,354,147,14,14,metal,edge);
        // Three large spoked driving wheels and their characteristic coupling rod.
        foreach(float x in new float[]{142,216,290}) {
            Oval(g,x-27,118,54,54,dark,edge,2);
            Oval(g,x-21,124,42,42,C(130,23,47,60),C(190,112,157,177),1.2f);
            for(int i=0;i<10;i++) {
                double angle=i*Math.PI/5;
                Line(g,x,145,x+(float)Math.Cos(angle)*20,145+(float)Math.Sin(angle)*20,C(180,116,166,184),1.3f);
            }
            Oval(g,x-6,139,12,12,metal,cyan,1.4f);
        }
        Line(g,115,150,303,150,C(245,9,20,28),6);
        Line(g,115,150,303,150,C(210,158,189,200),2.8f);
        foreach(float x in new float[]{142,216,290}) Oval(g,x-3,147,6,6,gold,gold,.7f);
        Line(g,81,132,128,150,edge,2);
    }
    public static void Generate(string folder) {
        for(int variant=0;variant<3;variant++) {
            bool engine=variant==0, empty=variant==2;
            using(var bmp=new Bitmap(1200,540,PixelFormat.Format32bppArgb))
            using(var g=Graphics.FromImage(bmp)) {
                g.Clear(Color.Transparent); g.SmoothingMode=SmoothingMode.AntiAlias; g.ScaleTransform(3,3);
                if(engine) {
                    SteamLocomotive(g);
                    bmp.Save(System.IO.Path.Combine(folder,"ArkLocomotive.png"),ImageFormat.Png);
                    continue;
                }
                Color edge=empty?C(135,160,184,195):C(220,155,186,201);
                Color metal=empty?C(115,14,25,34):C(195,14,25,34);
                Color gold=C(230,221,177,94), cyan=C(180,122,208,216);
                // Draw running gear first: it remains visible beneath the transparent body.
                Box(g,38,134,324,13,C(220,9,17,23),C(120,118,149,164));
                foreach(float x in new float[]{65,100,300,335}) {
                    using(var b=new SolidBrush(C(235,12,21,28))) g.FillEllipse(b,x-14,140,28,28);
                    using(var p=new Pen(edge,1.6f)) g.DrawEllipse(p,x-14,140,28,28);
                    using(var p=new Pen(C(170,96,126,140),2)) g.DrawEllipse(p,x-8,146,16,16);
                    using(var b=new SolidBrush(edge)) g.FillEllipse(b,x-2,152,4,4);
                }
                Line(g,65,154,100,154,edge,3); Line(g,300,154,335,154,edge,3);
                Box(g,0,117,16,8,C(230,24,37,46),edge); Box(g,384,117,16,8,C(230,24,37,46),edge);
                PointF[] body=new[]{P(12,137),P(12,48),P(26,31),P(372,31),P(388,48),P(388,137)};
                Poly(g,body,metal,edge,1.8f);
                {
                    Line(g,27,38,371,38,C(120,172,197,207),2);
                    if(!empty) {
                        for(int x=39;x<=267;x+=57) {
                            Box(g,x,52,44,27,C(115,37,93,111),C(175,130,179,196));
                            Line(g,x+5,55,x+36,55,C(150,161,217,229),1.5f);
                        }
                        Box(g,330,48,34,79,C(45,57,78,90),C(160,117,153,171));
                        Box(g,337,54,20,25,C(115,37,93,111),C(150,130,179,196));
                        Line(g,335,96,340,96,gold,2);
                    } else {
                        using(var p=new Pen(C(140,145,170,183),1.4f)) {
                            p.DashPattern=new float[]{5,5}; g.DrawRectangle(p,30,46,340,81);
                        }
                        Line(g,192,56,208,56,edge,2); Line(g,200,48,200,64,edge,2);
                        for(int x=28;x<370;x+=18) Line(g,x,133,x+6,127,C(130,205,171,108),2);
                    }
                    Line(g,24,87,375,87,empty?C(90,153,177,184):cyan,1.5f);
                }
                Line(g,23,129,377,129,C(150,115,164,181),2);
                for(int x=29;x<381;x+=27) {
                    using(var b=new SolidBrush(C(130,174,192,197))) g.FillEllipse(b,x,133,2,2);
                }
                bmp.Save(System.IO.Path.Combine(folder,empty?"ArkExpansionCar.png":"ArkCarriage.png"),ImageFormat.Png);
            }
        }
    }
}
'@
[ArkTrainSprites]::Generate((Join-Path $PSScriptRoot '..\..\Assets\Sprites\UI\Train'))
