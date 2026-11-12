using System;
using System.Collections.Generic;

// Independent bounded snapshot reader validated against local captures.
internal static class EffectSnapshotReader {
    internal sealed class Item {public uint instance,effectId,sourceActor,skillId;public int skillLevel;public long durationMs,serverExpiryMs;}
    sealed class Candidate {internal int End;internal List<Item> Items=new List<Item>();}
    static bool Entry(byte[] b,int p,int end,out Item item,out int next){
        item=null;next=p;if(p<0||end-p<24)return false;int flags=b[p++];if(flags!=16&&flags!=17&&flags!=19)return false;
        uint instance;if(!DpsSignal.Var(b,ref p,end,out instance)||instance==0||end-p<20)return false;
        uint id=BitConverter.ToUInt32(b,p);p+=4;long duration=BitConverter.ToInt64(b,p);p+=8;long expiry=BitConverter.ToInt64(b,p);p+=8;
        bool forever=duration==-1&&expiry==4102412400000L;
        if(id==0||id>=1000000000||(!forever&&(duration<0||duration>34560000000L||expiry<1704067200000L||expiry>=4102412400000L||expiry-duration<1704067200000L)))return false;
        uint source=0,skill=0;if((flags&1)!=0&&!DpsSignal.Var(b,ref p,end,out source)||p>=end)return false;int level=b[p++];
        if((flags&2)!=0){if(end-p<4)return false;skill=BitConverter.ToUInt32(b,p);p+=4;}
        item=new Item{instance=instance,effectId=id,sourceActor=source,skillId=skill,skillLevel=level,durationMs=duration,serverExpiryMs=expiry};next=p;return true;
    }
    static bool Position(byte[] b,int at,int end){if(end-at<12)return false;for(int i=0;i<3;i++){float v=BitConverter.ToSingle(b,at+i*4);if(float.IsNaN(v)||float.IsInfinity(v)||Math.Abs(v)>100000000)return false;}return true;}
    internal static bool TryRead(byte[] b,int start,int end,int opcode,out Item[] result,out int matches){
        result=null;matches=0;if(b==null||start<0||end>b.Length||end-start>65536)return false;
        if(opcode==0x4136){
            List<PlayerShieldSignal.Status> statuses;
            if(EnemyDetailReader.InitialEffects(b,start,end,out statuses)){
                var exact=new List<Item>();foreach(var value in statuses)if(!value.Removing)
                    exact.Add(new Item{instance=value.Instance,effectId=value.Effect,sourceActor=value.Source,skillId=value.Skill,durationMs=value.Duration,serverExpiryMs=value.Expiry});
                result=exact.ToArray();matches=1;return true;
            }
        }
        var candidates=new List<Item[]>();int scanEnd=Math.Min(end-24,start+8192);
        for(int p=start+3;p<scanEnd;p++){
            int count=b[p];if(count==0||count>128||end-p<count*35+2)continue;
            Item first;int ignored;if(!Entry(b,p+1,end,out first,out ignored))continue;
            var paths=new List<Candidate>{new Candidate{End=p+1}};
            for(int index=0;index<count&&paths.Count>0;index++){
                var nextPaths=new List<Candidate>();
                foreach(var path in paths){Item value;int tail;if(!Entry(b,path.End,end,out value,out tail))continue;
                    for(int extra=0;extra<=1;extra++){
                        int pos=tail+extra,after=pos+12;if(!Position(b,pos,end))continue;
                        if(index+1==count){if(after>=end||b[after]!=(opcode==0x4136?7:15)||(opcode==0x4136&&(after+1>=end||b[after+1]!=2)))continue;}
                        else{Item peek;int n;if(!Entry(b,after,end,out peek,out n))continue;}
                        var copy=new List<Item>(path.Items);copy.Add(value);nextPaths.Add(new Candidate{End=after,Items=copy});
                    }
                }
                if(nextPaths.Count>4){paths.Clear();break;}paths=nextPaths;
            }
            foreach(var path in paths)if(path.Items.Count==count){candidates.Add(path.Items.ToArray());if(candidates.Count>1){matches=candidates.Count;return false;}}
        }
        matches=candidates.Count;if(matches!=1)return false;result=candidates[0];return true;
    }
    internal static void Verify(){
        var bytes=new List<byte>{0x33,0x36,12,0,1,19,7};bytes.AddRange(BitConverter.GetBytes(151100301u));bytes.AddRange(BitConverter.GetBytes(20000L));bytes.AddRange(BitConverter.GetBytes(1794400000000L));bytes.Add(12);bytes.Add(11);bytes.AddRange(BitConverter.GetBytes(15110037u));bytes.Add(0);
        for(int i=0;i<3;i++)bytes.AddRange(BitConverter.GetBytes(123.5f));bytes.Add(15);bytes.Add(0);
        Item[] result;int n;var b=bytes.ToArray();if(!TryRead(b,0,b.Length,0x3336,out result,out n)||result.Length!=1||result[0].sourceActor!=12||result[0].skillLevel!=11)throw new Exception("Snapshot fields or exact boundary rejected");
        for(int i=0;i<b.Length-2;i++)if(TryRead(b,0,i,0x3336,out result,out n))throw new Exception("Truncated snapshot accepted");
        b[b.Length-2]=14;if(TryRead(b,0,b.Length,0x3336,out result,out n))throw new Exception("Invalid snapshot boundary accepted");
    }
}
