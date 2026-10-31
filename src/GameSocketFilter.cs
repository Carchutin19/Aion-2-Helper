using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

// Standard game traffic plus exact Windows-owned alternate sockets. Ownership
// and alternate protocol verification are still enforced after capture.
internal sealed class GameSocketFilter {
 volatile string expression="tcp and port 0";
 volatile string alternateExpression="tcp and port 0";volatile bool hasAlternates;
 internal string Expression {get{return expression;}}
 internal string AlternateExpression {get{return alternateExpression;}}
 internal bool HasAlternates {get{return hasAlternates;}}
 internal static string StandardExpression(bool client){return client?"tcp port 13328":"tcp src port 13328";}
 internal void Update(Dictionary<string,string> flows,bool client){var rules=new SortedSet<string>(StringComparer.Ordinal);var ports=new SortedSet<int>();foreach(var item in flows){bool both=item.Value=="both";if(!both&&item.Value!="in"&&(!client||item.Value!="out"))continue;int arrow=item.Key.IndexOf('>');if(arrow<=0)continue;string server=item.Key.Substring(0,arrow),local=item.Key.Substring(arrow+1);int split=server.LastIndexOf(':'),other=local.LastIndexOf(':');if(split<=0||other<=0)continue;IPAddress host,self;int port,own;if(!IPAddress.TryParse(server.Substring(0,split),out host)||!IPAddress.TryParse(local.Substring(0,other),out self)||!int.TryParse(server.Substring(split+1),out port)||!int.TryParse(local.Substring(other+1),out own)||port<1||port>65535||own<1||own>65535||host.AddressFamily!=self.AddressFamily)continue;int gamePort=item.Value=="out"?own:port;
 // The baseline already covers13328. Web443 is never adopted by the router.
 // Their frequent open/close events must not reinstall an equivalent BPF.
 if(gamePort==13328||gamePort==443||both&&(own==13328||own==443))continue;
 rules.Add("(src host "+host+" and src port "+port+" and dst host "+self+" and dst port "+own+")");ports.Add(gamePort);}string baseline=StandardExpression(client),alternate=rules.Count==0?"tcp and port 0":"tcp and ("+string.Join(" or ",rules.Take(64))+")";string next=rules.Count==0?baseline:"("+baseline+") or ("+alternate+")";alternateExpression=alternate;hasAlternates=rules.Count>0;if(expression==next)return;expression=next;SupportDiagnostics.Event("windows-owned-game-filter",new{sockets=Math.Min(64,rules.Count),serverPorts=ports.ToArray(),optionalClientPosition=client,standardLaneIndependent=true,scope="Exact AION2 sockets; dual-owned endpoints require protocol verification"});}
}

// A discovery update must not flush queued data on the standard game handle.
internal sealed class CaptureFilterLane {
 readonly bool alternate,research;string applied;
 internal CaptureFilterLane(bool alternate,bool research){this.alternate=alternate;this.research=research;}
 internal bool Update(GameSocketFilter sockets,bool client,Action<string> install){string next=research?"tcp":alternate?sockets.AlternateExpression:GameSocketFilter.StandardExpression(client);if(next==applied)return false;install(next);applied=next;return true;}
}
