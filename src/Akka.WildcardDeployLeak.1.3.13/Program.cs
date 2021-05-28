using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Configuration;
using Akka.Routing;

namespace Akka.WildcardDeployLeak
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Config config = await File.ReadAllTextAsync("app.conf");
            var actorSystem = ActorSystem.Create("Wildcard", config);
            var parentActor = actorSystem.ActorOf(Props.Create(() => new ParentActor()), "api");

            var spawnCount = 100_000;
            Console.WriteLine($"Spawning and killing [{spawnCount}] actors");
            foreach (var i in Enumerable.Range(0, spawnCount))
            {
                if (i % 1000 == 0)
                    await Task.Delay(100);
                parentActor.Tell(Protocol.SpawnChild.Instance);
            }

            await actorSystem.Terminate();

            // all actors terminated
            Console.WriteLine("All actors terminated. Press any enter to exit.");

            Console.ReadLine();
        }
    }

    public static class Protocol
    {
        public sealed class SpawnChild
        {
            public static readonly SpawnChild Instance = new();
            private SpawnChild(){}
        }
    }

    public class ParentActor : UntypedActor
    {
        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case Protocol.SpawnChild _:
                    Context.ActorOf(ChildActor.MyProps);
                    break;
                default:
                    Unhandled(message);
                    break;
            }
        }
    }

    public class ChildActor : UntypedActor
    {
        public static Props MyProps { get; } = Props.Create<ChildActor>();

        protected override void OnReceive(object message)
        {
            
        }

        protected override void PreStart()
        {
            Context.ActorOf(WorkerActor.MyProps.WithRouter(FromConfig.Instance), "myRouter");
            Context.System.Scheduler.ScheduleTellOnce(TimeSpan.FromSeconds(30), 
                Self, PoisonPill.Instance, ActorRefs.NoSender);
        }
    }

    public class WorkerActor : UntypedActor
    {
        public static Props MyProps {get;} = Props.Create<WorkerActor>();

        protected override void OnReceive(object message)
        {
            
        }
    }
}
