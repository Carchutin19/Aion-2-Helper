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
    static bool Count(byte[] b,ref int p,int end,int maximum,out int count){count=0;uint n;if(!DpsSignal.Var(b,ref p,end,out n)||n>maximum)return false;count=(int)n;return true;}
    static bool Description(byte[] b,ref int p,int end){if(p>=end)return false;int flags=b[p++],n;if(flags>31)return false;if((flags&1)!=0&&(!Count(b,ref p,end,160,out n)||!Skip(ref p,end,n)))return false;if(!Skip(ref p,end,4))return false;return ((flags&2)==0||Skip(ref p,end,1))&&((flags&4)==0||Skip(ref p,end,1))&&((flags&8)==0||Skip(ref p,end,5))&&((flags&16)==0||Skip(ref p,end,1));}
    // Optional metadata is read only in a fully bounded supported spawn tail.
    // A status-bearing or unknown tail leaves level unknown, without affecting HP.
    internal static int? BaseLevel(byte[] b,int p,int end,int outer,int state){for(int i=0;i<StateBits.Length;i++)if((state&StateBits[i])!=0){if(StateSizes[i]==0){uint n;if(!DpsSignal.Var(b,ref p,end,out n))return null;}else if(!Skip(ref p,end,StateSizes[i]))return null;}int count;if(!Count(b,ref p,end,23,out count)||count!=0)return null;for(int bit=1;bit<=8;bit*=2)if((outer&bit)!=0&&!Skip(ref p,end,1))return null;if((outer&16)!=0&&!Skip(ref p,end,4))return null;if((outer&32)!=0&&!Description(b,ref p,end))return null;if((outer&64)!=0)return null;if(!Count(b,ref p,end,128,out count)||!Skip(ref p,end,count*6)||p>end-4)return null;int level=BitConverter.ToInt32(b,p);p+=4;if(level<1||level>200)return null;if((outer&128)!=0&&!Skip(ref p,end,8))return null;if((outer&256)!=0&&!Skip(ref p,end,4))return null;if((outer&512)!=0){if(p>end-2)return null;int flags=b[p++];p++;if(flags>63)return null;for(int i=0;i<GroggyBits.Length;i++)if((flags&GroggyBits[i])!=0&&!Skip(ref p,end,GroggySizes[i]))return null;if(!Skip(ref p,end,1)||((flags&32)!=0&&!Skip(ref p,end,8))||!Skip(ref p,end,1))return null;}if((outer&1024)!=0&&!Skip(ref p,end,9))return null;if((outer&2048)!=0&&!Skip(ref p,end,8))return null;if((outer&4096)!=0&&!Skip(ref p,end,4))return null;if((outer&8192)!=0&&!Skip(ref p,end,4))return null;if(!Count(b,ref p,end,128,out count)||!Skip(ref p,end,count*4))return null;if((outer&16384)!=0&&!Skip(ref p,end,8))return null;if((outer&32768)!=0&&!Skip(ref p,end,4))return null;return p==end?(int?)level:null;}
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
