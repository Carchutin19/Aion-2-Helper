using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;

internal sealed class EnemyDetailOptions {
    public bool Level=true,BaseLevel=true,Distance=true,Species=true,ExactPosition=false;
    public EnemyNameOptions DistanceText=new EnemyNameOptions{Marker=false,Bold=false,FontSize=14,Position=2,OffsetX=-5,OffsetY=-12,Color="#E1E8EE"};
    public EnemyNameOptions SpeciesText=new EnemyNameOptions{Marker=false,Bold=false,FontSize=12,Position=4,OffsetX=-7,OffsetY=4,Color="#BAC6CC"};
    internal EnemyDetailOptions Copy(){return new EnemyDetailOptions{Level=Level,BaseLevel=BaseLevel,Distance=Distance,Species=Species,ExactPosition=ExactPosition,DistanceText=DistanceText.Copy(),SpeciesText=SpeciesText.Copy()};}
    internal void Normalize(){if(DistanceText==null)DistanceText=new EnemyDetailOptions().DistanceText;if(SpeciesText==null)SpeciesText=new EnemyDetailOptions().SpeciesText;DistanceText.Normalize();SpeciesText.Normalize();DistanceText.Marker=SpeciesText.Marker=false;}
}
internal static class EnemyDetailReader {
    static readonly int[] StateBits={64,128,256,512,1024,2048,4096,8192,16384,32768},StateSizes={1,4,1,1,0,4,4,1,4,4},GroggyBits={1,2,4,8,16},GroggySizes={4,4,4,8,8};
    internal struct Position {internal float X,Y,Z;internal double Time;internal bool Known;}
    internal static bool Coordinates(byte[] b,int p,int end,out Position value){value=new Position();if(p<0||p>end-12)return false;float x=BitConverter.ToSingle(b,p),y=BitConverter.ToSingle(b,p+4),z=BitConverter.ToSingle(b,p+8);if(!Finite(x)||!Finite(y)||!Finite(z)||(x==0&&y==0&&z==0))return false;value=new Position{X=x,Y=y,Z=z,Known=true};return true;}
    static bool Finite(float x){return !float.IsNaN(x)&&!float.IsInfinity(x)&&Math.Abs(x)<1000000;}
    static bool Skip(ref int p,int end,int count){if(count<0||p>end-count)return false;p+=count;return true;}
    // NPC tails share bit-packed booleans across statuses, the outer flags and
    // stagger data. Byte-sized skips here would shift the level or reject it.
    struct Tail {
        readonly byte[] data;readonly int end;internal int Position;int bit,bitByte;
        internal Tail(byte[] data,int position,int end){this.data=data;this.end=end;Position=position;bit=8;bitByte=0;}
        internal bool Skip(int n){if(n<0||Position>end-n)return false;Position+=n;return true;}
        internal bool Byte(out int n){n=0;if(Position>=end)return false;n=data[Position++];return true;}
        internal bool Var(out uint n){return DpsSignal.Var(data,ref Position,end,out n);}
        internal bool Bit(){if(bit==8){if(!Byte(out bitByte))return false;bit=0;}bit++;return true;}
        internal bool U32(out uint n){n=0;if(Position>end-4)return false;n=BitConverter.ToUInt32(data,Position);Position+=4;return true;}
    }
    static bool TailCount(ref Tail t,int maximum,out int count){uint n;count=0;if(!t.Var(out n)||n>maximum)return false;count=(int)n;return true;}
    static bool TailStatus(ref Tail t){int flags;uint n;if(!t.Byte(out flags)||flags>31||!t.Var(out n)||!t.Skip(20)||((flags&1)!=0&&!t.Var(out n))||!t.Skip(1)||((flags&2)!=0&&!t.Skip(4))||!t.Bit()||((flags&4)!=0&&!t.Var(out n))||((flags&8)!=0&&!t.Skip(4))||((flags&16)!=0&&!t.Skip(12)))return false;return true;}
    static bool TailDescription(ref Tail t){int flags,n;if(!t.Byte(out flags)||flags>31||((flags&1)!=0&&(!TailCount(ref t,160,out n)||!t.Skip(n)))||!t.Skip(4))return false;return ((flags&2)==0||t.Skip(1))&&((flags&4)==0||t.Skip(1))&&((flags&8)==0||t.Skip(5))&&((flags&16)==0||t.Skip(1));}
    // Only accept a level when the complete supported tail was consumed.
    // Unsupported owner blocks or flags leave metadata unknown, not HP invalid.
    internal static int? BaseLevel(byte[] b,int p,int end,int outer,int state){
        for(int i=0;i<StateBits.Length;i++)if((state&StateBits[i])!=0){if(StateSizes[i]==0){uint n;if(!DpsSignal.Var(b,ref p,end,out n))return null;}else if(!Skip(ref p,end,StateSizes[i]))return null;}
        var t=new Tail(b,p,end);int count;if(!TailCount(ref t,128,out count))return null;for(int i=0;i<count;i++)if(!TailStatus(ref t))return null;
        for(int bit=1;bit<=4;bit*=2)if((outer&bit)!=0&&!t.Skip(1))return null;
        if((outer&8)!=0&&!t.Bit()||(outer&16)!=0&&!t.Skip(4)||(outer&32)!=0&&!TailDescription(ref t)||(outer&64)!=0)return null;
        uint level;if(!TailCount(ref t,128,out count)||!t.Skip(count*6)||!t.U32(out level)||level<1||level>200)return null;
        if((outer&128)!=0&&!t.Skip(8)||(outer&256)!=0&&!t.Skip(4))return null;
        if((outer&512)!=0){int flags;if(!t.Byte(out flags)||flags>63||!t.Skip(1))return null;for(int i=0;i<GroggyBits.Length;i++)if((flags&GroggyBits[i])!=0&&!t.Skip(GroggySizes[i]))return null;if(!t.Bit()||(flags&32)!=0&&!t.Skip(8)||!t.Bit())return null;}
        if((outer&1024)!=0&&!t.Skip(9)||(outer&2048)!=0&&!t.Skip(8)||(outer&4096)!=0&&!t.Skip(4)||(outer&8192)!=0&&!t.Skip(4)||!TailCount(ref t,128,out count)||!t.Skip(count*4)||(outer&16384)!=0&&!t.Skip(8)||(outer&32768)!=0&&!t.Skip(4))return null;
        return t.Position==end?(int?)level:null;
    }
    internal static int? DisplayLevel(int selfLevel,int? raw){return !raw.HasValue||selfLevel<=0?null:selfLevel==45?(int?)Math.Max(45,raw.Value):raw;}
    internal static bool Character(byte[] b,int start,int end,out int level,out Position position){level=0;position=new Position();uint actor;string name;byte role;if(!DpsSignal.PlayerIdentity(b,start,end,true,out actor,out name,out role))return false;int p=start+2;uint unused;if(!DpsSignal.Var(b,ref p,end,out unused))return false;for(int q=p;q<Math.Min(end-12,p+16);q++){int n=b[q];if(n<2||n>24||q+n+1>end)continue;string value;try{value=new UTF8Encoding(false,true).GetString(b,q+1,n);}catch(DecoderFallbackException){continue;}if(value!=name)continue;int at=q+1+n+2;if(at>end-11)continue;uint code=BitConverter.ToUInt32(b,at),lev=BitConverter.ToUInt32(b,at+5);if(code<5||code>36||(b[at+4]!=1&&b[at+4]!=2)||lev<1||lev>200)continue;level=(int)lev;Coordinates(b,at+162,end,out position);return true;}return false;}
    internal static bool Movement(byte[] b,int start,int end,out uint actor,out Position position){actor=0;position=new Position();if(b==null||start<0||end>b.Length||end-start<2)return false;int op=(b[start]<<8)|b[start+1],offset;switch(op){case 0x1b37:case 0x1c37:offset=-1;break;case 0x1a37:case 0x1e37:case 0x2037:case 0x2837:case 0x2937:case 0x2a37:offset=2;break;case 0x1f37:case 0x2f37:offset=1;break;case 0x3c37:offset=1;break;default:return false;}int p=start+2;if(!DpsSignal.Var(b,ref p,end,out actor)||actor==0||p>=end||(op==0x3c37&&b[p]!=3))return false;if(offset<0)offset=1+(b[p]&1);return Coordinates(b,p+offset,end,out position);}
    // Cast aim coordinates can be an impact/ground point, not the caster.
    // Distance uses only an identified character profile or movement position.
    internal static double? Distance(Position self,Position target,double now,bool continuous=false){if(!self.Known||!target.Known||(!continuous&&Math.Max(0,now-self.Time)>5))return null;double x=self.X-target.X,y=self.Y-target.Y,z=self.Z-target.Z,d=Math.Sqrt(x*x+y*y+z*z)/100;return d<=2000?(double?)d:null;}
    internal static string LevelText(int? adjusted,int? raw,bool includeBase){if(!adjusted.HasValue)return includeBase&&raw.HasValue?"— ("+raw.Value+")":"";return adjusted.Value+(includeBase&&raw.HasValue&&raw!=adjusted?" ("+raw.Value+")":"");}
}
internal static class EnemySpecies {
    static readonly Dictionary<string,int> table=Load();
    static Dictionary<string,int> Load(){try{using(var stream=typeof(EnemySpecies).Assembly.GetManifestResourceStream("Aion2NpcSpecies.json")){if(stream==null)return new Dictionary<string,int>();using(var reader=new StreamReader(stream))return new JavaScriptSerializer{MaxJsonLength=4000000}.Deserialize<Dictionary<string,int>>(reader.ReadToEnd());}}catch(IOException){return new Dictionary<string,int>();}catch(ArgumentException){return new Dictionary<string,int>();}}
    static readonly string[] English={"Cogni","Fera","Natura","Varian","Special"},Spanish={"Cognis","Feras","Naturas","Varians","Especiales"};
    internal static string Name(uint npc,string language){int id;if(!table.TryGetValue(npc.ToString(),out id))return null;return id>=2&&id<=6?(language=="es"?Spanish:English)[id-2]:null;}
}
