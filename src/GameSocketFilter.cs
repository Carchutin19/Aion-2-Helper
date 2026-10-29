using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

// Diagnostic B only: exact Windows-owned incoming sockets, metadata only.
// Candidate streams never dispatch data into production overlay signals.
internal sealed class GameSocketFilter {
 volatile string expression="tcp and port 0";
 internal string Expression {get{return expression;}}
 internal void Update(Dictionary<string,string> flows,bool client){var rules=new List<string>();var ports=new SortedSet<int>();foreach(var item in flows){if(item.Value!="in"&&(!client||item.Value!="out")||rules.Count>=64)continue;int arrow=item.Key.IndexOf('>');if(arrow<=0)continue;string server=item.Key.Substring(0,arrow),local=item.Key.Substring(arrow+1);int split=server.LastIndexOf(':'),other=local.LastIndexOf(':');if(split<=0||other<=0)continue;IPAddress host,self;int port,own;if(!IPAddress.TryParse(server.Substring(0,split),out host)||!IPAddress.TryParse(local.Substring(0,other),out self)||!int.TryParse(server.Substring(split+1),out port)||!int.TryParse(local.Substring(other+1),out own)||port<1||port>65535||own<1||own>65535||host.AddressFamily!=self.AddressFamily)continue;if(item.Value=="out"&&own==443)continue;rules.Add("(src host "+host+" and src port "+port+" and dst host "+self+" and dst port "+own+")");ports.Add(port);}rules.Sort(StringComparer.Ordinal);string baseline=client?"tcp port 13328":"tcp src port 13328";string next=rules.Count==0?baseline:"("+baseline+") or (tcp and ("+string.Join(" or ",rules)+"))";if(expression==next)return;expression=next;SupportDiagnostics.Event("windows-owned-game-filter",new{sockets=rules.Count,serverPorts=ports.ToArray(),optionalClientPosition=client,scope="Original game port plus exact AION2 sockets; software ownership required"});}
}
