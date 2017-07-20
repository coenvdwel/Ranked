using Nancy;
using Nancy.Bootstrapper;
using Nancy.TinyIoc;

namespace Ranked.Utility
{
  public class Bootstrapper : DefaultNancyBootstrapper
  {
    protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
    {
      pipelines.BeforeRequest += ctx =>
      {
        ctx.CurrentUser = new Security.User(ctx.Request.Headers.Authorization);
        return null;
      };
    }
  }
}