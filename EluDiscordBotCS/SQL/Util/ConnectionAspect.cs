using PostSharp.Aspects;
using System;
using System.Collections.Generic;
using System.Text;

namespace EluDiscordBotCS.SQL.Util
{
  [Serializable]
  public class ConnectionAspect : OnMethodBoundaryAspect
  {
    public override void OnEntry(MethodExecutionArgs args)
    {
      ((ELUSQLInterface)args.Instance).m_Conn.Open();
    }

    public override void OnExit(MethodExecutionArgs args)
    {
      ((ELUSQLInterface)args.Instance).m_Conn.Close();
    }
  }
}
