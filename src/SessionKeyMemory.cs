// Adapted from Aion 2 Protocol Research, MIT, revision fe2122f0e54e1de854c11e91e3c7bf51916a4f52.
// Copyright (c) 2026 Aion 2 Protocol Research contributors.
// Full notice: assets/licenses/Aion2ProtocolResearch-MIT.txt (also embedded in the executable).
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

// Only QUERY_INFORMATION | VM_READ. No writes, injection, or full-memory fallback.
// Used by the exact-distance worker only, after a captured game handshake.
internal sealed class SessionKeyMemory:IDisposable {
    [StructLayout(LayoutKind.Sequential)] struct Region {internal IntPtr Base,Allocation;internal uint AllocationProtection;internal UIntPtr Size;internal uint State,Protection,Type;}
    [StructLayout(LayoutKind.Sequential)] struct Module {internal IntPtr Base;internal uint Size;internal IntPtr Entry;}
    [DllImport("kernel32.dll",SetLastError=true)] static extern IntPtr OpenProcess(uint access,bool inherit,int pid);
    [DllImport("kernel32.dll")] static extern bool GetExitCodeProcess(IntPtr process,out uint code);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr process);
    [DllImport("kernel32.dll",SetLastError=true)] static extern bool ReadProcessMemory(IntPtr process,IntPtr address,[Out]byte[] bytes,UIntPtr size,out UIntPtr read);
    [DllImport("kernel32.dll")] static extern UIntPtr VirtualQueryEx(IntPtr process,IntPtr address,out Region region,UIntPtr size);
    [DllImport("psapi.dll",SetLastError=true)] static extern bool EnumProcessModulesEx(IntPtr process,[Out]IntPtr[] modules,int size,out int needed,uint flags);
    [DllImport("psapi.dll",SetLastError=true)] static extern bool GetModuleInformation(IntPtr process,IntPtr module,out Module info,int size);
    [DllImport("psapi.dll",CharSet=CharSet.Unicode)] static extern uint GetModuleBaseNameW(IntPtr process,IntPtr module,StringBuilder name,int size);
    IntPtr handle;int processId;long vtable,owner;
    static readonly byte?[] Signature=Pattern("B9 38 01 00 00 E8 ?? ?? ?? ?? 48 8B D8 48 85 C0 0F 84 ?? ?? ?? ?? 48 89 6C 24 30 48 8D 0D ?? ?? ?? ?? 48 89 74 24 38 33 D2 BE 01 00 00 00 41 B8 00 01 00 00 89 70 08 89 70 0C 48 8D 05 ?? ?? ?? ?? 48 89 03");
    static byte?[] Pattern(string source){var parts=source.Split(' ');var result=new byte?[parts.Length];for(int i=0;i<parts.Length;i++)if(parts[i]!="??")result[i]=Convert.ToByte(parts[i],16);return result;}
    byte[] Read(long address,int size){if(address<=0||size<0||size>8*1024*1024)return null;var data=new byte[size];UIntPtr actual;return ReadProcessMemory(handle,new IntPtr(address),data,new UIntPtr((uint)size),out actual)&&actual.ToUInt64()==(ulong)size?data:null;}
    static bool Usable(Region r){return r.State==0x1000&&(r.Protection&0x101)==0;}
    IEnumerable<Region> Regions(long start,long end){long at=start;int queries=0;while(at<end&&queries++<100000){Region r;if(VirtualQueryEx(handle,new IntPtr(at),out r,new UIntPtr((uint)Marshal.SizeOf(typeof(Region))))==UIntPtr.Zero)yield break;long next=r.Base.ToInt64()+(long)r.Size.ToUInt64();if(next<=at)yield break;at=next;if(r.Base.ToInt64()<end&&next>start)yield return r;}}
    void Open(int pid,Func<bool> cancelled){Dispose();handle=OpenProcess(0x410,false,pid);if(handle==IntPtr.Zero)throw new Win32Exception(Marshal.GetLastWin32Error());processId=pid;try{
        var modules=new IntPtr[1024];int needed;if(!EnumProcessModulesEx(handle,modules,modules.Length*IntPtr.Size,out needed,3)||needed<=0||needed>modules.Length*IntPtr.Size)throw new InvalidOperationException("Cannot identify Aion module");
        var name=new StringBuilder(1024);GetModuleBaseNameW(handle,modules[0],name,name.Capacity);if(!name.ToString().Equals("AION2.exe",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Exact distance only reads AION2.exe");
        Module module;if(!GetModuleInformation(handle,modules[0],out module,Marshal.SizeOf(typeof(Module))))throw new Win32Exception(Marshal.GetLastWin32Error());
        long start=module.Base.ToInt64(),end=start+module.Size,found=0;int hits=0;long bytes=0;
        foreach(var region in Regions(start,end)){if(cancelled())throw new OperationCanceledException();uint protection=region.Protection&255;if(!Usable(region)||(protection!=0x10&&protection!=0x20&&protection!=0x40&&protection!=0x80))continue;long at=Math.Max(start,region.Base.ToInt64()),limit=Math.Min(end,region.Base.ToInt64()+(long)region.Size.ToUInt64());while(at<limit){if(cancelled())throw new OperationCanceledException();int length=(int)Math.Min(8*1024*1024,limit-at);bytes+=length;if(bytes>512L*1024*1024)throw new InvalidOperationException("Exact-distance signature scan limit reached");var block=Read(at,length);if(block!=null)for(int i=0;i<=block.Length-Signature.Length;i++){if(block[i]!=0xb9||block[i+1]!=0x38)continue;bool match=true;for(int k=2;k<Signature.Length;k++)if(Signature[k].HasValue&&block[i+k]!=Signature[k].Value){match=false;break;}if(!match)continue;long candidate=at+i+34+BitConverter.ToInt32(block,i+30);var method=Read(candidate,8);if(method!=null&&BitConverter.ToInt64(method,0)>=start&&BitConverter.ToInt64(method,0)<end&&candidate!=found){found=candidate;hits++;}}if(length<=96)break;at+=length-96;}}
        if(hits!=1)throw new InvalidOperationException("Unsupported Aion version: exact-distance signature unavailable");vtable=found;
    }catch{Dispose();throw;}}
    byte[] State(long address){var data=Read(address,0x124);if(data==null||BitConverter.ToInt64(data,0)!=vtable||BitConverter.ToUInt32(data,0x18)!=214||BitConverter.ToUInt32(data,0x1c)>255||BitConverter.ToUInt32(data,0x20)>255)return null;var seen=new bool[256];for(int i=0;i<256;i++){byte b=data[0x24+i];if(seen[b])return null;seen[b]=true;}return Read(BitConverter.ToInt64(data,0x10),214);}
    static bool Slots(byte[] block,int at){long a=BitConverter.ToInt64(block,at+16),b=BitConverter.ToInt64(block,at+32);return BitConverter.ToInt64(block,at)!=0&&BitConverter.ToInt64(block,at+8)!=0&&a>0x10&&b>0x10&&BitConverter.ToInt64(block,at+24)==a-0x10&&BitConverter.ToInt64(block,at+40)==b-0x10;}
    byte[] Owner(long candidate){var slots=Read(candidate+0xe10,0x30);if(slots==null||!Slots(slots,0))return null;byte[] first=State(BitConverter.ToInt64(slots,16)),second=State(BitConverter.ToInt64(slots,32));bool same=first!=null&&second!=null;if(same)for(int i=0;i<214;i++)same&=first[i]==second[i];if(second!=null)Array.Clear(second,0,second.Length);if(same)return first;if(first!=null)Array.Clear(first,0,first.Length);return null;}
    static bool Equal(byte[] a,byte[] b){if(a==null||b==null||a.Length!=b.Length)return false;bool equal=true;for(int i=0;i<a.Length;i++)equal&=a[i]==b[i];return equal;}
    static bool Accepted(byte[] key,Func<byte[],bool> matches){if(key==null)return false;if(matches==null||matches(key))return true;Array.Clear(key,0,key.Length);return false;}
    internal byte[] ReadKey(int pid,Func<bool> cancelled,Func<byte[],bool> matches=null){uint exit;if(handle==IntPtr.Zero||processId!=pid||!GetExitCodeProcess(handle,out exit)||exit!=259)Open(pid,cancelled);if(cancelled())throw new OperationCanceledException();        if(owner!=0){var key=Owner(owner);if(Accepted(key,matches))return key;owner=0;}
        var regions=new List<Region>();foreach(var region in Regions(0x200000000,0x300000000)){if(cancelled())throw new OperationCanceledException();uint p=region.Protection&255;if(Usable(region)&&region.Type==0x20000&&region.Size.ToUInt64()==65536&&(p==4||p==8||p==0x40||p==0x80))regions.Add(region);}
        regions.Reverse();long bytes=0;foreach(var region in regions){if(cancelled())throw new OperationCanceledException();bytes+=65536;if(bytes>256L*1024*1024)break;var block=Read(region.Base.ToInt64(),65536);if(block==null)continue;for(int at=0;at<=block.Length-0xe40;at+=8){if(BitConverter.ToInt64(block,at)!=vtable+0x148)continue;long candidate=region.Base.ToInt64()+at;var key=Owner(candidate);if(Accepted(key,matches)){owner=candidate;return key;}}
            // Same bounded owner arena: some owner variants use another vtable.
            // Validate both complete state objects; never scan all private memory.
            for(int at=0;at<=block.Length-0x30;at+=16){if(!Slots(block,at))continue;long candidate=region.Base.ToInt64()+at-0xe10;if(candidate<=0)continue;var key=Owner(candidate);if(Accepted(key,matches)){owner=candidate;return key;}}
        }
        return null;
    }
    public void Dispose(){if(handle!=IntPtr.Zero){CloseHandle(handle);handle=IntPtr.Zero;}processId=0;vtable=owner=0;}
}
