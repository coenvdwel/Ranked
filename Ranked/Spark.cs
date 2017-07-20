using Nancy.Hosting.Self;
using Ranked.Utility;
using System;
using System.Configuration;
using Topshelf;

namespace Ranked
{
  public class Spark : NancyHost
  {
    public static Uri Uri = new Uri($"http://localhost:{ConfigurationManager.AppSettings["port"]}");

    public Spark() : base(Uri, new Bootstrapper(), new HostConfiguration { UrlReservations = new UrlReservations { CreateAutomatically = true } })
    {
    }

    public static void Main()
    {
      HostFactory.Run(x =>
      {
        x.Service<Spark>(y =>
        {
          y.ConstructUsing(_ => new Spark());
          y.WhenStarted(z => z.Start());
          y.WhenStopped(z => z.Stop());
        });

        x.RunAsLocalSystem();
        x.SetServiceName("Ranked");
        x.StartAutomatically();
      });
    }
  }
}