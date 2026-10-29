using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Web.Script.Serialization;

internal static class GameSocketVerification {
 const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
 [StructLayout(LayoutKind.Sequential)]struct Filter {internal uint Length;internal IntPtr Instructions;}
 [StructLayout(LayoutKind.Sequential)]struct Header {internal uint Seconds,Microseconds,Caplen,Length;}
 [DllImport("wpcap.dll",CallingConvention=CallingConvention.Cdecl)]static extern IntPtr pcap_open_dead(int link,int snaplen);
 [DllImport("wpcap.dll",CallingConvention=CallingConvention.Cdecl)]static extern int pcap_compile(IntPtr handle,out Filter filter,string expression,int optimize,uint mask);
 [DllImport("wpcap.dll",CallingConvention=CallingConvention.Cdecl)]static extern uint pcap_offline_filter(ref Filter filter,ref Header header,byte[] packet);
 [DllImport("wpcap.dll",CallingConvention=CallingConvention.Cdecl)]static extern void pcap_freecode(ref Filter filter);
 [DllImport("wpcap.dll",CallingConvention=CallingConvention.Cdecl)]static extern void pcap_close(IntPtr handle);
 static void Check(bool value,string message){if(!value)throw new Exception(message);}
 static void Port(byte[] p,int at,int port){p[at]=(byte)(port>>8);p[at+1]=(byte)port;}
 static byte[] Packet(string src,string dst,int port,int local){byte[] a=IPAddress.Parse(src).GetAddressBytes(),b=IPAddress.Parse(dst).GetAddressBytes();bool v6=a.Length==16;int tcp=v6?54:34;var p=new byte[tcp+24];Port(p,12,v6?0x86dd:0x800);p[14]=(byte)(v6?0x60:0x45);if(v6){Port(p,18,24);p[20]=6;p[21]=64;}else{Port(p,16,44);p[22]=64;p[23]=6;}Buffer.BlockCopy(a,0,p,v6?22:26,a.Length);Buffer.BlockCopy(b,0,p,v6?38:30,b.Length);Port(p,tcp,port);Port(p,tcp+2,local);p[tcp+12]=0x50;return p;}
 static bool Matches(IntPtr dead,string expression,byte[] packet){Filter filter;Check(pcap_compile(dead,out filter,expression,1,uint.MaxValue)==0,"Scoped BPF must compile");try{var header=new Header{Seconds=1000,Caplen=(uint)packet.Length,Length=(uint)packet.Length};return pcap_offline_filter(ref filter,ref header,packet)>0;}finally{pcap_freecode(ref filter);}}
 static void Var(List<byte> b,uint value){do{byte x=(byte)(value&127);value>>=7;if(value>0)x|=128;b.Add(x);}while(value>0);}
 static byte[] Energy(uint actor,uint value){var b=new List<byte>(new byte[]{0,0x8d});Var(b,actor);b.AddRange(new byte[]{1,1,3});b.AddRange(BitConverter.GetBytes(value));return b.ToArray();}
 static byte[] Frames(params byte[][] bodies){var b=new List<byte>();foreach(var body in bodies){Var(b,(uint)body.Length+4);b.AddRange(body);}return b.ToArray();}
 static Segment Segment(int port,byte[] data,uint seq=100,string src="192.0.2.1",string dst="192.0.2.2",int local=50000){return new Segment{Src=src,Dst=dst,SrcPort=port,DstPort=local,Seq=seq,Data=data};}
 internal static void Run(string root){NpcapSupport.Current.EnsureAvailable();var filter=new GameSocketFilter();var flows=new Dictionary<string,string>{{"192.0.2.1:62819>192.0.2.2:50000","in"},{"192.0.2.2:50000>192.0.2.1:62819","out"},{"2001:db8::1:55949>2001:db8::2:50001","in"}};
 IntPtr dead=pcap_open_dead(1,65535);Check(dead!=IntPtr.Zero,"Offline BPF handle");try{
 filter.Update(flows,false);Check(Matches(dead,filter.Expression,Packet("192.0.2.1","192.0.2.2",62819,50000)),"Owned alternate incoming passes");Check(!Matches(dead,filter.Expression,Packet("192.0.2.2","192.0.2.1",50000,62819)),"Outgoing disabled by default");Check(!Matches(dead,filter.Expression,Packet("192.0.2.3","192.0.2.2",62819,50000)),"Same port on unrelated endpoint excluded");Check(Matches(dead,filter.Expression,Packet("2001:db8::1","2001:db8::2",55949,50001)),"IPv6 exact incoming passes");
 filter.Update(flows,true);Check(Matches(dead,filter.Expression,Packet("192.0.2.2","192.0.2.1",50000,62819)),"Explicit optional exact-distance capture includes owned outgoing socket");filter.Update(new Dictionary<string,string>(),true);Check(Matches(dead,filter.Expression,Packet("192.0.2.2","192.0.2.1",50000,13328)),"Standard outgoing handshake available before socket refresh");Check(!Matches(dead,filter.Expression,Packet("192.0.2.1","192.0.2.2",62819,50000)),"No all-TCP alternate fallback");
 }finally{pcap_close(dead);}
 using(var recorder=new Recorder(root,false)){typeof(Recorder).GetField("flows",Flags).SetValue(recorder,flows);typeof(Recorder).GetField("active",Flags).SetValue(recorder,true);var method=typeof(Recorder).GetMethod("Tracked",Flags);Check((bool)method.Invoke(recorder,new object[]{Segment(62819,new byte[]{1})}),"Incoming alternate requires Windows ownership");var outgoing=Segment(50000,new byte[]{1},100,"192.0.2.2","192.0.2.1",62819);Check(!(bool)method.Invoke(recorder,new object[]{outgoing}),"Outgoing software gate disabled by default");recorder.SetClientTraffic(true);Check((bool)method.Invoke(recorder,new object[]{outgoing}),"Outgoing optional software gate preserves ownership");Check(!(bool)method.Invoke(recorder,new object[]{Segment(62819,new byte[]{1},100,"192.0.2.3")}),"Unowned alternate rejected");}
 var statics=BindingFlags.Static|BindingFlags.NonPublic;byte[] fixtureKey=(byte[])typeof(ClientPositionVerification).GetField("Key",statics).GetValue(null);int lookups=0;
 using(var reader=new ClientPositionReader(delegate(int pid){lookups++;return (byte[])fixtureKey.Clone();},false))using(var cipher=new PositionRc4(fixtureKey)){
 reader.Configure(true);var move=(byte[])typeof(ClientPositionVerification).GetMethod("Move",statics).Invoke(null,new object[]{100f,200f,300f,true});var payload=(byte[])typeof(ClientPositionVerification).GetMethod("Session",statics).Invoke(null,new object[]{cipher,new byte[][]{move}});
 var outgoing=new Segment{Src="127.0.0.1",Dst="127.0.0.2",SrcPort=50000,DstPort=54304,Seq=1000,Data=payload};reader.Consume(outgoing,10,42);reader.Maintain();Check(lookups==0,"Alternate positions require explicit owned-socket dispatch");reader.Consume(outgoing,10,42,true);reader.Maintain();var reading=reader.Read("127.0.0.2:54304>127.0.0.1:50000",10);Check(lookups==1&&reading!=null&&reading.Position.X==100,"Owned alternate position handshake and injected key fixture reach existing exact-distance reader");}
 File.WriteAllText(Path.Combine(root,"game-socket-test.txt"),"PASS: actual offline IPv4/IPv6 BPF, alternate owned sockets, wrong endpoints rejected, optional owned outgoing and original startup handshake, no broad alternate TCP fallback, incoming/outgoing software ownership checks. No live capture or packet sends.");
 }
}
