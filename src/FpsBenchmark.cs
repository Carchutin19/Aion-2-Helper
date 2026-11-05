using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;

internal sealed class FpsBenchmarkResult {
    internal readonly double Average, P99;
    internal readonly int SlowFrames, FrameCount;
    internal FpsBenchmarkResult(double average,double p99,int slow,int count){Average=average;P99=p99;SlowFrames=slow;FrameCount=count;}

    internal static FpsBenchmarkResult Calculate(List<double> intervals){
        if(intervals==null||intervals.Count<60)return null;
        double total=0;int slow=0;
        foreach(double interval in intervals){total+=interval;if(interval>50)slow++;}
        if(total<57000||total>61000)return null;
        intervals.Sort();
        int rank=(int)Math.Ceiling(intervals.Count*.99)-1;
        return new FpsBenchmarkResult(1000.0*intervals.Count/total,intervals[rank],slow,intervals.Count);
    }
    internal string Wire(){return string.Format(CultureInfo.InvariantCulture,"{0:R} {1:R} {2} {3}",Average,P99,SlowFrames,FrameCount);}
    internal static FpsBenchmarkResult Parse(string[] fields){
        double average,p99;int slow,count;
        if(fields.Length!=6||!double.TryParse(fields[2],NumberStyles.Float,CultureInfo.InvariantCulture,out average)||
            !double.TryParse(fields[3],NumberStyles.Float,CultureInfo.InvariantCulture,out p99)||!int.TryParse(fields[4],out slow)||!int.TryParse(fields[5],out count)||
            !(average>0&&average<=1200)||!(p99>0&&p99<=61000)||count<60||count>65536||slow<0||slow>count)return null;
        return new FpsBenchmarkResult(average,p99,slow,count);
    }
    internal static void Verify(){
        if(FpsMonitor.CountdownSeconds(0)!=60||FpsMonitor.CountdownSeconds(1.01)!=59||FpsMonitor.CountdownSeconds(59.5)!=1||FpsMonitor.CountdownSeconds(60.25)!=0)
            throw new Exception("The benchmark overlay must count down once per second");
        var samples=new FpsSamples(42);
        samples.Add(42,1,10);samples.Add(42,1,10.016);
        if(!samples.BeginBenchmark(42))throw new Exception("Benchmark requires a live game surface");
        double timestamp=11;
        samples.Add(42,1,timestamp);
        for(int i=0;i<3600;i++){
            samples.Add(42,2,timestamp);
            samples.Add(43,1,timestamp);
            timestamp+=i%300==0?.065:1.0/60;
            samples.Add(42,1,timestamp);
        }
        var result=Calculate(samples.EndBenchmark());
        if(result==null||result.FrameCount!=3600||result.SlowFrames!=12||Math.Abs(result.P99-1000.0/60)>.01||result.Average>=60)
            throw new Exception("Benchmark percentiles, slow frames and FPS must come from one surface");
        if(Parse(("BENCH id "+result.Wire()).Split(' ')).SlowFrames!=12||Parse("BENCH id NaN 15 1 60".Split(' '))!=null)
            throw new Exception("Benchmark pipe metrics are validated");
        if(Calculate(new List<double>())!=null||samples.EndBenchmark()!=null)
            throw new Exception("A partial benchmark cannot publish stale metrics");
    }
}

// All test timing is on the same worker that receives the presentation events.
internal sealed class FpsBenchmarkWorker {
    [DllImport("user32.dll")] internal static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr window,out uint pid);
    string id;
    FpsSamples source;
    double armed,started;
    int process;
    internal bool Active {get{return id!=null;}}

    internal void Cancel(){if(source!=null)source.EndBenchmark();source=null;id=null;}
    internal void Arm(string token,double now){Cancel();id=token;armed=now;}
    internal void Update(double now,HashSet<int> gameIds,FpsSamples candidate,Action<string> send){
        if(id==null)return;
        uint pid;GetWindowThreadProcessId(GetForegroundWindow(),out pid);
        if(source==null){
            if(now-armed>15){Finish(send,"NOFRAMES");return;}
            if(now-armed<2||!gameIds.Contains((int)pid)||candidate==null||!candidate.BeginBenchmark((int)pid))return;
            source=candidate;process=(int)pid;started=now;send("BENCH "+id+" RUN");return;
        }
        if(pid!=(uint)process||!gameIds.Contains(process)){Finish(send,"FOCUS");return;}
        if(candidate!=null&&candidate!=source){Finish(send,"NOFRAMES");return;}
        if(now-started<60)return;
        if(source!=candidate){Finish(send,"NOFRAMES");return;}
        var result=FpsBenchmarkResult.Calculate(source.EndBenchmark());
        source=null;
        Finish(send,result==null?"NOFRAMES":result.Wire());
    }
    void Finish(Action<string> send,string value){string token=id;Cancel();send("BENCH "+token+" "+value);}
}
