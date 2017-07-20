using Nancy;

namespace Ranked.Modules
{
  public class Base : NancyModule
  {
    public Base()
    {
      Get["/"] = _ => View["Content/ranked.html"];
    }
  }
}