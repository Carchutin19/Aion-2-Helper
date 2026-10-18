using System;
using System.Collections.Generic;

internal static class ResourceIdentityVerification {
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static void Var(List<byte> bytes,uint value){do{bytes.Add((byte)((value&127)|(value>=128?128u:0u)));value>>=7;}while(value>0);}
    static byte[] Energy(uint actor,params uint[] values){var b=new List<byte>{0,0x8d};Var(b,actor);b.Add(1);Var(b,(uint)values.Length);foreach(uint v in values){b.Add(3);b.AddRange(BitConverter.GetBytes(v));}return b.ToArray();}
    internal static void Run(){
        foreach(var invalid in new[]{Energy(77,100000001),Energy(77,uint.MaxValue),Energy(77,1000,2000)}){
            var dps=new DpsSignal();dps.Configure(new DpsOptions{Enabled=true,Scope="global"});var good=Energy(42,1000);dps.Consume(good,0,good.Length,1,"A");
            dps.Consume(invalid,0,invalid.Length,2,"A");Check(dps.Read(2).SelfActor==42,"Invalid or repeated energy cannot replace DPS self identity");
            var own=new PlayerVitalSignal();own.Configure(true);var profile=EnemyTargetVerification.Player(42,"Local",900,1000,true);own.Consume(profile,0,profile.Length,1,"A");own.Consume(invalid,0,invalid.Length,2,"A");
            Check(own.Read(false,2)!=null&&own.Read(false,2).Actor==42&&own.Read(false,2).Maximum==1000,"Invalid identity cannot clear confirmed player resources");
            uint actor;uint? current,maximum;bool identity;Check(!EnemyHealthSignal.Points(invalid,0,invalid.Length,out actor,out current,out maximum,out identity),"Enemy and target readers reject malformed identity atomically");
        }
        uint identified;foreach(uint value in new[]{0u,100000000u}){var edge=Energy(42,value);Check(DashSignal.EnergyActor(edge,0,edge.Length,out identified)&&identified==42,"Actual empty and boundary energy remain valid identities");}
        var complete=Energy(42,1000);for(int n=0;n<complete.Length;n++)Check(!DashSignal.EnergyActor(complete,0,n,out identified)&&identified==0,"Truncated identity never exposes a partial actor");
        // Count is a varint in the wire envelope, independently of how many
        // currently known resource kinds the application understands.
        var many=new List<byte>{0,0x8d,42,1};Var(many,128);many.Add(3);many.AddRange(BitConverter.GetBytes(1000u));for(int i=1;i<128;i++){many.Add(250);many.AddRange(new byte[4]);}
        var reader=new DpsSignal();reader.Configure(new DpsOptions{Enabled=true});var bytes=many.ToArray();reader.Consume(bytes,0,bytes.Length,1,"large");Check(reader.Read(1).SelfActor==42,"A valid multibyte resource count must still identify the player");
    }
}
